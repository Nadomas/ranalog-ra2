using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// S11-08/09: UI Toolkit shell present + polished chrome + mode/nudge/cycle APIs respond.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotMvpUiVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;

    public bool AutoRun
    {
        get => autoRun;
        set => autoRun = value;
    }

    void Start()
    {
        if (!autoRun)
            return;
        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        yield return null;
        yield return null;

        var app = FindFirstObjectByType<RobotMvpPlayableApp>();
        var shell = FindFirstObjectByType<RobotMvpUiShell>();
        var doc = FindFirstObjectByType<UIDocument>();
        var hasUi = shell != null && doc != null && doc.visualTreeAsset != null;
        var root = doc != null ? doc.rootVisualElement : null;
        var panelOk = doc != null && doc.panelSettings != null;
        var rootOk = root != null && root.Q("side-panel") != null &&
                     root.Q("flow") != null && root.Q("admit-badge") != null &&
                     root.Q("bottom-bar") != null;

        var modeOk = app != null && app.TryUiSetMode(Ra2.Robot.WorkshopMode.Design);
        yield return null;
        if (app != null)
            app.TryUiPolyNudge();
        yield return null;
        var cfgOk = app != null && app.TryUiSetMode(Ra2.Robot.WorkshopMode.Configure);
        yield return null;
        if (app != null)
            app.TryUiCycleBind();
        yield return null;
        var testOk = app != null && app.TryUiSetMode(Ra2.Robot.WorkshopMode.Test);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return null;
        var inst = app?.Chrome?.Session?.TestInstance != null;
        if (!inst && app != null)
        {
            // One more mode nudge — Test spawn can lag one frame behind UI Toolkit enable flip.
            app.TryUiSetMode(Ra2.Robot.WorkshopMode.Design);
            yield return null;
            app.TryUiSetMode(Ra2.Robot.WorkshopMode.Test);
            yield return new WaitForFixedUpdate();
            yield return null;
            inst = app?.Chrome?.Session?.TestInstance != null;
        }
        var imguiSuppressed = app?.Chrome != null && app.Chrome.SuppressImgui;

        var pass = hasUi && rootOk && panelOk && modeOk && cfgOk && testOk && inst && imguiSuppressed;
        Debug.Log(
            $"[S11-09] VERIFIER_DONE pass={pass} ui={hasUi} root={rootOk} panel={panelOk} design={modeOk} " +
            $"cfg={cfgOk} test={testOk} inst={inst} imgui_off={imguiSuppressed}");
        Debug.Log(
            $"[S11-08] VERIFIER_DONE pass={pass} ui={hasUi} root={rootOk} design={modeOk} " +
            $"cfg={cfgOk} test={testOk} inst={inst} imgui_off={imguiSuppressed}");
    }
}
