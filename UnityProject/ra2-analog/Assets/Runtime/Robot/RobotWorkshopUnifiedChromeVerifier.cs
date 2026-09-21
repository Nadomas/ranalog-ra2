using System.Collections;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S11-06: one workshop chrome — Design polygon nudge → Configure binding cycle → Test → Admit.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotWorkshopUnifiedChromeVerifier : MonoBehaviour
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

        // Default session mode is Design — leave it so Design-gated nudge can be rejected.
        var okLeaveDesign = chrome.TrySetMode(WorkshopMode.Configure, out _);
        yield return null;
        var rejectNudge = !chrome.TryDesignNudge(new Vector2(0.1f, 0f), out var nudgeEarly) &&
                          nudgeEarly == "not_in_design";

        var okDesign = chrome.TrySetMode(WorkshopMode.Design, out _);
        yield return null;

        var bp = chrome.Session.WorkingBlueprint;
        var before = bp != null && bp.Chassis.BaseplatePoints != null && bp.Chassis.BaseplatePoints.Length > 0
            ? bp.Chassis.BaseplatePoints[0]
            : Vector2.zero;
        var okNudge = chrome.TryDesignNudge(new Vector2(0.12f, 0f), out var nudgeErr);
        yield return null;
        var after = bp != null && bp.Chassis.BaseplatePoints != null && bp.Chassis.BaseplatePoints.Length > 0
            ? bp.Chassis.BaseplatePoints[0]
            : Vector2.zero;
        var nudged = okNudge && Vector2.Distance(before, after) > 0.05f;

        var rejectCycle = !chrome.TryConfigureCycleBinding(out _, out var cycleEarly) &&
                          cycleEarly == "not_in_configure";

        var okCfg = chrome.TrySetMode(WorkshopMode.Configure, out _);
        yield return null;
        chrome.SelectBindGroup(RobotControlConfigurer.BindingGroupId.Turn);
        var turnBefore = RobotControlConfigurer.GetGroupBinding(bp, RobotControlConfigurer.BindingGroupId.Turn);
        var okCycle = chrome.TryConfigureCycleBinding(out var applied, out var cycleErr);
        yield return null;
        var turnAfter = RobotControlConfigurer.GetGroupBinding(bp, RobotControlConfigurer.BindingGroupId.Turn);
        var cycled = okCycle && !string.IsNullOrEmpty(applied) &&
                     !string.Equals(turnBefore, turnAfter, System.StringComparison.Ordinal);

        var okTest = chrome.TrySetMode(WorkshopMode.Test, out _);
        yield return new WaitForFixedUpdate();
        var hasInst = chrome.Session.TestInstance != null;
        var okAdmit = chrome.TryPrepareAdmit(out var admitErr);
        yield return null;
        var okAdmitTest = chrome.TryTestAdmitClone(out var admitTestErr);
        yield return new WaitForFixedUpdate();

        var pass = okLeaveDesign && rejectNudge && okDesign && nudged && rejectCycle && okCfg && cycled &&
                   okTest && hasInst && okAdmit && okAdmitTest &&
                   chrome.Session.Mode == WorkshopMode.Test;

        Debug.Log(
            $"[S11-06] VERIFIER_DONE pass={pass} leave_design={okLeaveDesign} reject_nudge={rejectNudge} design={okDesign} nudged={nudged}/{nudgeErr} " +
            $"reject_cycle={rejectCycle} cfg={okCfg} cycled={cycled}/{applied}/{cycleErr} " +
            $"test={okTest} inst={hasInst} admit={okAdmit}/{admitErr} admit_test={okAdmitTest}/{admitTestErr} " +
            $"status={chrome.Status}");
    }
}
