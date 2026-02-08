using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace SymmetryBreakStudio.TastyGrassShader
{
    public static class SharedTools
    {
        public static void BlitTexture2D(Texture2D source, Texture2D target)
        {
            RenderTexture currentActive = RenderTexture.active;
            RenderTexture tmp = RenderTexture.GetTemporary(target.width, target.height, 1, target.graphicsFormat);
            Graphics.Blit(source, tmp);
            RenderTexture.active = tmp;
            target.ReadPixels(
                new Rect(0, 0, target.width,
                    target.height), 0, 0);
            target.Apply();
            RenderTexture.active = currentActive;
            RenderTexture.ReleaseTemporary(tmp);
        }


        public static void StoreRenderTexture(RenderTexture source, Texture2D target)
        {
            RenderTexture currentActive = RenderTexture.active;
            RenderTexture.active = source;
            target.ReadPixels(
                new Rect(0, 0, target.width,
                    target.height), 0, 0);
            target.Apply();
            RenderTexture.active = currentActive;
        }

        public static void SetupChunks(List<TgsInstance> chunks, int reqCount)
        {
            Profiler.BeginSample("SetupChunks");

            if (reqCount == chunks.Count)
            {
                goto End;
            }

            foreach (TgsInstance instance in chunks)
            {
                instance.Release();
            }

            chunks.Clear();
            chunks.Capacity = Mathf.Max(1, reqCount);
            for (int i = 0; i < reqCount; i++)
            {
                TgsInstance newInstance = new();
                newInstance.MarkGeometryDirty();
                newInstance.MarkMaterialDirty();
                chunks.Add(newInstance);
            }

            End:
            Profiler.EndSample();
        }

        /// <summary>
        /// Call from within OnDrawGizmo*
        /// </summary>
        /// <param name="chunks"></param>
        public static void DrawChunksGizmos(List<TgsInstance> chunks)
        {
            foreach (TgsInstance instance in chunks)
            {
                if (instance.actualBladeCount > 0)
                {                
                    Bounds chunk = instance.tightBounds;
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(chunk.center, chunk.size);
                }
                
                Bounds chunkLoose = instance.looseBounds;
                Gizmos.color = Color.black;
                Gizmos.DrawWireCube(chunkLoose.center, chunkLoose.size);

#if TASTY_GRASS_SHADER_DEBUG
                Bounds meshDebug = instance.debugMeshChunk;
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(meshDebug.center, meshDebug.size);
#endif
            }
        }

        public static void AppendMemoryStatsFromChunks(List<TgsInstance> chunks, ref TgsStats stats)
        {
            foreach (TgsInstance instance in chunks)
            {
                stats.GrassMeshBytes += instance.GetGrassBufferMemoryByteSize();
                stats.ChunkCountWithGrass += instance.IsRenderable ? 1 : 0;
                stats.TotalBlades += instance.actualBladeCount;
            }

            stats.ChunkCount += chunks.Count;
        }

        /// <summary>
        /// A handy struct to collect all performance related stats about a layer.
        /// </summary>
        public struct TgsStats
        {
            public int GrassMeshBytes;
            public int ChunkCount;
            public int ChunkCountWithGrass;
            [FormerlySerializedAs("totalBlades")] public long TotalBlades;
        }
    }
}