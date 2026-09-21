using Ra2.Robot;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// S11-08: UI Toolkit workshop shell for the MVP playable player.
/// Presentation-only — drives <see cref="RobotMvpPlayableApp"/> / workshop session.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(UIDocument))]
public sealed class RobotMvpUiShell : MonoBehaviour
{
    [SerializeField] RobotMvpPlayableApp app;
    [SerializeField] UIDocument document;

    Label modeLabel;
    Label statusLabel;
    Label polyInfo;
    Label bindInfo;
    Label resultsBody;
    Label arenaCaption;
    VisualElement panelDesign;
    VisualElement panelConfigure;
    VisualElement panelTest;
    VisualElement resultsOverlay;
    Button btnDesign;
    Button btnConfigure;
    Button btnTest;
    Button btnAdmitTest;
    Button btnFight;
    Button btnReset;

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
    }

    void OnEnable()
    {
        TryBindUi();
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
        arenaCaption = root.Q<Label>("arena-caption");
        panelDesign = root.Q("panel-design");
        panelConfigure = root.Q("panel-configure");
        panelTest = root.Q("panel-test");
        resultsOverlay = root.Q("results-overlay");
        btnDesign = root.Q<Button>("btn-design");
        btnConfigure = root.Q<Button>("btn-configure");
        btnTest = root.Q<Button>("btn-test");
        btnAdmitTest = root.Q<Button>("btn-admit-test");
        btnFight = root.Q<Button>("btn-fight");
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
        Wire(root.Q<Button>("btn-tank"), () => app.TryUiTankPreset());
        Wire(btnReset, () => app.TryUiResetTest());
        Wire(root.Q<Button>("btn-admit"), () => app.TryUiPrepareAdmit());
        Wire(btnAdmitTest, () => app.TryUiTestAdmit());
        Wire(btnFight, () => app.TryUiLocalFight());
        Wire(root.Q<Button>("btn-results-close"), () => HideResults());

        bound = true;
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
        SetActive(btnDesign, mode == WorkshopMode.Design);
        SetActive(btnConfigure, mode == WorkshopMode.Configure);
        SetActive(btnTest, mode == WorkshopMode.Test);
        SetVisible(panelDesign, mode == WorkshopMode.Design);
        SetVisible(panelConfigure, mode == WorkshopMode.Configure);
        SetVisible(panelTest, mode == WorkshopMode.Test);

        if (modeLabel != null)
            modeLabel.text = mode.ToString().ToUpperInvariant();

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

        if (btnAdmitTest != null)
            btnAdmitTest.SetEnabled(session.LastAdmitBlueprint != null);
        if (btnFight != null)
            btnFight.SetEnabled(!app.FightRunning &&
                                (mode == WorkshopMode.Test || session.LastAdmitBlueprint != null || bp != null));
        if (btnReset != null)
            btnReset.SetEnabled(mode == WorkshopMode.Test);

        if (arenaCaption != null)
        {
            arenaCaption.text = mode == WorkshopMode.Test
                ? "TEST ROOM · WASD"
                : mode == WorkshopMode.Configure
                    ? "CONFIGURE · BINDINGS"
                    : "DESIGN · CHASSIS";
        }

        if (!string.IsNullOrEmpty(app.PendingResultsText))
        {
            ShowResults(app.PendingResultsText);
            app.ClearPendingResults();
        }
    }

    public void ShowResults(string body)
    {
        if (resultsBody != null)
            resultsBody.text = body ?? "";
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
