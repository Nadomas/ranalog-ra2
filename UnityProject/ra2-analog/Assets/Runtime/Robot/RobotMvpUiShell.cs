using Ra2.Robot;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UIElements;

/// <summary>
/// S11-08/09: UI Toolkit workshop shell for the MVP playable player.
/// Presentation-only — drives <see cref="RobotMvpPlayableApp"/> / workshop session.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(UIDocument))]
public sealed class RobotMvpUiShell : MonoBehaviour
{
    const string PanelSettingsResource = "Mvp/MvpPanelSettings";
    const string UxmlResource = "Mvp/MvpWorkshop";

    [SerializeField] RobotMvpPlayableApp app;
    [SerializeField] UIDocument document;

    Label modeLabel;
    Label statusLabel;
    Label polyInfo;
    Label bindInfo;
    Label resultsBody;
    Label resultsTitle;
    Label arenaCaption;
    Label admitBadge;
    Label helpLine;
    Label fightPill;
    Label fightTimer;
    Label fightYou;
    Label fightAi;
    Label stepDesign;
    Label stepConfigure;
    Label stepTest;
    Label stepFight;
    VisualElement panelDesign;
    VisualElement panelConfigure;
    VisualElement panelTest;
    VisualElement resultsOverlay;
    VisualElement fightHud;
    VisualElement wireList;
    TextField lanHostField;
    Button btnDesign;
    Button btnConfigure;
    Button btnTest;
    Button btnAdmitTest;
    Button btnFight;
    Button btnUdpFight;
    Button btnLanHost;
    Button btnLanJoin;
    Button btnReset;
    int lastWireFingerprint = int.MinValue;

    bool bound;

    public void Bind(RobotMvpPlayableApp playable)
    {
        app = playable;
    }

    void Awake()
    {
        if (document == null)
            document = GetComponent<UIDocument>();
        if (app == null)
            app = GetComponent<RobotMvpPlayableApp>() ?? FindFirstObjectByType<RobotMvpPlayableApp>();
        EnsureDocumentReady();
        EnsureUiEventSystem();
    }

    void OnEnable()
    {
        EnsureDocumentReady();
        EnsureUiEventSystem();
        TryBindUi();
    }

    void EnsureDocumentReady()
    {
        if (document == null)
            document = GetComponent<UIDocument>();
        if (document == null)
            return;

        if (document.panelSettings == null)
        {
            var fromResources = Resources.Load<PanelSettings>(PanelSettingsResource);
            if (fromResources != null)
            {
                document.panelSettings = fromResources;
            }
            else
            {
                var created = ScriptableObject.CreateInstance<PanelSettings>();
                created.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                created.referenceResolution = new Vector2Int(1920, 1080);
                created.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                created.match = 0.5f;
                created.sortingOrder = 100;
                document.panelSettings = created;
                Debug.LogWarning("[S11-09] UIDocument.panelSettings was null — created runtime PanelSettings");
            }
        }

        if (document.visualTreeAsset == null)
        {
            var uxml = Resources.Load<VisualTreeAsset>(UxmlResource);
            if (uxml != null)
            {
                document.visualTreeAsset = uxml;
                Debug.Log("[S11-09] Loaded MvpWorkshop UXML from Resources");
            }
            else
                Debug.LogError("[S11-09] UIDocument.visualTreeAsset missing and Resources load failed");
        }

        // Force panel rebuild after late assigns (player often ships with null panelSettings).
        document.enabled = false;
        document.enabled = true;
        Debug.Log(
            $"[S11-09] UIDocument ready panel={(document.panelSettings != null)} " +
            $"uxml={(document.visualTreeAsset != null)} rootKids={(document.rootVisualElement != null ? document.rootVisualElement.childCount : -1)}");
    }

    static void EnsureUiEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
        DontDestroyOnLoad(go);
    }

    void Update()
    {
        if (!bound)
            TryBindUi();
        if (!bound || app?.Chrome?.Session == null)
            return;
        RefreshChrome();
    }

    void TryBindUi()
    {
        if (document == null)
            document = GetComponent<UIDocument>();
        var root = document != null ? document.rootVisualElement : null;
        if (root == null || root.childCount == 0)
            return;

        modeLabel = root.Q<Label>("mode-label");
        statusLabel = root.Q<Label>("status");
        polyInfo = root.Q<Label>("poly-info");
        bindInfo = root.Q<Label>("bind-info");
        resultsBody = root.Q<Label>("results-body");
        resultsTitle = root.Q<Label>("results-title");
        arenaCaption = root.Q<Label>("arena-caption");
        admitBadge = root.Q<Label>("admit-badge");
        helpLine = root.Q<Label>("help-line");
        fightPill = root.Q<Label>("fight-pill");
        fightTimer = root.Q<Label>("fight-timer");
        fightYou = root.Q<Label>("fight-you");
        fightAi = root.Q<Label>("fight-ai");
        fightHud = root.Q("fight-hud");
        stepDesign = root.Q<Label>("step-design");
        stepConfigure = root.Q<Label>("step-configure");
        stepTest = root.Q<Label>("step-test");
        stepFight = root.Q<Label>("step-fight");
        panelDesign = root.Q("panel-design");
        panelConfigure = root.Q("panel-configure");
        panelTest = root.Q("panel-test");
        resultsOverlay = root.Q("results-overlay");
        wireList = root.Q("wire-list");
        lanHostField = root.Q<TextField>("lan-host-field");
        btnDesign = root.Q<Button>("btn-design");
        btnConfigure = root.Q<Button>("btn-configure");
        btnTest = root.Q<Button>("btn-test");
        btnAdmitTest = root.Q<Button>("btn-admit-test");
        btnFight = root.Q<Button>("btn-fight");
        btnUdpFight = root.Q<Button>("btn-udp-fight");
        btnLanHost = root.Q<Button>("btn-lan-host");
        btnLanJoin = root.Q<Button>("btn-lan-join");
        btnReset = root.Q<Button>("btn-reset");

        Wire(btnDesign, () => app.TryUiSetMode(WorkshopMode.Design));
        Wire(btnConfigure, () => app.TryUiSetMode(WorkshopMode.Configure));
        Wire(btnTest, () => app.TryUiSetMode(WorkshopMode.Test));
        Wire(root.Q<Button>("btn-poly-prev"), () => app.TryUiPolyStep(-1));
        Wire(root.Q<Button>("btn-poly-next"), () => app.TryUiPolyStep(1));
        Wire(root.Q<Button>("btn-poly-nudge"), () => app.TryUiPolyNudge());
        Wire(root.Q<Button>("btn-bind-drive"), () => app.TryUiSelectBind(RobotControlConfigurer.BindingGroupId.Drive));
        Wire(root.Q<Button>("btn-bind-turn"), () => app.TryUiSelectBind(RobotControlConfigurer.BindingGroupId.Turn));
        Wire(root.Q<Button>("btn-bind-cycle"), () => app.TryUiCycleBind());
        Wire(root.Q<Button>("btn-tank"), () =>
        {
            app.TryUiTankPreset();
            lastWireFingerprint = int.MinValue;
        });
        Wire(btnReset, () => app.TryUiResetTest());
        Wire(root.Q<Button>("btn-admit"), () => app.TryUiPrepareAdmit());
        Wire(btnAdmitTest, () => app.TryUiTestAdmit());
        Wire(btnFight, () => app.TryUiLocalFight());
        Wire(btnUdpFight, () => app.TryUiUdpFight());
        Wire(btnLanHost, () => app.TryUiLanHost(lanHostField != null ? lanHostField.value : "127.0.0.1"));
        Wire(btnLanJoin, () => app.TryUiLanJoin(lanHostField != null ? lanHostField.value : "127.0.0.1"));
        Wire(root.Q<Button>("btn-results-close"), () => HideResults());

        bound = true;
        lastWireFingerprint = int.MinValue;
        RefreshChrome();
    }

    static void Wire(Button button, System.Action action)
    {
        if (button == null || action == null)
            return;
        button.clicked += action;
    }

    void RefreshChrome()
    {
        var chrome = app.Chrome;
        var session = chrome?.Session;
        if (session == null)
            return;

        var mode = session.Mode;
        var hasAdmit = session.LastAdmitBlueprint != null;
        var fighting = app.FightRunning;

        SetActive(btnDesign, mode == WorkshopMode.Design);
        SetActive(btnConfigure, mode == WorkshopMode.Configure);
        SetActive(btnTest, mode == WorkshopMode.Test);
        SetVisible(panelDesign, mode == WorkshopMode.Design);
        SetVisible(panelConfigure, mode == WorkshopMode.Configure);
        SetVisible(panelTest, mode == WorkshopMode.Test);

        if (modeLabel != null)
            modeLabel.text = mode.ToString().ToUpperInvariant();

        RefreshFlow(mode, hasAdmit, fighting);

        if (admitBadge != null)
        {
            admitBadge.text = hasAdmit ? "ADMIT READY" : "NO ADMIT";
            if (hasAdmit)
                admitBadge.AddToClassList("ready");
            else
                admitBadge.RemoveFromClassList("ready");
        }

        if (statusLabel != null)
            statusLabel.text = string.IsNullOrEmpty(app.FightStatus)
                ? chrome.Status
                : $"{chrome.Status}\n{app.FightStatus}";

        var bp = session.WorkingBlueprint;
        if (polyInfo != null && bp != null)
        {
            polyInfo.text =
                $"pts={RobotChassisPolygonEditor.PointCount(bp)}/{RobotChassisPolygonEditor.MaxPoints}  sel={chrome.PolySelectedIndex}";
        }

        if (bindInfo != null && bp != null)
        {
            var drive = RobotControlConfigurer.GetGroupBinding(bp, RobotControlConfigurer.BindingGroupId.Drive);
            var turn = RobotControlConfigurer.GetGroupBinding(bp, RobotControlConfigurer.BindingGroupId.Turn);
            bindInfo.text = $"Drive={drive}  Turn={turn}  sel={chrome.BindGroup}";
        }

        if (mode == WorkshopMode.Configure)
            RebuildWireCanvas(bp);

        if (btnAdmitTest != null)
            btnAdmitTest.SetEnabled(hasAdmit);
        if (btnFight != null)
            btnFight.SetEnabled(!fighting &&
                                (mode == WorkshopMode.Test || hasAdmit || bp != null));
        if (btnUdpFight != null)
            btnUdpFight.SetEnabled(!fighting &&
                                   (mode == WorkshopMode.Test || hasAdmit || bp != null));
        if (btnLanHost != null)
            btnLanHost.SetEnabled(!fighting && bp != null);
        if (btnLanJoin != null)
            btnLanJoin.SetEnabled(!fighting && bp != null);
        if (btnReset != null)
            btnReset.SetEnabled(mode == WorkshopMode.Test);

        if (arenaCaption != null)
        {
            arenaCaption.text = fighting
                ? "ARENA · LOCAL DUEL"
                : mode == WorkshopMode.Test
                    ? "DRIVE ROOM · WASD"
                    : mode == WorkshopMode.Configure
                        ? "WIRE · BINDINGS"
                        : "DESIGN · CHASSIS";
        }

        if (helpLine != null)
        {
            helpLine.text = fighting
                ? "Ram the red AI. Stay in the ring. Immobile or out = loss."
                : mode == WorkshopMode.Test
                    ? "Drive with WASD. Prepare Admit, then Start Local Fight."
                    : mode == WorkshopMode.Configure
                        ? "Pick Drive/Turn, Cycle binding, or apply TankSteer."
                        : "Nudge chassis points, then Wire bindings.";
        }

        if (fightPill != null)
        {
            if (fighting)
            {
                fightPill.text = "LIVE";
                fightPill.RemoveFromClassList("hidden");
            }
            else
            {
                fightPill.AddToClassList("hidden");
            }
        }

        if (fightHud != null)
        {
            if (fighting)
            {
                fightHud.RemoveFromClassList("hidden");
                if (fightYou != null)
                    fightYou.text = app.FightYouLabel;
                if (fightAi != null)
                    fightAi.text = app.FightAiLabel;
                if (fightTimer != null)
                {
                    var left = Mathf.CeilToInt(app.FightSecondsLeft);
                    fightTimer.text = $"{left / 60}:{left % 60:00}";
                }
            }
            else
            {
                fightHud.AddToClassList("hidden");
            }
        }

        if (!string.IsNullOrEmpty(app.PendingResultsText))
        {
            ShowResults(app.PendingResultsText);
            app.ClearPendingResults();
        }
    }

    void RebuildWireCanvas(RobotBlueprint bp)
    {
        if (wireList == null)
            return;

        var fp = 0;
        if (bp?.Wirings != null)
        {
            unchecked
            {
                for (var i = 0; i < bp.Wirings.Length; i++)
                {
                    var w = bp.Wirings[i];
                    fp = (fp * 397) ^ (w.ComponentId?.GetHashCode() ?? 0);
                    fp = (fp * 397) ^ (w.ControlSlotId?.GetHashCode() ?? 0);
                    fp = (fp * 397) ^ (w.Channel?.GetHashCode() ?? 0);
                    fp = (fp * 397) ^ w.Sign.GetHashCode();
                }

                fp ^= bp.Wirings.Length;
            }
        }

        if (fp == lastWireFingerprint && wireList.childCount > 0)
            return;
        lastWireFingerprint = fp;
        wireList.Clear();
        if (bp?.Wirings == null || bp.Wirings.Length == 0)
        {
            var empty = new Label("No wires — apply TankSteer.");
            empty.AddToClassList("tool-help");
            wireList.Add(empty);
            return;
        }

        for (var i = 0; i < bp.Wirings.Length; i++)
        {
            var idx = i;
            var w = bp.Wirings[i];
            var row = new VisualElement();
            row.AddToClassList("wire-row");

            var label = new Label($"{w.ControlSlotId} → {w.ComponentId}");
            label.AddToClassList("wire-row-label");
            row.Add(label);

            var signBtn = new Button(() =>
            {
                app.TryUiFlipWire(idx);
                lastWireFingerprint = int.MinValue;
            })
            {
                text = w.Sign >= 0f ? "+1" : "−1"
            };
            signBtn.AddToClassList("tool-btn");
            signBtn.AddToClassList("wire-mini");
            row.Add(signBtn);

            var chBtn = new Button(() =>
            {
                app.TryUiCycleWireChannel(idx);
                lastWireFingerprint = int.MinValue;
            })
            {
                text = string.IsNullOrEmpty(w.Channel) ? "?" : w.Channel
            };
            chBtn.AddToClassList("tool-btn");
            chBtn.AddToClassList("wire-mini");
            row.Add(chBtn);

            wireList.Add(row);
        }
    }

    void RefreshFlow(WorkshopMode mode, bool hasAdmit, bool fighting)
    {
        var onDesign = mode == WorkshopMode.Design;
        var onConfigure = mode == WorkshopMode.Configure;
        var onTest = mode == WorkshopMode.Test && !fighting;
        var pastDesign = onConfigure || onTest || fighting || hasAdmit;
        var pastConfigure = onTest || fighting || hasAdmit;
        var pastTest = fighting || hasAdmit;

        SetFlow(stepDesign, onDesign, pastDesign && !onDesign);
        SetFlow(stepConfigure, onConfigure, pastConfigure && !onConfigure);
        SetFlow(stepTest, onTest, pastTest && !onTest);
        SetFlow(stepFight, fighting, false);
    }

    static void SetFlow(Label step, bool active, bool done)
    {
        if (step == null)
            return;
        step.RemoveFromClassList("active");
        step.RemoveFromClassList("done");
        if (active)
            step.AddToClassList("active");
        else if (done)
            step.AddToClassList("done");
    }

    public void ShowResults(string body)
    {
        if (resultsBody != null)
            resultsBody.text = body ?? "";
        if (resultsTitle != null)
            resultsTitle.text = "Fight complete";
        SetVisible(resultsOverlay, true);
    }

    public void HideResults()
    {
        SetVisible(resultsOverlay, false);
        app?.ClearPendingResults();
    }

    static void SetActive(Button button, bool on)
    {
        if (button == null)
            return;
        if (on)
            button.AddToClassList("active");
        else
            button.RemoveFromClassList("active");
    }

    static void SetVisible(VisualElement element, bool on)
    {
        if (element == null)
            return;
        if (on)
            element.RemoveFromClassList("hidden");
        else
            element.AddToClassList("hidden");
    }
}
