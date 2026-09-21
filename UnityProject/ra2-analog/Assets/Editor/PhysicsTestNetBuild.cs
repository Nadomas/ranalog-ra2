using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Builds Windows player / Dedicated Server player for S2-06..08 cross-process smokes.
/// </summary>
public static class PhysicsTestNetBuild
{
    const string ScenePath = "Assets/Scenes/PhysicsTest.unity";
    const string PlayerOut = "../Builds/PhysicsTestPlayer/PhysicsTestPlayer.exe";
    const string DedicatedOut = "../Builds/PhysicsTestDedicated/PhysicsTestDedicated.exe";

    [MenuItem("Tools/RA2/Build PhysicsTest Windows Player (S2 net)")]
    public static void BuildWindowsPlayer()
    {
        EnsureSceneInBuildSettings();
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = Path.GetFullPath(Path.Combine(Application.dataPath, PlayerOut)),
            target = BuildTarget.StandaloneWindows64,
            subtarget = (int)StandaloneBuildSubtarget.Player,
            options = BuildOptions.Development
        };
        var report = BuildPipeline.BuildPlayer(opts);
        LogReport("WindowsPlayer", report);
    }

    [MenuItem("Tools/RA2/Build PhysicsTest Dedicated Server (S2-08)")]
    public static void BuildDedicatedServer()
    {
        EnsureSceneInBuildSettings();
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = Path.GetFullPath(Path.Combine(Application.dataPath, DedicatedOut)),
            target = BuildTarget.StandaloneWindows64,
            subtarget = (int)StandaloneBuildSubtarget.Server,
            options = BuildOptions.Development
        };
        var report = BuildPipeline.BuildPlayer(opts);
        LogReport("DedicatedServer", report);
    }

    static void EnsureSceneInBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes;
        for (var i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].path == ScenePath)
            {
                if (!scenes[i].enabled)
                {
                    scenes[i].enabled = true;
                    EditorBuildSettings.scenes = scenes;
                }

                return;
            }
        }

        var next = new EditorBuildSettingsScene[scenes.Length + 1];
        for (var i = 0; i < scenes.Length; i++)
            next[i] = scenes[i];
        next[scenes.Length] = new EditorBuildSettingsScene(ScenePath, true);
        EditorBuildSettings.scenes = next;
        Debug.Log($"[PhysicsTestNetBuild] Added {ScenePath} to EditorBuildSettings.");
    }

    static void LogReport(string label, BuildReport report)
    {
        var summary = report.summary;
        Debug.Log(
            $"[PhysicsTestNetBuild] {label} result={summary.result} " +
            $"time={summary.totalTime} size={summary.totalSize} path={summary.outputPath}");
        if (summary.result != BuildResult.Succeeded)
            Debug.LogError($"[PhysicsTestNetBuild] {label} FAILED: {summary.result}");
    }
}
