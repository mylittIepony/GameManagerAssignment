using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace UIEffectsPro.Editor
{
    /// <summary>
    /// A static utility class for managing UIEffectProfile presets within the Unity Editor.
    /// It handles creation, loading, and listing of preset asset files.
    /// </summary>
    public static class PresetManager
    {
        // The default directory path where new presets will be stored.
        private const string DEFAULT_PRESET_DIRECTORY = "Assets/UIEffectsPro/Presets";

        /// <summary>
        /// Creates a new preset asset from a given UIEffectProfile instance.
        /// </summary>
        /// <param name="profile">The profile instance to save as a preset.</param>
        /// <param name="path">The full asset path where the preset will be saved (e.g., "Assets/MyPresets/NewPreset.asset").</param>
        /// <returns>True if the preset was created successfully, false otherwise.</returns>
        public static bool CreatePresetFromProfile(UIEffectsPro.Runtime.UIEffectProfile profile, string path)
        {
            // Validate that the source profile is not null.
            if (profile == null)
            {
                Debug.LogError("PresetManager: Cannot create preset from a null profile.");
                return false;
            }

            // Validate that the provided path is valid.
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("PresetManager: An invalid path was provided for preset creation.");
                return false;
            }

            try
            {
                // Ensure the target directory exists before attempting to create the asset.
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    // Refresh the AssetDatabase to make sure Unity recognizes the new directory.
                    AssetDatabase.Refresh();
                }

                // If a preset already exists at the path, ask the user for confirmation to overwrite it.
                if (File.Exists(path))
                {
                    if (!EditorUtility.DisplayDialog("Overwrite Preset",
                        $"A preset already exists at:\n{path}\n\nDo you want to overwrite it?",
                        "Overwrite", "Cancel"))
                    {
                        // If the user cancels, abort the creation process.
                        return false;
                    }
                }

                // Clone the original profile to avoid modifying it directly.
                // The new preset will be an independent copy.
                var presetCopy = profile.Clone();
                // Set the name of the ScriptableObject to match the file name.
                presetCopy.name = Path.GetFileNameWithoutExtension(path);

                // Create the asset file in the project.
                AssetDatabase.CreateAsset(presetCopy, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"PresetManager: Successfully created preset at {path}");
                return true;
            }
            catch (System.Exception e)
            {
                // Log any exceptions that occur during the file operations.
                Debug.LogError($"PresetManager: Failed to create preset at {path}. Error: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Searches the entire project for all UIEffectProfile assets and returns them as a list.
        /// </summary>
        /// <returns>A sorted list of PresetInfo objects, each representing a preset found in the project.</returns>
        public static List<PresetInfo> ListAllPresets()
        {
            var presets = new List<PresetInfo>();

            // Find all assets of type UIEffectProfile using their GUIDs.
            string[] guids = AssetDatabase.FindAssets("t:UIEffectProfile");

            foreach (string guid in guids)
            {
                // Convert the GUID to its corresponding asset path.
                string path = AssetDatabase.GUIDToAssetPath(guid);
                // Load the asset from the path.
                var profile = AssetDatabase.LoadAssetAtPath<UIEffectsPro.Runtime.UIEffectProfile>(path);

                if (profile != null)
                {
                    // If the asset is loaded successfully, create a PresetInfo object and add it to the list.
                    presets.Add(new PresetInfo
                    {
                        path = path,
                        displayName = profile.name, // The user-friendly name of the asset.
                        fileName = Path.GetFileNameWithoutExtension(path)
                    });
                }
            }
            
            // Sort the presets alphabetically by their display name for a better user experience.
            presets.Sort((a, b) => string.Compare(a.displayName, b.displayName, System.StringComparison.OrdinalIgnoreCase));

            return presets;
        }

        /// <summary>
        /// Loads a UIEffectProfile preset from a specific asset path.
        /// </summary>
        /// <param name="path">The asset path of the preset to load.</param>
        /// <returns>The loaded UIEffectProfile instance, or null if loading fails.</returns>
        public static UIEffectsPro.Runtime.UIEffectProfile LoadPreset(string path)
        {
            // Validate that the path is not null or empty.
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("PresetManager: An invalid path was provided for preset loading.");
                return null;
            }

            try
            {
                // Attempt to load the asset from the given path.
                var profile = AssetDatabase.LoadAssetAtPath<UIEffectsPro.Runtime.UIEffectProfile>(path);

                if (profile == null)
                {
                    Debug.LogError($"PresetManager: Could not load preset from {path}. The file may not exist or may not be a valid UIEffectProfile.");
                    return null;
                }

                Debug.Log($"PresetManager: Successfully loaded preset from {path}");
                return profile;
            }
            catch (System.Exception e)
            {
                // Log any exceptions that occur during the loading process.
                Debug.LogError($"PresetManager: Failed to load preset from {path}. Error: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Creates a set of default, pre-configured presets for common UI styles.
        /// This is useful for providing users with examples out-of-the-box.
        /// </summary>
        public static void CreateDefaultPresets()
        {
            EnsurePresetDirectoryExists();
            
            // Create each of the predefined presets.
            CreateDefaultPreset();
            CreateMaterialDesignPreset();
            CreateiOSStylePreset();
            CreateNeumorphicPreset();
            
            // Save all changes to the AssetDatabase.
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("PresetManager: Default presets created successfully.");
        }

        /// <summary>
        /// A helper method that checks if the default preset directory exists and creates it if it doesn't.
        /// </summary>
        private static void EnsurePresetDirectoryExists()
        {
            if (!Directory.Exists(DEFAULT_PRESET_DIRECTORY))
            {
                Directory.CreateDirectory(DEFAULT_PRESET_DIRECTORY);
                AssetDatabase.Refresh();
            }
        }
        
        /// <summary>
        /// Creates a basic "Default" preset with default values.
        /// </summary>
        private static void CreateDefaultPreset()
        {
            string path = Path.Combine(DEFAULT_PRESET_DIRECTORY, "Default.asset");
            
            // Only create the asset if it doesn't already exist to avoid overwriting user changes.
            if (!File.Exists(path))
            {
                var profile = ScriptableObject.CreateInstance<UIEffectsPro.Runtime.UIEffectProfile>();
                profile.name = "Default";
                profile.ResetToDefaults();
                
                AssetDatabase.CreateAsset(profile, path);
            }
        }
        
        /// <summary>
        /// Creates a preset based on Material Design principles.
        /// </summary>
        private static void CreateMaterialDesignPreset()
        {
            string path = Path.Combine(DEFAULT_PRESET_DIRECTORY, "MaterialDesign.asset");
            
            if (!File.Exists(path))
            {
                var profile = ScriptableObject.CreateInstance<UIEffectsPro.Runtime.UIEffectProfile>();
                profile.name = "Material Design";
                profile.globalCornerRadius = 8f;
                profile.borderWidth = 0f;
                profile.fillColor = new Color(0.2f, 0.4f, 0.8f, 1f);
                profile.borderColor = new Color(0.1f, 0.3f, 0.7f, 1f);
                
                AssetDatabase.CreateAsset(profile, path);
            }
        }
        
        /// <summary>
        /// Creates a preset that mimics the rounded, clean style of iOS UI elements.
        /// </summary>
        private static void CreateiOSStylePreset()
        {
            string path = Path.Combine(DEFAULT_PRESET_DIRECTORY, "iOS_Rounded.asset");
            
            if (!File.Exists(path))
            {
                var profile = ScriptableObject.CreateInstance<UIEffectsPro.Runtime.UIEffectProfile>();
                profile.name = "iOS Rounded";
                profile.globalCornerRadius = 12f;
                profile.borderWidth = 1f;
                profile.fillColor = new Color(0.95f, 0.95f, 0.97f, 1f);
                profile.borderColor = new Color(0.8f, 0.8f, 0.8f, 1f);
                
                AssetDatabase.CreateAsset(profile, path);
            }
        }
        
        /// <summary>
        /// Creates a preset for a Neumorphic (soft UI) design style.
        /// </summary>
        private static void CreateNeumorphicPreset()
        {
            string path = Path.Combine(DEFAULT_PRESET_DIRECTORY, "Neumorphic.asset");
            
            if (!File.Exists(path))
            {
                var profile = ScriptableObject.CreateInstance<UIEffectsPro.Runtime.UIEffectProfile>();
                profile.name = "Neumorphic";
                profile.globalCornerRadius = 16f;
                profile.borderWidth = 0f;
                profile.fillColor = new Color(0.9f, 0.9f, 0.92f, 1f);
                profile.borderColor = new Color(0.8f, 0.8f, 0.82f, 1f);
                
                AssetDatabase.CreateAsset(profile, path);
            }
        }
        
        /// <summary>
        /// A simple data structure to hold key information about a preset.
        /// This is used for displaying presets in editor UI like dropdowns.
        /// </summary>
        [System.Serializable]
        public class PresetInfo
        {
            /// <summary>
            /// The full asset path to the preset file.
            /// </summary>
            public string path;

            /// <summary>
            /// The display name of the preset, typically the same as the asset name.
            /// </summary>
            public string displayName;

            /// <summary>
            /// The name of the file without the extension.
            /// </summary>
            public string fileName;
        }
    }
}