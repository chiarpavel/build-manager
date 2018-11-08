using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public static class BuildManager {
    public const string BUILD_TARGET_MARKER = "{$t}";
    public const string VERSION_NUMBER_MARKER = "{$v}";
    public const string BUILD_NUMBER_MARKER = "{$b}";

    const string PATH = "Assets/BuildManager/Editor/";
    const string FILE_NAME = "BuildManagerData.asset";

    static BuildManagerData _buildData;
    public static BuildManagerData BuildData {
        get {
            if (_buildData == null)
                _buildData = LoadData();
            return _buildData;
        }
    }

    static bool building = false;
    static Dictionary<BuildTarget, string> extensions;
    static Dictionary<BuildTarget, BuildTargetGroup> groups;

    static BuildManager() {
        extensions = new Dictionary<BuildTarget, string>() {
            {BuildTarget.Android, ".apk"},
            {BuildTarget.iOS, ""},
            {BuildTarget.StandaloneWindows, ".exe"},
            {BuildTarget.StandaloneWindows64, ".exe"},
            // {BuildTarget.StandaloneOSX, ".app"},
            // {BuildTarget.StandaloneLinux, ""},
            // {BuildTarget.StandaloneLinux64, ""},
            // {BuildTarget.StandaloneLinuxUniversal, ""},
        };
        groups = new Dictionary<BuildTarget, BuildTargetGroup>() {
            {BuildTarget.Android, BuildTargetGroup.Android},
            {BuildTarget.iOS, BuildTargetGroup.iOS},
            {BuildTarget.StandaloneWindows, BuildTargetGroup.Standalone},
            {BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone},
            // {BuildTarget.StandaloneOSX, BuildTargetGroup.Standalone},
            // {BuildTarget.StandaloneLinux, BuildTargetGroup.Standalone},
            // {BuildTarget.StandaloneLinux64, BuildTargetGroup.Standalone},
            // {BuildTarget.StandaloneLinuxUniversal, BuildTargetGroup.Standalone},
        };
    }

    [PostProcessBuildAttribute]
    static void HandlePostBuildEvent(BuildTarget target, string path) {
        if (!building) IncrementBuildNumber();
    }

    public static void Build() {
        if (BuildData.buildOnlyForCurrentTarget) {
            CheckSupport(EditorUserBuildSettings.activeBuildTarget);
            Build(EditorUserBuildSettings.activeBuildTarget);
        } else {
            if (BuildData.buildTargets.Length == 0) {
                Debug.LogWarning("No build targets selected.");
                return;
            }

            building = true;

            List<BuildTarget> buildsToDo = new List<BuildTarget>(BuildData.buildTargets);

            BuildTarget initialBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup initialBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            if (buildsToDo.Contains(EditorUserBuildSettings.activeBuildTarget)) {
                Build(EditorUserBuildSettings.activeBuildTarget);
                buildsToDo.Remove(EditorUserBuildSettings.activeBuildTarget);
            }

            while (buildsToDo.Count > 0) {
                BuildTarget target = buildsToDo[0];

                CheckSupport(target);
                EditorUserBuildSettings.SwitchActiveBuildTarget(groups[target], target);
                Build(target);
                buildsToDo.Remove(target);
            }

            if (EditorUserBuildSettings.activeBuildTarget != initialBuildTarget) {
                EditorUserBuildSettings.SwitchActiveBuildTarget(initialBuildTargetGroup, initialBuildTarget);
            }

            building = false;
            IncrementBuildNumber();
        }
    }

    [MenuItem("Build/Configuration")]
    static void OpenConfiguration() {
        Selection.activeObject = BuildData;
    }

    static void CheckSupport(BuildTarget target) {
        if (!extensions.ContainsKey(target) || !groups.ContainsKey(target)) {
            EditorWindow.GetWindow(Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
            throw new NotImplementedException("BuildManager support for " + target + " not implemented. Please use the Build window.");
        }
    }

    static void Build(BuildTarget target) {
        BuildPlayerOptions options = new BuildPlayerOptions();

        options.locationPathName = BuildData.path
            .Replace(BUILD_TARGET_MARKER, target.ToString())
            .Replace(VERSION_NUMBER_MARKER, BuildData.versionNumber)
            .Replace(BUILD_NUMBER_MARKER, BuildData.buildNumber.ToString()) +
            extensions[target];

        var enabledScenes = Array.FindAll(EditorBuildSettings.scenes, s => s.enabled);
        options.scenes = Array.ConvertAll<EditorBuildSettingsScene, string>(enabledScenes, s => s.path);

        options.target = EditorUserBuildSettings.activeBuildTarget;

        options.targetGroup = groups[target];

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded) {
            Debug.Log("Built " + Path.GetFileName(options.locationPathName));
        } else {
            Debug.LogError("Failed to build " + Path.GetFileName(options.locationPathName) + " : " + report.summary.result);
        }
    }

    static void IncrementBuildNumber() {
        BuildData.buildNumber++;
        UpdateBuildNumber();
    }

    public static void UpdateVersionNumber() {
        PlayerSettings.bundleVersion = BuildData.versionNumber;
    }

    public static void UpdateBuildNumber() {
        string buildNumberString = BuildData.buildNumber.ToString();
        PlayerSettings.Android.bundleVersionCode = BuildData.buildNumber;
        PlayerSettings.iOS.buildNumber = buildNumberString;
        PlayerSettings.macOS.buildNumber = buildNumberString;
        PlayerSettings.tvOS.buildNumber = buildNumberString;
    }

    static BuildManagerData LoadData() {
        BuildManagerData data = AssetDatabase.LoadAssetAtPath<BuildManagerData>(PATH + FILE_NAME);

        if (data == null) {
            CreatePath(PATH);
            data = ScriptableObject.CreateInstance<BuildManagerData>();
            AssetDatabase.CreateAsset(data, PATH + FILE_NAME);
            Debug.Log("Created new version data file at " + PATH + FILE_NAME);
        }

        return data;
    }

    private static void CreatePath(string path, char separator = '/') {
        List<string> parts = new List<string>(path.Split(separator));
        parts.RemoveAll(p => p == "");

        for (int i = 0; i < parts.Count; i++) {
            string partialPath = string.Join(separator.ToString(), parts.GetRange(0, i + 1).ToArray());
            if (!AssetDatabase.IsValidFolder(partialPath)) {
                string parentFolder = string.Join(separator.ToString(), parts.GetRange(0, i).ToArray());
                string newFolderName = parts[i];
                AssetDatabase.CreateFolder(parentFolder, newFolderName);
            }
        }
    }
}
