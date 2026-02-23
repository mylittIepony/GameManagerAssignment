using System.Collections;
using UnityEngine;
#if TGS_URP_INSTALLED
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif
using UnityEngine.SceneManagement;

namespace SymmetryBreakStudio.TastyGrassShader.Example
{
    /// <summary>
    /// The Demo UI for the Tasty Grass Shader. ONLY work with the URP for now.
    /// </summary>
    public class DemoGUI : MonoBehaviour
    {
        public GameObject unityTerrain;
        public string lightSettingsScene;
        public TgsWindSettings windSettings;
#if TGS_URP_INSTALLED
        const float frameSmoothTime = 1.0f;
        const float sliderValueLabelWidth = 220.0f;

#if TASTY_GRASS_SHADER_DEBUG
        public Mesh mesh;
#endif
        float _frameTimeVelocity, _smoothedFrameTime;
        bool originalAlphaClip;
        float originalDensity, originalLodScale, originalLodFalloffExponent;

        bool storedOriginalSettings;
        TgsForUnityTerrain tgsTerrain;

        void Update()
        {
            _smoothedFrameTime =
                Mathf.SmoothDamp(_smoothedFrameTime, Time.deltaTime, ref _frameTimeVelocity, frameSmoothTime);
            if (!storedOriginalSettings && TastyGrassShaderGlobalSettings.LastActiveInstance != null)
            {
                storedOriginalSettings = true;
                TastyGrassShaderGlobalSettings settings = TastyGrassShaderGlobalSettings.LastActiveInstance;
                originalDensity = settings.densityScale;
                originalLodScale = settings.lodScale;
                originalLodFalloffExponent = settings.lodFalloffExponent;
            }
        }


        IEnumerator LoadLightScene()
        {
            if (SceneUtility.GetBuildIndexByScenePath(lightSettingsScene) > 0)
            {
                if (!SceneManager.GetSceneByName(lightSettingsScene).isLoaded)
                {
                    yield return SceneManager.LoadSceneAsync(lightSettingsScene, LoadSceneMode.Additive);
                    Scene scene = SceneManager.GetSceneByName(lightSettingsScene);
                    SceneManager.SetActiveScene(scene);
                }
            }
        }

        void OnEnable()
        {
            tgsTerrain = unityTerrain.GetComponents<TgsForUnityTerrain>()[0];
            StartCoroutine(LoadLightScene());
            Application.targetFrameRate = 250; // bump max frame rate to 250 for benchmarks.
        }

        void OnDestroy()
        {
            if (storedOriginalSettings && TastyGrassShaderGlobalSettings.LastActiveInstance != null)
            {
                TastyGrassShaderGlobalSettings settings = TastyGrassShaderGlobalSettings.LastActiveInstance;
                settings.densityScale = originalDensity;
                settings.lodScale = originalLodScale;
                settings.lodFalloffExponent = originalLodFalloffExponent;
            }
        }

        void OnGUI()
        {
            if (TastyGrassShaderGlobalSettings.LastActiveInstance == null)
            {
                GUI.Window(2, new Rect(10.0f, 10.0f, 300.0f, 600.0f), AddRenderFeatureWindow, "Error");
                return;
            }

            GUI.Window(0, new Rect(10.0f, 10.0f, 300.0f, 520.0f), MainGuiWindow, "Settings");
#if TASTY_GRASS_SHADER_DEBUG
        GUI.Window(1, new Rect(500.0f, 10.0f, 300.0f, 600.0f), DebugWindow, "Debug");
#endif
        }

        void AddRenderFeatureWindow(int id)
        {
            GUILayout.Label("<b> Please add the Tasty Grass Shader Render Feature. </b>");
        }

        void MainGuiWindow(int id)
        {
            GUILayout.Label("<b> Tasty Grass Shader - Demo (v. 2.2.1) </b>");
            TastyGrassShaderGlobalSettings settings = TastyGrassShaderGlobalSettings.LastActiveInstance;
            GUILayout.Label("<b>General</b>");
            GUILayout.Label($"Screen Resolution: {Screen.width}x{Screen.height}");

            GUILayout.Label($"FPS: {1.0f / _smoothedFrameTime:F} ({_smoothedFrameTime * 1000.0f:F}MS)");
            float bakingPercentage = TgsGlobalStatus.instancesReady / (float)TgsGlobalStatus.instances * 100.0f;
            GUILayout.Label(
                $"Baking Status: {bakingPercentage:F}%\n({TgsGlobalStatus.instances} Chunks Active, {TgsGlobalStatus.instancesReady} Chunks Ready)");

            UniversalRenderPipelineAsset activeUrp = (UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline;

            GUILayout.Space(12.0f);
            GUILayout.Label("<b>Tasty Grass Shader - Settings</b>");
            CheckBox("Enable Tasty Grass Shader", ref TgsManager.Enable);
            settings.densityScale = SliderFloat("Global Density", settings.densityScale, 0.01f, 4.0f);
            settings.lodScale = SliderFloat("Global Lod Scale", settings.lodScale, 0.01f, 4.0f);
            settings.lodFalloffExponent =
                SliderFloat("Global LOD Falloff Exponent", settings.lodFalloffExponent, 1f, 10.0f);

            GUILayout.Space(12.0f);
            GUILayout.Label("<b>Rendering Settings</b>");
            bool msaaActive = activeUrp.msaaSampleCount == 4;
            CheckBox("MSAA 4x", ref msaaActive);

            CheckBox("No Alpha To Coverage", ref settings.xrPassthroughAlphaFix);

            activeUrp.msaaSampleCount = msaaActive ? 4 : 1;

            if (GUILayout.Button("Setup Benchmarking Setting"))
            {
                Screen.SetResolution(1920, 1080, FullScreenMode.Windowed, 300);
                activeUrp.msaaSampleCount = 1;
                settings.xrPassthroughAlphaFix = true;
                settings.densityScale = 0.5f;
                settings.lodScale = 1.0f;
                settings.lodFalloffExponent = 2.5f;
            }

            GUILayout.Space(12.0f);
            if (GUILayout.Button("Exit"))
            {
                Application.Quit();
            }
        }

#if TASTY_GRASS_SHADER_DEBUG
    void DebugWindow(int id)
    {
        // GUILayout.Label($"index count = {mesh.GetIndexCount(0)}");
        // GUILayout.Label($"index count from buffer = {mesh.GetIndexBuffer().count}");
        // GUILayout.Label($"vertex count from buffer= {mesh.GetVertexBuffer(0).count}");
    }
#endif
        float SliderFloat(string label, float value, float min, float max)
        {
            GUILayout.Label(label);
            GUILayout.BeginHorizontal();
            GUILayout.Label("   ");
            float outValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(sliderValueLabelWidth));
            GUILayout.Label($"{outValue:F}");
            GUILayout.EndHorizontal();
            return outValue;
        }

        void SliderInt(string label, ref int value, int min, int max)
        {
            GUILayout.Label(label);
            GUILayout.BeginHorizontal();
            GUILayout.Label("   ");
            value = Mathf.RoundToInt(
                GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(sliderValueLabelWidth)));
            GUILayout.Label($"{value:F}");
            GUILayout.EndHorizontal();
        }

        void CheckBox(string label, ref bool value)
        {
            value = GUILayout.Toggle(value, label);
        }
#else
        void OnGUI()
        {
            GUI.Window(2, new Rect(10.0f, 10.0f, 300.0f, 600.0f), UrpOnlyWindow, "Error");
        }
        
        void UrpOnlyWindow(int id)
        {
            GUILayout.Label("<b>The Tasty Grass Shader Demo currently only works with the Universal Render Pipeline (URP). </b>");
        }

#endif
    }
}