using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// S2-07 thin host combat authority. Applies impulses to Rigidbodies and sets disable flags.
/// Clients may only submit <see cref="PhysicsTestCombatRequest"/>; outcomes are host events.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-120)]
public sealed class PhysicsTestCombatAuthority : MonoBehaviour
{
    [Serializable]
    public struct TrackedRobot
    {
        public int RobotId;
        public int AllowedSourceId;
        public PhysicsTestDrive Drive;
        public PhysicsTestDisableFlag DisableFlag;
    }

    [SerializeField] TrackedRobot[] robots = Array.Empty<TrackedRobot>();
    [SerializeField] int hitsToDisable = 1;
    [SerializeField] PhysicsTestUdpTransport udp;

    readonly List<PhysicsTestCombatRequest> drainBuffer = new List<PhysicsTestCombatRequest>(16);
    readonly int[] hitCounts = new int[8];
    int sequence;
    int appliedImpulses;
    int rejectedUnauthorized;
    PhysicsTestCombatEvent lastEvent;

    public int AppliedImpulses => appliedImpulses;
    public int RejectedUnauthorized => rejectedUnauthorized;
    public PhysicsTestCombatEvent LastEvent => lastEvent;
    public int Sequence => sequence;

    public void Configure(PhysicsTestUdpTransport transport, TrackedRobot[] tracked, int disableAfterHits = 1)
    {
        udp = transport;
        robots = tracked ?? Array.Empty<TrackedRobot>();
        hitsToDisable = Mathf.Max(1, disableAfterHits);
    }

    public bool IsDisabled(int robotId)
    {
        for (var i = 0; i < robots.Length; i++)
        {
            if (robots[i].RobotId != robotId)
                continue;
            return robots[i].DisableFlag != null && robots[i].DisableFlag.Disabled;
        }

        return false;
    }

    public int GetHitCount(int robotId)
    {
        if (robotId < 0 || robotId >= hitCounts.Length)
            return 0;
        return hitCounts[robotId];
    }

    /// <summary>Host-local apply (also used when request arrives over UDP).</summary>
    public bool TryApply(PhysicsTestCombatRequest request)
    {
        // Validate attacker source binding (clients are not trusted for outcomes).
        var attackerOk = false;
        for (var a = 0; a < robots.Length; a++)
        {
            if (robots[a].RobotId == request.AttackerRobotId &&
                robots[a].AllowedSourceId == request.SourceId)
            {
                attackerOk = true;
                break;
            }
        }

        if (!attackerOk)
        {
            rejectedUnauthorized++;
            return false;
        }

        for (var i = 0; i < robots.Length; i++)
        {
            var b = robots[i];
            if (b.RobotId != request.TargetRobotId)
                continue;

            if (b.DisableFlag != null && b.DisableFlag.Disabled)
                return false;

            var rb = b.Drive != null ? b.Drive.GetComponent<Rigidbody>() : null;
            if (rb == null)
                return false;

            rb.AddForce(request.ImpulseWorld, ForceMode.Impulse);
            appliedImpulses++;

            if (request.TargetRobotId >= 0 && request.TargetRobotId < hitCounts.Length)
                hitCounts[request.TargetRobotId]++;

            var hits = GetHitCount(request.TargetRobotId);
            var disabled = hits >= hitsToDisable;
            if (disabled && b.DisableFlag != null)
                b.DisableFlag.SetDisabled(true);

            sequence++;
            lastEvent = new PhysicsTestCombatEvent
            {
                Sequence = sequence,
                AttackerRobotId = request.AttackerRobotId,
                TargetRobotId = request.TargetRobotId,
                ImpulseWorld = request.ImpulseWorld,
                TargetDisabled = disabled,
                TargetHitCount = hits
            };

            if (udp != null)
                udp.PublishCombatEvent(lastEvent);

            return true;
        }

        rejectedUnauthorized++;
        return false;
    }

    void FixedUpdate()
    {
        if (udp == null)
            return;

        drainBuffer.Clear();
        udp.DrainCombatRequests(drainBuffer);
        for (var i = 0; i < drainBuffer.Count; i++)
            TryApply(drainBuffer[i]);
    }
}
