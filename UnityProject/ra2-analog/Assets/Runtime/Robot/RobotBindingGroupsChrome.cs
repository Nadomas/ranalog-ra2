using Ra2.Robot;
using UnityEngine;

/// <summary>
/// Thin local-only binding-groups IMGUI (S5-02). Not product polish.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotBindingGroupsChrome : MonoBehaviour
{
    RobotBlueprint blueprint;
    string status = "ready";
    RobotControlConfigurer.BindingGroupId selected = RobotControlConfigurer.BindingGroupId.Drive;

    public RobotBlueprint Blueprint => blueprint;
    public string Status => status;
    public RobotControlConfigurer.BindingGroupId Selected => selected;

    public void Bind(RobotBlueprint bp)
    {
        blueprint = bp;
        if (bp != null)
            RobotControlConfigurer.ApplyDrivePreset(bp, RobotControlConfigurer.DrivePreset.TankSteer);
        status = bp == null ? "no_blueprint" : "groups_ready";
    }

    public bool TrySelect(RobotControlConfigurer.BindingGroupId group)
    {
        selected = group;
        status = $"sel={RobotControlConfigurer.DisplayNameForGroup(group)}";
        return true;
    }

    public bool TryCycleSelected(out string applied, out string error)
    {
        applied = null;
        if (blueprint == null)
        {
            error = "no_blueprint";
            status = error;
            return false;
        }

        var ok = RobotControlConfigurer.TryCycleGroupBinding(blueprint, selected, out applied, out error);
        var conflicts = RobotControlConfigurer.FindBindingGroupConflicts(blueprint);
        status = ok
            ? $"cycle {selected}={applied} conflicts={conflicts.Count}"
            : $"cycle_fail={error}";
        return ok;
    }

    public bool TryApplySelected(string binding, out string error)
    {
        if (blueprint == null)
        {
            error = "no_blueprint";
            status = error;
            return false;
        }

        var ok = RobotControlConfigurer.TryApplyGroupBinding(blueprint, selected, binding, out error);
        status = ok ? $"apply {selected}={binding}" : $"apply_fail={error}";
        return ok;
    }

    void OnGUI()
    {
        if (blueprint == null)
            return;

        const float w = 320f;
        var y = 12f;
        GUI.Box(new Rect(12f, y, w, 210f), "Binding groups (thin)");
        y += 28f;

        var drive = RobotControlConfigurer.GetGroupBinding(blueprint, RobotControlConfigurer.BindingGroupId.Drive);
        var turn = RobotControlConfigurer.GetGroupBinding(blueprint, RobotControlConfigurer.BindingGroupId.Turn);
        GUI.Label(new Rect(22f, y, w - 20f, 20f), $"Drive={drive}  Turn={turn}");
        y += 24f;

        if (GUI.Button(new Rect(22f, y, 90f, 28f), "Drive"))
            TrySelect(RobotControlConfigurer.BindingGroupId.Drive);
        if (GUI.Button(new Rect(118f, y, 90f, 28f), "Turn"))
            TrySelect(RobotControlConfigurer.BindingGroupId.Turn);
        if (GUI.Button(new Rect(214f, y, 90f, 28f), "Cycle"))
            TryCycleSelected(out _, out _);
        y += 36f;

        if (GUI.Button(new Rect(22f, y, 140f, 28f), "Overlap warn test"))
        {
            TryApplySelected("W/S", out _);
            TrySelect(RobotControlConfigurer.BindingGroupId.Turn);
            TryApplySelected("W/S", out _);
            var c = RobotControlConfigurer.FindBindingGroupConflicts(blueprint);
            status = $"overlap_test conflicts={c.Count}";
        }

        y += 36f;
        GUI.Label(new Rect(22f, y, w - 20f, 40f), status);
    }
}
