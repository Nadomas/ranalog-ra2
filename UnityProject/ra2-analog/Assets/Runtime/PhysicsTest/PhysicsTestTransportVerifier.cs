using System.Collections;
using System.Globalization;
using UnityEngine;

/// <summary>
/// S2-02 verifier. Commands go only through loopback transport → host → authority → drive.
/// Reuses unauthorized reject + direct SetCommand overwrite smoke from S2-01.
/// </summary>
[DisallowMultipleComponent]
public sealed class PhysicsTestTransportVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun;
    [SerializeField] float phaseSeconds = 1.2f;
    [SerializeField] PhysicsTestLoopbackTransport transport;
    [SerializeField] PhysicsTestTransportClient client;
    [SerializeField] PhysicsTestTransportHost host;
    [SerializeField] PhysicsTestLocalAuthority authority;
    [SerializeField] PhysicsTestDrive driveA;
    [SerializeField] PhysicsTestDrive driveB;

    public bool AutoRun
    {
        get => autoRun;
        set => autoRun = value;
    }

    public void Configure(
        PhysicsTestLoopbackTransport loopback,
        PhysicsTestTransportClient transportClient,
        PhysicsTestTransportHost transportHost,
        PhysicsTestLocalAuthority auth,
        PhysicsTestDrive a,
        PhysicsTestDrive b)
    {
        transport = loopback;
        client = transportClient;
        host = transportHost;
        authority = auth;
        driveA = a;
        driveB = b;
    }

    void Start()
    {
        if (!autoRun)
            return;

        ResolveRefs();
        StartCoroutine(RunSequence());
    }

    void ResolveRefs()
    {
        if (transport == null)
            transport = FindAnyObjectByType<PhysicsTestLoopbackTransport>();
        if (client == null)
            client = FindAnyObjectByType<PhysicsTestTransportClient>();
        if (host == null)
            host = FindAnyObjectByType<PhysicsTestTransportHost>();
        if (authority == null)
            authority = FindAnyObjectByType<PhysicsTestLocalAuthority>();
        if (driveA == null)
        {
            var go = GameObject.Find("Robot_A");
            if (go != null) driveA = go.GetComponent<PhysicsTestDrive>();
        }

        if (driveB == null)
        {
            var go = GameObject.Find("Robot_B");
            if (go != null) driveB = go.GetComponent<PhysicsTestDrive>();
        }
    }

    IEnumerator RunSequence()
    {
        foreach (var src in FindObjectsByType<PhysicsTestCommandSource>(FindObjectsSortMode.None))
            src.enabled = false;

        var startA = driveA.transform.position;
        var startB = driveB.transform.position;
        var rejectedBefore = authority.RejectedUnauthorized;
        var snapshotsBefore = client.ReceivedSnapshotBatches;

        yield return RunPhase("A_forward", 0, 0, new PhysicsTestDriveCommand { Move = 1f }, phaseSeconds);
        yield return RunPhase("B_forward", 1, 1, new PhysicsTestDriveCommand { Move = 1f }, phaseSeconds);

        var end = Time.time + phaseSeconds;
        while (Time.time < end)
        {
            client.Send(new PhysicsTestCommandEnvelope(0, 0, new PhysicsTestDriveCommand { Move = 0.6f, Turn = 0.4f }));
            client.Send(new PhysicsTestCommandEnvelope(1, 1, new PhysicsTestDriveCommand { Move = 0.6f, Turn = -0.4f }));
            yield return new WaitForFixedUpdate();
        }

        // Unauthorized source must be rejected on host after crossing transport.
        client.Send(new PhysicsTestCommandEnvelope(0, 99, new PhysicsTestDriveCommand { Move = 1f }));
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        var rejectOk = authority.RejectedUnauthorized > rejectedBefore;

        // Cheat direct SetCommand — authority idle overwrite must win.
        driveA.SetCommand(new PhysicsTestDriveCommand { Move = 1f });
        client.Send(new PhysicsTestCommandEnvelope(0, 0, default));
        client.Send(new PhysicsTestCommandEnvelope(1, 1, default));
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        var cheatOverwritten = Mathf.Abs(driveA.CurrentCommand.Move) < 0.01f;

        // Give client Update a frame to drain host pose snapshots.
        yield return null;
        yield return null;

        var deltaA = Vector3.Distance(startA, driveA.transform.position);
        var deltaB = Vector3.Distance(startB, driveB.transform.position);
        var nan = IsBad(driveA.transform.position) || IsBad(driveB.transform.position) ||
                  IsBad(driveA.GetComponent<Rigidbody>().linearVelocity) ||
                  IsBad(driveB.GetComponent<Rigidbody>().linearVelocity);

        var poses = client.LastPoses;
        var poseA = FindPose(poses, 0);
        var poseB = FindPose(poses, 1);
        var poseOk = poseA.HasValue && poseB.HasValue &&
                     Vector3.Distance(poseA.Value.Position, driveA.transform.position) < 0.35f &&
                     Vector3.Distance(poseB.Value.Position, driveB.transform.position) < 0.35f;

        var transportOk = transport.DeliveredCommands > 0 &&
                          host.DrainedToAuthority > 0 &&
                          client.ReceivedSnapshotBatches > snapshotsBefore;

        var bothMoved = deltaA > 0.05f && deltaB > 0.05f;
        var pass = !nan && bothMoved && rejectOk && cheatOverwritten && transportOk && poseOk;

        Debug.Log(
            $"[S2-02] VERIFIER_DONE pass={pass} nan={nan} " +
            $"A_delta={F3(deltaA)} B_delta={F3(deltaB)} both_moved={bothMoved} " +
            $"reject_ok={rejectOk} cheat_overwritten={cheatOverwritten} " +
            $"transport_ok={transportOk} pose_ok={poseOk} " +
            $"enqueued={transport.EnqueuedCommands} delivered={transport.DeliveredCommands} " +
            $"drained={host.DrainedToAuthority} snaps={client.ReceivedSnapshotBatches} " +
            $"rejected_total={authority.RejectedUnauthorized} applied={authority.AppliedCommands}");

        foreach (var src in FindObjectsByType<PhysicsTestCommandSource>(FindObjectsSortMode.None))
            src.enabled = true;
    }

    IEnumerator RunPhase(string name, int robotId, int sourceId, PhysicsTestDriveCommand cmd, float seconds)
    {
        if (seconds <= 0f)
            yield break;

        var end = Time.time + seconds;
        while (Time.time < end)
        {
            client.Send(new PhysicsTestCommandEnvelope(robotId, sourceId, cmd));
            var other = robotId == 0 ? 1 : 0;
            client.Send(new PhysicsTestCommandEnvelope(other, other, default));
            yield return new WaitForFixedUpdate();
        }

        Debug.Log($"[S2-02] PHASE {name} done");
    }

    static PhysicsTestPoseSnapshot? FindPose(PhysicsTestPoseSnapshot[] poses, int robotId)
    {
        if (poses == null)
            return null;
        for (var i = 0; i < poses.Length; i++)
        {
            if (poses[i].RobotId == robotId)
                return poses[i];
        }

        return null;
    }

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
