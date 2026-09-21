using Ra2.Robot;
using UnityEngine;

/// <summary>
/// Thin local-only chassis polygon IMGUI (S4-02). Not product polish.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotChassisPolygonChrome : MonoBehaviour
{
    RobotBlueprint blueprint;
    string status = "ready";
    int selectedIndex;

    public RobotBlueprint Blueprint => blueprint;
    public string Status => status;
    public int SelectedIndex => selectedIndex;
    public int PointCount => RobotChassisPolygonEditor.PointCount(blueprint);

    public void Bind(RobotBlueprint bp)
    {
        blueprint = bp;
        selectedIndex = 0;
        status = bp == null ? "no_blueprint" : $"pts={PointCount}";
    }

    public bool TryNudgeSelected(Vector2 delta, out string error)
    {
        if (blueprint == null)
        {
            error = "no_blueprint";
            status = error;
            return false;
        }

        var ok = RobotChassisPolygonEditor.TryNudgePoint(blueprint, selectedIndex, delta, out error);
        status = ok ? $"nudged i={selectedIndex} pts={PointCount}" : $"nudge_fail={error}";
        return ok;
    }

    public bool TryAddCorner(out string error)
    {
        if (blueprint == null)
        {
            error = "no_blueprint";
            status = error;
            return false;
        }

        var pts = RobotChassisPolygonEditor.GetPoints(blueprint);
        var tip = pts.Length > 0 ? pts[pts.Length - 1] + new Vector2(0.15f, 0.15f) : Vector2.zero;
        var ok = RobotChassisPolygonEditor.TryAddPoint(blueprint, tip, out error);
        if (ok)
            selectedIndex = PointCount - 1;
        status = ok ? $"added pts={PointCount}" : $"add_fail={error}";
        return ok;
    }

    public bool TryRemoveSelected(out string error)
    {
        if (blueprint == null)
        {
            error = "no_blueprint";
            status = error;
            return false;
        }

        var ok = RobotChassisPolygonEditor.TryRemovePoint(blueprint, selectedIndex, out error);
        if (ok && selectedIndex >= PointCount)
            selectedIndex = Mathf.Max(0, PointCount - 1);
        status = ok ? $"removed pts={PointCount}" : $"remove_fail={error}";
        return ok;
    }

    void OnGUI()
    {
        if (blueprint == null)
            return;

        const float w = 300f;
        var y = 12f;
        GUI.Box(new Rect(12f, y, w, 200f), "Chassis polygon (thin)");
        y += 28f;
        GUI.Label(new Rect(22f, y, w - 20f, 20f),
            $"pts={PointCount}/{RobotChassisPolygonEditor.MaxPoints}  sel={selectedIndex}");
        y += 24f;

        if (GUI.Button(new Rect(22f, y, 70f, 28f), "Prev"))
            selectedIndex = PointCount == 0 ? 0 : (selectedIndex - 1 + PointCount) % PointCount;
        if (GUI.Button(new Rect(98f, y, 70f, 28f), "Next"))
            selectedIndex = PointCount == 0 ? 0 : (selectedIndex + 1) % PointCount;
        if (GUI.Button(new Rect(174f, y, 70f, 28f), "Nudge+X"))
            TryNudgeSelected(new Vector2(0.1f, 0f), out _);
        y += 36f;

        if (GUI.Button(new Rect(22f, y, 100f, 28f), "Add corner"))
            TryAddCorner(out _);
        if (GUI.Button(new Rect(130f, y, 100f, 28f), "Remove"))
            TryRemoveSelected(out _);
        y += 36f;

        GUI.Label(new Rect(22f, y, w - 20f, 40f), status);
    }
}
