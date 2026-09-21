using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// S2-02 logical client peer. Sends commands only via <see cref="PhysicsTestLoopbackTransport"/>;
/// never writes <see cref="PhysicsTestDrive.SetCommand"/>. S2-04 optional pose interpolation
/// is presentation-only (does not touch Rigidbodies). Prediction stays OFF.
/// </summary>
[DisallowMultipleComponent]
public sealed class PhysicsTestTransportClient : MonoBehaviour
{
    [SerializeField] PhysicsTestLoopbackTransport transport;
    [SerializeField] bool interpolatePoses;

    readonly List<PhysicsTestPoseSnapshot[]> snapshotScratch = new List<PhysicsTestPoseSnapshot[]>(8);

    PhysicsTestPoseSnapshot[] lastPoses = System.Array.Empty<PhysicsTestPoseSnapshot>();
    PhysicsTestPoseSnapshot[] presentedPoses = System.Array.Empty<PhysicsTestPoseSnapshot>();
    PhysicsTestPoseSnapshot[] interpFrom = System.Array.Empty<PhysicsTestPoseSnapshot>();
    PhysicsTestPoseSnapshot[] interpTo = System.Array.Empty<PhysicsTestPoseSnapshot>();
    PhysicsTestPoseSnapshot[] stepPrev = System.Array.Empty<PhysicsTestPoseSnapshot>();
    float interpFromTime;
    float interpToTime;
    int receivedSnapshotBatches;
    float maxPoseStep;

    public PhysicsTestPoseSnapshot[] LastPoses => lastPoses;
    public PhysicsTestPoseSnapshot[] PresentedPoses =>
        interpolatePoses && presentedPoses.Length > 0 ? presentedPoses : lastPoses;
    public int ReceivedSnapshotBatches => receivedSnapshotBatches;
    public float MaxPoseStep => maxPoseStep;
    public bool InterpolatePoses => interpolatePoses;

    public void Configure(PhysicsTestLoopbackTransport loopback)
    {
        transport = loopback;
    }

    public void SetInterpolatePoses(bool enabled)
    {
        interpolatePoses = enabled;
        if (!enabled)
            presentedPoses = lastPoses;
    }

    public void ResetPoseMetrics()
    {
        maxPoseStep = 0f;
        receivedSnapshotBatches = 0;
        lastPoses = System.Array.Empty<PhysicsTestPoseSnapshot>();
        presentedPoses = System.Array.Empty<PhysicsTestPoseSnapshot>();
        interpFrom = System.Array.Empty<PhysicsTestPoseSnapshot>();
        interpTo = System.Array.Empty<PhysicsTestPoseSnapshot>();
        stepPrev = System.Array.Empty<PhysicsTestPoseSnapshot>();
    }

    public void Send(PhysicsTestCommandEnvelope envelope)
    {
        if (transport == null)
            return;
        transport.EnqueueCommand(envelope);
    }

    void Update()
    {
        if (transport == null)
            return;

        snapshotScratch.Clear();
        if (transport.DrainSnapshots(snapshotScratch) > 0)
        {
            var newest = snapshotScratch[snapshotScratch.Count - 1];
            if (interpolatePoses && lastPoses.Length > 0)
            {
                interpFrom = Clone(lastPoses);
                interpFromTime = interpToTime > 0f ? interpToTime : Time.time;
            }

            lastPoses = newest;
            interpTo = newest;
            interpToTime = Time.time;
            receivedSnapshotBatches += snapshotScratch.Count;
        }

        if (interpolatePoses)
            PresentInterpolated();
        else
            presentedPoses = lastPoses;

        TrackPresentedStep(PresentedPoses);
    }

    void PresentInterpolated()
    {
        if (interpTo.Length == 0)
        {
            presentedPoses = lastPoses;
            return;
        }

        if (interpFrom.Length == 0 || interpToTime <= interpFromTime)
        {
            presentedPoses = interpTo;
            return;
        }

        var span = interpToTime - interpFromTime;
        var t = span > 1e-4f ? Mathf.Clamp01((Time.time - interpFromTime) / span) : 1f;
        // Hold at newest once past the interval — no extrapolation / prediction.
        if (t >= 1f)
        {
            presentedPoses = interpTo;
            return;
        }

        if (presentedPoses.Length != interpTo.Length)
            presentedPoses = new PhysicsTestPoseSnapshot[interpTo.Length];

        for (var i = 0; i < interpTo.Length; i++)
        {
            var a = FindPose(interpFrom, interpTo[i].RobotId);
            var b = interpTo[i];
            presentedPoses[i] = new PhysicsTestPoseSnapshot
            {
                RobotId = b.RobotId,
                Position = Vector3.Lerp(a.Position, b.Position, t),
                Rotation = Quaternion.Slerp(a.Rotation, b.Rotation, t),
                LinearVelocity = Vector3.Lerp(a.LinearVelocity, b.LinearVelocity, t)
            };
        }
    }

    void TrackPresentedStep(PhysicsTestPoseSnapshot[] poses)
    {
        if (poses == null || poses.Length == 0)
            return;

        if (stepPrev.Length == poses.Length)
        {
            for (var i = 0; i < poses.Length; i++)
            {
                var prior = FindPose(stepPrev, poses[i].RobotId);
                var step = Vector3.Distance(prior.Position, poses[i].Position);
                if (step > maxPoseStep)
                    maxPoseStep = step;
            }
        }

        stepPrev = Clone(poses);
    }

    static PhysicsTestPoseSnapshot FindPose(PhysicsTestPoseSnapshot[] poses, int robotId)
    {
        for (var i = 0; i < poses.Length; i++)
        {
            if (poses[i].RobotId == robotId)
                return poses[i];
        }

        return poses.Length > 0 ? poses[0] : default;
    }

    static PhysicsTestPoseSnapshot[] Clone(PhysicsTestPoseSnapshot[] src)
    {
        var copy = new PhysicsTestPoseSnapshot[src.Length];
        for (var i = 0; i < src.Length; i++)
            copy[i] = src[i];
        return copy;
    }
}
