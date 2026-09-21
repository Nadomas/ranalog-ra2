using System.Collections;
using System.Globalization;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S7-03: collision/contact relative speed → concussion/piercing hit → degrade (host apply).
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotContactWeaponHitVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;
    [SerializeField] float driveSeconds = 1.4f;
    [SerializeField] PhysicsMaterial slideMaterial;

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

        // --- Unit path: relative speed → impact → degrade (no Transform) ---
        var bpUnit = RobotBlueprint.CreateRa2ConstructionSampleA(new Vector3(0f, 0.85f, 0f), 0f);
        RobotControlConfigurer.ApplyDrivePreset(bpUnit, RobotControlConfigurer.DrivePreset.TankSteer);
        if (!RobotSpawnService.TryValidate(bpUnit, out var admitUnit))
        {
            Debug.Log($"[S7-03] VERIFIER_DONE pass=False reason=admit_unit err={admitUnit}");
            yield break;
        }

        var unitVictim = RobotSpawnService.Spawn(
            bpUnit, 90, 90, null, slideMaterial, new Color(0.4f, 0.7f, 0.4f));
        var accum = 0f;
        var softOk = RobotContactWeaponHit.TryApplyFromContact(
            unitVictim, 0.9f, 0.4f, relativeSpeed: 0.2f, ref accum,
            out var softOut, out var softSev, out var softImpact, out var softErr);
        var softPass = softOk && softOut == RobotWeaponHitOutcome.None &&
                       softImpact <= 0f && softSev <= 0f && softErr == null;

        var midOk = RobotContactWeaponHit.TryApplyFromContact(
            unitVictim, 0.9f, 0.4f, relativeSpeed: 6.5f, ref accum,
            out var midOut, out var midSev, out var midImpact, out var midErr);
        var midPass = midOk && midErr == null && midImpact > 0f &&
                      midOut == RobotWeaponHitOutcome.Degraded &&
                      Approx(unitVictim.Drive.DrivePowerScale, RobotDamageService.DegradedDrivePowerScale);

        RobotSpawnService.Despawn(unitVictim);

        // --- Physics path: head-on drive contact applies via probe ---
        var bpA = RobotBlueprint.CreateRa2ConstructionSampleA(new Vector3(-2.2f, 0.85f, 0f), 90f);
        var bpB = RobotBlueprint.CreateRa2ConstructionSampleB(new Vector3(2.2f, 0.85f, 0f), -90f);
        RobotControlConfigurer.ApplyDrivePreset(bpA, RobotControlConfigurer.DrivePreset.TankSteer);
        RobotControlConfigurer.ApplyDrivePreset(bpB, RobotControlConfigurer.DrivePreset.TankSteer);

        string errA = null;
        string errB = null;
        if (!RobotSpawnService.TryValidate(bpA, out errA) ||
            !RobotSpawnService.TryValidate(bpB, out errB))
        {
            Debug.Log($"[S7-03] VERIFIER_DONE pass=False reason=admit A={errA} B={errB}");
            yield break;
        }

        var attacker = RobotSpawnService.Spawn(bpA, 0, 0, null, slideMaterial, new Color(0.2f, 0.55f, 1f));
        var victim = RobotSpawnService.Spawn(bpB, 1, 1, null, slideMaterial, new Color(1f, 0.4f, 0.2f));

        var probe = attacker.Assembly.Root.AddComponent<RobotContactWeaponProbe>();
        probe.Bind(attacker);
        probe.ConfigureMix(0.9f, 0.4f);
        probe.HostAuthority = true;

        // Extra impulse toward each other to guarantee contact within drive window.
        if (attacker.Assembly.RootBody != null)
            attacker.Assembly.RootBody.AddForce(Vector3.right * 18f, ForceMode.VelocityChange);
        if (victim.Assembly.RootBody != null)
            victim.Assembly.RootBody.AddForce(Vector3.left * 18f, ForceMode.VelocityChange);

        var cmd = new RobotWiringDriveResolver.ControlState { ForwardBack = 1f, LeftRight = 0f };
        var end = Time.time + driveSeconds;
        while (Time.time < end)
        {
            attacker.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpA, cmd));
            victim.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpB, cmd));
            yield return new WaitForFixedUpdate();
        }

        var contactHit = probe.HitCount > 0 && probe.LastImpact > 0f;
        var victimHurt = victim.Drive != null &&
                         (victim.Drive.DrivePowerScale < 0.99f ||
                          RobotDamageService.IsFunctionallyDisabled(victim) ||
                          probe.LastOutcome != RobotWeaponHitOutcome.None);
        // If physics contact was soft, fall back: host still applied unit path above.
        var physicsPass = contactHit && victimHurt;

        var pass = softPass && midPass && physicsPass;
        Debug.Log(
            $"[S7-03] VERIFIER_DONE pass={pass} soft={softPass} unit_degrade={midPass} " +
            $"physics={physicsPass} hits={probe.HitCount} last_speed={F2(probe.LastRelativeSpeed)} " +
            $"last_impact={F2(probe.LastImpact)} last_out={probe.LastOutcome} " +
            $"victim_scale={(victim.Drive != null ? F2(victim.Drive.DrivePowerScale) : "n/a")} " +
            $"mid_impact={F2(midImpact)} mid_sev={F2(midSev)}");

        RobotSpawnService.Despawn(attacker);
        RobotSpawnService.Despawn(victim);
    }

    static bool Approx(float a, float b) => Mathf.Abs(a - b) < 0.02f;

    static string F2(float v) => v.ToString("F2", CultureInfo.InvariantCulture);
}
