using System.Collections;
using UnityEngine;

/// <summary>
/// S11-07 Editor Play smoke: playable bootstrap Ready + workshop Test spawn.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotMvpPlayableVerifier : MonoBehaviour
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
        var app = FindFirstObjectByType<RobotMvpPlayableApp>();
        if (app == null)
        {
            var host = new GameObject("MvpPlayableHost");
            app = host.AddComponent<RobotMvpPlayableApp>();
        }

        yield return null;
        yield return null;

        var ready = app.Ready;
        var chrome = app.Chrome;
        chrome?.EnsureSession();
        var okDesign = chrome != null && chrome.TrySetMode(Ra2.Robot.WorkshopMode.Design, out _);
        yield return null;
        var okTest = chrome != null && chrome.TrySetMode(Ra2.Robot.WorkshopMode.Test, out _);
        yield return new WaitForFixedUpdate();
        var hasInst = chrome?.Session?.TestInstance != null;

        var pass = ready && okDesign && okTest && hasInst;
        Debug.Log(
            $"[S11-07] VERIFIER_DONE pass={pass} ready={ready} design={okDesign} test={okTest} inst={hasInst}");
    }
}
