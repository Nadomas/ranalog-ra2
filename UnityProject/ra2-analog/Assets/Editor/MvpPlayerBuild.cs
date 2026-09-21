using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// S11-07/08: MvpPlayable scene + Windows player with UI Toolkit workshop shell.
/// </summary>
public static class MvpPlayerBuild
{
    public const string ScenePath = "Assets/Scenes/MvpPlayable.unity";
    const string PlayerOut = "../Builds/Ra2MvpPlayer/Ra2MvpPlayer.exe";
    const string UiFolder = "Assets/UI/Mvp";
    const string UxmlPath = UiFolder + "/MvpWorkshop.uxml";
    const string UssPath = UiFolder + "/MvpWorkshop.uss";
    const string PanelSettingsPath = UiFolder + "/MvpPanelSettings.asset";

    [MenuItem("Tools/RA2/Build MvpPlayable Scene (S11-07)")]
    public static void BuildSceneFromMenu()
    {
        BuildPlayableScene();
    }

    [MenuItem("Tools/RA2/Build MVP Windows Player (S11-07)")]
    public static void BuildWindowsPlayerFromMenu()
    {
        BuildPlayableScene();
        EnsureSceneInBuildSettingsFirst();
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = Path.GetFullPath(Path.Combine(Application.dataPath, PlayerOut)),
            target = BuildTarget.StandaloneWindows64,
            subtarget = (int)StandaloneBuildSubtarget.Player,
            options = BuildOptions.Development
        };
        var report = BuildPipeline.BuildPlayer(opts);
        var summary = report.summary;
        Debug.Log(
            $"[S11-07] PLAYER_BUILD result={summary.result} time={summary.totalTime} " +
            $"size={summary.totalSize} path={summary.outputPath}");
        if (summary.result != BuildResult.Succeeded)
            Debug.LogError($"[S11-07] PLAYER_BUILD FAILED: {summary.result}");
        else
            Debug.Log("[S11-08] PLAYER_BUILD_WITH_UIToolkit");
    }

    public static void BuildPlayableScene()
    {
        EnsureUiAssets();
        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var arena = new GameObject("Arena");
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(arena.transform, false);
        floor.transform.localScale = new Vector3(2.5f, 1f, 2.5f);
        Object.DestroyImmediate(floor.GetComponent<MeshCollider>());
        var box = floor.AddComponent<BoxCollider>();
        box.size = new Vector3(10f, 0.1f, 10f);
        box.center = new Vector3(0f, -0.05f, 0f);
        var floorRb = floor.AddComponent<Rigidbody>();
        floorRb.isKinematic = true;

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.transform.position = new Vector3(0f, 12f, -14f);
        cam.transform.rotation = Quaternion.Euler(40f, 0f, 0f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
        camGo.AddComponent<AudioListener>();

        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        light.color = new Color(1f, 0.95f, 0.88f);
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var host = new GameObject("MvpPlayableHost");
        var app = host.AddComponent<RobotMvpPlayableApp>();
        host.AddComponent<RobotMvpPlayableVerifier>().AutoRun = false;
        host.AddComponent<RobotMvpUiVerifier>().AutoRun = false;

        var uiGo = new GameObject("MvpUi");
        uiGo.transform.SetParent(host.transform, false);
        var doc = uiGo.AddComponent<UIDocument>();
        doc.visualTreeAsset = uxml;
        doc.panelSettings = panelSettings;
        var shell = uiGo.AddComponent<RobotMvpUiShell>();
        shell.Bind(app);

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ScenePath)) ?? "Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        Debug.Log($"[S11-07] MvpPlayable scene saved: {ScenePath}");
        Debug.Log("[S11-08] UI_DOCUMENT wired (UI Toolkit)");
    }

    static void EnsureUiAssets()
    {
        if (!AssetDatabase.IsValidFolder("Assets/UI"))
            AssetDatabase.CreateFolder("Assets", "UI");
        if (!AssetDatabase.IsValidFolder(UiFolder))
            AssetDatabase.CreateFolder("Assets/UI", "Mvp");

        if (AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath) == null)
        {
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            settings.sortingOrder = 100;
            AssetDatabase.CreateAsset(settings, PanelSettingsPath);
            Debug.Log($"[S11-08] Created PanelSettings at {PanelSettingsPath}");
        }

        AssetDatabase.ImportAsset(UxmlPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(UssPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void EnsureSceneInBuildSettingsFirst()
    {
        var scenes = EditorBuildSettings.scenes;
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        list.Add(new EditorBuildSettingsScene(ScenePath, true));
        for (var i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].path == ScenePath)
                continue;
            list.Add(scenes[i]);
        }

        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log($"[S11-07] EditorBuildSettings first scene={ScenePath}");
    }
}
