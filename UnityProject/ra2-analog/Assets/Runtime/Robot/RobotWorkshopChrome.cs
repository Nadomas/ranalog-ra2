using Ra2.Robot;
using UnityEngine;

/// <summary>
/// Thin local-only Construction/Configure workshop chrome (IMGUI).
/// S11-06: Design embeds chassis polygon edits; Configure embeds Drive/Turn binding cycle.
/// Same blueprint path as combat admit.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotWorkshopChrome : MonoBehaviour
{
    [SerializeField] PhysicsMaterial slideMaterial;
    [SerializeField] Color bodyColor = new Color(0.55f, 0.72f, 0.9f);

    RobotWorkshopSession session;
    string status = "ready";
    string lastJsonLen = "-";
    int polySelectedIndex;
    RobotControlConfigurer.BindingGroupId bindGroup = RobotControlConfigurer.BindingGroupId.Drive;

    public RobotWorkshopSession Session => session;
    public string Status => status;
    public string LastJsonLen => lastJsonLen;
    public int PolySelectedIndex => polySelectedIndex;
    public RobotControlConfigurer.BindingGroupId BindGroup => bindGroup;

    /// <summary>When true, hide IMGUI (S11-08 UI Toolkit owns chrome).</summary>
    public bool SuppressImgui { get; set; }

    public void Configure(PhysicsMaterial slide)
    {
        slideMaterial = slide;
    }

    public void EnsureSession()
    {
        if (session != null)
            return;

        session = new RobotWorkshopSession();
        var bp = RobotBlueprint.CreateRa2ConstructionSampleA(new Vector3(0f, 0.55f, 0f), 0f);
        RobotControlConfigurer.ApplyDrivePreset(bp, RobotControlConfigurer.DrivePreset.TankSteer);
        session.SetWorkingBlueprint(bp);
        status = "session_ready";
    }

    public bool TrySetMode(WorkshopMode mode, out string error)
    {
        EnsureSession();
        if (slideMaterial == null)
            slideMaterial = Resources.Load<PhysicsMaterial>("PhysicsTestSlide");

        var ok = session.TrySwitchMode(mode, transform, slideMaterial, bodyColor, out error);
        status = ok ? $"mode={mode}" : $"mode_fail={error}";
        return ok;
    }

    public bool TryApplyTankPreset(out string error)
    {
        EnsureSession();
        error = null;
        if (session.WorkingBlueprint == null)
        {
            error = "no_blueprint";
            status = error;
            return false;
        }

        RobotControlConfigurer.ApplyDrivePreset(
            session.WorkingBlueprint, RobotControlConfigurer.DrivePreset.TankSteer);
        status = "preset=TankSteer";
        return true;
    }

    public bool TryPrepareAdmit(out string error)
    {
        EnsureSession();
        var ok = session.TryPrepareCombatAdmit(out _, out var json, out error);
        lastJsonLen = ok && json != null ? json.Length.ToString() : "-";
        status = ok ? $"admit_ready json_len={lastJsonLen}" : $"admit_fail={error}";
        return ok;
    }

    /// <summary>S6-04: spawn Test from last Prepare Admit JSON clone.</summary>
    public bool TryTestAdmitClone(out string error)
    {
        EnsureSession();
        if (slideMaterial == null)
            slideMaterial = Resources.Load<PhysicsMaterial>("PhysicsTestSlide");

        var ok = session.TryEnterTestFromAdmit(transform, slideMaterial, bodyColor, out error);
        status = ok
            ? $"admit_test ms={session.LastAdmitTestMs:F1}"
            : $"admit_test_fail={error}";
        return ok;
    }

    /// <summary>S6-02: despawn+respawn Test instance from the same working blueprint (local-only chrome).</summary>
    public bool TryResetTest(out string error)
    {
        EnsureSession();
        if (slideMaterial == null)
            slideMaterial = Resources.Load<PhysicsMaterial>("PhysicsTestSlide");

        var ok = session.TryResetTest(transform, slideMaterial, bodyColor, out error);
        status = ok
            ? $"reset ms={session.LastResetMs:F1}"
            : $"reset_fail={error}";
        return ok;
    }

    /// <summary>S11-06 Design: nudge selected chassis polygon point (local-only).</summary>
    public bool TryDesignNudge(Vector2 delta, out string error)
    {
        EnsureSession();
        if (session.Mode != WorkshopMode.Design)
        {
            error = "not_in_design";
            status = error;
            return false;
        }

        var bp = session.WorkingBlueprint;
        if (bp == null)
        {
            error = "no_blueprint";
            status = error;
            return false;
        }

        var pts = RobotChassisPolygonEditor.PointCount(bp);
        if (pts > 0)
            polySelectedIndex = ((polySelectedIndex % pts) + pts) % pts;

        var ok = RobotChassisPolygonEditor.TryNudgePoint(bp, polySelectedIndex, delta, out error);
        status = ok
            ? $"design_nudge i={polySelectedIndex} pts={RobotChassisPolygonEditor.PointCount(bp)}"
            : $"nudge_fail={error}";
        return ok;
    }

    /// <summary>S11-06 Configure: cycle selected Drive/Turn binding group.</summary>
    public bool TryConfigureCycleBinding(out string applied, out string error)
    {
        applied = null;
        EnsureSession();
        if (session.Mode != WorkshopMode.Configure)
        {
            error = "not_in_configure";
            status = error;
            return false;
        }

        var bp = session.WorkingBlueprint;
        if (bp == null)
        {
            error = "no_blueprint";
            status = error;
            return false;
        }

        var ok = RobotControlConfigurer.TryCycleGroupBinding(bp, bindGroup, out applied, out error);
        var conflicts = RobotControlConfigurer.FindBindingGroupConflicts(bp);
        status = ok
            ? $"cfg_cycle {bindGroup}={applied} conflicts={conflicts.Count}"
            : $"cycle_fail={error}";
        return ok;
    }

    public void SelectBindGroup(RobotControlConfigurer.BindingGroupId group)
    {
        bindGroup = group;
        status = $"sel={RobotControlConfigurer.DisplayNameForGroup(group)}";
    }

    public bool TryFlipWireSign(int index, out string error)
    {
        EnsureSession();
        var ok = RobotControlConfigurer.TryFlipWireSign(session.WorkingBlueprint, index, out error);
        status = ok ? $"wire_flip={index}" : $"wire_flip_fail={error}";
        return ok;
    }

    public bool TryCycleWireChannel(int index, out string error)
    {
        EnsureSession();
        var ok = RobotControlConfigurer.TryCycleWireChannel(session.WorkingBlueprint, index, out error);
        status = ok ? $"wire_ch={index}" : $"wire_ch_fail={error}";
        return ok;
    }

    public bool TryCycleSlotKind(int index, out string error)
    {
        EnsureSession();
        var ok = RobotControlConfigurer.TryCycleSlotKind(session.WorkingBlueprint, index, out error);
        status = ok ? $"slot_kind={index}" : $"slot_kind_fail={error}";
        return ok;
    }

    public bool TryCycleSlotBinding(int index, out string error)
    {
        EnsureSession();
        var ok = RobotControlConfigurer.TryCycleSlotBinding(session.WorkingBlueprint, index, out error);
        status = ok ? $"slot_bind={index}" : $"slot_bind_fail={error}";
        return ok;
    }

    public void StepPolySelection(int delta)
    {
        EnsureSession();
        var pts = RobotChassisPolygonEditor.PointCount(session?.WorkingBlueprint);
        if (pts <= 0)
        {
            polySelectedIndex = 0;
            return;
        }

        polySelectedIndex = ((polySelectedIndex + delta) % pts + pts) % pts;
        status = $"sel={polySelectedIndex} pts={pts}";
    }

    void Start() => EnsureSession();

    void OnGUI()
    {
        if (SuppressImgui)
            return;
        EnsureSession();
        const float w = 300f;
        var boxH = session.Mode == WorkshopMode.Design || session.Mode == WorkshopMode.Configure ? 420f : 300f;
        var y = 12f;
        GUI.Box(new Rect(12f, y, w, boxH), "Workshop (thin)");
        y += 28f;
        GUI.Label(new Rect(22f, y, w - 20f, 20f), $"Mode: {session.Mode}  switches={session.SwitchCount}");
        y += 24f;

        if (GUI.Button(new Rect(22f, y, 80f, 28f), "Design"))
            TrySetMode(WorkshopMode.Design, out _);
        if (GUI.Button(new Rect(108f, y, 80f, 28f), "Configure"))
            TrySetMode(WorkshopMode.Configure, out _);
        if (GUI.Button(new Rect(194f, y, 80f, 28f), "Test"))
            TrySetMode(WorkshopMode.Test, out _);
        y += 36f;

        if (session.Mode == WorkshopMode.Design && session.WorkingBlueprint != null)
        {
            var pts = RobotChassisPolygonEditor.PointCount(session.WorkingBlueprint);
            GUI.Label(new Rect(22f, y, w - 20f, 20f),
                $"Polygon pts={pts}/{RobotChassisPolygonEditor.MaxPoints} sel={polySelectedIndex}");
            y += 24f;
            if (GUI.Button(new Rect(22f, y, 70f, 28f), "Prev") && pts > 0)
                polySelectedIndex = (polySelectedIndex - 1 + pts) % pts;
            if (GUI.Button(new Rect(98f, y, 70f, 28f), "Next") && pts > 0)
                polySelectedIndex = (polySelectedIndex + 1) % pts;
            if (GUI.Button(new Rect(174f, y, 90f, 28f), "Nudge+X"))
                TryDesignNudge(new Vector2(0.1f, 0f), out _);
            y += 36f;
        }
        else if (session.Mode == WorkshopMode.Configure && session.WorkingBlueprint != null)
        {
            var drive = RobotControlConfigurer.GetGroupBinding(
                session.WorkingBlueprint, RobotControlConfigurer.BindingGroupId.Drive);
            var turn = RobotControlConfigurer.GetGroupBinding(
                session.WorkingBlueprint, RobotControlConfigurer.BindingGroupId.Turn);
            GUI.Label(new Rect(22f, y, w - 20f, 20f), $"Drive={drive}  Turn={turn}");
            y += 24f;
            if (GUI.Button(new Rect(22f, y, 70f, 28f), "Drive"))
                SelectBindGroup(RobotControlConfigurer.BindingGroupId.Drive);
            if (GUI.Button(new Rect(98f, y, 70f, 28f), "Turn"))
                SelectBindGroup(RobotControlConfigurer.BindingGroupId.Turn);
            if (GUI.Button(new Rect(174f, y, 90f, 28f), "Cycle"))
                TryConfigureCycleBinding(out _, out _);
            y += 36f;
        }

        if (GUI.Button(new Rect(22f, y, 160f, 28f), "Apply TankSteer"))
            TryApplyTankPreset(out _);
        y += 36f;

        GUI.enabled = session.Mode == WorkshopMode.Test;
        if (GUI.Button(new Rect(22f, y, 160f, 28f), "Reset Test"))
            TryResetTest(out _);
        GUI.enabled = true;
        y += 36f;

        if (GUI.Button(new Rect(22f, y, 160f, 28f), "Prepare Admit"))
            TryPrepareAdmit(out _);
        y += 36f;

        GUI.enabled = session.LastAdmitBlueprint != null;
        if (GUI.Button(new Rect(22f, y, 160f, 28f), "Test Admit Clone"))
            TryTestAdmitClone(out _);
        GUI.enabled = true;
        y += 36f;

        GUI.Label(new Rect(22f, y, w - 20f, 40f), status);
    }
}
