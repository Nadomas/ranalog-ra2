using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// S11-07: build first playable Windows MVP player (workshop + local fight).
/// </summary>
public static class MvpPlayerBuild
{
    public const string ScenePath = "Assets/Scenes/MvpPlayable.unity";
    const string PlayerOut = "../Builds/Ra2MvpPlayer/Ra2MvpPlayer.exe";

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
    }

    public static void BuildPlayableScene()
    {
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
        camGo.AddComponent<AudioListener>();

        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var host = new GameObject("MvpPlayableHost");
        host.AddComponent<RobotMvpPlayableApp>();
        // Editor-only auto verifier: keep off for interactive/player boots; smoke uses -ra2-mvp-smoke.
        host.AddComponent<RobotMvpPlayableVerifier>().AutoRun = false;

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ScenePath)) ?? "Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        Debug.Log($"[S11-07] MvpPlayable scene saved: {ScenePath}");
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
