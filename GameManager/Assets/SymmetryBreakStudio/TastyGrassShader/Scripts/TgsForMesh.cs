using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
#if TASTY_GRASS_SHADER_DEBUG
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace SymmetryBreakStudio.TastyGrassShader
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [HelpURL(
        "https://github.com/SymmetryBreakStudio/TastyGrassShader/wiki/1.-Quick-Start#adding-grass-to-a-mesh--meshfilter-and-meshrenderer")]
    [AddComponentMenu("Symmetry Break Studio/Tasty Grass Shader/Tasty Grass Shader For Mesh")]
    public class TgsForMesh : MonoBehaviour
    {
        public enum GrassMeshError
        {
            None,
            MissingMeshFilter,
            MeshNoReadWrite,
            MissingVertexColor,
            MissingMesh
        }

        [SerializeField] List<TgsMeshLayer> layers = new();

        [Tooltip("Wind setting used for this object.")]
        public TgsWindSettings windSettings;

        // In case the mesh is replaced by static batching, we have no reliable way of getting the original one.
        // Therefore, we keep a reference around 
        public Mesh sharedMeshReference;

        Matrix4x4 _previousLocalToWorld;
        [NonSerialized] [HideInInspector] public bool UpdateOnNextTick;

        void Update()
        {
            if (!Application.isPlaying || UpdateOnNextTick)
            {
                // Update the mesh filter, so that PolyBrush works in edit mode.
                // Note that this should not be done in play mode, because the user may use static batching,
                // which breaks TGS.
                if (!Application.isPlaying)
                {
                    sharedMeshReference = GetComponent<MeshFilter>().sharedMesh;
                }

                OnPropertiesMayChanged();
                UpdateOnNextTick = false;
            }
        }

        void OnEnable()
        {
#if TASTY_GRASS_SHADER_DEBUG
            UnsafeUtility.SetLeakDetectionMode(NativeLeakDetectionMode.EnabledWithStackTrace);
#endif
            MarkGeometryDirty();
            OnPropertiesMayChanged();
        }

        void OnDisable()
        {
            foreach (TgsMeshLayer tgsMeshLayer in layers)
            {
                tgsMeshLayer.Release();
            }
        }

        void OnDrawGizmosSelected()
        {
            foreach (TgsMeshLayer layer in layers)
            {
                SharedTools.DrawChunksGizmos(layer._chunks);
            }
        }

        void OnTransformParentChanged()
        {
            MarkGeometryDirty();
        }

        void OnValidate()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            sharedMeshReference = meshFilter.sharedMesh;
        }

        /// <summary>
        ///     Get the layer at the given index. Will throw an exception if the index does not exist.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TgsMeshLayer GetLayerByIndex(int index)
        {
            return index < layers.Count ? layers[index] : null;
        }

        /// <summary>
        ///     Gets the count of layers.
        /// </summary>
        /// <returns></returns>
        public int GetLayerCount()
        {
            return layers.Count;
        }

        /// <summary>
        ///     Adds a new layer.
        /// </summary>
        /// <returns>The index of the new layer</returns>
        public int AddNewLayer()
        {
            TgsMeshLayer newLayer = new();
            layers.Add(newLayer);
            return layers.Count - 1;
        }

        /// <summary>
        ///     Removes the layer at the given index. This function may throw an exception if the index is invalid.
        /// </summary>
        /// <param name="index"></param>
        public void RemoveLayerAt(int index)
        {
            TgsMeshLayer tgsMeshLayer = layers[index];
            tgsMeshLayer.Release();
            layers.RemoveAt(index);
        }

        public static GrassMeshError CheckForErrorsMeshFilter(TgsForMesh tgsForMesh, MeshFilter meshFilter)
        {
            if (!meshFilter)
            {
                return GrassMeshError.MissingMeshFilter;
            }

            if (!meshFilter.sharedMesh)
            {
                return GrassMeshError.MissingMesh;
            }

            if (!meshFilter.sharedMesh.isReadable)
            {
                return GrassMeshError.MeshNoReadWrite;
            }

            bool anyLayerNeedsVertexColor = false;
            foreach (var layer in tgsForMesh.layers)
            {
                if (layer.distribution != TgsMeshLayer.DensityColorChannelMask.Fill)
                {
                    anyLayerNeedsVertexColor = true;
                    break;
                }
            }

            if (anyLayerNeedsVertexColor && !meshFilter.sharedMesh.HasVertexAttribute(VertexAttribute.Color))
            {
                return GrassMeshError.MissingVertexColor;
            }

            return GrassMeshError.None;
        }

        public GrassMeshError CheckForErrors()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            return CheckForErrorsMeshFilter(this, meshFilter);
        }

        public void MarkGeometryDirty()
        {
            foreach (TgsMeshLayer tgsMeshLayer in layers)
            {
                tgsMeshLayer.MarkGeometryDirty();
            }
        }

        public void MarkMaterialDirty()
        {
            foreach (TgsMeshLayer tgsMeshLayer in layers)
            {
                tgsMeshLayer.MarkMaterialDirty();
            }
        }


        bool IsThisSelectedInEditor()
        {
#if UNITY_EDITOR
            return UnityEditor.Selection.Contains(gameObject);
#else
            return false;
#endif
        }

        public void OnPropertiesMayChanged()
        {
            if (sharedMeshReference == null)
            {
                // Attempt to get the mesh, in case this is a user spawned instance.
                sharedMeshReference = GetComponent<MeshFilter>().sharedMesh;
            }

            CheckForErrorsMeshFilter(this, GetComponent<MeshFilter>());
            if (sharedMeshReference)
            {
                MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
                Matrix4x4 localToWorld = transform.localToWorldMatrix;
                if (localToWorld != _previousLocalToWorld)
                {
                    MarkGeometryDirty();
                }

                _previousLocalToWorld = localToWorld;
                foreach (TgsMeshLayer tgsMeshLayer in layers)
                {
                    // For the Editor mode, always rebuild the mesh when selected, since we can't be certain if something changed.
                    if (IsThisSelectedInEditor())
                    {
                        tgsMeshLayer.MarkGeometryDirty();
                        tgsMeshLayer.MarkMaterialDirty();
                    }


#if TASTY_GRASS_SHADER_DEBUG
                tgsMeshLayer.debugUsingGameObject = gameObject;
#endif
                    tgsMeshLayer.CheckForChange(localToWorld, sharedMeshReference, meshRenderer.bounds,
                        gameObject.layer);
                    foreach (var chunk in tgsMeshLayer._chunks)
                    {
                        chunk.UsedWindSettings = windSettings;
                    }
                }
            }
        }

        public SharedTools.TgsStats GetMemoryStats()
        {
            SharedTools.TgsStats stats = new SharedTools.TgsStats();
            foreach (TgsMeshLayer layer in layers)
            {
                SharedTools.AppendMemoryStatsFromChunks(layer._chunks, ref stats);
            }

            return stats;
        }
    }

    [Serializable]
    public class TgsMeshLayer
    {
        public enum DensityColorChannelMask
        {
            Fill,
            Red,
            Green,
            Blue,
            Alpha
        }

        [HideInInspector] public bool hide;

        [FormerlySerializedAs("quickSettings")]
        public TgsPreset.Settings settings = TgsPreset.Settings.GetDefault();

        public DensityColorChannelMask distribution;

        internal List<TgsInstance> _chunks = new();

        internal TgsMeshLayer()
        {
        }

        void SetupChunks(int reqCount)
        {
            Profiler.BeginSample("SetupInstances");

            if (reqCount == _chunks.Count)
            {
                goto End;
            }

            foreach (TgsInstance instance in _chunks)
            {
                instance.Release();
            }

            _chunks.Clear();
            _chunks.Capacity = Mathf.Max(1, reqCount);
            for (int i = 0; i < reqCount; i++)
            {
                TgsInstance newInstance = new();
                newInstance.MarkGeometryDirty();
                newInstance.MarkMaterialDirty();
                _chunks.Add(newInstance);
            }

            End:
            Profiler.EndSample();
        }

#if TASTY_GRASS_SHADER_DEBUG
        public GameObject debugUsingGameObject;
#endif

        public void CheckForChange(Matrix4x4 localToWorldMatrix, Mesh mesh, Bounds worldSpaceBounds, int unityLayer)
        {
            int chunkSize = TgsGlobalSettings.GlobalChunkSize;
            Vector3Int chunksPerAxis = new(
                (int)TgsInstance.CeilingDivisionFloat(worldSpaceBounds.size.x, (float)chunkSize),
                (int)TgsInstance.CeilingDivisionFloat(worldSpaceBounds.size.y, (float)chunkSize),
                (int)TgsInstance.CeilingDivisionFloat(worldSpaceBounds.size.z, (float)chunkSize));

            chunksPerAxis =
                Vector3Int.Max(chunksPerAxis, Vector3Int.one); // Ensure that we have at least one chunk per axis.
            int totalChunks = chunksPerAxis.x * chunksPerAxis.y * chunksPerAxis.z;

            SharedTools.SetupChunks(_chunks, totalChunks);

            for (int index = 0; index < _chunks.Count; index++)
            {
                TgsInstance chunk = _chunks[index];

#if TASTY_GRASS_SHADER_DEBUG
                chunk.debugUsingGameObject = debugUsingGameObject;
#endif
                chunk.Hide = hide;
                chunk.UnityLayer = unityLayer;
                if (chunk.isGeometryDirty || settings.HasChangedSinceLastCall())
                {
                    // TODO: fix that this doesn'T work for full flat meshes (unity plane for example)
                    // TODO: fix that with very large meshes every lags
                    // TODO: fix that the buffers for the mesh arent recycled.
                    Vector3Int chunkIndex = new Vector3Int(
                        index % chunksPerAxis.x,
                        (index / chunksPerAxis.x) % chunksPerAxis.y,
                        index / (chunksPerAxis.x * chunksPerAxis.y));

                    Vector3 boundsMin = new Vector3(
                        chunkIndex.x * chunkSize,
                        chunkIndex.y * chunkSize,
                        chunkIndex.z * chunkSize);

                    Vector3 boundsMax = new Vector3(
                        (chunkIndex.x + 1) * chunkSize,
                        (chunkIndex.y + 1) * chunkSize,
                        (chunkIndex.z + 1) * chunkSize);

                    // Sometimes (like with the Unity Plane Mesh) the center of a triangle is right on the border of the AABB. Given the nature of floating point precision, this leads to false negatives. Therefore, we add a little nudge to prevent this issue.
                    Vector3 floatingPointSafeZone = new Vector3(0.0005f, 0.0005f, 0.0005f);
                    Bounds chunkBounds = new Bounds
                    {
                        min = boundsMin - floatingPointSafeZone + worldSpaceBounds.min,
                        max = boundsMax + floatingPointSafeZone + worldSpaceBounds.min
                    };

                    TgsInstance.TgsInstanceRecipe tgsInstanceRecipe = TgsInstance.TgsInstanceRecipe.BakeFromMesh(
                        localToWorldMatrix,
                        settings,
                        mesh,
                        worldSpaceBounds,
                        chunkBounds);

                    if (distribution != DensityColorChannelMask.Fill)
                    {
                        Vector4 densityMask = new(
                            distribution == DensityColorChannelMask.Red ? 1.0f : 0.0f,
                            distribution == DensityColorChannelMask.Green ? 1.0f : 0.0f,
                            distribution == DensityColorChannelMask.Blue ? 1.0f : 0.0f,
                            distribution == DensityColorChannelMask.Alpha ? 1.0f : 0.0f);

                        tgsInstanceRecipe.SetupDistributionByVertexColor(densityMask);
                    }

                    chunk.SetBakeParameters(tgsInstanceRecipe);
                    chunk.MarkGeometryDirty();
                    chunk.MarkMaterialDirty();
                }
            }
        }

        public void MarkGeometryDirty()
        {
            foreach (TgsInstance chunk in _chunks)
            {
                chunk.MarkGeometryDirty();
            }
        }

        public void MarkMaterialDirty()
        {
            foreach (TgsInstance chunk in _chunks)
            {
                chunk.MarkMaterialDirty();
            }
        }

        public void Release()
        {
            SetupChunks(-1);
        }

        public string GetEditorName(int index)
        {
            string layerName =
                $"#{index} - {(settings.preset != null ? settings.preset.name : "NO PRESET DEFINED")} ({distribution.ToString()}) {(hide ? "(Hidden)" : "")}";
            return layerName;
        }
    }
}