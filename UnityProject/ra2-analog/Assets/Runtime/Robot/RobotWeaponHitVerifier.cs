using System.Collections;
using System.Globalization;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S7-02 thin weapon hit: concussion+piercing×impact → degrade → functional disable (host apply).
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotWeaponHitVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;
    [SerializeField] float driveSeconds = 0.45f;
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

        var bp = RobotBlueprint.CreateRa2ConstructionSampleA(new Vector3(0f, 0.85f, 0f), 0f);
        RobotControlConfigurer.ApplyDrivePreset(bp, RobotControlConfigurer.DrivePreset.TankSteer);

        if (!RobotSpawnService.TryValidate(bp, out var admitErr))
        {
            Debug.Log($"[S7-02] VERIFIER_DONE pass=False reason=admit err={admitErr}");
            yield break;
        }

        var bot = RobotSpawnService.Spawn(bp, 0, 0, null, slideMaterial, new Color(0.25f, 0.7f, 0.35f));
        var cmd = new RobotWiringDriveResolver.ControlState { ForwardBack = 1f, LeftRight = 0f };
        var end = Time.time + driveSeconds;
        while (Time.time < end)
        {
            bot.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bp, cmd));
            yield return new WaitForFixedUpdate();
        }

        // Sample axe-like mix from ORIGINAL_AS_IS / GDD (0.9 / 0.4).
        var axe = new RobotWeaponHit(concussion: 0.9f, piercing: 0.4f, impact: 0.35f);
        var weakSeverity = RobotDamageService.ComputeHitSeverity(axe);
        var accum = 0f;

        if (!RobotDamageService.TryApplyWeaponHit(bot, axe, ref accum, out var weakOut, out var weakApplied, out var weakErr))
        {
            Debug.Log($"[S7-02] VERIFIER_DONE pass=False reason=weak_hit err={weakErr}");
            RobotSpawnService.Despawn(bot);
            yield break;
        }

        var weakOk = weakOut == RobotWeaponHitOutcome.None &&
                     !RobotDamageService.IsFunctionallyDisabled(bot) &&
                     Approx(bot.Drive.DrivePowerScale, 1f) &&
                     Approx(weakApplied, weakSeverity);

        // Medium impact crosses degrade threshold (~0.52 with axe mix).
        var mid = new RobotWeaponHit(concussion: 0.9f, piercing: 0.4f, impact: 0.85f);
        if (!RobotDamageService.TryApplyWeaponHit(bot, mid, ref accum, out var midOut, out var midApplied, out var midErr))
        {
            Debug.Log($"[S7-02] VERIFIER_DONE pass=False reason=mid_hit err={midErr}");
            RobotSpawnService.Despawn(bot);
            yield break;
        }

        var midOk = midOut == RobotWeaponHitOutcome.Degraded &&
                    !RobotDamageService.IsFunctionallyDisabled(bot) &&
                    Approx(bot.Drive.DrivePowerScale, RobotDamageService.DegradedDrivePowerScale) &&
                    (bot.MotorDrive == null || Approx(bot.MotorDrive.DrivePowerScale, RobotDamageService.DegradedDrivePowerScale));

        // Second medium hit accumulates past disable threshold.
        if (!RobotDamageService.TryApplyWeaponHit(bot, mid, ref accum, out var killOut, out var killApplied, out var killErr))
        {
            Debug.Log($"[S7-02] VERIFIER_DONE pass=False reason=kill_hit err={killErr}");
            RobotSpawnService.Despawn(bot);
            yield break;
        }

        var disabledOk = killOut == RobotWeaponHitOutcome.Disabled &&
                         RobotDamageService.IsFunctionallyDisabled(bot) &&
                         accum >= RobotDamageService.DisableSeverityThreshold;

        var posBefore = bot.Drive.transform.position;
        end = Time.time + 0.4f;
        while (Time.time < end)
        {
            bot.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bp, cmd));
            yield return new WaitForFixedUpdate();
        }

        var stuckOk = Vector3.Distance(posBefore, bot.Drive.transform.position) < 0.35f;
        var pass = weakOk && midOk && disabledOk && stuckOk;

        Debug.Log(
            $"[S7-02] VERIFIER_DONE pass={pass} weak={weakOk} degrade={midOk} disabled={disabledOk} stuck={stuckOk} " +
            $"accum={F2(accum)} weak_sev={F2(weakApplied)} mid_sev={F2(midApplied)} kill_sev={F2(killApplied)} " +
            $"scale={(bot.Drive != null ? F2(bot.Drive.DrivePowerScale) : "n/a")}");

        RobotSpawnService.Despawn(bot);
    }

    static bool Approx(float a, float b) => Mathf.Abs(a - b) < 0.02f;

    static string F2(float v) => v.ToString("F2", CultureInfo.InvariantCulture);
}
