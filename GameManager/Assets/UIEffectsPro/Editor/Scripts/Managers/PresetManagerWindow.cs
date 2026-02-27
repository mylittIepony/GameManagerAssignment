
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UIEffectsPro.Editor
{
    /// <summary>
    /// Manages the editor window for viewing, applying, and managing UIEffectProfile presets.
    /// This window allows users to interact with ScriptableObject presets stored in the project.
    /// </summary>
    public class PresetManagerWindow : EditorWindow
    {
        // Constants for window configuration.
        private const string WINDOW_TITLE = "Preset Manager";
        private const string MENU_PATH = "Window/UI Effects Pro/Preset Manager";
        private const float MIN_WINDOW_WIDTH = 280f;
        private const float MIN_WINDOW_HEIGHT = 320f;

        // Private fields for managing the window's state.
        private Vector2 _scrollPosition;
        private List<PresetManager.PresetInfo> _presetList;
        private int _selectedPresetIndex = -1;
        private UIEffectsPro.Runtime.UIEffectProfile _previewProfile;

        // GUIStyles for custom drawing of the preset list.
        private GUIStyle _presetButtonStyle;
        private GUIStyle _selectedPresetStyle;
        private bool _stylesInitialized = false;

        /// <summary>
        /// An action that is invoked when a preset is selected to be loaded into another window.
        /// This allows for communication between different editor windows.
        /// </summary>
        public System.Action<UIEffectsPro.Runtime.UIEffectProfile> OnPresetSelected;

        /// <summary>
        /// Creates and shows the Preset Manager window.
        /// Accessible from the Unity Editor's top menu.
        /// </summary>
        [MenuItem(MENU_PATH)]
        public static void ShowWindow()
        {
            PresetManagerWindow window = GetWindow<PresetManagerWindow>(WINDOW_TITLE);
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
            window.Show();
        }

        /// <summary>
        /// Creates and shows the Preset Manager window with a callback for when a preset is selected.
        /// </summary>
        /// <param name="onPresetSelected">The action to execute when a preset is loaded.</param>
        /// <returns>The instance of the window.</returns>
        public static PresetManagerWindow ShowWindow(System.Action<UIEffectsPro.Runtime.UIEffectProfile> onPresetSelected)
        {
            PresetManagerWindow window = GetWindow<PresetManagerWindow>(WINDOW_TITLE);
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
            window.OnPresetSelected = onPresetSelected;
            window.Show();
            return window;
        }

        /// <summary>
        /// Called when the window is enabled. Used for initialization.
        /// </summary>
        private void OnEnable()
        {
            RefreshPresetList();
        }

        /// <summary>
        /// Called every frame to draw the window's GUI.
        /// This is the main rendering loop for the editor window.
        /// </summary>
        private void OnGUI()
        {
            InitializeStyles();

            DrawHeader();
            DrawPresetList();
            DrawActionButtons();
        }

        /// <summary>
        /// Initializes the GUI styles used for drawing the preset buttons.
        /// This is done once to avoid performance issues from creating styles inside OnGUI.
        /// </summary>
        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _presetButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 5, 5),
                margin = new RectOffset(2, 2, 1, 1)
            };

            _selectedPresetStyle = new GUIStyle(_presetButtonStyle)
            {
                // Use the 'active' background state for the 'normal' state to make it look permanently selected.
                normal = { background = EditorStyles.miniButton.active.background }
            };

            _stylesInitialized = true;
        }

        /// <summary>
        /// Draws the header section of the window, including the title and action buttons.
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.LabelField("UI Effect Presets", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh", EditorStyles.miniButtonLeft))
                {
                    RefreshPresetList();
                }

                if (GUILayout.Button("Create Defaults", EditorStyles.miniButtonRight))
                {
                    PresetManager.CreateDefaultPresets();
                    RefreshPresetList();
                }
            }

            EditorGUILayout.Space(5);
        }

        /// <summary>
        /// Draws the scrollable list of available presets.
        /// </summary>
        private void DrawPresetList()
        {
            if (_presetList == null || _presetList.Count == 0)
            {
                EditorGUILayout.HelpBox("No presets found. Click 'Create Defaults' to create some sample presets.", MessageType.Info);
                return;
            }

            using (var scrollScope = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scrollScope.scrollPosition;

                for (int i = 0; i < _presetList.Count; i++)
                {
                    var preset = _presetList[i];
                    bool isSelected = i == _selectedPresetIndex;

                    // Use a different style for the selected button to provide visual feedback.
                    GUIStyle buttonStyle = isSelected ? _selectedPresetStyle : _presetButtonStyle;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // Main button to select and preview the preset.
                        if (GUILayout.Button(preset.displayName, buttonStyle, GUILayout.ExpandWidth(true)))
                        {
                            SelectPreset(i);
                        }

                        // Button to immediately apply the preset to selected GameObjects.
                        if (GUILayout.Button("Apply", EditorStyles.miniButton, GUILayout.Width(50)))
                        {
                            ApplyPreset(preset);
                        }

                        // Button to load the preset into the main UIEffectsWindow for editing.
                        if (GUILayout.Button("Load", EditorStyles.miniButton, GUILayout.Width(50)))
                        {
                            LoadPresetInMainWindow(preset);
                        }
                    }

                    // If this preset is selected, draw a preview of its properties.
                    if (isSelected && _previewProfile != null)
                    {
                        DrawPresetPreview(_previewProfile);
                    }
                }
            }
        }

        /// <summary>
        /// Draws a read-only preview of a UIEffectProfile's properties.
        /// </summary>
        /// <param name="profile">The profile to preview.</param>
        private void DrawPresetPreview(UIEffectsPro.Runtime.UIEffectProfile profile)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Preview", EditorStyles.miniLabel);

                // Disable the controls to make the preview read-only.
                EditorGUI.BeginDisabledGroup(true);

                // Display corner radius information.
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Radius:", GUILayout.Width(50));
                    if (profile.useIndividualCorners)
                    {
                        EditorGUILayout.LabelField($"TL:{profile.cornerRadiusTopLeft:F0} TR:{profile.cornerRadiusTopRight:F0} BL:{profile.cornerRadiusBottomLeft:F0} BR:{profile.cornerRadiusBottomRight:F0}", EditorStyles.miniLabel);
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"{profile.globalCornerRadius:F0}", EditorStyles.miniLabel);
                    }
                }

                // Display border information.
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Border:", GUILayout.Width(50));
                    EditorGUILayout.LabelField($"{profile.borderWidth:F0}px", EditorStyles.miniLabel);

                    Rect colorRect = GUILayoutUtility.GetRect(20, EditorGUIUtility.singleLineHeight);
                    EditorGUI.DrawRect(colorRect, profile.borderColor);
                }

                // Display fill color information.
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Fill:", GUILayout.Width(50));

                    Rect colorRect = GUILayoutUtility.GetRect(20, EditorGUIUtility.singleLineHeight);
                    EditorGUI.DrawRect(colorRect, profile.fillColor);

                    EditorGUILayout.LabelField($"RGB({profile.fillColor.r:F2}, {profile.fillColor.g:F2}, {profile.fillColor.b:F2})", EditorStyles.miniLabel);
                }

                EditorGUI.EndDisabledGroup();
            }
        }

        /// <summary>
        /// Draws the action buttons at the bottom of the window (e.g., Delete, Duplicate).
        /// </summary>
        private void DrawActionButtons()
        {
            EditorGUILayout.Space(5);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

                if (GUILayout.Button("Apply Selected to Scene Objects"))
                {
                    ApplySelectedToSceneObjects();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Delete Selected"))
                    {
                        DeleteSelectedPreset();
                    }

                    if (GUILayout.Button("Duplicate Selected"))
                    {
                        DuplicateSelectedPreset();
                    }
                }
            }
        }

        /// <summary>
        /// Reloads the list of presets from the project files.
        /// </summary>
        private void RefreshPresetList()
        {
            _presetList = PresetManager.ListAllPresets();
            _selectedPresetIndex = -1;
            _previewProfile = null;
        }

        /// <summary>
        /// Selects a preset from the list by its index.
        /// </summary>
        /// <param name="index">The index of the preset to select.</param>
        private void SelectPreset(int index)
        {
            if (index < 0 || index >= _presetList.Count) return;

            _selectedPresetIndex = index;
            var presetInfo = _presetList[index];
            _previewProfile = PresetManager.LoadPreset(presetInfo.path);
        }

        /// <summary>
        /// Applies a given preset to all currently selected GameObjects in the scene.
        /// </summary>
        /// <param name="presetInfo">The preset to apply.</param>
        private void ApplyPreset(PresetManager.PresetInfo presetInfo)
        {
            var profile = PresetManager.LoadPreset(presetInfo.path);
            if (profile == null) return;

            GameObject[] selectedObjects = Selection.gameObjects;
            if (selectedObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select one or more GameObjects to apply the preset.", "OK");
                return;
            }

            int appliedCount = 0;

            foreach (GameObject obj in selectedObjects)
            {
                // The effect can be applied to either an Image or a RawImage component.
                var image = obj.GetComponent<UnityEngine.UI.Image>();
                var rawImage = obj.GetComponent<UnityEngine.UI.RawImage>();

                if (image == null && rawImage == null) continue;

                // Find or add the UIEffectComponent to the GameObject.
                var effectComponent = obj.GetComponent<UIEffectsPro.Runtime.UIEffectComponent>();
                if (effectComponent == null)
                {
                    // Use Undo.AddComponent to make sure this action can be undone.
                    effectComponent = Undo.AddComponent<UIEffectsPro.Runtime.UIEffectComponent>(obj);
                }
                
                // Record the component state before changing it for undo support.
                Undo.RecordObject(effectComponent, "Apply UI Effect Preset");
                effectComponent.SetProfile(profile);

                appliedCount++;
            }
            
            // Show a confirmation dialog to the user.
            EditorUtility.DisplayDialog("Preset Applied",
                $"Applied '{presetInfo.displayName}' to {appliedCount} object(s).", "OK");
        }

        /// <summary>
        /// Loads the selected preset into the main UIEffectsWindow via the OnPresetSelected action.
        /// </summary>
        /// <param name="presetInfo">The preset information to load.</param>
        private void LoadPresetInMainWindow(PresetManager.PresetInfo presetInfo)
        {
            var profile = PresetManager.LoadPreset(presetInfo.path);
            if (profile != null && OnPresetSelected != null)
            {
                // Invoke the callback to notify the listener (e.g., the main window).
                OnPresetSelected.Invoke(profile);
                Debug.Log($"Loaded preset '{presetInfo.displayName}' in main window");
            }

            // Optional: try to find the main window to bring it to front.
            var mainWindow = GetWindow<UIEffectsWindow>(null, false);
            if (mainWindow != null)
            {
                Debug.Log($"Preset '{presetInfo.displayName}' selected for main window");
            }
        }

        /// <summary>
        /// Helper method to apply the currently selected preset to scene objects.
        /// </summary>
        private void ApplySelectedToSceneObjects()
        {
            if (_selectedPresetIndex < 0 || _selectedPresetIndex >= _presetList.Count)
            {
                EditorUtility.DisplayDialog("No Preset Selected", "Please select a preset first.", "OK");
                return;
            }

            ApplyPreset(_presetList[_selectedPresetIndex]);
        }

        /// <summary>
        /// Deletes the currently selected preset asset file after user confirmation.
        /// </summary>
        private void DeleteSelectedPreset()
        {
            if (_selectedPresetIndex < 0 || _selectedPresetIndex >= _presetList.Count) return;

            var preset = _presetList[_selectedPresetIndex];

            // Show a confirmation dialog to prevent accidental deletion.
            if (EditorUtility.DisplayDialog("Delete Preset",
                $"Are you sure you want to delete '{preset.displayName}'?\n\nThis action cannot be undone.",
                "Delete", "Cancel"))
            {
                AssetDatabase.DeleteAsset(preset.path);
                AssetDatabase.Refresh(); // Refresh the asset database to reflect the change.
                RefreshPresetList();

                Debug.Log($"Deleted preset '{preset.displayName}'");
            }
        }

        /// <summary>
        /// Duplicates the currently selected preset, creating a new asset file with a unique name.
        /// </summary>
        private void DuplicateSelectedPreset()
        {
            if (_selectedPresetIndex < 0 || _selectedPresetIndex >= _presetList.Count) return;

            var preset = _presetList[_selectedPresetIndex];
            var profile = PresetManager.LoadPreset(preset.path);

            if (profile != null)
            {
                // Generate a unique path for the new preset copy.
                string newName = $"{preset.fileName}_Copy";
                string newPath = $"Assets/UIEffectsPro/Presets/{newName}.asset";

                // Ensure the path is unique by appending a number if the file already exists.
                int counter = 1;
                while (AssetDatabase.LoadAssetAtPath<UIEffectsPro.Runtime.UIEffectProfile>(newPath) != null)
                {
                    newName = $"{preset.fileName}_Copy{counter}";
                    newPath = $"Assets/UIEffectsPro/Presets/{newName}.asset";
                    counter++;
                }
                
                // Create the new preset asset.
                if (PresetManager.CreatePresetFromProfile(profile, newPath))
                {
                    RefreshPresetList();
                    Debug.Log($"Duplicated preset '{preset.displayName}' as '{newName}'");
                }
            }
        }
    }
}