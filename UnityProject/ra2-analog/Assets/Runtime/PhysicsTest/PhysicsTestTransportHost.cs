using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// S2-02 listen-host peer. Drains loopback commands into <see cref="PhysicsTestLocalAuthority"/>
/// and publishes thin pose snapshots. Sole path from transport to drive authority.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-150)]
public sealed class PhysicsTestTransportHost : MonoBehaviour
{
    [Serializable]
    public struct TrackedRobot
    {
        public int RobotId;
        public PhysicsTestDrive Drive;
    }

    [SerializeField] PhysicsTestLoopbackTransport transport;
    [SerializeField] PhysicsTestLocalAuthority authority;
    [SerializeField] TrackedRobot[] robots = Array.Empty<TrackedRobot>();

    readonly List<PhysicsTestCommandEnvelope> drainBuffer = new List<PhysicsTestCommandEnvelope>(32);
    PhysicsTestPoseSnapshot[] poseBuffer = Array.Empty<PhysicsTestPoseSnapshot>();

    int drainedToAuthority;

    public int DrainedToAuthority => drainedToAuthority;

    public void Configure(
        PhysicsTestLoopbackTransport loopback,
        PhysicsTestLocalAuthority auth,
        TrackedRobot[] tracked)
    {
        transport = loopback;
        authority = auth;
        robots = tracked ?? Array.Empty<TrackedRobot>();
    }

    void FixedUpdate()
    {
        if (transport == null || authority == null)
            return;

        drainBuffer.Clear();
        transport.DrainCommands(drainBuffer);
        for (var i = 0; i < drainBuffer.Count; i++)
        {
            authority.Submit(drainBuffer[i]);
            drainedToAuthority++;
        }

        PublishPoses();
    }

    void PublishPoses()
    {
        if (robots == null || robots.Length == 0)
            return;

        if (poseBuffer.Length != robots.Length)
            poseBuffer = new PhysicsTestPoseSnapshot[robots.Length];

        for (var i = 0; i < robots.Length; i++)
        {
            var drive = robots[i].Drive;
            if (drive == null)
            {
                poseBuffer[i] = default;
                continue;
            }

            var rb = drive.GetComponent<Rigidbody>();
            poseBuffer[i] = new PhysicsTestPoseSnapshot
            {
                RobotId = robots[i].RobotId,
                Position = drive.transform.position,
                Rotation = drive.transform.rotation,
                LinearVelocity = rb != null ? rb.linearVelocity : Vector3.zero
            };
        }

        transport.PublishSnapshots(poseBuffer);
    }
}
