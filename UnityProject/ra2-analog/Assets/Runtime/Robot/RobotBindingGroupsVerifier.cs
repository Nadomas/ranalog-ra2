using System.Collections;
using Ra2.Robot;
using UnityEngine;

/// <summary>
/// S5-02: binding groups UX thin — apply/cycle Drive+Turn, conflict warn, JSON round-trip.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotBindingGroupsVerifier : MonoBehaviour
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
        var bp = RobotBlueprint.CreateRa2ConstructionSampleA(new Vector3(0f, 0.85f, 0f), 0f);
        var chrome = gameObject.AddComponent<RobotBindingGroupsChrome>();
        chrome.Bind(bp);
        yield return null;

        var okDrive = chrome.TrySelect(RobotControlConfigurer.BindingGroupId.Drive);
        var okApply = chrome.TryApplySelected("Up/Down", out var eApply);
        var driveBind = RobotControlConfigurer.GetGroupBinding(bp, RobotControlConfigurer.BindingGroupId.Drive);
        var applyOk = okDrive && okApply && driveBind == "Up/Down";

        chrome.TrySelect(RobotControlConfigurer.BindingGroupId.Turn);
        var okCycle = chrome.TryCycleSelected(out var cycled, out var eCycle);
        var turnBind = RobotControlConfigurer.GetGroupBinding(bp, RobotControlConfigurer.BindingGroupId.Turn);
        var cycleOk = okCycle && !string.IsNullOrEmpty(cycled) && turnBind == cycled;

        // Force overlap conflict.
        RobotControlConfigurer.TryApplyGroupBinding(
            bp, RobotControlConfigurer.BindingGroupId.Drive, "W/S", out _);
        RobotControlConfigurer.TryApplyGroupBinding(
            bp, RobotControlConfigurer.BindingGroupId.Turn, "W/S", out _);
        var conflicts = RobotControlConfigurer.FindBindingGroupConflicts(bp);
        var conflictOk = conflicts.Count >= 1 && conflicts[0].Contains("group_binding_overlap");

        // Clear overlap then JSON round-trip.
        RobotControlConfigurer.TryApplyGroupBinding(
            bp, RobotControlConfigurer.BindingGroupId.Turn, "A/D", out _);
        RobotControlConfigurer.TryApplyGroupBinding(
            bp, RobotControlConfigurer.BindingGroupId.Drive, "S/W", out _);

        var json = RobotBlueprintSerializer.ToJson(bp);
        var loaded = RobotBlueprintSerializer.FromJson(json);
        var bindOk = RobotControlConfigurer.GetGroupBinding(loaded, RobotControlConfigurer.BindingGroupId.Drive) == "S/W" &&
                     RobotControlConfigurer.GetGroupBinding(loaded, RobotControlConfigurer.BindingGroupId.Turn) == "A/D";
        var wireOk = RobotControlConfigurer.FindWiringConflicts(loaded).Count == 0;
        var clearConflicts = RobotControlConfigurer.FindBindingGroupConflicts(loaded).Count == 0;

        var pass = applyOk && cycleOk && conflictOk && bindOk && wireOk && clearConflicts &&
                   !string.IsNullOrEmpty(json);

        Debug.Log(
            $"[S5-02] VERIFIER_DONE pass={pass} apply={applyOk}/{eApply} drive={driveBind} " +
            $"cycle={cycleOk}/{eCycle} cycled={cycled} turn={turnBind} conflict={conflictOk} " +
            $"bind_json={bindOk} wires_ok={wireOk} clear_conflicts={clearConflicts} " +
            $"json_len={json?.Length ?? 0} status={chrome.Status}");

        yield return null;
    }
}
