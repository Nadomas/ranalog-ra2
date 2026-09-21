using System.Collections;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S6-04: Prepare Admit → Test Admit Clone (reject without admit; spawn combat JSON clone in Test).
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotWorkshopAdmitTestVerifier : MonoBehaviour
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

        var rejectEarly = !chrome.TryTestAdmitClone(out var earlyErr) && earlyErr == "no_admit";

        var okDesign = chrome.TrySetMode(WorkshopMode.Design, out _);
        yield return null;
        var okCfg = chrome.TrySetMode(WorkshopMode.Configure, out _);
        yield return null;
        var okTest = chrome.TrySetMode(WorkshopMode.Test, out _);
        yield return new WaitForFixedUpdate();
        var testBefore = chrome.Session.TestInstance;
        var okAdmit = chrome.TryPrepareAdmit(out var admitErr);
        yield return null;
        var leftTest = chrome.Session.Mode != WorkshopMode.Test && chrome.Session.TestInstance == null;

        var okAdmitTest = chrome.TryTestAdmitClone(out var admitTestErr);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        var inst = chrome.Session.TestInstance;
        var modeTest = chrome.Session.Mode == WorkshopMode.Test;
        var newInst = inst != null && !ReferenceEquals(inst, testBefore);
        var drove = false;
        if (inst?.Drive != null)
        {
            var start = inst.Drive.transform.position;
            var end = Time.time + 0.35f;
            while (Time.time < end)
            {
                inst.Drive.SetCommand(new PhysicsTestDriveCommand { Move = 1f });
                yield return new WaitForFixedUpdate();
            }

            inst.Drive.SetCommand(new PhysicsTestDriveCommand { Brake = true });
            drove = Vector3.Distance(start, inst.Drive.transform.position) > 0.04f;
        }

        var pass = rejectEarly && okDesign && okCfg && okTest && okAdmit && leftTest &&
                   okAdmitTest && modeTest && newInst && drove;

        Debug.Log(
            $"[S6-04] VERIFIER_DONE pass={pass} reject_early={rejectEarly}/{earlyErr} " +
            $"design={okDesign} cfg={okCfg} test={okTest} admit={okAdmit}/{admitErr} left_test={leftTest} " +
            $"admit_test={okAdmitTest}/{admitTestErr} mode_test={modeTest} new_inst={newInst} drove={drove} " +
            $"ms={chrome.Session?.LastAdmitTestMs:F1} status={chrome.Status}");
    }
}
