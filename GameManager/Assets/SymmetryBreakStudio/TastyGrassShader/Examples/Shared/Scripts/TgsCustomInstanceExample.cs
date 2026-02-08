using System;
using SymmetryBreakStudio.TastyGrassShader;
using UnityEngine;

namespace SymmetryBreakStudio.TastyGrassShader.Example
{
    /// <summary>
    /// Minimal example of a custom TgsInstance.
    /// </summary>
    [ExecuteInEditMode,
     RequireComponent(typeof(MeshFilter),
         typeof(MeshRenderer))] // This is not technically part of this example, but needed to enforce that we are using a mesh.
    public class TgsCustomInstanceExample : MonoBehaviour
    {
        /// <summary>
        /// Instances represent a "slot" for a individual block of grass.
        /// You only need to create an instance once, it is intended to be "recyclable" for minimal Garbage Collection overhead.
        /// </summary>
        TgsInstance _instance = new TgsInstance();

        /// <summary>
        /// Settings contain all information of *what* kind of grass to grow.
        /// </summary>
        public TgsPreset.Settings settings = TgsPreset.Settings.GetDefault();

        /// <summary>
        /// The wind settings to use. Since they are often shared across the entire scene, they are stored separately.
        /// </summary>
        public TgsWindSettings windSettings;

        private void OnEnable()
        {
            _instance.Hide = false;
            BakeInstance();
        }

        private void OnValidate()
        {
            // For editor only.
            BakeInstance();
        }

        void BakeInstance()
        {
            if (settings.preset == null)
            {
                // We must have a preset defined.
                return;
            }

            if (windSettings == null)
            {
                // We must have a wind setting.
                return;
            }

            // The mesh that is used to grow grass on.
            Mesh sharedMesh = GetComponent<MeshFilter>().sharedMesh;
            // This is the matrix that is used to transform the mesh vertices into world-space.
            Matrix4x4 localToWorldMatrix = transform.localToWorldMatrix;
            // The bounding box that is used to frustum cull and also store the grass.
            // (Grass blades are stored relative to the bounding box of the mesh for smaller memory footprint.)
            Bounds boundingBox = GetComponent<MeshRenderer>().bounds;

            // Create the recipe, a container for *what* kind of grass to grow and *where* (Mesh, chunk of a heightmap, ...) to grow that grass. 
            TgsInstance.TgsInstanceRecipe recipe =
                TgsInstance.TgsInstanceRecipe.BakeFromMesh(localToWorldMatrix, settings, sharedMesh, boundingBox);

            // Apply the recipe.
            _instance.SetBakeParameters(recipe);

            // Mark the geometry of this instance "dirty". This will tell TGS that something about the geometry has changed,
            // either something about the grass it self, or something about the geometry that the grass is growing on.
            _instance.MarkGeometryDirty();

            // Don't forget to setup a Wind Setting, or the grass won't show up.
            _instance.UsedWindSettings = windSettings;

            // Marks the "look" of the grass dirty.
            // This is used when only indirect aspects of the grass have changed, such as the wind.
            _instance.MarkMaterialDirty();
        }

        private void OnDisable()
        {
            // We can also hide instances. This will make them invisible, but still keep them alive. 
            _instance.Hide = true;
        }

        private void OnDestroy()
        {
            // Don't forget to release the instance, or it will leak internally!
            _instance.Release();
        }
    }
}