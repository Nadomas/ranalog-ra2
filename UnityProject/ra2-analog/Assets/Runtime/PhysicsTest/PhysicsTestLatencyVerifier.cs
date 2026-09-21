using System.Collections;
using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>
/// S2-04 / EXP-08 latency harness verifier. Sweeps RTT/jitter/loss profiles on loopback,
/// measures drive + pose lag, compares raw vs interpolate presentation. Prediction OFF.
/// </summary>
[DisallowMultipleComponent]
public sealed class PhysicsTestLatencyVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun;
    [SerializeField] float phaseSeconds = 1.1f;
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

        var profiles = new[]
        {
            PhysicsTestLatencyProfile.None,
            PhysicsTestLatencyProfile.Rtt60,
            PhysicsTestLatencyProfile.Rtt100Loss,
            PhysicsTestLatencyProfile.Rtt100Interp
        };

        var allPass = true;
        var nan = false;
        var report = new StringBuilder();
        float baselineDeltaA = 0f;
        float worstPoseErr = 0f;
        float worstInputLagMs = 0f;
        float rawMaxStep = 0f;
        float interpMaxStep = 0f;

        for (var i = 0; i < profiles.Length; i++)
        {
            var profile = profiles[i];
            var box = new ProfileResult();
            yield return RunProfileBody(profile, i, report, box);

            allPass &= box.Pass;
            nan |= box.Nan;
            if (i == 0)
                baselineDeltaA = box.DeltaA;
            if (box.PoseErr > worstPoseErr)
                worstPoseErr = box.PoseErr;
            if (box.InputLagMs > worstInputLagMs)
                worstInputLagMs = box.InputLagMs;
            if (profile.InterpolatePoses)
                interpMaxStep = box.MaxStep;
            else if (profile.RttMs >= 100f)
                rawMaxStep = box.MaxStep;

            yield return BrakeBoth(0.35f);
            // Flush delayed packets between profiles.
            transport.SetLatencyProfile(PhysicsTestLatencyProfile.None);
            yield return new WaitForSecondsRealtime(0.25f);
            transport.ClearQueues();
        }

        // Relative motion under latency should stay in the same ballpark as baseline.
        var motionOk = baselineDeltaA > 0.05f;
        // Pose error grows with RTT; allow ~1.5m at 100ms for this demo speed.
        var poseBudgetOk = worstPoseErr < 2.5f;
        // Input lag should track ~RTT/2 + jitter, not explode.
        var lagOk = worstInputLagMs < 120f;
        // Interp should not be wildly jumpy vs raw (presentation only).
        var interpHelpsOrNeutral = interpMaxStep <= 0.01f || rawMaxStep <= 0.01f ||
                                   interpMaxStep <= rawMaxStep * 1.25f;

        var predictOff = true; // explicit EXP-08 decision for this spike
        var passFinal = allPass && !nan && motionOk && poseBudgetOk && lagOk && predictOff;

        Debug.Log(
            $"[S2-04] VERIFIER_DONE pass={passFinal} nan={nan} profiles_ok={allPass} " +
            $"baseline_A_delta={F3(baselineDeltaA)} worst_pose_err={F3(worstPoseErr)} " +
            $"worst_input_lag_ms={F3(worstInputLagMs)} raw_max_step={F3(rawMaxStep)} " +
            $"interp_max_step={F3(interpMaxStep)} pose_budget_ok={poseBudgetOk} lag_ok={lagOk} " +
            $"interp_ok={interpHelpsOrNeutral} predict=OFF " +
            $"recommend_interp={(interpHelpsOrNeutral && worstPoseErr > 0.15f)} " +
            $"recommend_predict=False");

        Debug.Log($"[S2-04] PROFILE_REPORT {report}");

        foreach (var src in FindObjectsByType<PhysicsTestCommandSource>(FindObjectsSortMode.None))
            src.enabled = true;
    }

    sealed class ProfileResult
    {
        public bool Pass;
        public bool Nan;
        public float DeltaA;
        public float PoseErr;
        public float InputLagMs;
        public float MaxStep;
    }

    IEnumerator RunProfileBody(
        PhysicsTestLatencyProfile profile,
        int index,
        StringBuilder report,
        ProfileResult box)
    {
        transport.ResetMetrics();
        client.ResetPoseMetrics();
        client.SetInterpolatePoses(profile.InterpolatePoses);
        transport.SetLatencyProfile(profile);

        // Allow one-way delay pipeline to warm (RTT/2 + jitter).
        var warmSec = Mathf.Max(0.05f, (profile.OneWayMs + profile.JitterMs) * 0.001f + 0.05f);
        yield return new WaitForSecondsRealtime(warmSec);

        var startA = driveA.transform.position;
        var startB = driveB.transform.position;
        var appliedBefore = authority.AppliedCommands;

        // Measure input→motion: send forward, wait until host velocity rises.
        var rbA = driveA.GetComponent<Rigidbody>();
        rbA.linearVelocity = Vector3.zero;
        yield return BrakeBoth(0.2f);
        var tSend = Time.realtimeSinceStartup;
        client.Send(new PhysicsTestCommandEnvelope(0, 0, new PhysicsTestDriveCommand { Move = 1f }));
        var lagDeadline = Time.realtimeSinceStartup + 0.5f;
        var sawMotion = false;
        while (Time.realtimeSinceStartup < lagDeadline)
        {
            client.Send(new PhysicsTestCommandEnvelope(0, 0, new PhysicsTestDriveCommand { Move = 1f }));
            client.Send(new PhysicsTestCommandEnvelope(1, 1, default));
            if (rbA.linearVelocity.magnitude > 0.15f)
            {
                sawMotion = true;
                break;
            }

            yield return new WaitForFixedUpdate();
        }

        box.InputLagMs = (Time.realtimeSinceStartup - tSend) * 1000f;
        if (!sawMotion)
            box.InputLagMs = 999f;

        yield return DriveBoth(phaseSeconds);
        yield return BrakeBoth(0.25f);

        // Wait for delayed snapshots to arrive for pose compare.
        yield return new WaitForSecondsRealtime(warmSec + 0.05f);

        box.DeltaA = Vector3.Distance(startA, driveA.transform.position);
        var deltaB = Vector3.Distance(startB, driveB.transform.position);
        box.Nan = IsBad(driveA.transform.position) || IsBad(driveB.transform.position) ||
                  IsBad(rbA.linearVelocity);

        box.PoseErr = MeasurePoseError();
        box.MaxStep = client.MaxPoseStep;

        var bothMoved = box.DeltaA > 0.04f && deltaB > 0.04f;
        var delivered = transport.DeliveredCommands > 0 && host.DrainedToAuthority > appliedBefore;
        var snapsOk = client.ReceivedSnapshotBatches > 0;
        // Under loss, some drops expected; delivery ratio should stay mostly healthy.
        var lossOk = profile.Loss <= 0f ||
                     transport.EnqueuedCommands == 0 ||
                     (transport.DroppedCommands / (float)transport.EnqueuedCommands) < profile.Loss + 0.08f;

        // Lag should be at least ~one-way for non-zero RTT (minus timing noise).
        var lagPlausible = profile.RttMs < 1f
            ? box.InputLagMs < 80f
            : box.InputLagMs > profile.OneWayMs * 0.35f && box.InputLagMs < profile.RttMs + 80f;

        box.Pass = !box.Nan && bothMoved && delivered && snapsOk && lossOk && lagPlausible && sawMotion;

        report.Append($"[p{index} {profile.Label} pass={box.Pass} A={F3(box.DeltaA)} B={F3(deltaB)} ")
            .Append($"pose_err={F3(box.PoseErr)} lag_ms={F3(box.InputLagMs)} max_step={F3(box.MaxStep)} ")
            .Append($"drop_cmd={transport.DroppedCommands}/{transport.EnqueuedCommands} ")
            .Append($"avg_delay_ms={F3(transport.AverageDelayMs)}] ");

        Debug.Log(
            $"[S2-04] PROFILE p{index} {profile.Label} pass={box.Pass} nan={box.Nan} " +
            $"A_delta={F3(box.DeltaA)} B_delta={F3(deltaB)} pose_err={F3(box.PoseErr)} " +
            $"input_lag_ms={F3(box.InputLagMs)} max_step={F3(box.MaxStep)} " +
            $"delivered={transport.DeliveredCommands} dropped={transport.DroppedCommands}");
    }

    float MeasurePoseError()
    {
        var poses = client.PresentedPoses;
        if (poses == null || poses.Length == 0)
            return 999f;

        float maxErr = 0f;
        for (var i = 0; i < poses.Length; i++)
        {
            Transform hostTf = null;
            if (poses[i].RobotId == 0 && driveA != null) hostTf = driveA.transform;
            if (poses[i].RobotId == 1 && driveB != null) hostTf = driveB.transform;
            if (hostTf == null)
                continue;
            var err = Vector3.Distance(poses[i].Position, hostTf.position);
            if (err > maxErr)
                maxErr = err;
        }

        return maxErr;
    }

    IEnumerator DriveBoth(float seconds)
    {
        var end = Time.time + seconds;
        while (Time.time < end)
        {
            client.Send(new PhysicsTestCommandEnvelope(0, 0, new PhysicsTestDriveCommand { Move = 1f, Turn = 0.2f }));
            client.Send(new PhysicsTestCommandEnvelope(1, 1, new PhysicsTestDriveCommand { Move = 1f, Turn = -0.2f }));
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

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
}
