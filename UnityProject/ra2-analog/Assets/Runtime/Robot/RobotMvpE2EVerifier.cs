using System.Collections;
using System.Globalization;
using System.IO;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S11-E2E: one-scene Design→polygon→Configure groups→Test→Fight→Results smoke.
/// Local combat only (no UDP) — proves touchable loop glue in a single Play.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotMvpE2EVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;
    [SerializeField] float driveSeconds = 0.35f;
    [SerializeField] float immobileNeed = 1.0f;
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

        var color = new Color(0.35f, 0.6f, 0.85f);
        var workshop = new RobotWorkshopSession();
        var bp = RobotBlueprint.CreateRa2ConstructionSampleA(new Vector3(-3.5f, 0.85f, 0f), 90f);
        workshop.SetWorkingBlueprint(bp);

        // Design
        if (!workshop.TrySwitchMode(WorkshopMode.Design, null, slideMaterial, color, out var err))
        {
            Debug.Log($"[S11-E2E] VERIFIER_DONE pass=False reason=design err={err}");
            yield break;
        }

        // Polygon edit smoke on working blueprint
        var polyOk = RobotChassisPolygonEditor.TryNudgePoint(
            workshop.WorkingBlueprint, 0, new Vector2(0.08f, 0f), out var polyErr);
        var ptsOk = RobotChassisPolygonEditor.PointCount(workshop.WorkingBlueprint) <=
                    RobotChassisPolygonEditor.MaxPoints;

        // Configure + binding groups
        if (!workshop.TrySwitchMode(WorkshopMode.Configure, null, slideMaterial, color, out err))
        {
            Debug.Log($"[S11-E2E] VERIFIER_DONE pass=False reason=configure err={err}");
            yield break;
        }

        RobotControlConfigurer.ApplyDrivePreset(
            workshop.WorkingBlueprint, RobotControlConfigurer.DrivePreset.TankSteer);
        var groupOk = RobotControlConfigurer.TryApplyGroupBinding(
            workshop.WorkingBlueprint,
            RobotControlConfigurer.BindingGroupId.Drive,
            "W/S",
            out var groupErr);
        groupOk = groupOk && RobotControlConfigurer.TryApplyGroupBinding(
            workshop.WorkingBlueprint,
            RobotControlConfigurer.BindingGroupId.Turn,
            "A/D",
            out groupErr);

        // Test spawn
        if (!workshop.TrySwitchMode(WorkshopMode.Test, null, slideMaterial, color, out err))
        {
            Debug.Log($"[S11-E2E] VERIFIER_DONE pass=False reason=test err={err}");
            yield break;
        }

        var testDrive = workshop.TestInstance?.Drive;
        if (testDrive == null)
        {
            Debug.Log("[S11-E2E] VERIFIER_DONE pass=False reason=no_test_drive");
            yield break;
        }

        var cmd = new RobotWiringDriveResolver.ControlState { ForwardBack = 1f, LeftRight = 0f };
        var end = Time.time + driveSeconds;
        while (Time.time < end)
        {
            testDrive.SetCommand(
                RobotWiringDriveResolver.ResolveTankDrive(workshop.WorkingBlueprint, cmd));
            yield return new WaitForFixedUpdate();
        }

        if (!workshop.TryPrepareCombatAdmit(out var admitBp, out _, out err))
        {
            Debug.Log($"[S11-E2E] VERIFIER_DONE pass=False reason=admit err={err}");
            yield break;
        }

        // Fight → Immobilized
        MatchSummary fightSummary = MatchSummary.None;
        var fightOk = false;
        yield return RunLocalFight(admitBp, slideMaterial, cmd, s =>
        {
            fightOk = s.Outcome.Finished && s.Outcome.Reason == MatchWinReason.Immobilized;
            fightSummary = s;
        });

        if (!fightOk)
        {
            Debug.Log(
                $"[S11-E2E] VERIFIER_DONE pass=False reason=fight " +
                $"finished={fightSummary.Outcome.Finished} reason={fightSummary.Outcome.Reason}");
            yield break;
        }

        // Results persist + view
        var view = gameObject.AddComponent<MatchResultsView>();
        var presented = MatchResultsStub.Present(fightSummary, "e2e", persist: true, view: view);
        yield return null;

        var path = MatchSummaryStore.DefaultPathFor(fightSummary.SessionId);
        var loaded = MatchSummaryStore.TryLoad(path, out var dto, out var loadErr);
        var resultsOk = loaded &&
                        dto.reason == nameof(MatchWinReason.Immobilized) &&
                        File.Exists(path) &&
                        view.Visible &&
                        !string.IsNullOrEmpty(presented);

        var pass = polyOk && ptsOk && groupOk && fightOk && resultsOk &&
                   workshop.SwitchCount >= 2;

        Debug.Log(
            $"[S11-E2E] VERIFIER_DONE pass={pass} poly={polyOk}/{polyErr} pts_ok={ptsOk} " +
            $"group={groupOk}/{groupErr} fight={fightOk} reason={fightSummary.Outcome.Reason} " +
            $"winner={fightSummary.Outcome.WinnerRobotId} results={resultsOk} path={path} " +
            $"load_err={loadErr ?? "none"} switches={workshop.SwitchCount}");

        yield return new WaitForSecondsRealtime(0.35f);
    }

    IEnumerator RunLocalFight(
        RobotBlueprint admitBp,
        PhysicsMaterial slide,
        RobotWiringDriveResolver.ControlState cmd,
        System.Action<MatchSummary> onDone)
    {
        var bpA = RobotBlueprintSerializer.FromJson(RobotBlueprintSerializer.ToJson(admitBp));
        bpA.RootPosition = new Vector3(-3.5f, 0.85f, 0f);
        bpA.RootYawDegrees = 90f;
        var bpB = RobotBlueprint.CreateRa2ConstructionSampleB(new Vector3(3.5f, 0.85f, 0f), -90f);
        RobotControlConfigurer.ApplyDrivePreset(bpB, RobotControlConfigurer.DrivePreset.TankSteer);

        string errA = null;
        string errB = null;
        var okA = RobotSpawnService.TryValidate(bpA, out errA);
        var okB = RobotSpawnService.TryValidate(bpB, out errB);
        if (!okA || !okB)
        {
            Debug.Log($"[S11-E2E] fight admit fail A={errA ?? "?"} B={errB ?? "?"}");
            onDone(MatchSummary.None);
            yield break;
        }

        var a = RobotSpawnService.Spawn(bpA, 0, 0, null, slide, new Color(0.2f, 0.55f, 1f));
        var b = RobotSpawnService.Spawn(bpB, 1, 1, null, slide, new Color(1f, 0.4f, 0.2f));

        var end = Time.time + driveSeconds;
        while (Time.time < end)
        {
            a.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpA, cmd));
            b.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpB, cmd));
            yield return new WaitForFixedUpdate();
        }

        if (!RobotDamageService.TryFunctionalDisable(a, out var dmgErr))
        {
            Debug.Log($"[S11-E2E] disable fail err={dmgErr}");
            RobotSpawnService.Despawn(a);
            RobotSpawnService.Despawn(b);
            onDone(MatchSummary.None);
            yield break;
        }

        var rules = new ImmobilityWinEvaluator(new[] { 0, 1 }, immobileSeconds: immobileNeed, speedThreshold: 0.2f);
        var positions = new Vector3[2];
        var disabled = new bool[2];
        MatchOutcome outcome = MatchOutcome.None;
        var start = Time.time;
        var safety = start + immobileNeed + 2.5f;
        while (Time.time < safety && !outcome.Finished)
        {
            b.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpB, cmd));
            a.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpA, cmd));
            positions[0] = a.Drive.transform.position;
            positions[1] = b.Drive.transform.position;
            disabled[0] = RobotDamageService.IsFunctionallyDisabled(a);
            disabled[1] = RobotDamageService.IsFunctionallyDisabled(b);
            outcome = rules.Tick(Time.fixedDeltaTime, positions, disabled);
            yield return new WaitForFixedUpdate();
        }

        var summary = new MatchSummary(
            outcome,
            matchDurationSeconds: Time.time - start,
            immobileSecondsLoser: rules.GetImmobileSeconds(0),
            immobileSecondsWinner: rules.GetImmobileSeconds(1),
            loserWasDisabled: true,
            sessionId: "s11-e2e-" + System.DateTime.UtcNow.ToString("HHmmss", CultureInfo.InvariantCulture));

        RobotSpawnService.Despawn(a);
        RobotSpawnService.Despawn(b);
        onDone(summary);
    }
}
