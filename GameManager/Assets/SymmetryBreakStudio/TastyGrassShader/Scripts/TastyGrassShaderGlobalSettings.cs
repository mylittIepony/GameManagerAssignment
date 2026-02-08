#if TGS_URP_INSTALLED
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace SymmetryBreakStudio.TastyGrassShader
{
    // NOTE: keep in sync with TastyGrassShaderSettings.
    public class TastyGrassShaderGlobalSettings : ScriptableRendererFeature
    {
        public static TastyGrassShaderGlobalSettings LastActiveInstance;

        [Header("Visual")]
        [Tooltip(
            "Optional custom material for rendering. If None/Null the default internal material will be used. Helpful, when using custom lighting models or other assets/effects that affect the global rendering are used. See the TgsAmplify")]
        public Material customRenderingMaterial;


        [Header("Performance & Quality")]
        [Tooltip(
            "The Limit of Milliseconds spend per Camera per Frame that are used to bake Grass. Higher values increase the risk of stutter, but update quicker. ")]
        [Range(0.5f, 12.0f)]
        public float bakingTimeBudget = 4.0f;

        [Tooltip("Global multiplication for the amount value.")] [Range(0.001f, 2.0f)]
        public float densityScale = 1.0f;

        [Tooltip(
            "The exponent for the internal LOD factor. Higher values will reduce the amount of blades visible at distance. This can be used to improve performance.")]
        [Range(0.5f, 10.0f)]
        public float lodFalloffExponent = 2.5f;

        [Tooltip("Global multiplication for of the LOD.")] [Range(0.001f, 4.0f)]
        public float lodScale = 1.0f;

        [Tooltip(
            "Minimum % of Level-Of-Detail for a instance to be rendered. Higher values can help with CPU overhead, but may introduce pop-in artefacts.")]
        [Range(TgsGlobalSettings.MinLodCutoff, 1.0f)]
        public float lodCutoff = 0.05f;

        [Min(16), Delayed,
         Tooltip(
             "Controls how big the invisible groups of grass (= Instance) should be at maximum. Higher values reduce CPU load, while lower reduce GPU load. Generally, you should favour higher chunks sizes as CPU overhead usually outweighs the saved GPU power.")]
        public int chunkSize = 64;

        [Tooltip("Enable shadows for grass, if enabled by the preset.")]
        public bool enableShadows = true;

        [Header("Various")]
        [FormerlySerializedAs("noAlphaToCoverage")]
        [Tooltip(
            "Fixes alpha issues with XR by disabling alpha-to-coverage and using simple alpha clipping instead. Note that this prevents MSAA from working with the grass. May only work if customRenderingMaterial is set to null or the TgsFallbackShader in URP is used.")]
        public bool xrPassthroughAlphaFix;
        [Tooltip("Amount of seconds that a chunk is allowed to be alive after it had been culled by the LoD system. Higher values fix disappearing or flickering grass introduced by quickly rendering multiple locations, such as with baking reflection probes."), Min(0.0f)]
        public float ChunkCullTimeout = 1.0f;
        #if TASTY_GRASS_SHADER_DEBUG
        public bool DebugFreezeBakes = false;
        #endif
        public override void Create()
        {
        }


        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            LastActiveInstance = this;
            // Apply the settings from this Scriptable Render Feature to the internal TgsGlobalSettings
            TgsGlobalSettings.GlobalDensityScale = densityScale;
            TgsGlobalSettings.GlobalLodScale = lodScale;
            TgsGlobalSettings.GlobalLodFalloffExponent = lodFalloffExponent;
            TgsGlobalSettings.GlobalBakingTimeBudget = bakingTimeBudget;
            TgsGlobalSettings.CustomRenderingMaterial = customRenderingMaterial;
            TgsGlobalSettings.XrPassthroughAlphaFix = xrPassthroughAlphaFix;
            TgsGlobalSettings.GlobalLodCutoff = lodCutoff;
            TgsGlobalSettings.GlobalChunkSize = chunkSize;
            TgsGlobalSettings.ChunkCullTimeout = ChunkCullTimeout;
            TgsGlobalSettings.EnableShadows = enableShadows;
            
#if TASTY_GRASS_SHADER_DEBUG
            TgsGlobalSettings.DebugFreezeBakes = DebugFreezeBakes;
#endif
        }
    }
}
#endif