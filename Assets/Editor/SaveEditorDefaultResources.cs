//
// Copyright (c) 2017 eppz! mobile, Gergely Borbás (SP)
//
// http://www.twitter.com/_eppz
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE
// OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;


namespace EPPZ.Editor.Utils
{

    /// <summary>
    /// Simple Editor Tool that exports every image Asset from Editor Asset Bundle to `Assets/Editor Default Resources`.
    /// Read more at http://eppz.eu/blog/unity-editor-icon-assets/
    /// http://twitter.com/_eppz
    /// </summary>
    public class SaveEditorDefaultResources : EditorWindow
    {


        static UnityEngine.Object[] _assets;
        static string[] _assetNames;
        static string log_export;


        #region UI

        [MenuItem("Window/eppz!/Icon")]
        public static void ShowWindow()
        { EditorWindow.GetWindow(typeof(SaveEditorDefaultResources), false, "Icons"); }

        void OnGUI()
        {
            if (GUILayout.Button("Get Assets"))
            { GetAssets(); }

            if (GUILayout.Button("Save Assets"))
            { SaveAssets(); }
        }

        #endregion


        #region Export

        void GetAssets()
        {
            AssetBundle editorAssetBundle = EditorGUIUtility_GetEditorAssetBundle();
            _assets = editorAssetBundle.LoadAllAssets();
            _assetNames = editorAssetBundle.GetAllAssetNames();
            List<Type> types = new List<Type>();

            // Collect asset data.
            string log = "";
            string log_types = "";
            foreach (UnityEngine.Object eachAsset in _assets)
            {
                // Data.
                Type eachAssetType = eachAsset.GetType();
                string eachAssetTypeDescription = eachAssetType.ToString();
                string eachAssetName = _assetNames[Array.IndexOf(_assets, eachAsset)]; // Corresponding asset name (with subpath)

                // Log.
                log += String.Format("{0}, {1}, {2}\n", eachAssetName, eachAsset.name, eachAssetTypeDescription);

                // Save types.
                if (types.Contains(eachAssetType)) continue;
                log_types += String.Format("{0}\n", eachAssetTypeDescription);
                types.Add(eachAssetType);
            }

            // Save log.
            File.WriteAllText("Assets/Editor Default Resources.txt", log);
            File.WriteAllText("Assets/Editor Default Resources (types).txt", log_types);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        void SaveAssets()
        {
            log_export = "";
            foreach (UnityEngine.Object eachAsset in _assets)
            {
                // Create full asset path.
                string eachAssetName = _assetNames[Array.IndexOf(_assets, eachAsset)]; // Corresponding asset name (with subpath)
                string eachAssetPath = Application.dataPath + "/Editor Default Resources/" + eachAssetName;

                // Create folder.
                Directory.CreateDirectory(Path.GetDirectoryName(eachAssetPath));
                Debug.Log("Directory.CreateDirectory(`" + Path.GetDirectoryName(eachAssetPath) + "`)");

                // Write.
                WriteFileFromAssetToPath(eachAsset, eachAssetPath);
            }
            File.WriteAllText("Assets/Editor Default Resources (Export).txt", log_export);
        }

        void WriteFileFromAssetToPath(UnityEngine.Object asset, string path)
        {
            // Write asset.
            if (asset is Texture2D) WriteFileFromTextureToPath(asset as Texture2D, path);
            if (asset is TextAsset) WriteFileFromTextAssetToPath(asset as TextAsset, path);
        }

        void WriteFileFromTextureToPath(Texture2D texture, string path)
        {
            try
            {
                // Access raw texture data.
                Texture2D copy = new Texture2D(texture.width, texture.height, texture.format, texture.mipmapCount > 1);
                copy.LoadRawTextureData(texture.GetRawTextureData());
                copy.Apply();
                               byte[] bytes = copy.EncodeToPNG();
                string path_png = String.Format(
                    "{0}/{1}.{2}",
                    Path.GetDirectoryName(path),
                    Path.GetFileNameWithoutExtension(path),
                    "png"
                );
                File.WriteAllBytes(path_png, bytes);
                log_export += "File.WriteAllBytes(`" + path_png + "`, <bytes>);\n";
            }
            catch (Exception exception)
            {
                string text = exception.Message + "\n" + exception.StackTrace;
                string path_txt = String.Format(
                    "{0}/{1} (Exception).{2}.txt",
                    Path.GetDirectoryName(path),
                    Path.GetFileNameWithoutExtension(path),
                    Path.GetExtension(path)
                );
                File.WriteAllText(path_txt, text);
                log_export += "File.WriteAllBytes(`" + path_txt + "`, <text>);\n";
            }
        }

        void WriteFileFromTextAssetToPath(TextAsset textAsset, string path)
        {
            File.WriteAllText(path, textAsset.text);
            log_export += "File.WriteAllBytes(`" + path + "`, <textAsset.text>);\n";
        }

        #endregion


        #region Internal API access

        AssetBundle EditorGUIUtility_GetEditorAssetBundle()
        {
            return Type.GetType("UnityEditor.EditorGUIUtility,UnityEditor.dll")
                .GetMethod("GetEditorAssetBundle", BindingFlags.Static | BindingFlags.NonPublic) // internal
                .Invoke(null, null) as AssetBundle;
        }

        string EditorResourcesUtility_generatedIconsPath
        {
            get
            {
                return Type.GetType("UnityEditorInternal.EditorResourcesUtility,UnityEditor.dll")
                    .GetProperty("generatedIconsPath", BindingFlags.Static | BindingFlags.Public)
                    .GetValue(null, null) as string;
            }
        }

        string EditorResourcesUtility_iconsPath
        {
            get
            {
                return Type.GetType("UnityEditorInternal.EditorResourcesUtility,UnityEditor.dll")
                    .GetProperty("iconsPath", BindingFlags.Static | BindingFlags.Public)
                    .GetValue(null, null) as string;
            }
        }

        #endregion


    }
}