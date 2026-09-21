using System.Collections;
using System.IO;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S11-07: first playable Windows player entry — workshop + local fight + results.
/// Works in Editor and Standalone (no Editor menu required at runtime).
/// Pass <c>-ra2-mvp-smoke</c> to auto-run Design→Test→Fight→Results→quit.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotMvpPlayableApp : MonoBehaviour
{
    [SerializeField] PhysicsMaterial slideMaterial;
    [SerializeField] Color bodyColor = new Color(0.55f, 0.72f, 0.9f);
    [SerializeField] float fightDriveSeconds = 0.4f;
    [SerializeField] float immobileNeed = 1.0f;
    [SerializeField] float interactiveFightSeconds = 75f;
    [SerializeField] float arenaForfeitRadius = 12.5f;

    RobotWorkshopChrome chrome;
    MatchResultsView resultsView;
    PhysicsMaterial runtimeSlide;
    PhysicsMaterial runtimeGrip;
    bool fightRunning;
    string fightStatus = "";
    string pendingResultsText;
    RobotSpawnedInstance wiredInput;
    bool smokeMode;

    public bool Ready { get; private set; }
    public string FightStatus => fightStatus;
    public bool FightRunning => fightRunning;
    public RobotWorkshopChrome Chrome => chrome;
    public string PendingResultsText => pendingResultsText;
    public bool HasResultsOverlay => !string.IsNullOrEmpty(pendingResultsText);

    void Awake()
    {
        EnsureWorld();
        EnsureChrome();
        smokeMode = HasCliFlag("-ra2-mvp-smoke");
        Ready = true;
        Debug.Log("[S11-07] PLAYABLE_READY");
        Debug.Log("[S11-08] UI_TOOLKIT_SHELL");
    }

    void Start()
    {
        smokeMode = HasCliFlag("-ra2-mvp-smoke");
        if (smokeMode)
            StartCoroutine(RunSmokeAndQuit());
    }

    void Update()
    {
        WireTestInput();
    }

    // IMGUI removed — S11-08 UI Toolkit owns chrome (RobotMvpUiShell).

    public void ClearPendingResults() => pendingResultsText = null;

    public bool TryUiSetMode(WorkshopMode mode)
    {
        EnsureChrome();
        return chrome.TrySetMode(mode, out _);
    }

    public void TryUiPolyStep(int delta)
    {
        EnsureChrome();
        chrome.StepPolySelection(delta);
    }

    public void TryUiPolyNudge()
    {
        EnsureChrome();
        chrome.TryDesignNudge(new Vector2(0.1f, 0f), out _);
    }

    public void TryUiSelectBind(RobotControlConfigurer.BindingGroupId group)
    {
        EnsureChrome();
        chrome.SelectBindGroup(group);
    }

    public void TryUiCycleBind()
    {
        EnsureChrome();
        chrome.TryConfigureCycleBinding(out _, out _);
    }

    public void TryUiTankPreset()
    {
        EnsureChrome();
        chrome.TryApplyTankPreset(out _);
    }

    public void TryUiResetTest()
    {
        EnsureChrome();
        chrome.TryResetTest(out _);
    }

    public void TryUiPrepareAdmit()
    {
        EnsureChrome();
        chrome.TryPrepareAdmit(out _);
    }

    public void TryUiTestAdmit()
    {
        EnsureChrome();
        chrome.TryTestAdmitClone(out _);
    }

    public void TryUiLocalFight()
    {
        if (!fightRunning)
            StartCoroutine(RunLocalFightFromWorkshop());
    }

    public void EnsureWorld()
    {
        if (Camera.main == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.transform.position = new Vector3(0f, 12f, -14f);
            cam.transform.rotation = Quaternion.Euler(40f, 0f, 0f);
            camGo.AddComponent<AudioListener>();
        }

        if (FindFirstObjectByType<Light>() == null)
        {
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        if (GameObject.Find("Floor") == null)
        {
            runtimeGrip ??= CreateRuntimeGrip();
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(2.5f, 1f, 2.5f);
            Object.Destroy(floor.GetComponent<MeshCollider>());
            var box = floor.AddComponent<BoxCollider>();
            box.size = new Vector3(10f, 0.1f, 10f);
            box.center = new Vector3(0f, -0.05f, 0f);
            box.sharedMaterial = runtimeGrip;
            var rb = floor.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (slideMaterial == null)
            slideMaterial = Resources.Load<PhysicsMaterial>("PhysicsTestSlide");
        if (slideMaterial == null)
        {
            runtimeSlide ??= CreateRuntimeSlide();
            slideMaterial = runtimeSlide;
        }
    }

    void EnsureChrome()
    {
        chrome = GetComponent<RobotWorkshopChrome>();
        if (chrome == null)
            chrome = gameObject.AddComponent<RobotWorkshopChrome>();
        chrome.Configure(slideMaterial);
        chrome.SuppressImgui = true;
        chrome.EnsureSession();

        resultsView = GetComponent<MatchResultsView>();
        if (resultsView == null)
            resultsView = gameObject.AddComponent<MatchResultsView>();
        resultsView.SuppressImgui = true;
        resultsView.Hide();
    }

    void WireTestInput()
    {
        if (chrome?.Session == null)
        {
            wiredInput = null;
            return;
        }

        // Test Room + interactive fight both need WASD on the player seat.
        RobotSpawnedInstance inst = null;
        if (fightRunning && fightPlayer != null)
            inst = fightPlayer;
        else if (chrome.Session.Mode == WorkshopMode.Test)
            inst = chrome.Session.TestInstance;

        if (inst == null || inst.Drive == null)
        {
            wiredInput = null;
            return;
        }

        if (ReferenceEquals(wiredInput, inst))
            return;

        var input = inst.Drive.GetComponent<PhysicsTestPlayerInput>();
        if (input == null)
            input = inst.Drive.gameObject.AddComponent<PhysicsTestPlayerInput>();
        input.enabled = true;
        wiredInput = inst;
    }

    RobotSpawnedInstance fightPlayer;

    public IEnumerator RunLocalFightFromWorkshop()
    {
        if (fightRunning)
            yield break;
        fightRunning = true;
        fightStatus = "fighting…";
        resultsView?.Hide();

        EnsureChrome();
        RobotBlueprint admitBp;
        if (chrome.Session.LastAdmitBlueprint != null)
        {
            admitBp = RobotBlueprintSerializer.FromJson(
                chrome.Session.LastAdmitJson ??
                RobotBlueprintSerializer.ToJson(chrome.Session.LastAdmitBlueprint));
        }
        else if (chrome.Session.WorkingBlueprint != null)
        {
            if (!chrome.Session.TryPrepareCombatAdmit(out admitBp, out _, out var err))
            {
                fightStatus = $"admit_fail={err}";
                fightRunning = false;
                yield break;
            }
        }
        else
        {
            fightStatus = "no_blueprint";
            fightRunning = false;
            yield break;
        }

        // Leave Test spawn so fight owns the arena.
        if (chrome.Session.Mode == WorkshopMode.Test)
            chrome.TrySetMode(WorkshopMode.Configure, out _);

        MatchSummary summary = MatchSummary.None;
        yield return RunLocalFight(admitBp, interactive: !smokeMode, s => summary = s);

        if (summary.Outcome.Finished)
        {
            MatchResultsStub.Present(summary, "mvp-player", persist: true, view: resultsView);
            pendingResultsText = MatchResultsStub.FormatReadable(summary, "mvp-player");
            fightStatus = $"done {summary.Outcome.Reason} winner={summary.Outcome.WinnerRobotId}";
            Debug.Log(
                $"[S11-07] FIGHT_DONE pass=True reason={summary.Outcome.Reason} " +
                $"winner={summary.Outcome.WinnerRobotId} loser={summary.Outcome.LoserRobotId}");
        }
        else
        {
            fightStatus = "fight_failed";
            Debug.Log("[S11-07] FIGHT_DONE pass=False");
        }

        fightRunning = false;
    }

    IEnumerator RunLocalFight(RobotBlueprint admitBp, bool interactive, System.Action<MatchSummary> onDone)
    {
        Physics.gravity = new Vector3(0f, -9.81f, 0f);

        var bpA = RobotBlueprintSerializer.FromJson(RobotBlueprintSerializer.ToJson(admitBp));
        bpA.RootPosition = new Vector3(-3.5f, 0.55f, 0f);
        bpA.RootYawDegrees = 90f;
        RobotControlConfigurer.ApplyDrivePreset(bpA, RobotControlConfigurer.DrivePreset.TankSteer);

        var bpB = RobotBlueprint.CreateRa2ConstructionSampleB(new Vector3(3.5f, 0.55f, 0f), -90f);
        RobotControlConfigurer.ApplyDrivePreset(bpB, RobotControlConfigurer.DrivePreset.TankSteer);

        var okA = RobotSpawnService.TryValidate(bpA, out var errA);
        var okB = RobotSpawnService.TryValidate(bpB, out var errB);
        if (!okA || !okB)
        {
            Debug.Log($"[S11-07] fight admit fail A={errA ?? "?"} B={errB ?? "?"}");
            onDone(MatchSummary.None);
            yield break;
        }

        EnsureArenaBounds();

        var a = RobotSpawnService.Spawn(bpA, 0, 0, null, slideMaterial, new Color(0.2f, 0.55f, 1f));
        var b = RobotSpawnService.Spawn(bpB, 1, 1, null, slideMaterial, new Color(1f, 0.4f, 0.2f));
        EnsureContactProbe(a);
        EnsureContactProbe(b);

        MatchSummary summary = MatchSummary.None;
        if (interactive)
            yield return RunInteractiveFight(a, b, bpA, bpB, s => summary = s);
        else
            yield return RunSmokeFight(a, b, bpA, bpB, s => summary = s);

        fightPlayer = null;
        wiredInput = null;
        onDone(summary);
    }

    IEnumerator RunInteractiveFight(
        RobotSpawnedInstance a,
        RobotSpawnedInstance b,
        RobotBlueprint bpA,
        RobotBlueprint bpB,
        System.Action<MatchSummary> onDone)
    {
        fightPlayer = a;
        WireTestInput();
        fightStatus = "fight · WASD you · AI hunts";

        var rules = new ImmobilityWinEvaluator(new[] { 0, 1 }, immobileSeconds: immobileNeed, speedThreshold: 0.25f);
        var positions = new Vector3[2];
        var disabled = new bool[2];
        MatchOutcome outcome = MatchOutcome.None;
        var start = Time.time;
        var safety = start + interactiveFightSeconds;

        while (Time.time < safety && !outcome.Finished)
        {
            var ai = RobotSimpleChaseAi.Seek(b.Drive.transform, a.Drive.transform.position);
            b.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpB, ai));
            // Player seat: PhysicsTestPlayerInput owns a.Drive command.

            positions[0] = a.Drive.transform.position;
            positions[1] = b.Drive.transform.position;
            disabled[0] = RobotDamageService.IsFunctionallyDisabled(a);
            disabled[1] = RobotDamageService.IsFunctionallyDisabled(b);
            outcome = rules.Tick(Time.fixedDeltaTime, positions, disabled);

            if (!outcome.Finished)
            {
                if (IsOutOfArena(positions[0]))
                    rules.ForceOutcome(winnerId: 1, loserId: 0, MatchWinReason.Immobilized);
                else if (IsOutOfArena(positions[1]))
                    rules.ForceOutcome(winnerId: 0, loserId: 1, MatchWinReason.Immobilized);
                outcome = rules.LastOutcome;
            }

            yield return new WaitForFixedUpdate();
        }

        if (!outcome.Finished)
        {
            // Timeout: who stayed more central wins (thin MVP stand-in for judges).
            var aDist = new Vector3(positions[0].x, 0f, positions[0].z).magnitude;
            var bDist = new Vector3(positions[1].x, 0f, positions[1].z).magnitude;
            if (aDist <= bDist)
                rules.ForceOutcome(winnerId: 0, loserId: 1, MatchWinReason.Immobilized);
            else
                rules.ForceOutcome(winnerId: 1, loserId: 0, MatchWinReason.Immobilized);
            outcome = rules.LastOutcome;
            Debug.Log($"[S11-10] FIGHT_TIMEOUT_CENTER winner={outcome.WinnerRobotId}");
        }

        var summary = new MatchSummary(
            outcome,
            matchDurationSeconds: Time.time - start,
            immobileSecondsLoser: rules.GetImmobileSeconds(0),
            immobileSecondsWinner: rules.GetImmobileSeconds(1),
            loserWasDisabled: outcome.Finished &&
                              ((outcome.LoserRobotId == 0 && disabled[0]) ||
                               (outcome.LoserRobotId == 1 && disabled[1])),
            sessionId: "mvp-player-local");

        RobotSpawnService.Despawn(a);
        RobotSpawnService.Despawn(b);
        onDone(summary);
    }

    IEnumerator RunSmokeFight(
        RobotSpawnedInstance a,
        RobotSpawnedInstance b,
        RobotBlueprint bpA,
        RobotBlueprint bpB,
        System.Action<MatchSummary> onDone)
    {
        var cmd = new RobotWiringDriveResolver.ControlState { ForwardBack = 1f, LeftRight = 0.15f };
        var end = Time.time + fightDriveSeconds;
        while (Time.time < end)
        {
            a.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpA, cmd));
            b.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpB, cmd));
            yield return new WaitForFixedUpdate();
        }

        if (!RobotDamageService.TryFunctionalDisable(a, out var dmgErr))
        {
            Debug.Log($"[S11-07] disable fail err={dmgErr}");
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
            var ai = RobotSimpleChaseAi.Seek(b.Drive.transform, a.Drive.transform.position);
            b.Drive.SetCommand(RobotWiringDriveResolver.ResolveTankDrive(bpB, ai));
            a.Drive.SetCommand(default);
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
            sessionId: "mvp-player-local");

        RobotSpawnService.Despawn(a);
        RobotSpawnService.Despawn(b);
        onDone(summary);
    }

    bool IsOutOfArena(Vector3 pos) =>
        pos.y < -2f || new Vector3(pos.x, 0f, pos.z).magnitude > arenaForfeitRadius;

    static void EnsureContactProbe(RobotSpawnedInstance inst)
    {
        if (inst?.Assembly?.Root == null)
            return;
        var probe = inst.Assembly.Root.GetComponent<RobotContactWeaponProbe>();
        if (probe == null)
            probe = inst.Assembly.Root.AddComponent<RobotContactWeaponProbe>();
        probe.Bind(inst);
        probe.ConfigureMix(0.85f, 0.35f);
        probe.HostAuthority = true;
    }

    void EnsureArenaBounds()
    {
        if (GameObject.Find("ArenaBounds") != null)
            return;

        var root = new GameObject("ArenaBounds");
        CreateWall(root.transform, "WallN", new Vector3(0f, 1f, 12f), new Vector3(26f, 2f, 1f));
        CreateWall(root.transform, "WallS", new Vector3(0f, 1f, -12f), new Vector3(26f, 2f, 1f));
        CreateWall(root.transform, "WallE", new Vector3(12f, 1f, 0f), new Vector3(1f, 2f, 26f));
        CreateWall(root.transform, "WallW", new Vector3(-12f, 1f, 0f), new Vector3(1f, 2f, 26f));
    }

    static void CreateWall(Transform parent, string name, Vector3 pos, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent, false);
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        Object.Destroy(wall.GetComponent<MeshRenderer>());
        var rb = wall.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    IEnumerator RunSmokeAndQuit()
    {
        yield return null;
        EnsureChrome();
        var okDesign = chrome.TrySetMode(WorkshopMode.Design, out _);
        yield return null;
        chrome.TryDesignNudge(new Vector2(0.1f, 0f), out _);
        yield return null;
        var okCfg = chrome.TrySetMode(WorkshopMode.Configure, out _);
        yield return null;
        chrome.TryConfigureCycleBinding(out _, out _);
        yield return null;
        var okTest = chrome.TrySetMode(WorkshopMode.Test, out _);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        var hasInst = chrome.Session.TestInstance != null;
        var okAdmit = chrome.TryPrepareAdmit(out _);
        yield return null;

        yield return RunLocalFightFromWorkshop();
        yield return new WaitForSecondsRealtime(0.2f);

        var pass = okDesign && okCfg && okTest && hasInst && okAdmit &&
                   fightStatus != null && fightStatus.StartsWith("done");
        var marker = Path.Combine(Application.persistentDataPath, "ra2-mvp-smoke.txt");
        try
        {
            File.WriteAllText(marker,
                $"pass={pass}\nstatus={fightStatus}\nunity={Application.unityVersion}\n");
        }
        catch
        {
            // ignore IO
        }

        Debug.Log(
            $"[S11-07] SMOKE_DONE pass={pass} design={okDesign} cfg={okCfg} test={okTest} " +
            $"inst={hasInst} admit={okAdmit} fight={fightStatus} marker={marker}");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(pass ? 0 : 1);
#endif
    }

    static bool HasCliFlag(string flag)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], flag, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    static PhysicsMaterial CreateRuntimeSlide() =>
        new PhysicsMaterial("RuntimeSlide")
        {
            dynamicFriction = 0.05f,
            staticFriction = 0.05f,
            bounciness = 0.1f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Average
        };

    static PhysicsMaterial CreateRuntimeGrip() =>
        new PhysicsMaterial("RuntimeGrip")
        {
            dynamicFriction = 0.85f,
            staticFriction = 0.95f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Average,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
}
