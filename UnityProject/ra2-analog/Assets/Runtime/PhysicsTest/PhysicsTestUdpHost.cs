using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// S2-06 UDP listen-host peer. Drains remote commands into local authority and publishes poses.
/// Same command→authority→drive path as loopback host; transport is UDP.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-150)]
public sealed class PhysicsTestUdpHost : MonoBehaviour
{
    [Serializable]
    public struct TrackedRobot
    {
        public int RobotId;
        public PhysicsTestDrive Drive;
    }

    [SerializeField] PhysicsTestUdpTransport transport;
    [SerializeField] PhysicsTestLocalAuthority authority;
    [SerializeField] TrackedRobot[] robots = Array.Empty<TrackedRobot>();

    readonly List<PhysicsTestCommandEnvelope> drainBuffer = new List<PhysicsTestCommandEnvelope>(32);
    PhysicsTestPoseSnapshot[] poseBuffer = Array.Empty<PhysicsTestPoseSnapshot>();
    int drainedToAuthority;

    public int DrainedToAuthority => drainedToAuthority;

    public void Configure(
        PhysicsTestUdpTransport udp,
        PhysicsTestLocalAuthority auth,
        TrackedRobot[] tracked)
    {
        transport = udp;
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
