using System.Collections;
using System.Globalization;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S6-01 verifier. Design → Configure → Test → Design → Test without scene reload; retain blueprint; timed switches.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotSeamlessLoopVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;
    [SerializeField] float driveSeconds = 0.8f;
    [SerializeField] PhysicsMaterial slideMaterial;
    [SerializeField] float maxModeSwitchMs = 50f;

    float lastDelta;
    float lastZ;
    bool lastNan;

    public bool AutoRun
    {
        get => autoRun;
        set => autoRun = value;
    }

    public void Configure(PhysicsMaterial slide)
    {
        slideMaterial = slide;
    }

    void Start()
    {
        if (!autoRun)
            return;
        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        if (slideMaterial == null)
            slideMaterial = Resources.Load<PhysicsMaterial>("PhysicsTestSlide");

        var session = new RobotWorkshopSession();
        var color = new Color(0.3f, 0.55f, 0.85f);

        session.SetWorkingBlueprint(
            RobotBlueprint.CreateRa2ConstructionSampleA(new Vector3(0f, 0.85f, 0f), 0f));
        if (!session.TrySwitchMode(WorkshopMode.Design, null, slideMaterial, color, out var err))
        {
            Debug.Log($"[S6-01] VERIFIER_DONE pass=False reason=design_switch err={err}");
            yield break;
        }

        var switchDesign = session.LastSwitchMs;

        if (!session.TrySwitchMode(WorkshopMode.Configure, null, slideMaterial, color, out err))
        {
            Debug.Log($"[S6-01] VERIFIER_DONE pass=False reason=configure_switch err={err}");
            yield break;
        }

        var switchConfigure = session.LastSwitchMs;
        RobotControlConfigurer.ApplyDrivePreset(
            session.WorkingBlueprint, RobotControlConfigurer.DrivePreset.ReversedDrive);
        RobotControlConfigurer.SetSlotBinding(session.WorkingBlueprint, "forward_back", "S/W");

        if (!session.TrySwitchMode(WorkshopMode.Test, null, slideMaterial, color, out err))
        {
            Debug.Log($"[S6-01] VERIFIER_DONE pass=False reason=test_switch err={err}");
            yield break;
        }

        var switchTest = session.LastSwitchMs;
        if (session.TestInstance?.Drive == null)
        {
            Debug.Log("[S6-01] VERIFIER_DONE pass=False reason=no_drive");
            yield break;
        }

        yield return DriveOnce(session.TestInstance.Drive, session.WorkingBlueprint);
        var delta1 = lastDelta;
        var z1 = lastZ;
        if (lastNan || delta1 < 0.1f)
        {
            Debug.Log($"[S6-01] VERIFIER_DONE pass=False reason=test_drive delta={F3(delta1)} nan={lastNan}");
            yield break;
        }

        if (!session.TrySwitchMode(WorkshopMode.Design, null, slideMaterial, color, out err))
        {
            Debug.Log($"[S6-01] VERIFIER_DONE pass=False reason=back_design err={err}");
            yield break;
        }

        var switchBackDesign = session.LastSwitchMs;
        // Destroy is end-of-frame; wait so leak check does not see a doomed Test robot.
        yield return null;
        var retainedReverse = session.WorkingBlueprint.Wirings != null &&
                              session.WorkingBlueprint.Wirings.Length > 0 &&
                              session.WorkingBlueprint.Wirings[0].Sign < 0f;
        var orphan = GameObject.Find("Ra2ConstructionSample_A");
        var noLeak = session.TestInstance == null && orphan == null;

        if (!session.TrySwitchMode(WorkshopMode.Test, null, slideMaterial, color, out err))
        {
            Debug.Log($"[S6-01] VERIFIER_DONE pass=False reason=retest_switch err={err}");
            yield break;
        }

        var switchRetest = session.LastSwitchMs;
        yield return DriveOnce(session.TestInstance.Drive, session.WorkingBlueprint);
        var delta2 = lastDelta;
        var z2 = lastZ;
        var nan2 = lastNan;

        if (!session.TryResetTest(null, slideMaterial, color, out err))
        {
            Debug.Log($"[S6-01] VERIFIER_DONE pass=False reason=reset err={err}");
            yield break;
        }

        var resetMs = session.LastResetMs;
        yield return DriveOnce(session.TestInstance.Drive, session.WorkingBlueprint);
        var delta3 = lastDelta;
        var z3 = lastZ;
        var nan3 = lastNan;

        session.TrySwitchMode(WorkshopMode.Design, null, slideMaterial, color, out _);

        // Non-spawn mode switches must be trivial (no scene load).
        var modeSwitchesFast = switchConfigure <= maxModeSwitchMs && switchBackDesign <= maxModeSwitchMs &&
                               switchDesign <= maxModeSwitchMs;
        // Spawn/reset include assembly — still seamless if well under a frame budget cluster.
        var spawnOk = switchTest < 200.0 && switchRetest < 200.0 && resetMs < 200.0;
        var retainedOk = retainedReverse && noLeak && !nan2 && !nan3 && delta2 > 0.1f && delta3 > 0.1f;
        var sameConfig = z1 > 0.1f && z2 > 0.1f && z3 > 0.1f;
        var pass = modeSwitchesFast && spawnOk && retainedOk && sameConfig;

        Debug.Log(
            $"[S6-01] VERIFIER_DONE pass={pass} mode_fast={modeSwitchesFast} spawn_ok={spawnOk} " +
            $"retained={retainedOk} same_config={sameConfig} no_leak={noLeak} " +
            $"ms_design={F1(switchDesign)} ms_cfg={F1(switchConfigure)} ms_test={F1(switchTest)} " +
            $"ms_back={F1(switchBackDesign)} ms_retest={F1(switchRetest)} ms_reset={F1(resetMs)} " +
            $"d1={F3(delta1)} z1={F3(z1)} d2={F3(delta2)} z2={F3(z2)} d3={F3(delta3)} z3={F3(z3)} " +
            $"switches={session.SwitchCount}");
    }

    IEnumerator DriveOnce(PhysicsTestDrive drive, RobotBlueprint bp)
    {
        var start = drive.transform.position;
        var end = Time.time + driveSeconds;
        var cmd = new RobotWiringDriveResolver.ControlState { ForwardBack = 1f, LeftRight = 0f };
        while (Time.time < end)
        {
            drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bp, cmd));
            yield return new WaitForFixedUpdate();
        }

        drive.SetCommand(new PhysicsTestDriveCommand { Brake = true });
        yield return new WaitForSeconds(0.12f);

        var travel = drive.transform.position - start;
        lastDelta = travel.magnitude;
        lastZ = travel.z;
        var rb = drive.GetComponent<Rigidbody>();
        lastNan = IsBad(drive.transform.position) || (rb != null && IsBad(rb.linearVelocity));
    }

    static bool IsBad(Vector3 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
        float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);

    static string F3(float v) => v.ToString("F3", CultureInfo.InvariantCulture);
    static string F1(double v) => v.ToString("F1", CultureInfo.InvariantCulture);
}
