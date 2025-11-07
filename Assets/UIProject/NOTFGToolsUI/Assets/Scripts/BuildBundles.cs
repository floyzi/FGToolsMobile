#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public class BuildBundles : EditorWindow
{
    [MenuItem("Assets/Build AssetBundles")]
    static void Build()
    {
        var windw = GetWindow<BuildBundles>("Build Bundles");
        windw.minSize = new Vector2(350, 70);
        windw.maxSize = new Vector2(350, 70);
    } 
        
    void OnGUI()
    {
        var t = (BuildTarget)EditorGUILayout.EnumPopup("Platform", BuildTarget.Android);

        if (GUILayout.Button("Build"))
        {
            var output = $"Assets/AssetBundles/{t}";
            if (!Directory.Exists(output))
                Directory.CreateDirectory(output);

            BuildPipeline.BuildAssetBundles(output, BuildAssetBundleOptions.None, t);
        }
    }
}
#endif