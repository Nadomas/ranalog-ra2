using UnityEngine;

/// <summary>
/// S11-16 local-only presentation: procedural arena dressing (no physics writes on dynamic bots).
/// Keeps colliders separate — visuals are kinematic/decoration only.
/// </summary>
public static class RobotMvpArenaDressing
{
    public const string RootName = "ArenaDressing";

    public static bool Ensure()
    {
        if (GameObject.Find(RootName) != null)
            return true;

        // Replace thin S11-11 ring if present as a loose root.
        var legacy = GameObject.Find("ArenaRing");
        if (legacy != null && legacy.transform.parent == null)
            Object.Destroy(legacy);

        var root = new GameObject(RootName);

        // Outer apron — reads as a pit sunk in a dark hall.
        var apron = Prim(PrimitiveType.Cylinder, root.transform, "Apron",
            new Vector3(0f, -0.04f, 0f), new Vector3(34f, 0.02f, 34f));
        Tint(apron, new Color(0.08f, 0.09f, 0.11f));

        var ring = Prim(PrimitiveType.Cylinder, root.transform, "ArenaRing",
            new Vector3(0f, 0.015f, 0f), new Vector3(22f, 0.025f, 22f));
        Tint(ring, new Color(0.42f, 0.32f, 0.12f));

        var pad = Prim(PrimitiveType.Cylinder, root.transform, "ArenaPad",
            new Vector3(0f, 0.03f, 0f), new Vector3(12f, 0.02f, 12f));
        Tint(pad, new Color(0.2f, 0.23f, 0.28f));

        // Center lane marks (spawn axis).
        var laneA = Prim(PrimitiveType.Cube, root.transform, "LaneX",
            new Vector3(0f, 0.04f, 0f), new Vector3(10f, 0.01f, 0.18f));
        Tint(laneA, new Color(0.85f, 0.55f, 0.12f));
        var laneB = Prim(PrimitiveType.Cube, root.transform, "LaneZ",
            new Vector3(0f, 0.04f, 0f), new Vector3(0.18f, 0.01f, 10f));
        Tint(laneB, new Color(0.85f, 0.55f, 0.12f));

        // Corner hazard posts.
        PlacePost(root.transform, "PostNE", new Vector3(9.5f, 1.1f, 9.5f));
        PlacePost(root.transform, "PostNW", new Vector3(-9.5f, 1.1f, 9.5f));
        PlacePost(root.transform, "PostSE", new Vector3(9.5f, 1.1f, -9.5f));
        PlacePost(root.transform, "PostSW", new Vector3(-9.5f, 1.1f, -9.5f));

        // Visible fence shells (physics walls stay in ArenaBounds).
        PlaceFence(root.transform, "FenceN", new Vector3(0f, 0.9f, 11.2f), new Vector3(22.5f, 1.6f, 0.18f));
        PlaceFence(root.transform, "FenceS", new Vector3(0f, 0.9f, -11.2f), new Vector3(22.5f, 1.6f, 0.18f));
        PlaceFence(root.transform, "FenceE", new Vector3(11.2f, 0.9f, 0f), new Vector3(0.18f, 1.6f, 22.5f));
        PlaceFence(root.transform, "FenceW", new Vector3(-11.2f, 0.9f, 0f), new Vector3(0.18f, 1.6f, 22.5f));

        // Thin hazard stripe along fence tops.
        PlaceStripe(root.transform, "StripeN", new Vector3(0f, 1.75f, 11.2f), new Vector3(22.5f, 0.12f, 0.22f));
        PlaceStripe(root.transform, "StripeS", new Vector3(0f, 1.75f, -11.2f), new Vector3(22.5f, 0.12f, 0.22f));
        PlaceStripe(root.transform, "StripeE", new Vector3(11.2f, 1.75f, 0f), new Vector3(0.22f, 0.12f, 22.5f));
        PlaceStripe(root.transform, "StripeW", new Vector3(-11.2f, 1.75f, 0f), new Vector3(0.22f, 0.12f, 22.5f));

        // Stand silhouettes (dark blocks outside the pit).
        PlaceStand(root.transform, "StandN", new Vector3(0f, 1.4f, 14.5f), new Vector3(20f, 2.4f, 1.2f));
        PlaceStand(root.transform, "StandS", new Vector3(0f, 1.4f, -14.5f), new Vector3(20f, 2.4f, 1.2f));
        PlaceStand(root.transform, "StandE", new Vector3(14.5f, 1.4f, 0f), new Vector3(1.2f, 2.4f, 16f));
        PlaceStand(root.transform, "StandW", new Vector3(-14.5f, 1.4f, 0f), new Vector3(1.2f, 2.4f, 16f));

        // Warm key + cool fill — arena reads as lit hall, not flat ambient.
        var key = new GameObject("ArenaKeyLight");
        key.transform.SetParent(root.transform, false);
        key.transform.position = new Vector3(-4f, 10f, -6f);
        var keyL = key.AddComponent<Light>();
        keyL.type = LightType.Point;
        keyL.range = 28f;
        keyL.intensity = 2.4f;
        keyL.color = new Color(1f, 0.92f, 0.78f);

        var fill = new GameObject("ArenaFillLight");
        fill.transform.SetParent(root.transform, false);
        fill.transform.position = new Vector3(5f, 8f, 5f);
        var fillL = fill.AddComponent<Light>();
        fillL.type = LightType.Point;
        fillL.range = 24f;
        fillL.intensity = 1.4f;
        fillL.color = new Color(0.55f, 0.7f, 1f);

        // Retint existing floor if present (scene or prior EnsureWorld).
        var floor = GameObject.Find("Floor");
        if (floor != null)
            Tint(floor, new Color(0.16f, 0.18f, 0.22f));

        Debug.Log($"[S11-16] ARENA_DRESSING ok kids={root.transform.childCount}");
        return true;
    }

    static void PlacePost(Transform parent, string name, Vector3 pos)
    {
        var post = Prim(PrimitiveType.Cylinder, parent, name, pos, new Vector3(0.55f, 1.1f, 0.55f));
        Tint(post, new Color(0.75f, 0.22f, 0.12f));
        var cap = Prim(PrimitiveType.Cube, parent, name + "Cap",
            pos + Vector3.up * 1.15f, new Vector3(0.7f, 0.15f, 0.7f));
        Tint(cap, new Color(0.95f, 0.75f, 0.15f));
    }

    static void PlaceFence(Transform parent, string name, Vector3 pos, Vector3 scale)
    {
        var fence = Prim(PrimitiveType.Cube, parent, name, pos, scale);
        Tint(fence, new Color(0.28f, 0.3f, 0.34f));
    }

    static void PlaceStripe(Transform parent, string name, Vector3 pos, Vector3 scale)
    {
        var stripe = Prim(PrimitiveType.Cube, parent, name, pos, scale);
        Tint(stripe, new Color(0.9f, 0.55f, 0.08f));
    }

    static void PlaceStand(Transform parent, string name, Vector3 pos, Vector3 scale)
    {
        var stand = Prim(PrimitiveType.Cube, parent, name, pos, scale);
        Tint(stand, new Color(0.1f, 0.11f, 0.13f));
    }

    static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 pos, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        Object.Destroy(go.GetComponent<Collider>());
        return go;
    }

    static void Tint(GameObject go, Color color)
    {
        var rend = go.GetComponent<MeshRenderer>();
        if (rend == null)
            return;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        var mat = new Material(shader) { name = "Runtime_" + go.name };
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else
            mat.color = color;
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0.15f);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.35f);
        rend.sharedMaterial = mat;
    }
}
