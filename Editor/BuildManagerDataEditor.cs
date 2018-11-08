using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BuildManagerData))]
public class BuildManagerDataEditor : Editor {
    const string PATH_HELP_BOX_MESSAGE = "The following strings will be replaced in the path name:\n"
        + "{$t} with the target platform,\n"
        + "{$v} with the version number,\n"
        + "{$b} with the build number";
    const string BUILD_TARGET_HELP_BOX_MESSAGE = "Only some build targets are supported, please use the build window if your target platform is not in this list: "
        + "Android, iOS, StandaloneWindows, StandaloneWindows64";

    SerializedProperty versionNumber;
    SerializedProperty buildNumber;
    SerializedProperty buildPath;
    SerializedProperty buildOnlyForCurrentTarget;
    SerializedProperty buildTargets;

    void OnEnable() {
        versionNumber = serializedObject.FindProperty("versionNumber");
        buildNumber = serializedObject.FindProperty("buildNumber");
        buildPath = serializedObject.FindProperty("path");
        buildOnlyForCurrentTarget = serializedObject.FindProperty("buildOnlyForCurrentTarget");
        buildTargets = serializedObject.FindProperty("buildTargets");

        Undo.undoRedoPerformed -= UndoRedoHandler; // make sure we're only registered once
        Undo.undoRedoPerformed += UndoRedoHandler;
    }

    void UndoRedoHandler() {
        serializedObject.Update();
        BuildManager.UpdateVersionNumber();
        BuildManager.UpdateBuildNumber();
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(versionNumber);
        if (EditorGUI.EndChangeCheck()) {
            Undo.RecordObject(this, "Changed version number");
            serializedObject.ApplyModifiedProperties();
            BuildManager.UpdateVersionNumber();
        }

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(buildNumber);
        if (EditorGUI.EndChangeCheck()) {
            Undo.RecordObject(this, "Changed build number");
            serializedObject.ApplyModifiedProperties();
            BuildManager.UpdateBuildNumber();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Open Build Folder") && buildPath.stringValue.Length > 0) {
            string selectedFolderPath = System.IO.Path.GetDirectoryName(buildPath.stringValue);
            EditorUtility.RevealInFinder(selectedFolderPath);
        }
        if (GUILayout.Button("Select Build Folder")) {
            string selectedFolderPath = EditorUtility.OpenFolderPanel("Select Build Folder", "", "");
            if (selectedFolderPath != "") {
                Undo.RecordObject(this, "Changed build path");
                buildPath.stringValue = selectedFolderPath + "/" + Application.productName + " {$t} {$v}b{$b}";
                serializedObject.ApplyModifiedProperties();
            }
        }

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(buildPath);
        if (EditorGUI.EndChangeCheck()) {
            Undo.RecordObject(this, "Changed build path");
            serializedObject.ApplyModifiedProperties();
        }
        EditorGUILayout.HelpBox(PATH_HELP_BOX_MESSAGE, MessageType.Info);

        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(buildOnlyForCurrentTarget);
        if (!buildOnlyForCurrentTarget.boolValue) {
            EditorGUILayout.PropertyField(buildTargets, true);
        }
        if (EditorGUI.EndChangeCheck()) {
            Undo.RecordObject(this, "Changed build target selection");
            serializedObject.ApplyModifiedProperties();
        }
        EditorGUILayout.HelpBox(BUILD_TARGET_HELP_BOX_MESSAGE, MessageType.Info);

        if (GUILayout.Button("Build")) {
            BuildManager.Build();
            GUIUtility.ExitGUI();
        }
    }
}
