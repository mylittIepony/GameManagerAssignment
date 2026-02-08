using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace SymmetryBreakStudio.TastyGrassShader
{
    /// <summary>
    ///     This is the global manager for Tasty Grass Shader.
    ///     For performance reasons, rendering all instances happens here in a central place.
    ///     Before 2.0, each mesh or terrain was responsible for rendering. Since 2.0 all this work is bundled here.
    ///     This removes duplicate work (like loading the collider positions) and allows to move expensive work into Burst
    ///     kernels.
    /// </summary>
    public static class TgsManager
    {
        public const int MaxColliderPerInstance = 8;
        public const string UrpDefaultMaterialPath = "Materials/TGS_URP_Default";
        public const string HdrpDefaultMaterialPath = "Materials/TGS_HDRP_Default";
        
        const string alphaClipKeywordString = "TGS_USE_ALPHACLIP";
        

        public const string SetupMenuItem = "Tools/Symmetry Break Studio/Tasty Grass Shader/Manually Run Setup";
        public static bool Enable = true;

        static float _activeGlobalDensityValue;
        static int _activeGlobalChunkSize;

        static bool _isInitialize;

        static Vector4[] _colliderBuffer;
        static readonly int TgsUseAlphaToCoverage = Shader.PropertyToID("_Tgs_UseAlphaToCoverage");
        /// <summary>
        /// Important: if you access something here, run TgsInstances.CheckGlobalIndices() first!
        /// </summary>
        internal static NativeHashMap<ulong, TgsInstanceProxy> allNativeInstaces;

        private static readonly Stopwatch Stopwatch = new Stopwatch();

        /// <summary>
        /// Sometimes, the resource loading gets messed up and gives false positives on missing assets. By tracking how much this error occured, we improve UX.
        /// </summary>
        private static uint missingRenderMaterialErrorCount, missingResoucesErrorCount;

        public static ComputeShader tgsComputeShader { get; private set; }
        public static Material PipelineDefaultRenderingMaterial { get; private set; }

        public static Material tgsMatNoAlphaClipping { get; }

        #region GPU Mesh Cache

        /// <summary>
        /// Contains the Unity Meshes that where converted to a Compute-Shader usable version this frame. When using chunks on Meshes, the underlying GPU buffer gets recreated a lot of times. This caching mechanism prevents that. See TgsInstance.GPUMesh for details.
        /// </summary>
        static Dictionary<Mesh, TgsInstance.GPUMesh> gpuMeshFrameCache = new Dictionary<Mesh, TgsInstance.GPUMesh>();

        /// <summary>
        /// Returns or creates a cached GPU usable Unity Mesh. See TgsInstance.GPUMesh for details.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="errorContext"></param>
        /// <param name="requiresVertexColor"></param>
        /// <returns></returns>
        public static TgsInstance.GPUMesh GetOrCreateCachedGPUMesh(Mesh mesh, UnityEngine.Object errorContext,
            bool requiresVertexColor)
        {
            if (!gpuMeshFrameCache.TryGetValue(mesh, out TgsInstance.GPUMesh gpuMesh))
            {
                gpuMesh = new TgsInstance.GPUMesh(mesh, errorContext, requiresVertexColor);
                gpuMeshFrameCache.Add(mesh, gpuMesh);
            }

            return gpuMesh;
        }

        static void ClearGpuMeshCache()
        {
            // Clear up the GPU Mesh frame cache.
            foreach (var cachedGpuMesh in gpuMeshFrameCache.Values)
            {
                cachedGpuMesh.Release();
            }

            gpuMeshFrameCache.Clear();
        }

        #endregion

        #region Shared Data Buffers

        private const int SharedBufferMaxSlots = 64;

        private static int sharedMetaBufferUsedSlots = 0;
        private static ComputeBuffer sharedMetaDataBuffer;

        private static TgsInstance.InstanceMetaDataGPU[] sharedMetaDataBufferCpu =
            new TgsInstance.InstanceMetaDataGPU[SharedBufferMaxSlots];

        internal static ComputeBuffer sharedPlacedGrassCount;
        internal static uint[] sharedPlacedGrassCountCpu = new uint[SharedBufferMaxSlots];

        internal static ComputeBuffer sharedNoiseSeetingsBuffer;

        static HashSet<TgsInstance> inFlightBakingInstances = new HashSet<TgsInstance>(SharedBufferMaxSlots);

        public static void GetInstanceMetaDataSlot(out ComputeBuffer metaDataBuffer, out int metaBufferIndex)
        {
            if (sharedMetaBufferUsedSlots + 1 < SharedBufferMaxSlots)
            {
                metaDataBuffer = sharedMetaDataBuffer;
                metaBufferIndex = sharedMetaBufferUsedSlots;
                sharedMetaBufferUsedSlots += 1;
            }
            else
            {
                Debug.LogAssertion(
                    "Out of slots for the MetaData Buffer. This code should be never reached. Please contact us for further assistance.");
                metaDataBuffer = sharedMetaDataBuffer;
                metaBufferIndex = 0;
            }
        }

        public static TgsInstance.InstanceMetaDataGPU GetInstanceMetaDataSlotData(int index)
        {
            return sharedMetaDataBufferCpu[index];
        }

        #endregion

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void SafeInitialize()
        {
            if (_isInitialize) 
                return;
            
            _isInitialize = true;
            _activeGlobalDensityValue = TgsGlobalSettings.GlobalDensityScale;
            _activeGlobalChunkSize = TgsGlobalSettings.GlobalChunkSize;
                
            Enable = true;
            RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
#endif

            // Initialize Global Buffers
            sharedMetaDataBuffer ??= new ComputeBuffer(
                SharedBufferMaxSlots,
                TgsInstance.InstanceMetaDataGPU.Stride);

            sharedPlacedGrassCount ??= new ComputeBuffer(
                SharedBufferMaxSlots,
                sizeof(int),
                ComputeBufferType.Raw);

            sharedNoiseSeetingsBuffer ??= new ComputeBuffer(
                SharedBufferMaxSlots * TgsPreset.NoiseSettingGPU.MaxCount,
                TgsPreset.NoiseSettingGPU.Stride);
            allNativeInstaces = new NativeHashMap<ulong, TgsInstanceProxy>(1, Allocator.Persistent);
        }

        private static void DoFrame(List<Camera> renderingCameras, Material renderingMaterial)
        {
            Profiler.BeginSample("DoFrame");

            var allUids = allNativeInstaces.GetKeyArray(Allocator.TempJob);
            
            LocalKeyword alphaClip =
                renderingMaterial.shader.keywordSpace.FindKeyword(alphaClipKeywordString);
            if (alphaClip.isValid)
            {
                renderingMaterial.SetKeyword(alphaClip, TgsGlobalSettings.XrPassthroughAlphaFix);
            }

            renderingMaterial.SetInteger(TgsUseAlphaToCoverage,
                TgsGlobalSettings.XrPassthroughAlphaFix ? 0 : 1);
                
            Profiler.BeginSample("shouldMarkAllInstancesDirty");
            // Check if the global density has changed.
            bool shouldMarkAllInstancesDirty =
                !Mathf.Approximately(_activeGlobalDensityValue, TgsGlobalSettings.GlobalDensityScale) ||
                _activeGlobalChunkSize != TgsGlobalSettings.GlobalChunkSize;
            _activeGlobalDensityValue = TgsGlobalSettings.GlobalDensityScale;
            _activeGlobalChunkSize = TgsGlobalSettings.GlobalChunkSize;
            
            if (shouldMarkAllInstancesDirty)
            {
                foreach (var instance in TgsInstance.AllInstances)
                {
                    instance.Value.MarkGeometryDirty();
                }
            }
            Profiler.EndSample();
            
            NativeArray<TgsLodCameraData> lodCameraData = new NativeArray<TgsLodCameraData>(renderingCameras.Count, Allocator.TempJob);
            for (int i = 0; i < renderingCameras.Count; i++)
            {
                lodCameraData[i] = new TgsLodCameraData(renderingCameras[i]);
            }
            
            Profiler.BeginSample("Collect Colliders");
            
            List<TgsCollider> activeColliders = TgsCollider._activeColliders;
            NativeArray<float4> activeCollidersNative =
                new NativeArray<float4>(activeColliders.Count, Allocator.TempJob);
            
            for (int i = 0; i < activeColliders.Count; i++)
            {
                TgsCollider collider = activeColliders[i];
                Transform transform = collider.transform;

                float4 colliderXyzw = float4.zero;
                colliderXyzw.xyz = transform.position;
                colliderXyzw.w = collider.radius * collider.radius * transform.lossyScale.sqrMagnitude;
                activeCollidersNative[i] = colliderXyzw;
            }
            Profiler.EndSample();
            

            var collidersOut =
                new NativeArray<float4>(allUids.Length * MaxColliderPerInstance, Allocator.TempJob);
            
            var vertexCountOut = new NativeArray<int>(allUids.Length * renderingCameras.Count, Allocator.TempJob);
            
            var toClearUids = new NativeList<ulong>(allUids.Length, Allocator.TempJob);
            var toBakeUids = new NativeList<ulong>(allUids.Length, Allocator.TempJob);
            var toRenderUids = new NativeList<ulong>(allUids.Length, Allocator.TempJob);
            
            TgsPreRenderJob job = new()
            {
                Instances = allNativeInstaces,
                InstanceKeys = allUids,
                CollidersIn = activeCollidersNative,
                CollidersOut = collidersOut,
                Cameras = lodCameraData,
                LodScale = TgsGlobalSettings.GlobalLodScale,
                LodFalloffExp = TgsGlobalSettings.GlobalLodFalloffExponent,
                LodCutoff = Mathf.Max(TgsGlobalSettings.GlobalLodCutoff, TgsGlobalSettings.MinLodCutoff),
                ChunkCullTimeout = TgsGlobalSettings.ChunkCullTimeout,
                RealtimeSinceStartUp = Time.realtimeSinceStartupAsDouble,
                VertexCountOut = vertexCountOut,
                ToBakeUids = toBakeUids.AsParallelWriter(),
                ToClearUids = toClearUids.AsParallelWriter(),
                ToRenderWriteUids = toRenderUids.AsParallelWriter(),
            };
            
            Profiler.BeginSample("Execute PreRendering Job");
            job.Schedule(allUids.Length, 64).Complete();
            Profiler.EndSample();

            Profiler.BeginSample("Clear Instances");
            foreach (var clearUid in toClearUids)
            {
                TgsInstance.AllInstances[clearUid].Clear();
            }
            Profiler.EndSample();
                
            #region Baking
            Profiler.BeginSample("Initialize Shared Meta Data Buffer");
            Stopwatch.Restart();
            inFlightBakingInstances.Clear();
            // Clear and initialize the meta data buffer.
            sharedMetaBufferUsedSlots = 0;
            Array.Fill(sharedMetaDataBufferCpu, new TgsInstance.InstanceMetaDataGPU
            {
                boundsMinX = 0xFFFFFFFF,
                boundsMinY = 0xFFFFFFFF,
                boundsMinZ = 0xFFFFFFFF
            });
            sharedPlacedGrassCount.SetData(sharedPlacedGrassCountCpu);

            Array.Fill<uint>(sharedPlacedGrassCountCpu, 0);
            sharedMetaDataBuffer.SetData(sharedMetaDataBufferCpu);

            Profiler.EndSample();
            Profiler.BeginSample("Bake Placement Mesh and Meta Data");
            foreach (var uid in toBakeUids)
            {
                TgsInstance instance = TgsInstance.AllInstances[uid];
                // We only apply the time budget in Game-Mode. Otherwise, Undo and other Actions might take too long to finish in an acceptable manner.
                if ((!(Stopwatch.ElapsedMilliseconds < TgsGlobalSettings.GlobalBakingTimeBudget) &&
                     Application.isPlaying)) break;
                
                // Check if the Instance would have a free slot in the shared meta buffer. Otherwise, starting to bake is pointless.

                bool allowBake = true;
#if TASTY_GRASS_SHADER_DEBUG
                    allowBake = !TgsGlobalSettings.DebugFreezeBakes;
#endif
                if (sharedMetaBufferUsedSlots + 1 < SharedBufferMaxSlots && allowBake)
                {
                    Stopwatch.Start();
                    if (instance.BakeNextRecipeStep1())
                    {
                        inFlightBakingInstances.Add(instance);
                    }
                    Stopwatch.Stop();
                }
            }
            Profiler.EndSample();

            // read back the meta pass data in a batch.
            if (sharedMetaBufferUsedSlots > 0)
            {
                Profiler.BeginSample("Readback Meta Data After First Pass");
                sharedMetaDataBuffer.GetData(sharedMetaDataBufferCpu);
                Profiler.EndSample();
                Profiler.BeginSample("Baking Grass To Placement Mesh");
                foreach (TgsInstance instance in inFlightBakingInstances)
                {
                    instance.BakeNextRecipeStep2();
                }
                Profiler.EndSample();
                Profiler.BeginSample("Readback Amount Of Spawned Grass");
                // Read back the amount of spawned grass.
                sharedPlacedGrassCount.GetData(sharedPlacedGrassCountCpu);
                Profiler.EndSample();
            }

            Profiler.BeginSample("Finalize Baking");
            foreach (var inFlightBakingInstance in inFlightBakingInstances)
            {
                inFlightBakingInstance.BakeNextRecipeStep3();
            }
            Profiler.EndSample();

            #endregion

            #region Rendering
            
            Profiler.BeginSample("Submit Drawcalls");

            _colliderBuffer ??= new Vector4[MaxColliderPerInstance];
            
            for (int cameraIndex = 0; cameraIndex < renderingCameras.Count; cameraIndex++)
            {
                Camera camera = renderingCameras[cameraIndex];
                bool isPreviewCamera = camera.cameraType == CameraType.Preview;
                int cameraSceneIndex = camera.gameObject.scene.buildIndex;
                bool shouldRender = !isPreviewCamera;

                // Handle Single Pass VR (if the package is installed)
                bool singlePassVr = false;
    #if TGS_UNITY_XR_MODULE_INSTALLED
                singlePassVr = camera.stereoEnabled &&
                                    UnityEngine.XR.XRSettings.stereoRenderingMode != UnityEngine.XR.XRSettings.StereoRenderingMode.MultiPass;
    #endif
                if (!shouldRender)
                {
                    continue;
                }
                
                for (int index = 0; index < toRenderUids.Length; index++)
                {
                    ulong uid = toRenderUids[index];
                    TgsInstance instance = TgsInstance.AllInstances[uid];
                    
                    if (instance.Hide) 
                        continue; // TODO filter out invisible instances earlier.
                    
                    TgsInstanceProxy instanceProxy = allNativeInstaces[uid];
                    int vertexCount =
                        vertexCountOut[instanceProxy.outWriteIndex * renderingCameras.Count + cameraIndex];

                    if (vertexCount == 0)
                    {
                        continue;
                    }
                    if (instanceProxy.outColliderCount > 0)
                    {
                        int baseIndex = instanceProxy.outWriteIndex * MaxColliderPerInstance;
                        for (int i = 0; i < instanceProxy.outColliderCount; i++)
                        {
                            _colliderBuffer[i] = collidersOut[baseIndex + i];
                        }
                    }
                    
                    instance.DrawAndUpdateMaterialPropertyBlock(vertexCount, camera,
                        _colliderBuffer,
                        instanceProxy.outColliderCount, singlePassVr, renderingMaterial);
                }
            }
            Profiler.EndSample();
            #endregion
            
            allUids.Dispose();
            vertexCountOut.Dispose();
            collidersOut.Dispose();
            activeCollidersNative.Dispose();
            lodCameraData.Dispose();
            toBakeUids.Dispose();
            toClearUids.Dispose();
            toRenderUids.Dispose();
            
            Profiler.EndSample();
        }
        private static void OnBeginContextRendering(ScriptableRenderContext context, List<Camera> renderingCameras)
        {
            if (!Enable) 
                return;

        
            Profiler.BeginSample("TGS - OnBeginFrameRendering");
            Material renderingMaterial;

            if (tgsComputeShader == null)
            {
                tgsComputeShader = Resources.Load<ComputeShader>("Shaders/TastyGrassShaderCompute");
            }

            if (tgsComputeShader)
            {
                if (PipelineDefaultRenderingMaterial == null)
                {
#if TGS_URP_INSTALLED
                    PipelineDefaultRenderingMaterial = Resources.Load<Material>(UrpDefaultMaterialPath);
#endif

#if TGS_HDRP_INSTALLED
                    if (PipelineDefaultRenderingMaterial == null)
                    {
                        PipelineDefaultRenderingMaterial = Resources.Load<Material>(HdrpDefaultMaterialPath);
                    }
#endif
                }

                renderingMaterial = TgsGlobalSettings.CustomRenderingMaterial != null
                    ? TgsGlobalSettings.CustomRenderingMaterial
                    : PipelineDefaultRenderingMaterial;
                
                if (renderingMaterial != null && renderingMaterial.shader)
                {
                    DoFrame(renderingCameras, renderingMaterial);
                    ClearGpuMeshCache();
                } 
                else
                {
                    missingRenderMaterialErrorCount++;
                    if (missingRenderMaterialErrorCount == 5)
                    {
                        Debug.LogError(
                            $"Tasty Grass Shader: Please run the Tasty Grass Setup ({SetupMenuItem}). (No rendering material found or it has no shader. Tasty Grass Shader will not work.)");
                    }
                }
            }
            else
            {

                missingResoucesErrorCount++;
                if (missingResoucesErrorCount == 5)
                {
                    Debug.LogError(
                        "Tasty Grass Shader: unable to locate resources. Ensure that the plugin is installed correctly and that all files in the Resource folder are present. Tasty Grass Shader will not work.");
                }
            }
            Profiler.EndSample();
        }


        public static void Release()
        {
            sharedMetaDataBuffer?.Release();
            sharedPlacedGrassCount?.Release();
            sharedNoiseSeetingsBuffer?.Release();
            
            // TgsInstance.Release() will also remove itself from the list, so we need a copy. 
            var allInstancesCopy = new List<TgsInstance>(TgsInstance.AllInstances.Values);
            foreach (TgsInstance instance in allInstancesCopy)
            {
                instance.Release();
            }
            
            if(allNativeInstaces.IsCreated)
                allNativeInstaces.Dispose();
        }

        static void OnBeforeAssemblyReload()
        {
            // Used in editor to prevent memory leaks.
            Release();
        }
        
        public struct TgsInstanceProxy
        {
            public Bounds looseBounds;
            public bool isGeometryDirty;
            
            public float lodBiasByPreset;

            public int bladeCount;

            public double LastRenderedTimestamp;
            public ulong globlaUid;

            // outputs
            public int outColliderCount;
            public int outWriteIndex;
            
            public TgsInstanceProxy(TgsInstance instance, ulong inGlobalUid)
            {
                looseBounds = instance.looseBounds;
                lodBiasByPreset = instance.GetActiveTgsInstanceRecipeBaseLodFactor();
                bladeCount = instance.actualBladeCount;
                globlaUid = inGlobalUid;
                isGeometryDirty = instance.isGeometryDirty;
                LastRenderedTimestamp = Time.timeSinceLevelLoadAsDouble;
                outColliderCount = 0;
                outWriteIndex = -1;
            }
        }
        
        struct TgsLodCameraData
        {
            public float3 positionWs;
            public float fovScalingFactor;

            public TgsLodCameraData(Camera camera)
            {
                if (camera != null)
                {
                    positionWs = camera.transform.position;
                    fovScalingFactor = TgsInstance.ComputeCameraFovScalingFactor(camera);
                }
                else
                {
                    Debug.LogError("Camera that is null passed into TGS.");
                    positionWs = new float3(-99999.0f);
                    fovScalingFactor = 0.0f;
                }

            }
        }
        
        [BurstCompile]
        struct TgsPreRenderJob : IJobParallelFor
        {
            private const float ReferenceLodSize = 32.0f;
            
            /// <summary>
            /// All instances as native structs to process.
            /// </summary>
            [NativeDisableParallelForRestriction]
            public NativeHashMap<ulong, TgsInstanceProxy> Instances;
            
            [ReadOnly]
            public NativeArray<ulong> InstanceKeys;

            /// <summary>
            /// A flat 2D array with all colliders to a specific chunk.
            /// </summary>
            [WriteOnly] [NativeDisableParallelForRestriction]
            public NativeArray<float4> CollidersOut;
            
            /// <summary>
            /// A flat 2D array with the vertex cound to render the specific chunk for each camera.
            /// </summary>
            [WriteOnly] [NativeDisableParallelForRestriction]
            public NativeArray<int> VertexCountOut;

            /// <summary>
            /// A list of all colliders to process.
            /// </summary>
            [ReadOnly] public NativeArray<float4> CollidersIn;

            /// <summary>
            /// All cameras that will render TGS.
            /// </summary>
            [ReadOnly] public NativeArray<TgsLodCameraData> Cameras;

            [WriteOnly] public NativeList<ulong>.ParallelWriter ToClearUids;
            [WriteOnly] public NativeList<ulong>.ParallelWriter ToBakeUids;
            [WriteOnly] public NativeList<ulong>.ParallelWriter ToRenderWriteUids;
            
            [ReadOnly] public float LodScale, LodFalloffExp, LodCutoff;
            [ReadOnly] public float ChunkCullTimeout; // Amount of seconds that a chunk is allowed to be not rendered due to LoD before cleared.
            [ReadOnly] public double RealtimeSinceStartUp;
            // Using Bounds.DistanceSqr might not be burst compatible, so we write it ourselves.
            static float DistanceToAabbSqr(float3 pos, float3x2 aabb)
            {
                float3 pointInAABB = math.clamp(pos, aabb.c0, aabb.c1);
                return math.distancesq(pos, pointInAABB);
            }

            public void Execute(int instanceIndex)
            {
                ulong uid = InstanceKeys[instanceIndex];
                TgsInstanceProxy instance = Instances[uid];
                float3x2 instanceAabb = new(instance.looseBounds.min, instance.looseBounds.max);

                float outMaxLodFactor = -1.0f;
                instance.outColliderCount = 0;
                instance.outWriteIndex = instanceIndex;
               
                int maxRenderingBladeCount = 0;
                for (int cameraIndex = 0; cameraIndex < Cameras.Length; cameraIndex++)
                {
                    TgsLodCameraData tgsLodCamera = Cameras[cameraIndex];
                    // Compute LOD based on approximate screen size.
                    float distance = math.sqrt(DistanceToAabbSqr(tgsLodCamera.positionWs, instanceAabb));
                    float objectPixelHeightApprox = ReferenceLodSize / distance / tgsLodCamera.fovScalingFactor;

                    float lod01 = objectPixelHeightApprox * LodScale * instance.lodBiasByPreset;
                    lod01 = lod01 > LodCutoff ? lod01 : 0.0f;
                    if (lod01 > 0.0f)
                    {
                        lod01 = math.pow(lod01, LodFalloffExp);
                        lod01 = math.saturate(lod01);
                    
                        outMaxLodFactor = math.max(outMaxLodFactor, lod01);
                    
                        int renderingBladeCount = (int)math.round( lod01 * instance.bladeCount);
                        maxRenderingBladeCount = math.max(renderingBladeCount, maxRenderingBladeCount);
                    
                        VertexCountOut[instanceIndex * Cameras.Length + cameraIndex] = renderingBladeCount * 3;
                    }
                }
                
                instance.outColliderCount = 0;
                if (maxRenderingBladeCount > 0)
                {
                    int baseIndex = instanceIndex * MaxColliderPerInstance;

                    for (int colliderIdx = 0;
                         colliderIdx < CollidersIn.Length && instance.outColliderCount < MaxColliderPerInstance;
                         colliderIdx++)
                    {
                        float4 collider = CollidersIn[colliderIdx];
                        float3 colliderCenter = collider.xyz;
                        float colliderRadiusSqr = collider.w;

                        if (DistanceToAabbSqr(colliderCenter, instanceAabb) < colliderRadiusSqr)
                        {
                            CollidersOut[baseIndex + instance.outColliderCount] =
                                new float4(colliderCenter.x, colliderCenter.y, colliderCenter.z,
                                    math.sqrt(colliderRadiusSqr));

                            instance.outColliderCount++;
                        }
                    }
                }

                if (outMaxLodFactor > 0.0f)
                {
                    instance.LastRenderedTimestamp = RealtimeSinceStartUp;
                    if (instance.isGeometryDirty)
                    {
                        ToBakeUids.AddNoResize(instance.globlaUid);
                    }
                    if (instance.bladeCount > 0)
                    {
                        ToRenderWriteUids.AddNoResize(instance.globlaUid);
                    }
                }
                else
                {
                    if (instance.bladeCount != 0)
                    {
                        if (RealtimeSinceStartUp - instance.LastRenderedTimestamp >= ChunkCullTimeout)
                        {
                            ToClearUids.AddNoResize(instance.globlaUid);
                        }
                    }
                }
                // TODO: writing back every time might stress the memory too much.
                Instances[uid] = instance; // TODO: seperate in/out?
            }
        }
    }
}
