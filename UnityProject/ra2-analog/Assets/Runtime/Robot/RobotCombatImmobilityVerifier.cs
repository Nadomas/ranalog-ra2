using System.Collections;
using System.Globalization;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S7/S8 thin combat: functional disable → immobility countdown → authoritative Immobilized win reason.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotCombatImmobilityVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;
    [SerializeField] float driveSecondsBeforeHit = 0.6f;
    [SerializeField] float immobileNeed = 1.2f;
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

        var bpA = RobotBlueprint.CreateRa2ConstructionSampleA(new Vector3(-3.5f, 0.85f, 0f), 90f);
        var bpB = RobotBlueprint.CreateRa2ConstructionSampleB(new Vector3(3.5f, 0.85f, 0f), -90f);
        RobotControlConfigurer.ApplyDrivePreset(bpA, RobotControlConfigurer.DrivePreset.TankSteer);
        RobotControlConfigurer.ApplyDrivePreset(bpB, RobotControlConfigurer.DrivePreset.TankSteer);

        string errA = null;
        string errB = null;
        if (!RobotSpawnService.TryValidate(bpA, out errA) ||
            !RobotSpawnService.TryValidate(bpB, out errB))
        {
            Debug.Log($"[S8-01] VERIFIER_DONE pass=False reason=admit A={errA} B={errB}");
            yield break;
        }

        var a = RobotSpawnService.Spawn(bpA, 0, 0, null, slideMaterial, new Color(0.2f, 0.55f, 1f));
        var b = RobotSpawnService.Spawn(bpB, 1, 1, null, slideMaterial, new Color(1f, 0.4f, 0.2f));

        // Both drive briefly (alive).
        var end = Time.time + driveSecondsBeforeHit;
        var cmd = new RobotWiringDriveResolver.ControlState { ForwardBack = 1f, LeftRight = 0f };
        while (Time.time < end)
        {
            a.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpA, cmd));
            b.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpB, cmd));
            yield return new WaitForFixedUpdate();
        }

        // S7: functional disable on A (motors/drive stop) — no Transform teleport.
        if (!RobotDamageService.TryFunctionalDisable(a, out var dmgErr))
        {
            Debug.Log($"[S8-01] VERIFIER_DONE pass=False reason=disable err={dmgErr}");
            yield break;
        }

        var disabledOk = RobotDamageService.IsFunctionallyDisabled(a);
        var posBefore = a.Drive.transform.position;

        // Keep commanding both; A should not advance while B still can.
        end = Time.time + 0.5f;
        while (Time.time < end)
        {
            a.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpA, cmd));
            b.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpB, cmd));
            yield return new WaitForFixedUpdate();
        }

        var aStuck = Vector3.Distance(posBefore, a.Drive.transform.position) < 0.35f;

        var rules = new ImmobilityWinEvaluator(new[] { 0, 1 }, immobileSeconds: immobileNeed, speedThreshold: 0.2f);
        var positions = new Vector3[2];
        var disabled = new bool[2];
        MatchOutcome outcome = MatchOutcome.None;
        var safety = Time.time + immobileNeed + 2.5f;
        while (Time.time < safety && !outcome.Finished)
        {
            // B keeps driving; A remains disabled (immobile).
            b.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpB, cmd));
            a.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpA, cmd));
            positions[0] = a.Drive.transform.position;
            positions[1] = b.Drive.transform.position;
            disabled[0] = RobotDamageService.IsFunctionallyDisabled(a);
            disabled[1] = RobotDamageService.IsFunctionallyDisabled(b);
            outcome = rules.Tick(Time.fixedDeltaTime, positions, disabled);
            yield return new WaitForFixedUpdate();
        }

        var pass = disabledOk && aStuck && outcome.Finished &&
                   outcome.Reason == MatchWinReason.Immobilized &&
                   outcome.WinnerRobotId == 1 && outcome.LoserRobotId == 0;

        Debug.Log(
            $"[S8-01] VERIFIER_DONE pass={pass} disabled={disabledOk} a_stuck={aStuck} " +
            $"finished={outcome.Finished} reason={outcome.Reason} winner={outcome.WinnerRobotId} " +
            $"loser={outcome.LoserRobotId} immobileA={F2(rules.GetImmobileSeconds(0))} " +
            $"immobileB={F2(rules.GetImmobileSeconds(1))}");

        RobotSpawnService.Despawn(a);
        RobotSpawnService.Despawn(b);
    }

    static string F2(float v) => v.ToString("F2", CultureInfo.InvariantCulture);
}
