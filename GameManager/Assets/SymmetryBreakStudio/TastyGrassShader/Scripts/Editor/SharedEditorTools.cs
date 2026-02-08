using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace SymmetryBreakStudio.TastyGrassShader.Editor
{
    public static class SharedEditorTools
    {
        public static readonly string productName = "Tasty Grass Shader " + UpdateHandler.ThisVersion;
        
        #region Render Pipeline Utilities
        public const string urpPackagePath = "Assets/SymmetryBreakStudio/TastyGrassShader/TGS_URP.unitypackage";
        public const string urpBackendPath = "Assets/SymmetryBreakStudio/TastyGrassShader/URP";
        public const string urpExamplesPath = "Assets/SymmetryBreakStudio/TastyGrassShader/Examples/URP";

        public const string hdrpPackagePath = "Assets/SymmetryBreakStudio/TastyGrassShader/TGS_HDRP.unitypackage";
        public const string hdrpBackendPath = "Assets/SymmetryBreakStudio/TastyGrassShader/HDRP";
        public const string hdrpExamplesPath = "Assets/SymmetryBreakStudio/TastyGrassShader/Examples/HDRP";
        
        public enum UnityRp
        {
            Unknown,
            BuiltIn,
            Universal,
            HighDefinition,
        }

        public static UnityRp GetActiveRenderPipeline(bool logErrors)
        {
            // NOTE/HACK: Just checking the string for a keyword,
            // instead comparing for the type. This prevents us from referencing URP or HDRP assemblies.
            RenderPipelineAsset activePipeline = GraphicsSettings.currentRenderPipeline;
            if (activePipeline == null)
            {
                if (logErrors)
                {
                    Debug.LogError("Tasty Grass Shader: Using the Built-In pipeline is not supported.");
                }

                return UnityRp.BuiltIn;
            }

            string activePipelineType = activePipeline.GetType().ToString();
#if TGS_URP_INSTALLED
            if (activePipelineType.Contains("Universal"))
            {
                return UnityRp.Universal;
            }
#endif

#if TGS_HDRP_INSTALLED
            if (activePipelineType.Contains("HighDefinition"))
            {
                return UnityRp.HighDefinition;
            }
#endif

            if (logErrors)
            {
                Debug.LogError(
                    "Tasty Grass Shader: unable to determine the used render pipeline. If you have the Universal- or High-Definition Rendering Pipeline active, then is this is likely a bug. We would be very happy if you reach out to us in that case. ");
            }

            return UnityRp.Unknown;
        }

        private static GlobalKeyword tgsEditorUseUrp = GlobalKeyword.Create("TGS_EDITOR_USE_URP");
        private static GlobalKeyword tgsEditorUseHdrp = GlobalKeyword.Create("TGS_EDITOR_USE_HDRP");

        public static void UpdateEditorShaderRenderPipelineVariants()
        {
            UnityRp activePipeline = GetActiveRenderPipeline(true);
            Shader.SetKeyword(tgsEditorUseUrp, activePipeline == UnityRp.Universal);
            Shader.SetKeyword(tgsEditorUseHdrp, activePipeline == UnityRp.HighDefinition);
        }

        static string LoadTextAssetOrEmpty(string path)
        {
            string result = string.Empty;
            TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (textAsset != null)
            {
                result = textAsset.text;
            }

            return result;
        }

        private static string urpVersionPath =
            "Assets/SymmetryBreakStudio/TastyGrassShader/URP/TGS_URP_BackendVersion.txt";

        public static string GetUrpSubpackageVersion()
        {
            return LoadTextAssetOrEmpty(urpVersionPath);
        }

        public static bool IsUrpPackageInstalled()
        {
            return !string.IsNullOrEmpty(GetUrpSubpackageVersion());
        }


        private static string hdrpVersionPath =
            "Assets/SymmetryBreakStudio/TastyGrassShader/HDRP/TGS_HDRP_BackendVersion.txt";

        public static string GetHdrpSubpackageVersion()
        {
            return LoadTextAssetOrEmpty(hdrpVersionPath);
        }

        public static bool IsHdrpPackageInstalled()
        {
            return !string.IsNullOrEmpty(GetHdrpSubpackageVersion());
        }
        #endregion
        
        public static string ReplaceInvalidChars(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));    
        }

        public static void CreateAssetInSceneFolder(GameObject sourceParentObject, Texture2D texture2D, string name)
        {
            Debug.Assert(sourceParentObject);
            Scene scene = sourceParentObject.scene;
            string scenePathFolderOnly = scene.path[0..(scene.path.IndexOf(scene.name, StringComparison.Ordinal) - 1)];
            if (!AssetDatabase.IsValidFolder(scenePathFolderOnly + "/" + scene.name))
            {
                AssetDatabase.CreateFolder(scenePathFolderOnly, scene.name);

            }
            string folderPath = $"{scenePathFolderOnly}/{scene.name}/Tex2D_{ReplaceInvalidChars(name)}_{Random.Range(int.MinValue, int.MaxValue):X}.asset";

            //TgsTextureStorage newTextureStorage = (TgsTextureStorage) ScriptableObject.CreateInstance(typeof(TgsTextureStorage));
                    
            AssetDatabase.CreateAsset(texture2D, folderPath);
        }
    }
}