using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-shot builder for Assets/Scenes/PhysicsTest.unity (STAGE-1 style smoke).
/// Invoked via: -executeMethod PhysicsTestSceneBuilder.Build
/// </summary>
public static class PhysicsTestSceneBuilder
{
    const string ScenePath = "Assets/Scenes/PhysicsTest.unity";

    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateMainCamera();
        CreateDirectionalLight();

        var arena = new GameObject("Arena");
        CreateFloor(arena.transform);
        CreateWalls(arena.transform);

        CreateRobot("Robot_A", new Vector3(-4f, 0.75f, 0f), new Vector3(6f, 0f, 0f), new Color(0.2f, 0.55f, 1f));
        CreateRobot("Robot_B", new Vector3(4f, 0.75f, 0f), new Vector3(-6f, 0f, 0f), new Color(1f, 0.35f, 0.2f));

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[PhysicsTestSceneBuilder] Saved {ScenePath}");
    }

    static void CreateMainCamera()
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        var cam = go.AddComponent<Camera>();
        cam.transform.position = new Vector3(0f, 12f, -14f);
        cam.transform.rotation = Quaternion.Euler(40f, 0f, 0f);
        cam.clearFlags = CameraClearFlags.Skybox;
        go.AddComponent<AudioListener>();
    }

    static void CreateDirectionalLight()
    {
        var go = new GameObject("Directional Light");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    static void CreateFloor(Transform parent)
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(parent, false);
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(2.5f, 1f, 2.5f); // 25x25 world units
        Object.DestroyImmediate(floor.GetComponent<MeshCollider>());
        var box = floor.AddComponent<BoxCollider>();
        box.size = new Vector3(10f, 0.05f, 10f);
        box.center = new Vector3(0f, -0.025f, 0f);
    }

    static void CreateWalls(Transform parent)
    {
        const float half = 12f;
        const float height = 2f;
        const float thickness = 0.5f;
        const float length = 24.5f;

        CreateWall(parent, "Wall_North", new Vector3(0f, height * 0.5f, half), new Vector3(length, height, thickness));
        CreateWall(parent, "Wall_South", new Vector3(0f, height * 0.5f, -half), new Vector3(length, height, thickness));
        CreateWall(parent, "Wall_East", new Vector3(half, height * 0.5f, 0f), new Vector3(thickness, height, length));
        CreateWall(parent, "Wall_West", new Vector3(-half, height * 0.5f, 0f), new Vector3(thickness, height, length));
    }

    static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent, false);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        Object.DestroyImmediate(wall.GetComponent<Collider>());
        wall.AddComponent<BoxCollider>();
    }

    static void CreateRobot(string name, Vector3 position, Vector3 initialVelocity, Color color)
    {
        var root = new GameObject(name);
        root.transform.position = position;

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Chassis";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(1.6f, 0.7f, 2.2f);
        Object.DestroyImmediate(body.GetComponent<Collider>());

        var bodyCol = body.AddComponent<BoxCollider>();
        var bodyRend = body.GetComponent<MeshRenderer>();
        bodyRend.sharedMaterial = CreateColorMaterial($"{name}_Body", color);

        // Simple "wheels" as child colliders (same Rigidbody via compound colliders on children).
        CreateWheel(root.transform, "Wheel_FL", new Vector3(-0.85f, -0.35f, 0.7f));
        CreateWheel(root.transform, "Wheel_FR", new Vector3(0.85f, -0.35f, 0.7f));
        CreateWheel(root.transform, "Wheel_RL", new Vector3(-0.85f, -0.35f, -0.7f));
        CreateWheel(root.transform, "Wheel_RR", new Vector3(0.85f, -0.35f, -0.7f));

        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 12f;
        rb.linearDamping = 0.15f;
        rb.angularDamping = 0.4f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.linearVelocity = initialVelocity;

        // Nose marker so facing is obvious in Play Mode.
        var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nose.name = "Nose";
        nose.transform.SetParent(root.transform, false);
        nose.transform.localPosition = new Vector3(0f, 0.15f, 1.25f);
        nose.transform.localScale = new Vector3(0.4f, 0.3f, 0.4f);
        Object.DestroyImmediate(nose.GetComponent<Collider>());
        nose.GetComponent<MeshRenderer>().sharedMaterial = CreateColorMaterial($"{name}_Nose", Color.white);
    }

    static void CreateWheel(Transform parent, string name, Vector3 localPos)
    {
        var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.name = name;
        wheel.transform.SetParent(parent, false);
        wheel.transform.localPosition = localPos;
        wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        wheel.transform.localScale = new Vector3(0.55f, 0.18f, 0.55f);
        Object.DestroyImmediate(wheel.GetComponent<Collider>());
        var col = wheel.AddComponent<SphereCollider>();
        col.radius = 0.5f;
        wheel.GetComponent<MeshRenderer>().sharedMaterial = CreateColorMaterial(name, new Color(0.12f, 0.12f, 0.12f));
    }

    static Material CreateColorMaterial(string name, Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
            mat = new Material(Shader.Find("Standard"));
        mat.name = name;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else
            mat.color = color;

        var dir = "Assets/Scenes/PhysicsTestMaterials";
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Scenes", "PhysicsTestMaterials");
        var path = $"{dir}/{name}.mat";
        AssetDatabase.CreateAsset(mat, path);
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }
}
