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
    RobotMvpFollowCamera followCam;
    PhysicsMaterial runtimeSlide;
    PhysicsMaterial runtimeGrip;
    bool fightRunning;
    string fightStatus = "";
    string pendingResultsText;
    RobotSpawnedInstance wiredInput;
    bool smokeMode;
    float fightEndsAt;
    string fightYouLabel = "YOU";
    string fightAiLabel = "AI";

    public bool Ready { get; private set; }
    public string FightStatus => fightStatus;
    public bool FightRunning => fightRunning;
    public RobotWorkshopChrome Chrome => chrome;
    public string PendingResultsText => pendingResultsText;
    public bool HasResultsOverlay => !string.IsNullOrEmpty(pendingResultsText);
    public float FightSecondsLeft => fightRunning ? Mathf.Max(0f, fightEndsAt - Time.time) : 0f;
    public string FightYouLabel => fightYouLabel;
    public string FightAiLabel => fightAiLabel;

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
#if UNITY_EDITOR
        if (!smokeMode && UnityEditor.EditorPrefs.GetBool("Ra2MvpForceSmoke", false))
        {
            smokeMode = true;
            UnityEditor.EditorPrefs.DeleteKey("Ra2MvpForceSmoke");
        }
#endif
        if (smokeMode)
            StartCoroutine(RunSmokeAndQuit());
        else
            StartCoroutine(BootIntoDriveRoom());
    }

    void Update()
    {
        WireTestInput();
        UpdateFollowCamera();
    }

    IEnumerator BootIntoDriveRoom()
    {
        yield return null;
        EnsureChrome();
        // Land in Drive so the arena isn't an empty grey slab on first launch.
        chrome.TrySetMode(WorkshopMode.Test, out _);
        yield return new WaitForFixedUpdate();
        UpdateFollowCamera();
    }

    void UpdateFollowCamera()
    {
        if (followCam == null)
            followCam = GetComponent<RobotMvpFollowCamera>() ?? gameObject.AddComponent<RobotMvpFollowCamera>();

        if (fightRunning && fightPlayer != null && fightOpponent != null)
        {
            followCam.SetTargets(fightPlayer.Drive.transform, fightOpponent.Drive.transform);
            return;
        }

        var test = chrome?.Session?.TestInstance;
        if (chrome?.Session != null && chrome.Session.Mode == WorkshopMode.Test && test?.Drive != null)
        {
            followCam.SetTargets(test.Drive.transform);
            return;
        }

        followCam.Clear();
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

    public void TryUiUdpFight()
    {
        if (!fightRunning)
            StartCoroutine(RunUdpFightFromWorkshop(interactive: !smokeMode));
    }

    public void TryUiFlipWire(int index)
    {
        EnsureChrome();
        chrome.TryFlipWireSign(index, out _);
    }

    public void TryUiCycleWireChannel(int index)
    {
        EnsureChrome();
        chrome.TryCycleWireChannel(index, out _);
    }

    public void TryUiLanHost(string _)
    {
        if (!fightRunning)
            StartCoroutine(RunLanHostFromWorkshop());
    }

    public void TryUiLanJoin(string hostAddress)
    {
        if (!fightRunning)
            StartCoroutine(RunLanJoinFromWorkshop(hostAddress));
    }

    IEnumerator RunLanHostFromWorkshop()
    {
        if (fightRunning)
            yield break;
        fightRunning = true;
        fightStatus = "lan host…";
        resultsView?.Hide();
        EnsureChrome();
        EnsureArenaBounds();

        if (!TryResolveAdmitBlueprint(out var admitBp, out var err))
        {
            fightStatus = $"lan_host_fail={err}";
            fightRunning = false;
            yield break;
        }

        if (chrome.Session.Mode == WorkshopMode.Test)
            chrome.TrySetMode(WorkshopMode.Configure, out _);

        fightYouLabel = "YOU · HOST";
        fightAiLabel = "PEER";
        fightEndsAt = Time.time + interactiveFightSeconds;

        var runner = new RobotMvpLanMatchRunner(slideMaterial, interactiveFightSeconds: interactiveFightSeconds);
        RobotMvpLanMatchRunner.Result result = default;
        yield return runner.RunHost(
            admitBp,
            status => fightStatus = status,
            onLive: (a, b) =>
            {
                fightPlayer = a;
                fightOpponent = b;
                // Host seat drive via LanHostLocalSeatDrive → authority (not PlayerInput).
            },
            onCleared: () =>
            {
                fightPlayer = null;
                fightOpponent = null;
                wiredInput = null;
            },
            done: r => result = r);

        PresentLanResult(result);
        fightRunning = false;
    }

    IEnumerator RunLanJoinFromWorkshop(string hostAddress)
    {
        if (fightRunning)
            yield break;
        fightRunning = true;
        fightStatus = "lan join…";
        resultsView?.Hide();
        EnsureChrome();

        if (!TryResolveAdmitBlueprint(out var admitBp, out var err))
        {
            fightStatus = $"lan_join_fail={err}";
            fightRunning = false;
            yield break;
        }

        if (chrome.Session.Mode == WorkshopMode.Test)
            chrome.TrySetMode(WorkshopMode.Configure, out _);

        fightYouLabel = "YOU · JOIN";
        fightAiLabel = "HOST";
        fightEndsAt = Time.time + interactiveFightSeconds;

        var runner = new RobotMvpLanMatchRunner(slideMaterial, interactiveFightSeconds: interactiveFightSeconds);
        RobotMvpLanMatchRunner.Result result = default;
        yield return runner.RunClient(
            admitBp,
            hostAddress,
            status => fightStatus = status,
            onCleared: () => { },
            done: r => result = r);

        PresentLanResult(result);
        fightRunning = false;
    }

    bool TryResolveAdmitBlueprint(out RobotBlueprint admitBp, out string error)
    {
        error = null;
        admitBp = null;
        if (chrome.Session.LastAdmitBlueprint != null)
        {
            admitBp = RobotBlueprintSerializer.FromJson(
                chrome.Session.LastAdmitJson ??
                RobotBlueprintSerializer.ToJson(chrome.Session.LastAdmitBlueprint));
            return true;
        }

        if (chrome.Session.WorkingBlueprint != null)
            return chrome.Session.TryPrepareCombatAdmit(out admitBp, out _, out error);

        error = "no_blueprint";
        return false;
    }

    void PresentLanResult(RobotMvpLanMatchRunner.Result result)
    {
        if (result.Ok && result.Summary.Outcome.Finished)
        {
            var side = result.WasHost ? "mvp-lan-host" : "mvp-lan-client";
            MatchResultsStub.Present(result.Summary, side, persist: true, view: resultsView);
            pendingResultsText = MatchResultsStub.FormatReadable(result.Summary, side);
            fightStatus =
                $"done lan {result.Summary.Outcome.Reason} winner={result.Summary.Outcome.WinnerRobotId}";
            Debug.Log(
                $"[S11-14] LAN_FIGHT_DONE pass=True host={result.WasHost} reason={result.Reason} " +
                $"winner={result.Summary.Outcome.WinnerRobotId} session={result.SessionId}");
        }
        else
        {
            fightStatus = $"lan_fail={result.Reason}";
            Debug.Log($"[S11-14] LAN_FIGHT_DONE pass=False host={result.WasHost} reason={result.Reason}");
        }
    }

    public void EnsureWorld()
    {
        if (Camera.main == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.transform.position = new Vector3(0f, 11f, -13f);
            cam.transform.rotation = Quaternion.Euler(38f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.08f, 0.1f);
            camGo.AddComponent<AudioListener>();
        }
        else
        {
            var cam = Camera.main;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.08f, 0.1f);
        }

        if (FindFirstObjectByType<Light>() == null)
        {
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(1f, 0.96f, 0.9f);
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        if (GameObject.Find("Floor") == null)
        {
            runtimeGrip ??= CreateRuntimeGrip();
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(2.6f, 1f, 2.6f);
            Object.Destroy(floor.GetComponent<MeshCollider>());
            var box = floor.AddComponent<BoxCollider>();
            box.size = new Vector3(10f, 0.1f, 10f);
            box.center = new Vector3(0f, -0.05f, 0f);
            box.sharedMaterial = runtimeGrip;
            var rb = floor.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            TintRenderer(floor, new Color(0.18f, 0.2f, 0.24f));
        }

        EnsureArenaDressing();

        if (slideMaterial == null)
            slideMaterial = Resources.Load<PhysicsMaterial>("PhysicsTestSlide");
        if (slideMaterial == null)
        {
            runtimeSlide ??= CreateRuntimeSlide();
            slideMaterial = runtimeSlide;
        }

        if (followCam == null)
            followCam = GetComponent<RobotMvpFollowCamera>() ?? gameObject.AddComponent<RobotMvpFollowCamera>();
    }

    void EnsureArenaDressing()
    {
        if (GameObject.Find("ArenaRing") != null)
            return;

        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "ArenaRing";
        Object.Destroy(ring.GetComponent<Collider>());
        ring.transform.position = new Vector3(0f, 0.02f, 0f);
        ring.transform.localScale = new Vector3(22f, 0.02f, 22f);
        TintRenderer(ring, new Color(0.35f, 0.28f, 0.14f, 1f));

        // Center pad so spawns read as a pit, not bare plane.
        var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = "ArenaPad";
        Object.Destroy(pad.GetComponent<Collider>());
        pad.transform.SetParent(ring.transform, false);
        pad.transform.localPosition = Vector3.zero;
        pad.transform.localScale = new Vector3(0.55f, 1.1f, 0.55f);
        TintRenderer(pad, new Color(0.22f, 0.25f, 0.3f));
    }

    static void TintRenderer(GameObject go, Color color)
    {
        var rend = go.GetComponent<MeshRenderer>();
        if (rend == null)
            return;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        var mat = new Material(shader) { name = "Runtime_" + go.name };
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else
            mat.color = color;
        rend.sharedMaterial = mat;
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
    RobotSpawnedInstance fightOpponent;

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

    IEnumerator RunUdpFightFromWorkshop(bool interactive)
    {
        if (fightRunning)
            yield break;
        fightRunning = true;
        fightStatus = "udp lobby…";
        resultsView?.Hide();
        EnsureChrome();
        EnsureArenaBounds();

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
                fightStatus = $"udp_admit_fail={err}";
                fightRunning = false;
                yield break;
            }
        }
        else
        {
            fightStatus = "udp_no_blueprint";
            fightRunning = false;
            yield break;
        }

        if (chrome.Session.Mode == WorkshopMode.Test)
            chrome.TrySetMode(WorkshopMode.Configure, out _);

        fightYouLabel = "YOU · UDP";
        fightAiLabel = "AI · NET";
        fightEndsAt = Time.time + interactiveFightSeconds;

        var runner = new RobotMvpUdpLoopbackRunner(slideMaterial, interactiveFightSeconds: interactiveFightSeconds);
        RobotMvpUdpLoopbackRunner.Result result = default;
        yield return runner.Run(
            admitBp,
            interactive,
            onFightLive: (a, b) =>
            {
                fightPlayer = a;
                fightOpponent = b;
                fightStatus = "udp fight · WASD host seat";
                WireTestInput();
            },
            onFightCleared: () =>
            {
                fightPlayer = null;
                fightOpponent = null;
                wiredInput = null;
            },
            done: r => result = r);

        if (result.Ok && result.HostSummary.Outcome.Finished)
        {
            MatchResultsStub.Present(result.HostSummary, "mvp-udp-host", persist: true, view: resultsView);
            pendingResultsText = MatchResultsStub.FormatReadable(result.HostSummary, "mvp-udp");
            fightStatus =
                $"done udp {result.HostSummary.Outcome.Reason} winner={result.HostSummary.Outcome.WinnerRobotId}";
            Debug.Log(
                $"[S11-12] UDP_FIGHT_DONE pass=True reason={result.Reason} " +
                $"winner={result.HostSummary.Outcome.WinnerRobotId} " +
                $"client_winner={result.ClientSummary.Outcome.WinnerRobotId} session={result.SessionId}");
        }
        else
        {
            fightStatus = $"udp_fail={result.Reason}";
            Debug.Log($"[S11-12] UDP_FIGHT_DONE pass=False reason={result.Reason}");
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
        fightOpponent = null;
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
        fightOpponent = b;
        fightEndsAt = Time.time + interactiveFightSeconds;
        WireTestInput();
        fightStatus = "fight · WASD you · AI hunts";
        fightYouLabel = "YOU";
        fightAiLabel = "AI";

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
        chrome.TryApplyTankPreset(out _);
        var wireOk = TrySmokeWireCanvas();
        yield return null;
        var okTest = chrome.TrySetMode(WorkshopMode.Test, out _);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        var hasInst = chrome.Session.TestInstance != null;
        var okAdmit = chrome.TryPrepareAdmit(out _);
        yield return null;

        yield return RunLocalFightFromWorkshop();
        yield return new WaitForSecondsRealtime(0.2f);
        var localOk = fightStatus != null && fightStatus.StartsWith("done");

        yield return RunUdpFightFromWorkshop(interactive: false);
        yield return new WaitForSecondsRealtime(0.5f);
        var udpOk = fightStatus != null && fightStatus.StartsWith("done udp");

        yield return RunLanSameProcessSmoke();
        yield return new WaitForSecondsRealtime(0.2f);
        var lanOk = fightStatus != null && fightStatus.StartsWith("done lan");

        var historyOk = MatchSummaryStore.TryListRecent(new System.Collections.Generic.List<MatchSummaryStore.Dto>(8), 8) > 0;
        Debug.Log($"[S11-15] HISTORY_LIST_SMOKE pass={historyOk}");

        var pass = okDesign && okCfg && okTest && hasInst && okAdmit && wireOk && localOk && udpOk && lanOk && historyOk;
        var marker = Path.Combine(Application.persistentDataPath, "ra2-mvp-smoke.txt");
        try
        {
            File.WriteAllText(marker,
                $"pass={pass}\nstatus={fightStatus}\nlocal_ok={localOk}\nudp_ok={udpOk}\nlan_ok={lanOk}\nwire_ok={wireOk}\nhistory_ok={historyOk}\nunity={Application.unityVersion}\n");
        }
        catch
        {
            // ignore IO
        }

        Debug.Log(
            $"[S11-07] SMOKE_DONE pass={pass} design={okDesign} cfg={okCfg} test={okTest} " +
            $"inst={hasInst} admit={okAdmit} wire={wireOk} local={localOk} udp={udpOk} lan={lanOk} " +
            $"history={historyOk} fight={fightStatus} marker={marker}");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(pass ? 0 : 1);
#endif
    }

    bool TrySmokeWireCanvas()
    {
        var bp = chrome.Session.WorkingBlueprint;
        if (bp?.Wirings == null || bp.Wirings.Length == 0)
            return false;
        var before = bp.Wirings[0].Sign;
        if (!chrome.TryFlipWireSign(0, out _))
            return false;
        if (Mathf.Approximately(bp.Wirings[0].Sign, before))
            return false;
        if (!chrome.TryCycleWireChannel(0, out _))
            return false;
        Debug.Log($"[S11-13] WIRE_CANVAS_SMOKE pass=True sign={bp.Wirings[0].Sign} ch={bp.Wirings[0].Channel}");
        return true;
    }

    IEnumerator RunLanSameProcessSmoke()
    {
        if (fightRunning)
            yield break;
        fightRunning = true;
        fightStatus = "lan smoke…";
        EnsureChrome();
        EnsureArenaBounds();

        if (!TryResolveAdmitBlueprint(out var admitBp, out var err))
        {
            fightStatus = $"lan_smoke_fail={err}";
            fightRunning = false;
            yield break;
        }

        var runner = new RobotMvpLanMatchRunner(
            slideMaterial,
            peerWaitSeconds: 8f,
            spawnWaitSeconds: 12f,
            immobile: 0.8f,
            interactiveFightSeconds: 6f);

        RobotMvpLanMatchRunner.Result hostResult = default;
        RobotMvpLanMatchRunner.Result clientResult = default;
        var hostDone = false;
        var clientDone = false;

        StartCoroutine(WrapLan(
            runner.RunHost(
                admitBp,
                s => fightStatus = "lan host: " + s,
                onLive: (a, b) =>
                {
                    fightPlayer = a;
                    fightOpponent = b;
                },
                onCleared: () =>
                {
                    fightPlayer = null;
                    fightOpponent = null;
                },
                done: r => hostResult = r),
            () => hostDone = true));

        // Client starts slightly later so host socket is listening.
        yield return new WaitForSecondsRealtime(0.35f);

        StartCoroutine(WrapLan(
            runner.RunClient(
                admitBp,
                "127.0.0.1",
                s => fightStatus = "lan join: " + s,
                onCleared: () => { },
                done: r => clientResult = r),
            () => clientDone = true));

        var deadline = Time.realtimeSinceStartup + 45f;
        while ((!hostDone || !clientDone) && Time.realtimeSinceStartup < deadline)
            yield return null;

        fightRunning = false;
        if (hostResult.Ok && clientResult.Ok &&
            hostResult.Summary.Outcome.WinnerRobotId == clientResult.Summary.Outcome.WinnerRobotId)
        {
            fightStatus =
                $"done lan smoke winner={hostResult.Summary.Outcome.WinnerRobotId}";
            Debug.Log(
                $"[S11-14] LAN_SMOKE_DONE pass=True winner={hostResult.Summary.Outcome.WinnerRobotId} " +
                $"host={hostResult.Reason} client={clientResult.Reason}");
        }
        else
        {
            fightStatus =
                $"lan_smoke_fail host={hostResult.Reason} client={clientResult.Reason} " +
                $"hostDone={hostDone} clientDone={clientDone}";
            Debug.Log($"[S11-14] LAN_SMOKE_DONE pass=False {fightStatus}");
        }
    }

    static IEnumerator WrapLan(IEnumerator inner, System.Action onDone)
    {
        yield return inner;
        onDone?.Invoke();
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
