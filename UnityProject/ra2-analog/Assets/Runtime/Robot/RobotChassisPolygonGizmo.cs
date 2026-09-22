using Ra2.Robot;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// S13-02 local-only Design presentation: freehand chassis polygon handles on the floor plane.
/// Mutates blueprint data via <see cref="RobotWorkshopChrome"/> — no dynamic Rigidbody transforms.
/// </summary>
[DisallowMultipleComponent]
public sealed class RobotChassisPolygonGizmo : MonoBehaviour
{
    const float HitRadius = 0.4f;
    const float HandleScale = 0.18f;
    const float DrawY = 0.06f;

    RobotWorkshopChrome chrome;
    LineRenderer outline;
    Transform handlesRoot;
    int dragIndex = -1;
    bool visible;

    public bool IsVisible => visible;
    public int DragIndex => dragIndex;

    public void Bind(RobotWorkshopChrome workshopChrome)
    {
        chrome = workshopChrome;
        EnsureVisuals();
    }

    void LateUpdate()
    {
        if (chrome == null || chrome.Session == null || chrome.Session.Mode != WorkshopMode.Design)
        {
            SetVisible(false);
            dragIndex = -1;
            return;
        }

        SetVisible(true);
        RefreshVisuals();
        HandlePointer();
    }

    void EnsureVisuals()
    {
        if (outline == null)
        {
            var lineGo = new GameObject("ChassisPolyOutline");
            lineGo.transform.SetParent(transform, false);
            outline = lineGo.AddComponent<LineRenderer>();
            outline.loop = true;
            outline.widthMultiplier = 0.05f;
            outline.positionCount = 0;
            outline.useWorldSpace = true;
            outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outline.receiveShadows = false;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            outline.sharedMaterial = new Material(shader) { color = new Color(0.95f, 0.75f, 0.2f, 0.95f) };
            if (outline.sharedMaterial.HasProperty("_BaseColor"))
                outline.sharedMaterial.SetColor("_BaseColor", new Color(0.95f, 0.75f, 0.2f, 0.95f));
        }

        if (handlesRoot == null)
        {
            var root = new GameObject("ChassisPolyHandles");
            root.transform.SetParent(transform, false);
            handlesRoot = root.transform;
        }
    }

    void SetVisible(bool on)
    {
        visible = on;
        if (outline != null)
            outline.enabled = on;
        if (handlesRoot != null)
            handlesRoot.gameObject.SetActive(on);
    }

    void RefreshVisuals()
    {
        var bp = chrome.Session.WorkingBlueprint;
        var pts = RobotChassisPolygonEditor.GetPoints(bp);
        var sel = chrome.PolySelectedIndex;
        EnsureHandleCount(pts.Length);

        outline.positionCount = pts.Length;
        for (var i = 0; i < pts.Length; i++)
        {
            var w = LocalToWorld(bp, pts[i]);
            outline.SetPosition(i, w);
            var handle = handlesRoot.GetChild(i);
            handle.position = w;
            handle.localScale = Vector3.one * (i == sel ? HandleScale * 1.35f : HandleScale);
            var rend = handle.GetComponent<MeshRenderer>();
            if (rend != null && rend.sharedMaterial != null)
            {
                var c = i == sel ? new Color(1f, 0.45f, 0.1f) : new Color(0.2f, 0.75f, 1f);
                if (rend.sharedMaterial.HasProperty("_BaseColor"))
                    rend.sharedMaterial.SetColor("_BaseColor", c);
                else
                    rend.sharedMaterial.color = c;
            }
        }
    }

    void EnsureHandleCount(int count)
    {
        while (handlesRoot.childCount < count)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "PolyHandle_" + handlesRoot.childCount;
            sphere.transform.SetParent(handlesRoot, false);
            Object.Destroy(sphere.GetComponent<Collider>());
            var rend = sphere.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            rend.sharedMaterial = new Material(shader);
        }

        while (handlesRoot.childCount > count)
            Object.Destroy(handlesRoot.GetChild(handlesRoot.childCount - 1).gameObject);
    }

    void HandlePointer()
    {
        var mouse = Mouse.current;
        var cam = Camera.main;
        if (mouse == null || cam == null)
            return;

        if (!TryRayFloor(cam, mouse.position.ReadValue(), out var hit))
            return;

        var bp = chrome.Session.WorkingBlueprint;
        if (bp == null)
            return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            dragIndex = FindNearest(bp, hit, HitRadius);
            if (dragIndex >= 0)
                chrome.TrySelectPolyIndex(dragIndex);
        }

        if (dragIndex >= 0 && mouse.leftButton.isPressed)
        {
            var local = WorldToLocal(bp, hit);
            chrome.TryDesignSetPoint(dragIndex, local, out _);
        }

        if (mouse.leftButton.wasReleasedThisFrame)
            dragIndex = -1;
    }

    static bool TryRayFloor(Camera cam, Vector2 screen, out Vector3 hit)
    {
        hit = default;
        var ray = cam.ScreenPointToRay(screen);
        var plane = new Plane(Vector3.up, new Vector3(0f, DrawY, 0f));
        if (!plane.Raycast(ray, out var dist))
            return false;
        hit = ray.GetPoint(dist);
        return true;
    }

    static int FindNearest(RobotBlueprint bp, Vector3 world, float radius)
    {
        var pts = RobotChassisPolygonEditor.GetPoints(bp);
        var best = -1;
        var bestDist = radius;
        for (var i = 0; i < pts.Length; i++)
        {
            var d = Vector3.Distance(LocalToWorld(bp, pts[i]), world);
            if (d > bestDist)
                continue;
            bestDist = d;
            best = i;
        }

        return best;
    }

    static Vector3 LocalToWorld(RobotBlueprint bp, Vector2 local)
    {
        var root = bp.RootPosition;
        var yaw = Quaternion.Euler(0f, bp.RootYawDegrees, 0f);
        var p = root + yaw * new Vector3(local.x, 0f, local.y);
        p.y = DrawY;
        return p;
    }

    static Vector2 WorldToLocal(RobotBlueprint bp, Vector3 world)
    {
        var root = bp.RootPosition;
        var inv = Quaternion.Inverse(Quaternion.Euler(0f, bp.RootYawDegrees, 0f));
        var local3 = inv * (new Vector3(world.x, root.y, world.z) - root);
        return new Vector2(local3.x, local3.z);
    }
}
