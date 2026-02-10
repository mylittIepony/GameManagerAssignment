using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace SymmetryBreakStudio.TastyGrassShader
{
    /// <summary>
    ///     The core of the Tasty Grass Shader. For most cases, the wrapper components GrasFieldMesh.cs and
    ///     GrassFieldTerrain.cs are sufficient.
    /// 
    ///     If you want to manage the grass differently, use this class directly.
    ///     Note that baking or rendering happens in TgsManager.cs.
    ///     <remarks>
    ///     You only need to create an instance once, it is intended to be recyclable for minimal Garbage Collection overhead.</remarks>
    /// </summary>
    public class TgsInstance
    {
        public static readonly Dictionary<ulong, TgsInstance> AllInstances = new();
        
#if TASTY_GRASS_SHADER_DEBUG
        /// <summary>
        ///     Enable to verify that the compute shader generated unique blades, and thus no memory is wasted.
        ///     The blade format must be GrassNodeReference.
        /// </summary>
        const bool ValidateNoDuplicateBlades = false;
#endif

        // NOTE: these also must be adjusted with TastyGrassShaderCommon.hlsl
        const int GrassNodeCompressedStride = sizeof(uint) * 4;
        const int GrassNodeReferenceStride = sizeof(float) * 15;

        const int GrassNodeStride = GrassNodeCompressedStride;
        const int BladesPerTriangleUpperLimit = 4096;

        const float GrassMaxVertexRangeSize = 2.0f;

        const float MaxGrassRootOffset = 4.0f;

        const int SizeofPlacementTriangle =
                sizeof(float) * 3 * 3 //positions
                + sizeof(float) * 3 * 3 //normals
                + sizeof(float) * 3 // density
                + sizeof(float) * 3 * 2 // UVs
                + sizeof(float) // triangle area
                + sizeof(float) * 3 // geometric_normal
                + sizeof(int) //buffer offset
                + sizeof(int) //blade count
            ;
        
        

        static readonly int PlacementVertices = Shader.PropertyToID("_PlacementVertices");
        static readonly int PlacementIndices = Shader.PropertyToID("_PlacementIndices");
        static readonly int GrassNodePrimitivesAppend = Shader.PropertyToID("_GrassFieldPrimitivesAppend");
        static readonly int IndirectDrawArgs = Shader.PropertyToID("_IndirectDrawArgs");
        static readonly int GrassNodePrimitives = Shader.PropertyToID("_GrassFieldPrimitives");
        static readonly int PositionBoundMin = Shader.PropertyToID("_PositionBoundMin");
        static readonly int PositionBoundMax = Shader.PropertyToID("_PositionBoundMax");

        static readonly int MeshChunkBoundsMin = Shader.PropertyToID("_MeshChunkBoundsMin");
        static readonly int MeshChunkBoundsMax = Shader.PropertyToID("_MeshChunkBoundsMax");


        static readonly int ObjectToWorld = Shader.PropertyToID("_ObjectToWorld");
        static readonly int Heightmap = Shader.PropertyToID("_Heightmap");
        static readonly int HeightmapResolutionXy = Shader.PropertyToID("_HeightmapResolutionXy");
        static readonly int HeightmapChunkOffsetSize = Shader.PropertyToID("_HeightmapChunkOffsetSize");
        static readonly int DensityUseChannelMask = Shader.PropertyToID("_DensityUseChannelMask");


        static readonly int DensityMapChannelMask = Shader.PropertyToID("_DensityMapChannelMask");
        static readonly int DensityFromTextureOnly = Shader.PropertyToID("_DensityFromTextureOnly");

        static readonly int DensityMap = Shader.PropertyToID("_DensityMap");
        static readonly int DensityMapUvFromHeightmapIdx = Shader.PropertyToID("_DensityMapUvFromHeightmapIdx");
        static readonly int NoiseParams = Shader.PropertyToID("_NoiseParams");


        internal static readonly int SphereCollider = Shader.PropertyToID("_SphereCollider");
        internal static readonly int SphereColliderCount = Shader.PropertyToID("_SphereColliderCount");

        static readonly int PlacementTrianglesR = Shader.PropertyToID("_PlacementTrianglesR");
        static readonly int PlacementTriangleCount = Shader.PropertyToID("_PlacementTriangleCount");
        static readonly int UsedPlacementTriangleCount = Shader.PropertyToID("_UsedPlacementTriangleCount");
        static readonly int PlacementTrianglesAppend = Shader.PropertyToID("_PlacementTrianglesAppend");
        static readonly int MetaDataRW = Shader.PropertyToID("_MetaDataRW");
        static readonly int MetaDataIndex = Shader.PropertyToID("_MetaDataIndex");


        static readonly int PlacementTriangleOffset = Shader.PropertyToID("_PlacementTriangleOffset");

        static readonly int ColorMap = Shader.PropertyToID("_ColorMap");
        static readonly int ColorMapBlend = Shader.PropertyToID("_ColorMapBlend");
        static readonly int ColorMapSt = Shader.PropertyToID("_ColorMapST");

        static readonly int PlacementNormals = Shader.PropertyToID("_PlacementNormals");
        static readonly int PlacementColors = Shader.PropertyToID("_PlacementColors");
        static readonly int PlacementUVs = Shader.PropertyToID("_PlacementUVs");

        /// <summary>
        ///     Holds the actual grass geometry.
        /// </summary>
        internal ComputeBuffer _grassPrimitivesBuffer;

        private int _innerActualBladeCount;
        
        /// <summary>
        /// The amount of blades that actually got written, in contrast to _grassPrimitivesBuffer.Count, which just gives us the capacity.
        /// </summary>
        public int actualBladeCount {
            get => _innerActualBladeCount;
            private set
            {
                _innerActualBladeCount = value;
                if (TgsManager.allNativeInstaces.IsCreated)
                {
                    var instance = TgsManager.allNativeInstaces[globalUid];
                    instance.bladeCount = value;
                    TgsManager.allNativeInstaces[globalUid] = instance;
                }

            }
            
        }
        
        /// <summary>
        /// The index of the instance during baking.
        /// </summary>
        private int _instanceMetaDataIndex;

        internal ComputeBuffer _externalMetaDataBuffer;

        bool _hasFinishedBaking;

        /// <summary>
        ///     Per-instance settings for the material, such as ground color, wind speed, ...
        /// </summary>
        internal MaterialPropertyBlock _materialPropertyBlock;

        internal ComputeBuffer _placementTriangleBuffer;

        /// <summary>
        ///     If true, the instance will not be rendered.
        /// </summary>
        public bool Hide;

        internal TgsInstanceRecipe nextTgsInstanceRecipe;

        public TgsWindSettings UsedWindSettings;

        private static ulong globalUidCounter = 0;
        private ulong globalUid;
        public TgsInstance()
        {
            TgsGlobalStatus.instances++;
            globalUid = globalUidCounter++;
            AllInstances.Add(globalUid, this);
            if(TgsManager.allNativeInstaces.IsCreated)
                TgsManager.allNativeInstaces.Add(globalUid, new TgsManager.TgsInstanceProxy(this, globalUid));
        }
        
        internal TgsInstanceRecipe activeTgsInstanceRecipe;

        /// <summary>
        /// Fast-Path for getting baseLodFactor from the preset. This is used by TgsInstancePreRendering, because it uses activeTgsInstanceRecipe, it will make a huge copy of that struct first.
        /// </summary>
        /// <returns></returns>
        public float GetActiveTgsInstanceRecipeBaseLodFactor()
        {
            float output = 0.0f;
            if (activeTgsInstanceRecipe.Settings.preset)
            {
                output = activeTgsInstanceRecipe.Settings.preset.baseLodFactor;
            }
            else
            {
                if (nextTgsInstanceRecipe.Settings.preset)
                {
                    output = nextTgsInstanceRecipe.Settings.preset.baseLodFactor;
                }
            }
        
            return output;
        }

        private bool innerIsGeometryDirty;

        public bool isGeometryDirty
        {
            get => innerIsGeometryDirty;
            private set
            {
                innerIsGeometryDirty = value;
                if (TgsManager.allNativeInstaces.IsCreated)
                {
                    var instance = TgsManager.allNativeInstaces[globalUid];
                    instance.isGeometryDirty = value;
                    // Also refresh the lod factor here, because isGeometryDirty might be changed due to an preset change.
                    instance.lodBiasByPreset = GetActiveTgsInstanceRecipeBaseLodFactor();
                    TgsManager.allNativeInstaces[globalUid] = instance;
                }
            }
        }

        public bool isMaterialDirty { get; private set; }


#if TASTY_GRASS_SHADER_DEBUG
        public GameObject debugUsingGameObject;
#endif
        /// <summary>
        ///     AABB that enclose the grass.
        /// </summary>
        public Bounds tightBounds { get; private set; }

        /// <summary>
        ///     AABB that encloses the placement mesh.
        /// </summary>
        ///
        private Bounds innerLooseBounds;
        public Bounds looseBounds
        {
            get => innerLooseBounds;
            private set
            {
                innerLooseBounds = value;
                if (TgsManager.allNativeInstaces.IsCreated)
                {
                    var instance = TgsManager.allNativeInstaces[globalUid];
                    instance.looseBounds = value;
                    TgsManager.allNativeInstaces[globalUid] = instance;
                }

            }
        }

#if TASTY_GRASS_SHADER_DEBUG
        public Bounds debugMeshChunk;
#endif
        /// <summary>
        ///     The layer used for rendering.
        /// </summary>
        public int UnityLayer = 0;

        /// <summary>
        ///     Marks the instance for re-bake. Call this function after changing settings like the preset or settings.
        /// </summary>
        public void MarkGeometryDirty()
        {
            isGeometryDirty = true;
            if (_hasFinishedBaking)
            {
                TgsGlobalStatus.instancesReady--;
                _hasFinishedBaking = false;
            }
            
        }

        /// <summary>
        ///     Marks the Instance for updating any purely material related properties (smoothness, texture, etc.).
        /// </summary>
        public void MarkMaterialDirty()
        {
            isMaterialDirty = true;
        }
        

        public bool IsRenderable => _grassPrimitivesBuffer is { count: > 0 };

        internal bool BakeNextRecipeStep1()
        {
            ComputeShader tgsComputeShader = TgsManager.tgsComputeShader;
            isGeometryDirty = false;

            if (activeTgsInstanceRecipe.Settings.preset != null)
            {
                activeTgsInstanceRecipe.Settings.preset.SetDirtyOnChangeList.Remove(this);
            }

            if (nextTgsInstanceRecipe.Settings.preset != null)
            {
                nextTgsInstanceRecipe.Settings.preset.SetDirtyOnChangeList.Add(this);
            }

            _materialPropertyBlock ??= new MaterialPropertyBlock();


            if (nextTgsInstanceRecipe.Settings.preset == null)
            {
                return false;
            }


            TgsManager.GetInstanceMetaDataSlot(out _externalMetaDataBuffer, out _instanceMetaDataIndex);
            tgsComputeShader.SetInt(MetaDataIndex, _instanceMetaDataIndex);
            nextTgsInstanceRecipe.Settings.preset.ApplyLayerSettingsToBuffer(TgsManager.sharedNoiseSeetingsBuffer,
                nextTgsInstanceRecipe.Settings, _instanceMetaDataIndex);


            int csMeshPassId = tgsComputeShader.FindKernel("MeshPass");
            int csTerrainPassId = tgsComputeShader.FindKernel("TerrainPass");
            int csBakePassId = tgsComputeShader.FindKernel("BakePass");

            switch (nextTgsInstanceRecipe.BakeMode)
            {
                case TgsInstanceRecipe.InstanceBakeMode.FromMesh:
                {

                    GPUMesh gpuMesh = TgsManager.GetOrCreateCachedGPUMesh(nextTgsInstanceRecipe.SharedMesh, null,
                        nextTgsInstanceRecipe.DistributionByVertexColorEnabled);

                    // placement mesh -> compute shader
                    // =============================================================================================================
                    int placementMeshTriangleCount = gpuMesh.indices.count / 3;
                    ApplyMeshParameters(tgsComputeShader, nextTgsInstanceRecipe, gpuMesh);

                    tgsComputeShader.SetBuffer(csMeshPassId, PlacementVertices, gpuMesh.vertices);
                    tgsComputeShader.SetBuffer(csMeshPassId, PlacementNormals, gpuMesh.normals);
                    tgsComputeShader.SetBuffer(csMeshPassId, PlacementColors, gpuMesh.colors);
                    tgsComputeShader.SetBuffer(csMeshPassId, PlacementUVs, gpuMesh.uvs);
                    tgsComputeShader.SetBuffer(csMeshPassId, PlacementIndices, gpuMesh.indices);

                    InitPlacementBuffer(placementMeshTriangleCount);

                    ApplyCommonBuffersAndParameters(tgsComputeShader, nextTgsInstanceRecipe,
                        placementMeshTriangleCount);

                    tgsComputeShader.SetVector(PositionBoundMin, nextTgsInstanceRecipe.WorldSpaceBounds.min);
                    tgsComputeShader.SetVector(PositionBoundMax, nextTgsInstanceRecipe.WorldSpaceBounds.max);

                    // Generative compute shader dispatch
                    // =============================================================================================================
                    tgsComputeShader.SetBuffer(csMeshPassId, PlacementTrianglesAppend, _placementTriangleBuffer);
                    tgsComputeShader.SetBuffer(csMeshPassId, MetaDataRW, _externalMetaDataBuffer);

                    tgsComputeShader.GetKernelThreadGroupSizes(
                        csMeshPassId,
                        out uint csMainKernelThreadCount,
                        out _,
                        out _);

                    int dispatchCount = CeilingDivision(placementMeshTriangleCount, (int)csMainKernelThreadCount);
                    tgsComputeShader.Dispatch(csMeshPassId, dispatchCount, 1, 1);
                }
                    break;
                case TgsInstanceRecipe.InstanceBakeMode.FromHeightmap:
                {
                    ApplyHeightmapParameters(tgsComputeShader, nextTgsInstanceRecipe);
                    tgsComputeShader.SetBuffer(csTerrainPassId, MetaDataRW, _externalMetaDataBuffer);
                    int placementMeshTriangleCount =
                        nextTgsInstanceRecipe.ChunkPixelSize.x * nextTgsInstanceRecipe.ChunkPixelSize.y *
                        2; // * 2, because two triangles per pixel.

                    InitPlacementBuffer(placementMeshTriangleCount);
                    ApplyCommonBuffersAndParameters(tgsComputeShader, nextTgsInstanceRecipe,
                        placementMeshTriangleCount);

                    // Generate placing triangles from heightmap
                    // =============================================================================================================
                    {
                        tgsComputeShader.SetVector(PositionBoundMin, nextTgsInstanceRecipe.WorldSpaceBounds.min);
                        tgsComputeShader.SetVector(PositionBoundMax, nextTgsInstanceRecipe.WorldSpaceBounds.max);
                        tgsComputeShader.SetBuffer(csTerrainPassId, PlacementTrianglesAppend, _placementTriangleBuffer);

                        tgsComputeShader.SetMatrix(ObjectToWorld, nextTgsInstanceRecipe.LocalToWorldMatrix);

                        tgsComputeShader.GetKernelThreadGroupSizes(csTerrainPassId, out uint xThreads, out _, out _);

                        int dispatchCount = CeilingDivision(placementMeshTriangleCount, (int)xThreads);
                        tgsComputeShader.Dispatch(csTerrainPassId, dispatchCount, 1, 1);
                    }
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }

        internal void BakeNextRecipeStep2()
        {
            ComputeShader tgsComputeShader = TgsManager.tgsComputeShader;
            InstanceMetaDataGPU instanceData = TgsManager.GetInstanceMetaDataSlotData(_instanceMetaDataIndex);
            int placementTriangleCount = (int)instanceData.placementTrianglesCount;
            int csBakePassId = tgsComputeShader.FindKernel("BakePass");
            ApplyCommonBuffersAndParameters(tgsComputeShader, nextTgsInstanceRecipe, placementTriangleCount);
            switch (nextTgsInstanceRecipe.BakeMode)
            {
                case TgsInstanceRecipe.InstanceBakeMode.FromMesh:
                    GPUMesh gpuMesh = TgsManager.GetOrCreateCachedGPUMesh(nextTgsInstanceRecipe.SharedMesh, null,
                        nextTgsInstanceRecipe.DistributionByVertexColorEnabled);
                    ApplyMeshParameters(tgsComputeShader, nextTgsInstanceRecipe, gpuMesh);
                    break;
                case TgsInstanceRecipe.InstanceBakeMode.FromHeightmap:
                    ApplyHeightmapParameters(tgsComputeShader, nextTgsInstanceRecipe);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            #region Unpack Tight AABB

            Vector3 minBounds = UnpackVector3_32Bit(
                instanceData.boundsMinX,
                instanceData.boundsMinY,
                instanceData.boundsMinZ,
                nextTgsInstanceRecipe.WorldSpaceBounds.min,
                nextTgsInstanceRecipe.WorldSpaceBounds.max);

            Vector3 maxBounds = UnpackVector3_32Bit(
                instanceData.boundsMaxX,
                instanceData.boundsMaxY,
                instanceData.boundsMaxZ,
                nextTgsInstanceRecipe.WorldSpaceBounds.min,
                nextTgsInstanceRecipe.WorldSpaceBounds.max);

            Bounds gpuComputedBounds = new();
            gpuComputedBounds.SetMinMax(minBounds, maxBounds);
            // Rendering Bounds
            // =============================================================================================================
            // NOTE: can't use Expand() on looseGrassFieldBounds directly, since its a get/set thing and the new value will never be written.
            gpuComputedBounds.Expand(MaxGrassRootOffset);
            tightBounds = gpuComputedBounds;


            float outTightBoundsMaxSideLength = Mathf.Max(tightBounds.size.x,
                Mathf.Max(tightBounds.size.y, tightBounds.size.z));

            _materialPropertyBlock.SetVector(PositionBoundMin, tightBounds.min);
            _materialPropertyBlock.SetVector(PositionBoundMax, tightBounds.max);

            tgsComputeShader.SetVector(PositionBoundMin, tightBounds.min);
            tgsComputeShader.SetVector(PositionBoundMax, tightBounds.max);
            tightBounds = gpuComputedBounds;

            #endregion

#if TASTY_GRASS_SHADER_DEBUG
            BladeCapacity = (int)instanceData.estMaxBlades;
            UsedPlacementTriangles = (int)instanceData.placementTrianglesCount;
#endif
            Texture2D densityMap;
            bool densityFromTextureOnly;
            switch (nextTgsInstanceRecipe.BakeMode)
            {
                case TgsInstanceRecipe.InstanceBakeMode.FromMesh:
                    densityMap = null;
                    densityFromTextureOnly = false;
                    break;
                case TgsInstanceRecipe.InstanceBakeMode.FromHeightmap:
                    densityMap = (Texture2D)nextTgsInstanceRecipe.DistributionTexture;
                    densityFromTextureOnly = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            int estMaxBlades = (int)instanceData.estMaxBlades;
            bool shouldBake = placementTriangleCount > 0 && estMaxBlades > 0;

            if (shouldBake)
            {
                if (_grassPrimitivesBuffer == null || _grassPrimitivesBuffer.count != estMaxBlades)
                {
                    _grassPrimitivesBuffer?.Release();
                    _grassPrimitivesBuffer = new ComputeBuffer(
                        estMaxBlades,
                        GrassNodeStride,
                        ComputeBufferType.Append);
                }

                _grassPrimitivesBuffer.SetCounterValue(0);

                // Dispatch Bake Pass
                // =========================================================================================================

                tgsComputeShader.SetBuffer(csBakePassId, NoiseParams, TgsManager.sharedNoiseSeetingsBuffer);
                tgsComputeShader.SetBuffer(csBakePassId, PlacementTrianglesR, _placementTriangleBuffer);
                tgsComputeShader.SetBuffer(csBakePassId, GrassNodePrimitivesAppend, _grassPrimitivesBuffer);

                tgsComputeShader.SetTexture(csBakePassId, DensityMap,
                    densityMap == null ? Texture2D.whiteTexture : densityMap);
                tgsComputeShader.SetInt(UsedPlacementTriangleCount, placementTriangleCount);
                tgsComputeShader.SetFloat(DensityFromTextureOnly, densityFromTextureOnly ? 1.0f : 0.0f);

                // Allows to handle more than 65535 triangles.
                // TODO: doesn't always work.
                const int maxThreadsPerDispatch = 65535;
                tgsComputeShader.GetKernelThreadGroupSizes(csBakePassId, out uint xThreads, out _, out _);

                int trianglesToProcess = placementTriangleCount;
                int loopIterations = 0;
                const int maxIterations = 100;
                while (trianglesToProcess > 0 && loopIterations < maxIterations)
                {
                    tgsComputeShader.SetInt(PlacementTriangleOffset, loopIterations * maxThreadsPerDispatch);
                    tgsComputeShader.Dispatch(csBakePassId, Mathf.Min(trianglesToProcess, maxThreadsPerDispatch), 1, 1);
                    loopIterations++;
                    trianglesToProcess -= maxThreadsPerDispatch;
                }

                // Copy the amount of spawned grass into a shared buffer.
                ComputeBuffer.CopyCount(_grassPrimitivesBuffer, TgsManager.sharedPlacedGrassCount,
                    sizeof(int) * _instanceMetaDataIndex);
            }
            else
            {
                // Release the blade buffer to indicate that there is nothing to render.
                _grassPrimitivesBuffer?.Release();
                _grassPrimitivesBuffer = null;
            }

            _materialPropertyBlock.SetBuffer(GrassNodePrimitives, _grassPrimitivesBuffer);

            _placementTriangleBuffer?.Release();
            _placementTriangleBuffer = null;
            
        }

        internal void BakeNextRecipeStep3()
        {
            if (_grassPrimitivesBuffer != null)
            {
                actualBladeCount = (int)TgsManager.sharedPlacedGrassCountCpu[_instanceMetaDataIndex];
                if (actualBladeCount == 0)
                {
                    Clear(false);
                }
            }
            else
            {
                Clear(false);
            }
            

#if TASTY_GRASS_SHADER_DEBUG
           // CheckForDuplicates();
#endif

            activeTgsInstanceRecipe = nextTgsInstanceRecipe;

            if (!_hasFinishedBaking)
            {
                TgsGlobalStatus.instancesReady++;
            }

            _hasFinishedBaking = true;
        }


        void ApplyHeightmapParameters(ComputeShader tgsComputeShader, TgsInstanceRecipe recipe)
        {
            int csTerrainPassId = tgsComputeShader.FindKernel("TerrainPass");

            // Setup the heightmap for the compute shader.
            // =============================================================================================================
            tgsComputeShader.SetTexture(csTerrainPassId, Heightmap, recipe.HeightmapTexture);

            tgsComputeShader.SetVector(HeightmapResolutionXy,
                new Vector2(recipe.HeightmapTexture.width, recipe.HeightmapTexture.height));

            tgsComputeShader.SetTexture(csTerrainPassId, DensityMap, recipe.DistributionTexture);
            tgsComputeShader.SetVector(DensityMapChannelMask, recipe.DistributionTextureChannelMask);
            tgsComputeShader.SetInt(DensityUseChannelMask, recipe.DistributionByTextureEnabled ? 1 : 0);

            {
                float heightmapPxToDensityMapUvX =
                    recipe.DistributionTexture.width / (float)recipe.HeightmapTexture.width /
                    recipe.DistributionTexture.width;
                float heightmapPxToDensityMapUvY =
                    recipe.DistributionTexture.height / (float)recipe.HeightmapTexture.height /
                    recipe.DistributionTexture.height;

                tgsComputeShader.SetVector(DensityMapUvFromHeightmapIdx,
                    new Vector4(
                        heightmapPxToDensityMapUvX * recipe.DistributionTextureScaleOffset.x,
                        heightmapPxToDensityMapUvY * recipe.DistributionTextureScaleOffset.y,
                        recipe.DistributionTextureScaleOffset.z,
                        recipe.DistributionTextureScaleOffset.w));
            }

            Debug.Assert(recipe.ChunkPixelSize is { x: > 0, y: > 0 });

            tgsComputeShader.SetVector(HeightmapChunkOffsetSize,
                new Vector4(recipe.HeightmapChunkPixelOffset.x, recipe.HeightmapChunkPixelOffset.y,
                    recipe.ChunkPixelSize.x,
                    recipe.ChunkPixelSize.y));
        }

        void ApplyMeshParameters(ComputeShader tgsComputeShader, TgsInstanceRecipe recipe, GPUMesh gpuMesh)
        {
            tgsComputeShader.SetVector(DensityMapChannelMask,
                nextTgsInstanceRecipe.DistributionByVertexColorMask);
            tgsComputeShader.SetInt(DensityUseChannelMask,
                nextTgsInstanceRecipe.DistributionByVertexColorEnabled && gpuMesh.HasVertexColor ? 1 : 0);
        }

        /// <summary>
        ///     Sets the bake parameters for the next bake. Don't forget to call MarkGeometryDirty() to apply the
        ///     changes.
        /// </summary>
        /// <param name="nextTgsInstanceRecipe"></param>
        public void SetBakeParameters(TgsInstanceRecipe nextTgsInstanceRecipe)
        {
            this.nextTgsInstanceRecipe = nextTgsInstanceRecipe;
            
            looseBounds = nextTgsInstanceRecipe.WorldSpaceBounds; // Set loose bounds here, so they can be used already to estimate the LOD, which is needed to determine the importance of a chunk for baking. 
            if (TgsManager.allNativeInstaces.IsCreated)
            {
                // Also update the lodBias here, because only now, we know which preset is used.
                var instance = TgsManager.allNativeInstaces[globalUid];
                instance.lodBiasByPreset = GetActiveTgsInstanceRecipeBaseLodFactor();
                TgsManager.allNativeInstaces[globalUid] = instance;
            }
            
        }

        internal void DrawAndUpdateMaterialPropertyBlock(int renderingVertexCount,
            Camera renderingCamera, Vector4[] colliderBuffer, int colliderCount, bool singlePassVr,
            Material renderingMaterial)
        {
            Profiler.BeginSample("DrawAndUpdateMaterialPropertyBlock");

            Profiler.BeginSample("Get Preset");
            TgsPreset tgsPreset = activeTgsInstanceRecipe.Settings.preset;
            Profiler.EndSample();   
            // If we have an exception during Drawing, it will teardown the entire frame. Therefore, carefully check for nulls, even if it degrades performance.
            // TODO: these null checks are somewhat expensive.
            Profiler.BeginSample("NullCheck");
            
            if (_grassPrimitivesBuffer != null && UsedWindSettings != null)
            {
                Profiler.EndSample();
                
                _materialPropertyBlock.SetInt(SphereColliderCount, colliderCount);
                Profiler.BeginSample("ApplyCollider");
                
                if (colliderCount > 0)
                {
                    _materialPropertyBlock.SetVectorArray(SphereCollider, colliderBuffer);
                }
                Profiler.EndSample();

                Profiler.BeginSample("isMaterialDirty");
                
                if (isMaterialDirty)
                {
                    isMaterialDirty = false;
                    tgsPreset.ApplyToMaterialPropertyBlock(_materialPropertyBlock);
                }
                Profiler.EndSample();

                Profiler.BeginSample("ApplyWindSettings");
                
                // Always apply the wind settings 
                UsedWindSettings.ApplyToMaterialPropertyBlock(_materialPropertyBlock);
                Profiler.EndSample();
                
#if false
                Profiler.BeginSample("Graphics.DrawProcedural");
                Graphics.DrawProcedural(
                    renderingMaterial,
                    tightBounds,
                    MeshTopology.Triangles,
                    renderingVertexCount,
                    singlePassVr ? 2 : 1,
                    renderingCamera,
                    _materialPropertyBlock,
                    useShadows && TgsGlobalSettings.EnableShadows ? ShadowCastingMode.TwoSided : ShadowCastingMode.Off,
                    true,
                    UnityLayer);
                Profiler.EndSample();
#endif
                
                Profiler.BeginSample("RenderParams");
                RenderParams renderParams = new RenderParams(renderingMaterial)
                {
                    worldBounds = tightBounds,
                    camera = renderingCamera,
                    shadowCastingMode = tgsPreset.castShadows && TgsGlobalSettings.EnableShadows ? ShadowCastingMode.TwoSided : ShadowCastingMode.Off,
                    receiveShadows = true,
                    layer = UnityLayer,
                    matProps = _materialPropertyBlock
                };
                Profiler.EndSample();
                Profiler.BeginSample("Graphics.RenderPrimitives");

                Graphics.RenderPrimitives(
                    renderParams,
                    MeshTopology.Triangles,
                    renderingVertexCount,
                    singlePassVr ? 2 : 1
                    );
                Profiler.EndSample();
                
            }
            else
            { 
#if TASTY_GRASS_SHADER_DEBUG
                Debug.LogError(
                    $"Tasty Grass Shader: Unable to render instance. _bakeOutputBuffer={_grassPrimitivesBuffer}, UsedWindSettings={UsedWindSettings}");
#endif
            }

            Profiler.EndSample();
        }

        public int GetGrassBufferMemoryByteSize()
        {
            if (_grassPrimitivesBuffer != null && _grassPrimitivesBuffer.IsValid())
            {
                #if TASTY_GRASS_SHADER_DEBUG
                if(actualBladeCount == 0 && _grassPrimitivesBuffer.count > 0)
                    Debug.LogError($"_grassPrimitivesBuffer.count {_grassPrimitivesBuffer.count} and bladeCount {actualBladeCount} are out-of-sync . ");
                #endif
                return _grassPrimitivesBuffer.count * _grassPrimitivesBuffer.stride;
            }

            return 0;
        }

        public void Clear(bool markDirty = true)
        {
            _placementTriangleBuffer?.Release();
            _placementTriangleBuffer = null;

            _grassPrimitivesBuffer?.Release();
            _grassPrimitivesBuffer = null;
            
            _materialPropertyBlock?.Clear();
            
            if(markDirty)
                MarkGeometryDirty(); // Re-Schedule the chunk for baking, but let the TgsManger decide if its the right place.
            
            actualBladeCount = 0;
            
        }
        public void Release()
        {
            Clear(false);

            TgsGlobalStatus.instances--;
            if (_hasFinishedBaking)
            {
                TgsGlobalStatus.instancesReady--;
            }

            AllInstances.Remove(globalUid);
            if (TgsManager.allNativeInstaces.IsCreated)
            {
                TgsManager.allNativeInstaces.Remove(globalUid);
            }
            
            if (activeTgsInstanceRecipe.Settings.preset != null)
            {
                activeTgsInstanceRecipe.Settings.preset.SetDirtyOnChangeList.Remove(this);
            }
        }
        
        public static float ComputeCameraFovScalingFactor(Camera camera)
        {
            return 2.0f * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
        }

        public static int CeilingDivision(int lhs, int rhs)
        {
            return (lhs + rhs - 1) / rhs;
        }

        public static float CeilingDivisionFloat(float lhs, float rhs)
        {
            return (lhs + rhs - 1) / rhs;
        }

        // See struct MetaData in TastyGrassShadercompute.compute
        public struct InstanceMetaDataGPU
        {
            public const int Stride =
                    sizeof(uint) // estMaxBlades
                    + sizeof(uint) // placementTriangles
                    + sizeof(uint) * 3 // boundsMin
                    + sizeof(uint) * 3 // boundsMax
                ;

            public uint estMaxBlades;
            public uint placementTrianglesCount;
            public uint boundsMinX, boundsMinY, boundsMinZ;
            public uint boundsMaxX, boundsMaxY, boundsMaxZ;
        }

        /// <summary>
        ///     A container for *what* kind of grass to grow and *where* (Mesh, chunk of a heightmap, ...) to grow that grass.
        ///     Use BakeFromHeightmap() or BakeFromMesh() to properly create an Recipe.
        /// </summary>
        public struct TgsInstanceRecipe
        {
            internal TgsPreset.Settings Settings;

            internal Matrix4x4 LocalToWorldMatrix;
            internal Bounds WorldSpaceBounds;

            // Heightmap distribution texture
            internal bool DistributionByTextureEnabled;
            internal Texture DistributionTexture;
            internal Vector4 DistributionTextureChannelMask;
            internal Vector4 DistributionTextureScaleOffset;

            // Camouflage
            internal float CamouflageFactor;
            internal Texture CamouflageTexture;
            internal Vector4 CamouflageTextureScaleOffset;

            // Heightmap related settings.
            internal Texture HeightmapTexture;
            internal Vector2Int HeightmapChunkPixelOffset;
            internal Vector2Int ChunkPixelSize;

            // Mesh related settings.
            internal Mesh SharedMesh;
            internal InstanceBakeMode BakeMode;

            // Mesh distribution by vertex color
            internal bool DistributionByVertexColorEnabled;
            internal Color DistributionByVertexColorMask;

            // Mesh
            internal Bounds MeshChunkBounds;

            static TgsInstanceRecipe GetDefaultInstance()
            {
                TgsInstanceRecipe tgsInstanceRecipe = new()
                {
                    DistributionTexture = Texture2D.whiteTexture,
                    DistributionTextureChannelMask = new Vector4(1.0f, 0.0f, 0.0f, 0.0f),
                    CamouflageTexture = Texture2D.whiteTexture,
                    HeightmapTexture = Texture2D.blackTexture,
                    DistributionByVertexColorMask = Color.white
                };

                return tgsInstanceRecipe;
            }


            public static TgsInstanceRecipe BakeFromHeightmap(
                Matrix4x4 localToWorldMatrix,
                TgsPreset.Settings settings,
                Texture heightmapTexture,
                Bounds heightmapChunkBounds,
                Vector2Int heightmapChunkPixelOffset,
                Vector2Int chunkPixelSize)
            {
                TgsInstanceRecipe newParameters = GetDefaultInstance();
                newParameters.BakeMode = InstanceBakeMode.FromHeightmap;
                newParameters.LocalToWorldMatrix = localToWorldMatrix;
                newParameters.Settings = settings;
                newParameters.HeightmapTexture = heightmapTexture;
                newParameters.WorldSpaceBounds = heightmapChunkBounds;
                newParameters.HeightmapChunkPixelOffset = heightmapChunkPixelOffset;
                newParameters.ChunkPixelSize = chunkPixelSize;

                return newParameters;
            }

            /// <summary>
            /// Bake from mesh as a single Chunk.
            /// </summary>
            /// <param name="localToWorldMatrix"></param>
            /// <param name="settings"></param>
            /// <param name="sharedMesh"></param>
            /// <param name="worldSpaceBounds"></param>
            /// <returns></returns>
            public static TgsInstanceRecipe BakeFromMesh(
                Matrix4x4 localToWorldMatrix,
                TgsPreset.Settings settings,
                Mesh sharedMesh,
                Bounds worldSpaceBounds)
            {
                return BakeFromMesh(localToWorldMatrix, settings, sharedMesh, worldSpaceBounds, worldSpaceBounds);
            }

            /// <summary>
            /// Bake from mesh, but only use a subsection of the mesh, specified in chunkBounds.
            /// </summary>
            /// <param name="localToWorldMatrix"></param>
            /// <param name="settings"></param>
            /// <param name="sharedMesh"></param>
            /// <param name="worldSpaceBounds"></param>
            /// <param name="chunkBounds"></param>
            /// <returns></returns>
            public static TgsInstanceRecipe BakeFromMesh(
                Matrix4x4 localToWorldMatrix,
                TgsPreset.Settings settings,
                Mesh sharedMesh,
                Bounds worldSpaceBounds,
                Bounds chunkBounds)
            {
                Debug.Assert(chunkBounds.size is { x: > 0.0f, y: > 0.0f, z: > 0.0f });
                TgsInstanceRecipe newParameters = GetDefaultInstance();
                newParameters.BakeMode = InstanceBakeMode.FromMesh;

                newParameters.LocalToWorldMatrix = localToWorldMatrix;
                newParameters.Settings = settings;
                newParameters.SharedMesh = sharedMesh;
                newParameters.WorldSpaceBounds = worldSpaceBounds;
                newParameters.MeshChunkBounds = chunkBounds;

                return newParameters;
            }

            public void SetupDistributionByTexture(Texture densityTexture, Vector4 channelMask, Vector4 scaleOffset)
            {
                if (BakeMode == InstanceBakeMode.FromMesh)
                {
                    Debug.LogError("SetupDistributionByTexture() is not supported with meshes currently.");
                    return;
                }

                DistributionByTextureEnabled = true;
                DistributionTexture = densityTexture == null ? Texture2D.whiteTexture : densityTexture;
                DistributionTextureChannelMask = channelMask;
                DistributionTextureScaleOffset = scaleOffset;
            }

            public void SetupCamouflage(Texture colorMap, Vector4 colorMapScaleOffset, float blendFactor)
            {
                if (BakeMode == InstanceBakeMode.FromMesh)
                {
                    Debug.LogError("SetupCamouflage() is not supported with meshes currently.");
                    return;
                }

                CamouflageTexture = colorMap == null ? Texture2D.whiteTexture : colorMap;
                CamouflageTextureScaleOffset = colorMapScaleOffset;
                CamouflageFactor = blendFactor;
            }

            public void SetupDistributionByVertexColor(Color mask)
            {
                if (BakeMode == InstanceBakeMode.FromHeightmap)
                {
                    Debug.LogError("SetupDistributionByVertexColor() is not supported with heightmaps.");
                    return;
                }

                DistributionByVertexColorEnabled = true;
                DistributionByVertexColorMask = mask;
            }


            internal enum InstanceBakeMode
            {
                FromMesh,
                FromHeightmap
            }
        }

        #region Internal Shared Functions

        static Vector3 UnpackVector3_32Bit(uint vX, uint vY, uint vZ, Vector3 min, Vector3 max)
        {
            double range = 4294967294.0; // == (1 << 32) - 1
            double x = vX / range;
            double y = vY / range;
            double z = vZ / range;
            return new Vector3(
                Mathf.Lerp(min.x, max.x, (float)x),
                Mathf.Lerp(min.y, max.y, (float)y),
                Mathf.Lerp(min.z, max.z, (float)z));
        }

        void InitPlacementBuffer(int placementMeshTriangleCount)
        {
            // Prepare placement triangle buffer
            if (_placementTriangleBuffer == null || placementMeshTriangleCount != _placementTriangleBuffer.count)
            {
                _placementTriangleBuffer?.Release();
                _placementTriangleBuffer = new ComputeBuffer(
                    placementMeshTriangleCount,
                    SizeofPlacementTriangle,
                    ComputeBufferType.Append);
            }

            _placementTriangleBuffer.SetCounterValue(0);
        }

        void ApplyCommonBuffersAndParameters(ComputeShader tgsComputeShader, TgsInstanceRecipe tgsInstanceRecipe,
            int placementMeshTriangleCount)
        {
            int csBakePassId = tgsComputeShader.FindKernel("BakePass");
            tgsComputeShader.SetMatrix(ObjectToWorld, tgsInstanceRecipe.LocalToWorldMatrix);

            tgsInstanceRecipe.Settings.preset.ApplyToComputeShader(tgsComputeShader, tgsInstanceRecipe.Settings,
                csBakePassId);
            tgsComputeShader.SetInt(PlacementTriangleCount, placementMeshTriangleCount);
            tgsComputeShader.SetTexture(csBakePassId, ColorMap, tgsInstanceRecipe.CamouflageTexture);
            tgsComputeShader.SetFloat(ColorMapBlend, tgsInstanceRecipe.CamouflageFactor);
            tgsComputeShader.SetVector(ColorMapSt, tgsInstanceRecipe.CamouflageTextureScaleOffset);

            tgsComputeShader.SetInt(MetaDataIndex, _instanceMetaDataIndex);

            tgsComputeShader.SetVector(MeshChunkBoundsMin, tgsInstanceRecipe.MeshChunkBounds.min);
            tgsComputeShader.SetVector(MeshChunkBoundsMax, tgsInstanceRecipe.MeshChunkBounds.max);
        }

        #endregion

#if TASTY_GRASS_SHADER_DEBUG
        // void CheckForDuplicates()
        // {
        //     if (ValidateNoDuplicateBlades && _bakeOutputBuffer != null)
        //     {
        //         ComputeBuffer count = new(1, 4, ComputeBufferType.Raw);
        //         ComputeBuffer.CopyCount(_bakeOutputBuffer, count, 0);
        //         int[] bladeCount = new int[1];
        //         count.GetData(bladeCount);
        //         count.Release();
        //
        //         var blades = new GrassBladeReference[bladeCount[0]];
        //         _bakeOutputBuffer.GetData(blades);
        //
        //         var bladeMap = new HashSet<GrassBladeReference>(blades.Length);
        //
        //         int duplicateCount = 0;
        //         foreach (GrassBladeReference blade in blades)
        //         {
        //             if (bladeMap.Contains(blade))
        //             {
        //                 duplicateCount++;
        //             }
        //
        //             bladeMap.Add(blade);
        //         }
        //
        //         if (duplicateCount > 0)
        //         {
        //             Debug.LogError($"Found {duplicateCount} duplicates!");
        //         }
        //         else
        //         {
        //             Debug.Log("Found no duplicates!");
        //         }
        //     }
        // }
        //
        // struct GrassBladeReference
        // {
        //     Vector3 root, side, tip;
        //     Vector3 normal;
        //     Vector3 color;
        // }
#endif
#if TASTY_GRASS_SHADER_DEBUG
        public int PlacedBlades;
        public int BladeCapacity;
        public int UsedPlacementTriangles;
#endif
        /// <summary>
        /// 
        /// 
        /// </summary>
        public struct GPUMesh
        {
            public readonly ComputeBuffer vertices, normals, colors, uvs, indices;
            public readonly bool HasVertexColor;

            static bool MeshHasVertexColor(Mesh placementMesh, UnityEngine.Object errorMessageContext, bool requested)
            {
                int vertexColorOffset = placementMesh.GetVertexAttributeOffset(VertexAttribute.Color);
                if (vertexColorOffset == -1)
                {
                    if (requested)
                    {
                        Debug.LogError(
                            "Density by vertex color was requested, but no color attribute could be found. Will use constant amount.",
                            errorMessageContext);
                    }

                    return false;
                }

                return true;
            }

            public GPUMesh(Mesh sharedMesh, UnityEngine.Object errorMessageContext, bool requiresVertexColor)
            {
                // Mesh to bindable mesh
                // --------------------------------
                // At best, we would bind the vertex and index buffer straight to the compute shader as a ByteAddressBuffer.
                // However, that requires "mesh.vertex/indexBufferTarget |= GraphicsBuffer.Target.Raw" to be executed, which breaks the 
                // mesh in build. This is an acknowledgment bug in Unity, but they will likely not fix it. 
                //
                // As a likely permanent workaround, we use the MeshAPI to get vertex and index buffer from the mesh,
                // and push them again to the GPU.
                // (Which wasts bandwidth, if you think about that this data is already on the GPU, just not in a way that is accessible.)
                HasVertexColor =
                    MeshHasVertexColor(sharedMesh, errorMessageContext, requiresVertexColor);

                Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(sharedMesh);
                Mesh.MeshData meshData = meshDataArray[0];

                var verticesCpu = new NativeArray<Vector3>(meshData.vertexCount, Allocator.Temp);
                var normalsCpu = new NativeArray<Vector3>(meshData.vertexCount, Allocator.Temp);
                var colorsCpu = new NativeArray<Color>(meshData.vertexCount, Allocator.Temp);
                var uvsCpu = new NativeArray<Vector2>(meshData.vertexCount, Allocator.Temp);

                var indicesCpu = new NativeArray<int>(meshData.GetSubMesh(0).indexCount, Allocator.Temp);

                meshData.GetVertices(verticesCpu);
                meshData.GetNormals(normalsCpu);
                if (HasVertexColor)
                {
                    meshData.GetColors(colorsCpu);
                }
                else
                {
                    // do nothing, the native array is initialized with 0 anyways.
                }

                if (sharedMesh.GetVertexAttributeOffset(VertexAttribute.TexCoord0) >= 0)
                {
                    meshData.GetUVs(0, uvsCpu);
                }

                meshData.GetIndices(indicesCpu, 0);

                vertices = new(verticesCpu.Length, sizeof(float) * 3);
                normals = new(normalsCpu.Length, sizeof(float) * 3);
                colors = new(colorsCpu.Length, sizeof(float) * 4);
                uvs = new(colorsCpu.Length, sizeof(float) * 4);
                indices = new(indicesCpu.Length, sizeof(int) * 1);


                vertices.SetData(verticesCpu);
                normals.SetData(normalsCpu);
                colors.SetData(colorsCpu);
                uvs.SetData(colorsCpu);
                indices.SetData(indicesCpu);

                indicesCpu.Dispose();
                colorsCpu.Dispose();
                normalsCpu.Dispose();
                verticesCpu.Dispose();
                uvsCpu.Dispose();

                meshDataArray.Dispose();
            }

            public void Release()
            {
                // Clean up
                indices.Dispose();
                colors.Dispose();
                normals.Dispose();
                vertices.Dispose();
                uvs.Dispose();
            }
        }
    }
}