using UnityEngine;
using UnityEditor;

public class BuildManagerData : ScriptableObject {
    public string versionNumber = "0.0.1";
    public int buildNumber = 0;
    public string path;
    public bool buildOnlyForCurrentTarget = true;
    public BuildTarget[] buildTargets;

    void Awake() {
        string projectPath = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
        path = path ??
            projectPath + "Build/" + Application.productName
            + " " + BuildManager.BUILD_TARGET_MARKER
            + " v" + BuildManager.VERSION_NUMBER_MARKER
            + "b" + BuildManager.BUILD_NUMBER_MARKER;
    }
}
