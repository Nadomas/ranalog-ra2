using Ra2.Robot;
using UnityEngine;

/// <summary>
/// Thin local-only Construction/Configure workshop chrome (IMGUI). Not product polish.
/// Drives <see cref="RobotWorkshopSession"/> — same blueprint path as combat admit.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotWorkshopChrome : MonoBehaviour
{
    [SerializeField] PhysicsMaterial slideMaterial;
    [SerializeField] Color bodyColor = new Color(0.55f, 0.72f, 0.9f);

    RobotWorkshopSession session;
    string status = "ready";
    string lastJsonLen = "-";

    public RobotWorkshopSession Session => session;
    public string Status => status;
    public string LastJsonLen => lastJsonLen;

    public void Configure(PhysicsMaterial slide)
    {
        slideMaterial = slide;
    }

    public void EnsureSession()
    {
        if (session != null)
            return;

        session = new RobotWorkshopSession();
        var bp = RobotBlueprint.CreateRa2ConstructionSampleA(new Vector3(0f, 0.85f, 0f), 0f);
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

    void Start() => EnsureSession();

    void OnGUI()
    {
        EnsureSession();
        const float w = 280f;
        var y = 12f;
        GUI.Box(new Rect(12f, y, w, 250f), "Workshop (thin)");
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

        GUI.Label(new Rect(22f, y, w - 20f, 40f), status);
    }
}
