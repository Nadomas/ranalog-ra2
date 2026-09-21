using System.Collections;
using System.Globalization;
using UnityEngine;

/// <summary>
/// S2-03 verifier. Same loopback command→authority→drive path as S2-02, plus illegal
/// client AddForce / velocity / teleport smoke that host state authority must overwrite.
/// </summary>
[DisallowMultipleComponent]
public sealed class PhysicsTestIllegalForceVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun;
    [SerializeField] float phaseSeconds = 1.0f;
    [SerializeField] PhysicsTestLoopbackTransport transport;
    [SerializeField] PhysicsTestTransportClient client;
    [SerializeField] PhysicsTestTransportHost host;
    [SerializeField] PhysicsTestLocalAuthority authority;
    [SerializeField] PhysicsTestHostStateAuthority stateAuthority;
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
        PhysicsTestHostStateAuthority stateAuth,
        PhysicsTestDrive a,
        PhysicsTestDrive b)
    {
        transport = loopback;
        client = transportClient;
        host = transportHost;
        authority = auth;
        stateAuthority = stateAuth;
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
        if (stateAuthority == null)
            stateAuthority = FindAnyObjectByType<PhysicsTestHostStateAuthority>();
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
        var reconcilesBefore = stateAuthority.ReconcileCount;

        yield return RunPhase("A_forward", 0, 0, new PhysicsTestDriveCommand { Move = 1f }, phaseSeconds);
        yield return RunPhase("B_forward", 1, 1, new PhysicsTestDriveCommand { Move = 1f }, phaseSeconds);

        var end = Time.time + phaseSeconds;
        while (Time.time < end)
        {
            client.Send(new PhysicsTestCommandEnvelope(0, 0, new PhysicsTestDriveCommand { Move = 0.5f, Turn = 0.3f }));
            client.Send(new PhysicsTestCommandEnvelope(1, 1, new PhysicsTestDriveCommand { Move = 0.5f, Turn = -0.3f }));
            yield return new WaitForFixedUpdate();
        }

        yield return BrakeBoth(0.6f);

        client.Send(new PhysicsTestCommandEnvelope(0, 99, new PhysicsTestDriveCommand { Move = 1f }));
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        var rejectOk = authority.RejectedUnauthorized > rejectedBefore;

        driveA.SetCommand(new PhysicsTestDriveCommand { Move = 1f });
        yield return BrakeBoth(0.2f);
        var cheatCmdOverwritten = Mathf.Abs(driveA.CurrentCommand.Move) < 0.01f;

        var rbA = driveA.GetComponent<Rigidbody>();
        var rbB = driveB.GetComponent<Rigidbody>();

        // Cheats after end-of-frame so post-physics commit cannot absorb them.
        // Reject checks run a few FixedUpdates later (host restore already applied).

        // --- Illegal client impulse (direct velocity delta) on Robot_A ---
        yield return BrakeBoth(0.25f);
        yield return new WaitForEndOfFrame();
        stateAuthority.TryGetCommitted(0, out var commitPosA, out _);
        var impulseBefore = rbA.linearVelocity;
        var cheatedImpulse = impulseBefore + new Vector3(0f, 40f, 30f);
        rbA.WakeUp();
        rbA.linearVelocity = cheatedImpulse;
        var impulseApplied = (rbA.linearVelocity - impulseBefore).sqrMagnitude > 100f;
        yield return BrakeBoth(0.12f);
        var impulseRejected =
            Vector3.Distance(rbA.position, commitPosA) < 1.25f &&
            (rbA.linearVelocity - cheatedImpulse).sqrMagnitude > 100f &&
            rbA.linearVelocity.y < 8f;

        // --- Illegal velocity write on Robot_B ---
        yield return BrakeBoth(0.25f);
        yield return new WaitForEndOfFrame();
        stateAuthority.TryGetCommitted(1, out var commitPosB, out _);
        var cheatedVel = new Vector3(25f, 8f, -18f);
        rbB.WakeUp();
        rbB.linearVelocity = cheatedVel;
        var velocityApplied = rbB.linearVelocity.sqrMagnitude > 100f;
        yield return BrakeBoth(0.12f);
        var velocityRejected =
            Vector3.Distance(rbB.position, commitPosB) < 1.25f &&
            (rbB.linearVelocity - cheatedVel).sqrMagnitude > 100f &&
            rbB.linearVelocity.magnitude < 6f;

        // --- Illegal transform/position teleport on Robot_A ---
        yield return BrakeBoth(0.25f);
        yield return new WaitForEndOfFrame();
        stateAuthority.TryGetCommitted(0, out var commitPosTp, out _);
        var rotBeforeTp = rbA.rotation;
        var cheatedPos = commitPosTp + new Vector3(0f, 12f, 8f);
        driveA.transform.SetPositionAndRotation(cheatedPos, Quaternion.Euler(0f, 90f, 0f));
        rbA.position = cheatedPos;
        rbA.rotation = Quaternion.Euler(0f, 90f, 0f);
        var teleportApplied = Vector3.Distance(rbA.position, commitPosTp) > 5f;
        yield return BrakeBoth(0.15f);
        var teleportRejected =
            Vector3.Distance(rbA.position, commitPosTp) < 1.25f &&
            Vector3.Distance(rbA.position, cheatedPos) > 5f &&
            Quaternion.Angle(rbA.rotation, rotBeforeTp) < 35f;

        var deltaA = Vector3.Distance(startA, driveA.transform.position);
        var deltaB = Vector3.Distance(startB, driveB.transform.position);
        var nan = IsBad(driveA.transform.position) || IsBad(driveB.transform.position) ||
                  IsBad(rbA.linearVelocity) || IsBad(rbB.linearVelocity);

        // After reject, A must not remain in the teleported/flung region.
        var aNotCheatedAway = deltaA < 40f;

        var bothMoved = deltaA > 0.05f && deltaB > 0.05f;
        var transportOk = transport.DeliveredCommands > 0 && host.DrainedToAuthority > 0;
        var reconciled = stateAuthority.ReconcileCount > reconcilesBefore;
        var forceOk = impulseApplied && impulseRejected &&
                      velocityApplied && velocityRejected &&
                      teleportApplied && teleportRejected &&
                      reconciled && aNotCheatedAway;

        var pass = !nan && bothMoved && rejectOk && cheatCmdOverwritten && transportOk && forceOk;

        Debug.Log(
            $"[S2-03] VERIFIER_DONE pass={pass} nan={nan} " +
            $"A_delta={F3(deltaA)} B_delta={F3(deltaB)} both_moved={bothMoved} a_not_cheated_away={aNotCheatedAway} " +
            $"reject_ok={rejectOk} cheat_cmd_ok={cheatCmdOverwritten} transport_ok={transportOk} " +
            $"impulse_applied={impulseApplied} impulse_rejected={impulseRejected} " +
            $"velocity_applied={velocityApplied} velocity_rejected={velocityRejected} " +
            $"teleport_applied={teleportApplied} teleport_rejected={teleportRejected} " +
            $"reconciled={reconciled} reconciles={stateAuthority.ReconcileCount} commits={stateAuthority.CommitCount} " +
            $"rejected_total={authority.RejectedUnauthorized} applied={authority.AppliedCommands}");

        foreach (var src in FindObjectsByType<PhysicsTestCommandSource>(FindObjectsSortMode.None))
            src.enabled = true;
    }

    IEnumerator IdleBoth(float seconds)
    {
        var end = Time.time + seconds;
        while (Time.time < end)
        {
            client.Send(new PhysicsTestCommandEnvelope(0, 0, default));
            client.Send(new PhysicsTestCommandEnvelope(1, 1, default));
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator BrakeBoth(float seconds)
    {
        var brake = new PhysicsTestDriveCommand { Brake = true };
        var end = Time.time + seconds;
        while (Time.time < end)
        {
            client.Send(new PhysicsTestCommandEnvelope(0, 0, brake));
            client.Send(new PhysicsTestCommandEnvelope(1, 1, brake));
            yield return new WaitForFixedUpdate();
        }
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

        Debug.Log($"[S2-03] PHASE {name} done");
    }

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
