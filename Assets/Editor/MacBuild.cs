using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class MacBuild
{
    public static void Build()
    {
        UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.ARM64;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 800;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = "Builds/Samurai vs Zombies.app",
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.Development
        });
        Debug.Log("Mac build result: " + report.summary.result + "; errors: " + report.summary.totalErrors);
        if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0)
            throw new System.Exception("Mac build failed: " + report.summary.result);
    }
}
