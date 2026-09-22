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
    string pendingResultsTitle;
    RobotSpawnedInstance wiredInput;
    bool smokeMode;
    bool soakMode;
    float fightEndsAt;
    string fightYouLabel = "YOU";
    string fightAiLabel = "AI";
    string fightLockPill;
    string fightYouBase = "YOU";
    string fightAiBase = "AI";

    public bool Ready { get; private set; }
    public string FightStatus => fightStatus;
    public bool FightRunning => fightRunning;
    public RobotWorkshopChrome Chrome => chrome;
    public string PendingResultsText => pendingResultsText;
    public string PendingResultsTitle => pendingResultsTitle;
    public bool HasResultsOverlay => !string.IsNullOrEmpty(pendingResultsText);
    public float FightSecondsLeft => fightRunning ? Mathf.Max(0f, fightEndsAt - Time.time) : 0f;
    public string FightYouLabel => fightYouLabel;
    public string FightAiLabel => fightAiLabel;
    /// <summary>S13-01: lock countdown pill text while a seat is accruing immobility; null when idle.</summary>
    public string FightLockPill => fightLockPill;
    public string LastControlDebug { get; private set; }

    /// <summary>S11-19: live command + binding readout for Test/Fight (local-only).</summary>
    public string FormatControlDebug()
    {
        EnsureChrome();
        RobotSpawnedInstance inst = null;
        if (fightRunning && fightPlayer != null)
            inst = fightPlayer;
        else if (chrome.Session?.Mode == WorkshopMode.Test)
            inst = chrome.Session.TestInstance;

        var text = RobotControlDebugFormatter.Format(
            chrome.Session?.WorkingBlueprint,
            inst?.Drive);
        LastControlDebug = text;
        return text;
    }

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
        soakMode = HasCliFlag("-ra2-mvp-soak");
#if UNITY_EDITOR
        if (!smokeMode && UnityEditor.EditorPrefs.GetBool("Ra2MvpForceSmoke", false))
        {
            smokeMode = true;
            UnityEditor.EditorPrefs.DeleteKey("Ra2MvpForceSmoke");
        }

        if (!soakMode && UnityEditor.EditorPrefs.GetBool("Ra2MvpForceSoak", false))
        {
            soakMode = true;
            smokeMode = true;
            UnityEditor.EditorPrefs.DeleteKey("Ra2MvpForceSoak");
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

    public void ClearPendingResults()
    {
        pendingResultsText = null;
        pendingResultsTitle = null;
    }

    void PresentPendingResults(MatchSummary summary, string side)
    {
        MatchResultsStub.Present(summary, side, persist: true, view: resultsView);
        pendingResultsText = MatchResultsStub.FormatReadable(summary, side);
        pendingResultsTitle = MatchResultsStub.FormatResultsTitle(summary);
    }

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

    public void TryUiFirePreset()
    {
        EnsureChrome();
        chrome.TryApplyFirePreset(out _, out _);
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

    public void TryUiCycleSlotKind(int index)
    {
        EnsureChrome();
        chrome.TryCycleSlotKind(index, out _);
    }

    public void TryUiCycleSlotBinding(int index)
    {
        EnsureChrome();
        chrome.TryCycleSlotBinding(index, out _);
    }

    public void TryUiSaveBlueprint()
    {
        EnsureChrome();
        chrome.TrySaveBlueprint(out _);
    }

    public void TryUiLoadBlueprint()
    {
        EnsureChrome();
        chrome.TryLoadBlueprint(out _);
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

        fightYouBase = "YOU · HOST";
        fightAiBase = "PEER";
        fightYouLabel = fightYouBase;
        fightAiLabel = fightAiBase;
        fightLockPill = null;
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
                fightLockPill = null;
            },
            onImmobilityTick: rules => PushImmobilityHud(rules),
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

        fightYouBase = "YOU · JOIN";
        fightAiBase = "HOST";
        fightYouLabel = fightYouBase;
        fightAiLabel = fightAiBase;
        fightLockPill = null;
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
            PresentPendingResults(result.Summary, side);
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
            cam.backgroundColor = new Color(0.045f, 0.05f, 0.07f);
            camGo.AddComponent<AudioListener>();
        }
        else
        {
            var cam = Camera.main;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.045f, 0.05f, 0.07f);
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

    void EnsureArenaDressing() => RobotMvpArenaDressing.Ensure();

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

        var gizmo = GetComponent<RobotChassisPolygonGizmo>();
        if (gizmo == null)
            gizmo = gameObject.AddComponent<RobotChassisPolygonGizmo>();
        gizmo.Bind(chrome);

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
            PresentPendingResults(summary, "mvp-player");
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

        fightYouBase = "YOU · UDP";
        fightAiBase = "AI · NET";
        fightYouLabel = fightYouBase;
        fightAiLabel = fightAiBase;
        fightLockPill = null;
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
                fightLockPill = null;
            },
            onImmobilityTick: rules => PushImmobilityHud(rules),
            done: r => result = r);

        if (result.Ok && result.HostSummary.Outcome.Finished)
        {
            PresentPendingResults(result.HostSummary, "mvp-udp-host");
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
        fightYouBase = "YOU";
        fightAiBase = "AI";
        fightYouLabel = "YOU";
        fightAiLabel = "AI";
        fightLockPill = null;

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
            PushImmobilityHud(rules);

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
                rules.ForceOutcome(winnerId: 0, loserId: 1, MatchWinReason.TimeExpired);
            else
                rules.ForceOutcome(winnerId: 1, loserId: 0, MatchWinReason.TimeExpired);
            outcome = rules.LastOutcome;
            Debug.Log($"[S12-02] FIGHT_TIMEOUT_CENTER reason=TimeExpired winner={outcome.WinnerRobotId}");
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
        var freehandOk = TrySmokeFreehandGizmo();
        var okCfg = chrome.TrySetMode(WorkshopMode.Configure, out _);
        yield return null;
        chrome.TryConfigureCycleBinding(out _, out _);
        yield return null;
        chrome.TryApplyTankPreset(out _);
        var fireOk = TrySmokeFireWiring();
        var wireOk = TrySmokeWireCanvas();
        var gridOk = TrySmokeControllerGrid();
        var saveOk = TrySmokeBlueprintSaveLoad();
        yield return null;
        var okTest = chrome.TrySetMode(WorkshopMode.Test, out _);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        var hasInst = chrome.Session.TestInstance != null;
        var debugOk = TrySmokeControlDebug();

        var robotTexOk = false;
        var probeInst = chrome.Session.TestInstance;
        var probeRoot = probeInst != null && probeInst.Assembly != null ? probeInst.Assembly.Root : null;
        // Unity destroyed-object gotcha: use == null (overloaded), not ?.
        if (probeRoot == null)
        {
            Debug.Log("[S12-05] ROBOT_TEX_PROBE root=null");
        }
        else
        {
            var rends = probeRoot.GetComponentsInChildren<MeshRenderer>(true);
            Debug.Log($"[S12-05] ROBOT_TEX_PROBE root={probeRoot.name} rends={rends.Length}");
            for (var i = 0; i < rends.Length; i++)
            {
                var mat = rends[i].sharedMaterial;
                if (mat == null)
                    continue;
                var n = mat.name;
                if (n.IndexOf("Mvp", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Team", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Rubber", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Metal", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Board", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Weapon", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Accent", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Battery", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Spin", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    robotTexOk = true;
                    break;
                }
            }
        }
        Debug.Log($"[S12-05] ROBOT_TEXTURED_SMOKE pass={robotTexOk}");

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

        var soakOk = true;
        var soakPass = 0;
        var soakFail = 0;
        if (soakMode)
        {
            const int soakRounds = 3;
            for (var i = 0; i < soakRounds; i++)
            {
                yield return RunUdpFightFromWorkshop(interactive: false);
                yield return new WaitForSecondsRealtime(0.35f);
                var u = fightStatus != null && fightStatus.StartsWith("done udp");
                yield return RunLanSameProcessSmoke();
                yield return new WaitForSecondsRealtime(0.35f);
                var l = fightStatus != null && fightStatus.StartsWith("done lan");
                if (u && l)
                    soakPass++;
                else
                    soakFail++;
                Debug.Log($"[S12-01] SOAK_ROUND i={i + 1}/{soakRounds} udp={u} lan={l}");
            }

            soakOk = soakFail == 0 && soakPass == soakRounds;
            Debug.Log($"[S12-01] SOAK_DONE pass={soakOk} ok={soakPass} fail={soakFail}");
        }

        var historyOk = MatchSummaryStore.TryListRecent(new System.Collections.Generic.List<MatchSummaryStore.Dto>(8), 8) > 0;
        Debug.Log($"[S11-15] HISTORY_LIST_SMOKE pass={historyOk}");

        EnsureArenaDressing();
        var arenaRoot = GameObject.Find(RobotMvpArenaDressing.RootName);
        var arenaOk = arenaRoot != null;
        var texturedOk = false;
        if (arenaRoot != null)
        {
            var apron = arenaRoot.transform.Find("Apron");
            var rend = apron != null ? apron.GetComponent<MeshRenderer>() : null;
            texturedOk = rend != null && rend.sharedMaterial != null &&
                         rend.sharedMaterial.name.IndexOf("Mvp", System.StringComparison.Ordinal) >= 0;
        }

        Debug.Log($"[S11-16] ARENA_SMOKE pass={arenaOk}");
        Debug.Log($"[S12-04] ARENA_TEXTURED_SMOKE pass={texturedOk}");

        var stalemateOk = TrySmokeStalemateLabel();
        var immobilityHudOk = TrySmokeImmobilityCountdownLabel();
        var starterMatsOk = TrySmokeStarterPartMats();

        var pass = okDesign && okCfg && okTest && hasInst && okAdmit && wireOk && gridOk && saveOk &&
                   debugOk && localOk && udpOk && lanOk && historyOk && arenaOk && texturedOk &&
                   robotTexOk && soakOk && stalemateOk && immobilityHudOk && freehandOk && fireOk &&
                   starterMatsOk;
        var marker = Path.Combine(Application.persistentDataPath, "ra2-mvp-smoke.txt");
        try
        {
            File.WriteAllText(marker,
                $"pass={pass}\nstatus={fightStatus}\nlocal_ok={localOk}\nudp_ok={udpOk}\nlan_ok={lanOk}\n" +
                $"wire_ok={wireOk}\ngrid_ok={gridOk}\nsave_ok={saveOk}\ndebug_ok={debugOk}\n" +
                $"history_ok={historyOk}\narena_ok={arenaOk}\ntextured_ok={texturedOk}\nrobot_tex_ok={robotTexOk}\n" +
                $"soak_ok={soakOk}\nstalemate_ok={stalemateOk}\nimmobility_hud_ok={immobilityHudOk}\n" +
                $"freehand_ok={freehandOk}\nfire_ok={fireOk}\nstarter_mats_ok={starterMatsOk}\n" +
                $"unity={Application.unityVersion}\n");
        }
        catch
        {
            // ignore IO
        }

        Debug.Log(
            $"[S11-07] SMOKE_DONE pass={pass} design={okDesign} cfg={okCfg} test={okTest} " +
            $"inst={hasInst} admit={okAdmit} wire={wireOk} grid={gridOk} save={saveOk} debug={debugOk} " +
            $"local={localOk} udp={udpOk} lan={lanOk} history={historyOk} arena={arenaOk} " +
            $"textured={texturedOk} robotTex={robotTexOk} soak={soakOk} stalemate={stalemateOk} " +
            $"immobHud={immobilityHudOk} freehand={freehandOk} fire={fireOk} starterMats={starterMatsOk} " +
            $"fight={fightStatus} marker={marker}");

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

    bool TrySmokeControllerGrid()
    {
        var bp = chrome.Session.WorkingBlueprint;
        if (bp?.ControlSlots == null || bp.ControlSlots.Length == 0)
            return false;
        var kind0 = bp.ControlSlots[0].Kind;
        var bind0 = bp.ControlSlots[0].InputBinding;
        if (!chrome.TryCycleSlotKind(0, out _))
            return false;
        if (bp.ControlSlots[0].Kind == kind0)
            return false;
        if (!chrome.TryCycleSlotBinding(0, out _))
            return false;
        if (string.Equals(bp.ControlSlots[0].InputBinding, bind0, System.StringComparison.Ordinal))
            return false;
        // Restore Analog+W/S so drive smoke stays sane.
        chrome.TryApplyTankPreset(out _);
        Debug.Log(
            $"[S11-17] CONTROLLER_GRID_SMOKE pass=True kind={kind0}->{RobotControlKind.Analog} " +
            $"bind_cycled=True slots={bp.ControlSlots.Length}");
        return true;
    }

    bool TrySmokeBlueprintSaveLoad()
    {
        var bp = chrome.Session.WorkingBlueprint;
        if (bp == null)
            return false;
        bp.Name = "smoke-save-bot";
        if (!chrome.TrySaveBlueprint(out _))
            return false;
        bp.Name = "mutated-in-memory";
        if (!chrome.TryLoadBlueprint(out _))
            return false;
        var loaded = chrome.Session.WorkingBlueprint;
        var ok = loaded != null &&
                 string.Equals(loaded.Name, "smoke-save-bot", System.StringComparison.Ordinal) &&
                 loaded.Wirings != null && loaded.Wirings.Length > 0;
        Debug.Log($"[S11-18] BLUEPRINT_SAVE_SMOKE pass={ok} name={loaded?.Name} wires={loaded?.Wirings?.Length}");
        return ok;
    }

    bool TrySmokeControlDebug()
    {
        var inst = chrome.Session.TestInstance;
        if (inst?.Drive == null)
            return false;
        inst.Drive.SetCommand(new PhysicsTestDriveCommand { Move = 1f, Turn = 0.25f, Fire = 0.5f });
        var text = FormatControlDebug();
        var ok = !string.IsNullOrEmpty(text) &&
                 text.Contains("M=1.00") &&
                 text.Contains("T=0.25") &&
                 text.Contains("Drive=");
        Debug.Log($"[S11-19] CONTROL_DEBUG_SMOKE pass={ok} text={text.Replace("\n", " | ")}");
        inst.Drive.SetCommand(default);
        return ok;
    }

    bool TrySmokeStalemateLabel()
    {
        var outcome = new MatchOutcome(true, 0, 1, MatchWinReason.TimeExpired);
        var summary = new MatchSummary(outcome, 75f, 0f, 0f, false, "s12-stalemate-smoke");
        var title = MatchResultsStub.FormatResultsTitle(summary);
        var body = MatchResultsStub.FormatReadable(summary, "smoke");
        var ok = title.Contains("TIME") && body.Contains("TimeExpired");
        Debug.Log($"[S12-02] STALEMATE_UX_SMOKE pass={ok} title={title}");
        return ok;
    }

    bool TrySmokeImmobilityCountdownLabel()
    {
        const float need = 1.0f;
        var side = ImmobilityWinEvaluator.FormatSideHud("YOU", 0.4f, need);
        var pill = ImmobilityWinEvaluator.FormatLockPill(0.4f, 0f, need);
        var idle = ImmobilityWinEvaluator.FormatSideHud("YOU", 0f, need);
        var ok = side.Contains("0.6") &&
                 pill != null && pill.Contains("LOCK YOU") && pill.Contains("0.6") &&
                 idle == "YOU";
        Debug.Log($"[S13-01] IMMOBILITY_HUD_SMOKE pass={ok} side={side} pill={pill}");
        return ok;
    }

    bool TrySmokeFreehandGizmo()
    {
        var bp = chrome.Session.WorkingBlueprint;
        var gizmo = GetComponent<RobotChassisPolygonGizmo>();
        if (bp == null || gizmo == null)
        {
            Debug.Log("[S13-02] FREEHAND_GIZMO_SMOKE pass=False missing");
            return false;
        }

        var pts = RobotChassisPolygonEditor.GetPoints(bp);
        if (pts.Length < 1)
        {
            Debug.Log("[S13-02] FREEHAND_GIZMO_SMOKE pass=False no_pts");
            return false;
        }

        var before = pts[0];
        var target = before + new Vector2(0.12f, -0.05f);
        var setOk = chrome.TryDesignSetPoint(0, target, out _);
        var after = RobotChassisPolygonEditor.GetPoints(bp)[0];
        var moved = setOk && (after - before).sqrMagnitude > 0.001f;
        var visible = gizmo.IsVisible;
        var ok = moved && visible && chrome.PolySelectedIndex == 0;
        Debug.Log(
            $"[S13-02] FREEHAND_GIZMO_SMOKE pass={ok} set={setOk} moved={moved} visible={visible} " +
            $"sel={chrome.PolySelectedIndex}");
        return ok;
    }

    bool TrySmokeFireWiring()
    {
        chrome.SelectBindGroup(RobotControlConfigurer.BindingGroupId.Fire);
        if (!chrome.TryConfigureCycleBinding(out var applied, out _))
        {
            Debug.Log("[S14-02] FIRE_WIRING_SMOKE pass=False cycle");
            return false;
        }

        if (!chrome.TryApplyFirePreset(out var detail, out var err))
        {
            Debug.Log($"[S14-02] FIRE_WIRING_SMOKE pass=False preset err={err}");
            return false;
        }

        var bp = chrome.Session.WorkingBlueprint;
        var fireBind = RobotControlConfigurer.GetGroupBinding(bp, RobotControlConfigurer.BindingGroupId.Fire);
        var hasFireWire = false;
        if (bp?.Wirings != null)
        {
            for (var i = 0; i < bp.Wirings.Length; i++)
            {
                if (string.Equals(bp.Wirings[i].ControlSlotId, "fire", System.StringComparison.Ordinal))
                {
                    hasFireWire = true;
                    break;
                }
            }
        }

        var ok = !string.IsNullOrEmpty(applied) &&
                 !string.IsNullOrEmpty(fireBind) &&
                 hasFireWire &&
                 !string.IsNullOrEmpty(detail);
        Debug.Log(
            $"[S14-02] FIRE_WIRING_SMOKE pass={ok} cycle={applied} bind={fireBind} detail={detail}");
        return ok;
    }

    bool TrySmokeStarterPartMats()
    {
        var kitOk = RobotMvpMaterialKit.Battery != null &&
                    RobotMvpMaterialKit.Spin != null &&
                    RobotMvpMaterialKit.Board != null &&
                    (RobotMvpMaterialKit.Battery.name.IndexOf("Battery", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     RobotMvpMaterialKit.Battery.name.IndexOf("Mvp", System.StringComparison.OrdinalIgnoreCase) >= 0) &&
                    (RobotMvpMaterialKit.Spin.name.IndexOf("Spin", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     RobotMvpMaterialKit.Spin.name.IndexOf("Mvp", System.StringComparison.OrdinalIgnoreCase) >= 0);
        Debug.Log(
            $"[S14-01] STARTER_PART_MATS_SMOKE pass={kitOk} " +
            $"battery={RobotMvpMaterialKit.Battery?.name} spin={RobotMvpMaterialKit.Spin?.name} " +
            $"board={RobotMvpMaterialKit.Board?.name}");
        return kitOk;
    }

    void PushImmobilityHud(ImmobilityWinEvaluator rules)
    {
        if (rules == null)
            return;
        var youAccum = rules.GetImmobileSeconds(0);
        var aiAccum = rules.GetImmobileSeconds(1);
        var need = rules.NeedSeconds;
        fightYouLabel = ImmobilityWinEvaluator.FormatSideHud(fightYouBase, youAccum, need);
        fightAiLabel = ImmobilityWinEvaluator.FormatSideHud(fightAiBase, aiAccum, need);
        fightLockPill = ImmobilityWinEvaluator.FormatLockPill(youAccum, aiAccum, need);
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
                onImmobilityTick: null,
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
