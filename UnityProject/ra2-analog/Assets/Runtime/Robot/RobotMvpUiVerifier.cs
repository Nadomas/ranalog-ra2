using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// S11-08: UI Toolkit shell present + mode/nudge/cycle APIs respond.
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
        var rootOk = doc != null && doc.rootVisualElement != null &&
                     doc.rootVisualElement.Q("side-panel") != null;

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
        var inst = app?.Chrome?.Session?.TestInstance != null;
        var imguiSuppressed = app?.Chrome != null && app.Chrome.SuppressImgui;

        var pass = hasUi && rootOk && modeOk && cfgOk && testOk && inst && imguiSuppressed;
        Debug.Log(
            $"[S11-08] VERIFIER_DONE pass={pass} ui={hasUi} root={rootOk} design={modeOk} " +
            $"cfg={cfgOk} test={testOk} inst={inst} imgui_off={imguiSuppressed}");
    }
}
