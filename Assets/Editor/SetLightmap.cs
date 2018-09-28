using UnityEngine;
using UnityEditor;

public class SetLightmap : EditorWindow
{
    string myString = "Hello World";
    bool groupEnabled;
    bool myBool = true;
    float myFloat = 1.23f;

    [MenuItem("Window/Set LightMap")]
    static void Init()
    {
        EditorWindow.GetWindow(typeof(SetLightmap));
    }

    private Texture2D _lightmap;

    void OnGUI()
    {
        _lightmap = EditorGUILayout.ObjectField("LightMap", _lightmap, typeof(Texture2D), true) as Texture2D;
        if (GUILayout.Button("Set"))
        {
            LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;
            LightmapData data = new LightmapData { lightmapColor = _lightmap };
            LightmapSettings.lightmaps = new[] { data };
        }

    }
}