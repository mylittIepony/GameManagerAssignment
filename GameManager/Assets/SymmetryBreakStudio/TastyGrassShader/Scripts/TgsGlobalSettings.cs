using System;
using UnityEngine;

namespace SymmetryBreakStudio.TastyGrassShader
{
    public static class TgsGlobalSettings
    {
        public const float MinLodCutoff = 0.01f; // The Lod CutOff shall never approach 0. Otherwise, we risk having a huge amount (+2000) active instances, which will slow down everything and also risk allocating too much graphics ram.
        
        public static float GlobalDensityScale = 1.0f;
        public static float GlobalLodScale = 1.0f;
        public static float GlobalLodFalloffExponent = 2.5f;

        [Obsolete("This variable doesn't work anymore. Use GlobalBakingTimeBudget instead.", true)]
        public static int GlobalMaxBakesPerFrame = 32;

        public static float ChunkCullTimeout = 1.0f; 

        public static float GlobalBakingTimeBudget = 4.0f;
        public static float GlobalLodCutoff = 0.05f;
        public static int GlobalChunkSize = 64;

        public static bool XrPassthroughAlphaFix;
        public static bool EnableShadows = true;
        
        public static Material CustomRenderingMaterial;
        
#if TASTY_GRASS_SHADER_DEBUG
        public static bool DebugFreezeBakes = false;
#endif
    }
}