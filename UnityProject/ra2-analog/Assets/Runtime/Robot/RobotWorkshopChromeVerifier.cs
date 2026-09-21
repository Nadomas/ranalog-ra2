using System.Collections;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// Thin Construction/Configure chrome spike: Design→Configure(preset)→Test→PrepareAdmit.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotWorkshopChromeVerifier : MonoBehaviour
{
    [SerializeField] bool autoRun = true;
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
        var chrome = gameObject.AddComponent<RobotWorkshopChrome>();
        chrome.Configure(slideMaterial);
        chrome.EnsureSession();
        yield return null;

        var okDesign = chrome.TrySetMode(WorkshopMode.Design, out var e0);
        yield return null;
        var okCfg = chrome.TrySetMode(WorkshopMode.Configure, out var e1);
        yield return null;
        var okPreset = chrome.TryApplyTankPreset(out var e2);
        yield return null;
        var okTest = chrome.TrySetMode(WorkshopMode.Test, out var e3);
        yield return new WaitForFixedUpdate();
        var hasInstance = chrome.Session.TestInstance != null;
        var okAdmit = chrome.TryPrepareAdmit(out var e4);
        yield return null;

        var pass = okDesign && okCfg && okPreset && okTest && hasInstance && okAdmit &&
                   chrome.Session.SwitchCount >= 2 &&
                   chrome.LastJsonLen != "-";

        Debug.Log(
            $"[S11-CHROME] VERIFIER_DONE pass={pass} design={okDesign}/{e0} cfg={okCfg}/{e1} " +
            $"preset={okPreset}/{e2} test={okTest}/{e3} inst={hasInstance} admit={okAdmit}/{e4} " +
            $"switches={chrome.Session?.SwitchCount} json_len={chrome.LastJsonLen} status={chrome.Status}");
    }
}
