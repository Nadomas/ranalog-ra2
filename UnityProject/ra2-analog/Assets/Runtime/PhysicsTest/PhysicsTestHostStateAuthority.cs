using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// S2-03 host-owned state commit/reconcile. Every FixedUpdate re-applies the last
/// post-physics commit before host commands/drive run, so illegal client Rigidbody
/// or transform writes between ticks cannot stick (EXP-06 thin).
/// Restores all Rigidbodies under each tracked robot (chassis + hinged wheels).
/// Correction snaps are authority enforcement — not a substitute drive model.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-200)]
public sealed class PhysicsTestHostStateAuthority : MonoBehaviour
{
    [Serializable]
    public struct TrackedRobot
    {
        public int RobotId;
        public PhysicsTestDrive Drive;
    }

    struct BodyState
    {
        public Rigidbody Body;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;
    }

    struct RobotCommit
    {
        public bool Valid;
        public BodyState[] Bodies;
    }

    [SerializeField] TrackedRobot[] robots = Array.Empty<TrackedRobot>();

    RobotCommit[] committed = Array.Empty<RobotCommit>();
    readonly List<Rigidbody> bodyScratch = new List<Rigidbody>(8);
    int reconcileCount;
    int commitCount;

    public int ReconcileCount => reconcileCount;
    public int CommitCount => commitCount;

    public void Configure(TrackedRobot[] tracked)
    {
        robots = tracked ?? Array.Empty<TrackedRobot>();
        committed = new RobotCommit[robots.Length];
    }

    void Awake()
    {
        if (committed.Length != robots.Length)
            committed = new RobotCommit[robots.Length];
    }

    void OnEnable()
    {
        StartCoroutine(CommitAfterPhysicsLoop());
    }

    void FixedUpdate()
    {
        if (robots == null || robots.Length == 0)
            return;

        if (committed.Length != robots.Length)
            committed = new RobotCommit[robots.Length];

        for (var i = 0; i < robots.Length; i++)
        {
            if (!committed[i].Valid || committed[i].Bodies == null)
                continue;

            var tampered = false;
            for (var b = 0; b < committed[i].Bodies.Length; b++)
            {
                var state = committed[i].Bodies[b];
                if (state.Body == null)
                    continue;
                if (IsTampered(state.Body, in state))
                    tampered = true;
            }

            if (tampered)
                reconcileCount++;

            for (var b = 0; b < committed[i].Bodies.Length; b++)
            {
                var state = committed[i].Bodies[b];
                if (state.Body == null)
                    continue;
                ApplyState(state.Body, in state);
            }
        }
    }

    IEnumerator CommitAfterPhysicsLoop()
    {
        var wait = new WaitForFixedUpdate();
        while (enabled)
        {
            yield return wait;
            CommitAll();
        }
    }

    void CommitAll()
    {
        if (robots == null || robots.Length == 0)
            return;

        if (committed.Length != robots.Length)
            committed = new RobotCommit[robots.Length];

        for (var i = 0; i < robots.Length; i++)
        {
            var drive = robots[i].Drive;
            if (drive == null)
                continue;

            drive.GetComponentsInChildren(true, bodyScratch);
            var bodies = new BodyState[bodyScratch.Count];
            for (var b = 0; b < bodyScratch.Count; b++)
                bodies[b] = Capture(bodyScratch[b]);

            committed[i] = new RobotCommit { Valid = bodies.Length > 0, Bodies = bodies };
            commitCount++;
        }
    }

    public bool TryGetCommitted(int robotId, out Vector3 position, out Vector3 linearVelocity)
    {
        position = default;
        linearVelocity = default;
        for (var i = 0; i < robots.Length; i++)
        {
            if (robots[i].RobotId != robotId || !committed[i].Valid || committed[i].Bodies == null)
                continue;

            // Root chassis is the drive Rigidbody.
            var drive = robots[i].Drive;
            if (drive == null)
                continue;
            var rootRb = drive.GetComponent<Rigidbody>();
            for (var b = 0; b < committed[i].Bodies.Length; b++)
            {
                if (committed[i].Bodies[b].Body != rootRb)
                    continue;
                position = committed[i].Bodies[b].Position;
                linearVelocity = committed[i].Bodies[b].LinearVelocity;
                return true;
            }
        }

        return false;
    }

    static BodyState Capture(Rigidbody rb) =>
        new BodyState
        {
            Body = rb,
            Position = rb.position,
            Rotation = rb.rotation,
            LinearVelocity = rb.linearVelocity,
            AngularVelocity = rb.angularVelocity
        };

    static void ApplyState(Rigidbody rb, in BodyState state)
    {
        rb.WakeUp();
        rb.position = state.Position;
        rb.rotation = state.Rotation;
        rb.linearVelocity = state.LinearVelocity;
        rb.angularVelocity = state.AngularVelocity;
    }

    static bool IsTampered(Rigidbody rb, in BodyState state)
    {
        if (Vector3.Distance(rb.position, state.Position) > 0.02f)
            return true;
        if (Quaternion.Angle(rb.rotation, state.Rotation) > 0.5f)
            return true;
        if ((rb.linearVelocity - state.LinearVelocity).sqrMagnitude > 0.01f)
            return true;
        if ((rb.angularVelocity - state.AngularVelocity).sqrMagnitude > 0.01f)
            return true;
        return false;
    }
}
