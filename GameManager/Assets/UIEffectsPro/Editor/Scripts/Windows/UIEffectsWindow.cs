using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UIEffectsPro.Editor.Localization;
using System.IO; // Used for file operations when importing textures.

namespace UIEffectsPro.Editor
{
    /// <summary>
    /// The main editor window for creating and managing UI effects.
    /// This window provides a centralized interface for configuring UIEffectProfile assets
    /// and applying them to UI elements in real-time.
    /// </summary>
    public class UIEffectsWindow : EditorWindow
    {
        // --- Constants for window configuration ---
        private const string WINDOW_TITLE = "UI Effects Pro";
        private const string MENU_PATH = "Window/UI Effects Pro/Effects Window";
        private const float MIN_WINDOW_WIDTH = 400f;
        private const float MIN_WINDOW_HEIGHT = 650f;
        
        // --- Shader paths used by the tool ---
        private const string SHADER_URP = "UIEffects/RoundedBorder_URP";
        private const string SHADER_BUILTIN = "UIEffects/RoundedBorder_Builtin";
        private const string SHADER_LEGACY = "UIEffects/RoundedBorder";
        
        // --- Profile Management ---
        // The in-memory profile currently being edited in the window.
        private Runtime.UIEffectProfile _workingProfile;
        // The serialized representation of the working profile, used for property fields and undo/redo.
        private SerializedObject _serializedProfile;
        
        // --- Live Preview System ---
        private bool _previewEnabled = false;
        private Runtime.UIEffectComponent _previewTarget;
        private Runtime.UIEffectProfile _originalPreviewProfile;
        
        // --- GUI State ---
        private Vector2 _scrollPosition;
        private bool _cornersFoldout = true;
        private bool _borderFoldout = true;
        private bool _fillFoldout = true;
        private bool _blurFoldout = false;
        private bool _shadowFoldout = false;
        private bool _gradientFoldout = false; 
        private bool _textureFoldout = false;
        
        private bool _showPerformanceWarnings = true;
        
        // --- Custom GUI Styles ---
        private GUIStyle _titleStyle;
        private GUIStyle _sectionHeaderStyle;
        private GUIStyle _panelStyle;
        private GUIStyle _previewPanelStyle;
        private GUIStyle _warningBoxStyle;
        private GUIStyle _infoBoxStyle;
        private GUIStyle _actionButtonStyle;
        private GUIStyle _primaryButtonStyle;
        private GUIStyle _secondaryButtonStyle;
        private GUIStyle _separatorStyle;
        private GUIStyle _statusLabelStyle;
        private bool _stylesInitialized = false;
        
        // --- Shader Status ---
        private bool _shaderMissing = false;
        private string _missingShaderMessage = "";

        // --- Asset Picker Management ---
        // Stores the control ID for the texture asset picker to handle its result.
        private int _texturePickerControlID = -1;

        #region Window Management
        
        /// <summary>
        /// Creates and shows the UIEffectsWindow instance.
        /// This method is called from the Unity main menu.
        /// </summary>
        [MenuItem(MENU_PATH)]
        public static void ShowWindow()
        {
            UIEffectsWindow window = GetWindow<UIEffectsWindow>(WINDOW_TITLE);
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
            window.Show();
        }
        
        /// <summary>
        /// Called when the window is enabled.
        /// Initializes the working profile, subscribes to selection and language changes.
        /// </summary>
        private void OnEnable()
        {
            if (_workingProfile == null)
            {
                CreateDefaultWorkingProfile();
            }

            UpdateSerializedObject();
            Selection.selectionChanged += OnSelectionChanged;
            CheckShaderAvailability();
            
            LocalizationManager.OnLanguageChanged += OnLanguageChanged;
        }
        
        /// <summary>
        /// Called when the window is disabled or closed.
        /// Cleans up by unsubscribing from events and disabling any active preview.
        /// </summary>
        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
            DisablePreview();
        }
        
        /// <summary>
        /// Callback for when the localization language changes. Repaints the window to show new text.
        /// </summary>
        private void OnLanguageChanged(SupportedLanguage newLanguage)
        {
            Repaint();
        }
        
        #endregion
        
        #region Professional Styling
        
        /// <summary>
        /// Initializes custom GUIStyles for a more professional and consistent look.
        /// This runs only once.
        /// </summary>
        private void InitializeStyles()
        {
            if (_stylesInitialized) return;
            
            _titleStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 15, 20),
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.2f, 0.2f, 0.2f) }
            };
            
            _sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(5, 5, 8, 5),
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.8f, 0.8f, 0.8f) : new Color(0.3f, 0.3f, 0.3f) }
            };
            
            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(15, 15, 12, 12),
                margin = new RectOffset(8, 8, 4, 8),
                normal = { 
                    background = EditorGUIUtility.isProSkin ? 
                        MakeTex(1, 1, new Color(0.25f, 0.25f, 0.25f, 0.3f)) : 
                        MakeTex(1, 1, new Color(0.9f, 0.9f, 0.9f, 0.3f))
                }
            };
            
            _previewPanelStyle = new GUIStyle(_panelStyle)
            {
                normal = { 
                    background = EditorGUIUtility.isProSkin ? 
                        MakeTex(1, 1, new Color(0.2f, 0.4f, 0.6f, 0.2f)) : 
                        MakeTex(1, 1, new Color(0.6f, 0.8f, 1f, 0.2f))
                }
            };
            
            _warningBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(0, 0, 5, 8),
                fontSize = 11
            };
            
            _infoBoxStyle = new GUIStyle(_warningBoxStyle)
            {
                normal = { 
                    background = EditorGUIUtility.isProSkin ? 
                        MakeTex(1, 1, new Color(0.2f, 0.5f, 0.8f, 0.15f)) : 
                        MakeTex(1, 1, new Color(0.5f, 0.7f, 1f, 0.15f))
                }
            };
            
            _primaryButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(15, 15, 8, 8),
                margin = new RectOffset(2, 2, 3, 3),
                normal = { 
                    background = EditorGUIUtility.isProSkin ? 
                        MakeTex(1, 1, new Color(0.3f, 0.6f, 1f, 0.8f)) : 
                        MakeTex(1, 1, new Color(0.4f, 0.7f, 1f, 0.9f)),
                    textColor = Color.white
                }
            };
            
            _secondaryButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                padding = new RectOffset(12, 12, 6, 6),
                margin = new RectOffset(2, 2, 2, 2)
            };
            
            _actionButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                padding = new RectOffset(20, 20, 10, 10),
                margin = new RectOffset(4, 4, 4, 4)
            };
            
            _separatorStyle = new GUIStyle()
            {
                normal = { 
                    background = EditorGUIUtility.isProSkin ? 
                        MakeTex(1, 1, new Color(0.4f, 0.4f, 0.4f, 0.6f)) : 
                        MakeTex(1, 1, new Color(0.6f, 0.6f, 0.6f, 0.6f))
                },
                margin = new RectOffset(10, 10, 8, 8),
                fixedHeight = 1
            };
            
            _statusLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleRight
            };
            
            _stylesInitialized = true;
        }
        
        /// <summary>
        /// A utility function to create a 1x1 texture of a solid color.
        /// Used to set the background of GUIStyles.
        /// </summary>
        private Texture2D MakeTex(int width, int height, Color color)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = color;
            
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
        
        #endregion
        
        #region GUI Drawing
        
        /// <summary>
        /// The main GUI loop, called multiple times per frame by the Unity Editor.
        /// </summary>
        private void OnGUI()
        {
            InitializeStyles();
            
            // Ensure the profile objects are valid.
            if (_workingProfile == null || _serializedProfile == null)
            {
                CreateDefaultWorkingProfile();
                UpdateSerializedObject();
            }
            
            _serializedProfile.Update();

            // Check for and handle the result from the texture object picker.
            HandleObjectPickerResult();
            
            DrawHeader();
            
            // Main scrollable area for all settings.
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(_scrollPosition, GUILayout.ExpandHeight(true)))
            {
                _scrollPosition = scrollScope.scrollPosition;
                
                DrawLanguageSelector();
                DrawSeparator();
                DrawShaderStatus();
                DrawPerformanceInfo();
                DrawSeparator();
                DrawPreviewSection();
                DrawSeparator();
                DrawProfileEditor();
                DrawSeparator();
                DrawActionButtons();
                
                GUILayout.Space(20);
            }
            
            // Apply any changes made to the serialized properties.
            if (_serializedProfile.ApplyModifiedProperties())
            {
                // If preview is active, force the component to update.
                if (_previewEnabled && _previewTarget != null)
                {
                    _previewTarget.ForceUpdate();
                    EditorUtility.SetDirty(_previewTarget);
                    SceneView.RepaintAll();
                }
            }
        }
        
        /// <summary>
        /// Draws the main title and subtitle of the window.
        /// </summary>
        private void DrawHeader()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.Space(10);
                EditorGUILayout.LabelField(LocalizedGUI.Text("WINDOW_TITLE"), _titleStyle);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField(LocalizedGUI.Text("WINDOW_SUBTITLE"), EditorStyles.centeredGreyMiniLabel);
                    GUILayout.FlexibleSpace();
                }
                
                GUILayout.Space(5);
            }
        }
        
        /// <summary>
        /// Draws the language selection dropdown menu.
        /// </summary>
        private void DrawLanguageSelector()
        {
            using (new EditorGUILayout.VerticalScope(_panelStyle))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    LocalizedGUI.LanguageSelector();
                    GUILayout.FlexibleSpace();
                }
            }
        }
        
        /// <summary>
        /// Draws a horizontal line to visually separate sections.
        /// </summary>
        private void DrawSeparator()
        {
            EditorGUILayout.Space(5);
            GUILayout.Box("", _separatorStyle, GUILayout.ExpandWidth(true));
            EditorGUILayout.Space(5);
        }
        
        /// <summary>
        /// Displays an error box if the required shaders are not found in the project.
        /// </summary>
        private void DrawShaderStatus()
        {
            if (_shaderMissing)
            {
                using (new EditorGUILayout.VerticalScope(_warningBoxStyle))
                {
                    EditorGUILayout.LabelField($"⚠ {LocalizedGUI.Text("SHADER_STATUS")}", _sectionHeaderStyle);
                    EditorGUILayout.HelpBox(_missingShaderMessage, MessageType.Error);
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(LocalizedGUI.Text("REFRESH_SHADER_CHECK"), _secondaryButtonStyle, GUILayout.Width(200)))
                        {
                            CheckShaderAvailability();
                        }
                    }
                }
                EditorGUILayout.Space(5);
            }
        }
        
        /// <summary>
        /// Shows performance-related warnings if expensive effects like blur or shadow are enabled.
        /// </summary>
        private void DrawPerformanceInfo()
        {
            if (_showPerformanceWarnings && _workingProfile != null)
            {
                bool hasBlur = _workingProfile.enableBlur;
                bool hasShadow = _workingProfile.enableShadow;
                bool hasTexture = _workingProfile.enableTexture && _workingProfile.overlayTexture != null;

                if (hasBlur || hasShadow || hasTexture)
                {
                    using (new EditorGUILayout.VerticalScope(_warningBoxStyle))
                    {
                        EditorGUILayout.LabelField(LocalizedGUI.Text("PERFORMANCE_IMPACT"), _sectionHeaderStyle);
                        
                        string warning = "";
                        string icon = "";
                        MessageType msgType = MessageType.Info;

                        int effectCount = (hasBlur ? 1 : 0) + (hasShadow ? 1 : 0) + (hasTexture ? 1 : 0);
                        if (effectCount >= 3)
                        {
                            warning = "Multiple effects enabled (Blur + Shadow + Texture). Consider optimizing for mobile platforms.";
                            icon = "⚠️ ";
                            msgType = MessageType.Warning;
                        }
                        else if (effectCount == 2)
                        {
                            if (hasBlur && hasShadow)
                            {
                                warning = LocalizedGUI.Text("BLUR_SHADOW_WARNING");
                                icon = "⚠️ ";
                                msgType = MessageType.Warning;
                            }
                            else if (hasBlur && hasTexture)
                            {
                                warning = "Blur + Texture effects enabled. May impact performance on mobile devices.";
                                icon = "⚠️ ";
                                msgType = MessageType.Warning;
                            }
                            else if (hasShadow && hasTexture)
                            {
                                warning = "Shadow + Texture effects enabled. Monitor performance on lower-end devices.";
                                icon = "";
                                msgType = MessageType.Info;
                            }
                        }
                        else if (hasBlur)
                        {
                            warning = LocalizedGUI.Text("BLUR_WARNING");
                            icon = "";
                            msgType = MessageType.Info;
                        }
                        else if (hasShadow)
                        {
                            warning = LocalizedGUI.Text("SHADOW_WARNING");
                            icon = "";
                            msgType = MessageType.Info;
                        }
                        else if (hasTexture)
                        {
                            if (_workingProfile.overlayTexture != null)
                            {
                                int textureSize = _workingProfile.overlayTexture.width * _workingProfile.overlayTexture.height;
                                if (textureSize > 1024 * 1024)
                                {
                                    warning = "Large texture detected. Consider using smaller textures for better performance.";
                                    icon = "⚠️ ";
                                    msgType = MessageType.Warning;
                                }
                                else
                                {
                                    warning = "Texture overlay enabled. Minimal performance impact.";
                                    icon = "";
                                    msgType = MessageType.Info;
                                }
                            }
                        }
                        
                        EditorGUILayout.HelpBox($"{icon} {warning}", msgType);
                        
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button(LocalizedGUI.Text("HIDE_TIPS"), EditorStyles.miniButton, GUILayout.Width(100)))
                            {
                                _showPerformanceWarnings = false;
                            }
                        }
                    }
                    EditorGUILayout.Space(5);
                }
            }
        }
        
        /// <summary>
        /// Draws the UI for the Live Preview feature, including Start/Stop buttons and status info.
        /// </summary>
        private void DrawPreviewSection()
        {
            using (new EditorGUILayout.VerticalScope(_previewPanelStyle))
            {
                EditorGUILayout.LabelField(LocalizedGUI.Text("LIVE_PREVIEW"), _sectionHeaderStyle);
                
                GameObject selectedObject = Selection.activeGameObject;
                bool hasValidTarget = selectedObject != null && 
                    (selectedObject.GetComponent<Image>() != null || selectedObject.GetComponent<RawImage>() != null);
                
                if (_previewEnabled && _previewTarget != null)
                {
                    using (new EditorGUILayout.VerticalScope(_infoBoxStyle))
                    {
                        EditorGUILayout.LabelField($"{LocalizedGUI.Text("PREVIEWING_ON")}: {_previewTarget.gameObject.name}", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(LocalizedGUI.Text("CHANGES_REALTIME"), EditorStyles.miniLabel);
                    }
                }
                
                EditorGUILayout.Space(8);
                
                EditorGUI.BeginDisabledGroup(!hasValidTarget || _shaderMissing);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(LocalizedGUI.Text("START_PREVIEW"), _primaryButtonStyle))
                    {
                        EnablePreviewOnSelected();
                    }
                    
                    if (GUILayout.Button(LocalizedGUI.Text("STOP_PREVIEW"), _secondaryButtonStyle))
                    {
                        DisablePreview();
                    }
                }
                
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.Space(5);
                
                if (_shaderMissing)
                {
                    EditorGUILayout.HelpBox(LocalizedGUI.Text("PREVIEW_UNAVAILABLE"), MessageType.Error);
                }
                else if (!hasValidTarget && selectedObject != null)
                {
                    EditorGUILayout.HelpBox(LocalizedGUI.Text("SELECTION_MUST_HAVE"), MessageType.Warning);
                }
                else if (selectedObject == null)
                {
                    using (new EditorGUILayout.VerticalScope(_infoBoxStyle))
                    {
                        EditorGUILayout.LabelField(LocalizedGUI.Text("QUICK_START"), EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(LocalizedGUI.Text("STEP_1"), EditorStyles.miniLabel);
                        EditorGUILayout.LabelField(LocalizedGUI.Text("STEP_2"), EditorStyles.miniLabel);
                        EditorGUILayout.LabelField(LocalizedGUI.Text("STEP_3"), EditorStyles.miniLabel);
                    }
                }
            }
        }
        
        /// <summary>
        /// The main container that draws all the individual effect setting sections.
        /// </summary>
        private void DrawProfileEditor()
        {
            using (new EditorGUILayout.VerticalScope(_panelStyle))
            {
                EditorGUILayout.LabelField(LocalizedGUI.Text("EFFECT_SETTINGS"), _sectionHeaderStyle);
                EditorGUILayout.Space(5);
                
                DrawCornerSettings();
                EditorGUILayout.Space(8);
                
                DrawBorderSettings();
                EditorGUILayout.Space(8);
                
                DrawFillSettings();
                EditorGUILayout.Space(8);
                
                DrawBlurSettings();
                EditorGUILayout.Space(8);
                
                DrawShadowSettings();
                EditorGUILayout.Space(8);
                
                DrawGradientSettings();
                EditorGUILayout.Space(8);
                
                DrawTextureSettings();
            }
        }
        
        /// <summary>
        /// Draws UI controls for corner radius settings.
        /// </summary>
        private void DrawCornerSettings()
        {
            _cornersFoldout = EditorGUILayout.Foldout(_cornersFoldout, LocalizedGUI.Text("CORNER_RADIUS"), true, EditorStyles.foldoutHeader);
            if (_cornersFoldout)
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    EditorGUI.indentLevel++;
                    
                    EditorGUILayout.PropertyField(_serializedProfile.FindProperty("cornerRadiusUnit"), LocalizedGUI.Content("UNIT"));
                    EditorGUILayout.PropertyField(_serializedProfile.FindProperty("useIndividualCorners"), LocalizedGUI.Content("INDIVIDUAL_CORNERS"));
                    
                    bool useIndividual = _serializedProfile.FindProperty("useIndividualCorners").boolValue;
                    string radiusLabel = (_workingProfile.cornerRadiusUnit == Runtime.UIEffectProfile.Unit.Percent) ? "%" : "px";
                    
                    EditorGUILayout.Space(3);
                    
                    if (useIndividual)
                    {
                        using (new EditorGUILayout.VerticalScope())
                        {
                            // LÍNIA MODIFICADA
                            EditorGUILayout.PropertyField(_serializedProfile.FindProperty("cornerRadiusTopLeft"), 
                                new GUIContent(LocalizedGUI.Text("TOP_LEFT"), $"{LocalizedGUI.Text("TOP_LEFT")} ({radiusLabel})"));
                            // LÍNIA MODIFICADA
                            EditorGUILayout.PropertyField(_serializedProfile.FindProperty("cornerRadiusTopRight"), 
                                new GUIContent(LocalizedGUI.Text("TOP_RIGHT"), $"{LocalizedGUI.Text("TOP_RIGHT")} ({radiusLabel})"));
                            // LÍNIA MODIFICADA
                            EditorGUILayout.PropertyField(_serializedProfile.FindProperty("cornerRadiusBottomLeft"), 
                                new GUIContent(LocalizedGUI.Text("BOTTOM_LEFT"), $"{LocalizedGUI.Text("BOTTOM_LEFT")} ({radiusLabel})"));
                            // LÍNIA MODIFICADA
                            EditorGUILayout.PropertyField(_serializedProfile.FindProperty("cornerRadiusBottomRight"), 
                                new GUIContent(LocalizedGUI.Text("BOTTOM_RIGHT"), $"{LocalizedGUI.Text("BOTTOM_RIGHT")} ({radiusLabel})"));
                        }
                    }
                    else
                    {
                        // LÍNIA MODIFICADA
                        EditorGUILayout.PropertyField(_serializedProfile.FindProperty("globalCornerRadius"), 
                            new GUIContent(LocalizedGUI.Text("GLOBAL_RADIUS"), $"{LocalizedGUI.Text("GLOBAL_RADIUS")} ({radiusLabel})"));
                    }
                    
                    EditorGUI.indentLevel--;
                }
            }
        }
        
        /// <summary>
        /// Draws UI controls for border settings.
        /// </summary>
        private void DrawBorderSettings()
        {
            _borderFoldout = EditorGUILayout.Foldout(_borderFoldout, LocalizedGUI.Text("BORDER_SETTINGS"), true, EditorStyles.foldoutHeader);
            if (_borderFoldout)
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    EditorGUI.indentLevel++;
                    
                    // ── Border Appearance ──
                    EditorGUILayout.LabelField("Appearance", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(_serializedProfile.FindProperty("borderWidthUnit"), LocalizedGUI.Content("UNIT"));
                    string borderLabel = (_workingProfile.borderWidthUnit == Runtime.UIEffectProfile.Unit.Percent) ? "%" : "px";
                    EditorGUILayout.PropertyField(_serializedProfile.FindProperty("borderWidth"), 
                        new GUIContent(LocalizedGUI.Text("WIDTH"), $"{LocalizedGUI.Text("WIDTH")} ({borderLabel})"));
                    EditorGUILayout.PropertyField(_serializedProfile.FindProperty("borderColor"), 
                        new GUIContent("Color A", "Primary border color"));

                    // ── Border Gradient ──
                    EditorGUILayout.Space(8);
                    DrawSeparatorLine();
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Border Gradient", EditorStyles.boldLabel);

                    EditorGUILayout.PropertyField(_serializedProfile.FindProperty("useBorderGradient"), 
                        new GUIContent("Enable", "Enable gradient color on the border line."));

                    if (_workingProfile.useBorderGradient)
                    {
                        EditorGUI.indentLevel++;
                        
                        EditorGUILayout.PropertyField(_serializedProfile.FindProperty("borderColorB"), 
                            new GUIContent("Color B", "Second color for the border gradient."));
                        
                        EditorGUILayout.PropertyField(_serializedProfile.FindProperty("borderGradientType"), 
                            new GUIContent("Type", "Gradient type: Linear, Radial, or Angular."));
                        
                        var gradType = _workingProfile.borderGradientType;
                        if (gradType == Runtime.UIEffectProfile.GradientParams.GradientType.Linear)
                        {
                            EditorGUILayout.PropertyField(_serializedProfile.FindProperty("borderGradientAngle"), 
                                new GUIContent("Angle", "Angle for the linear gradient (degrees)."));
                        }
                        else if (gradType == Runtime.UIEffectProfile.GradientParams.GradientType.Radial)
                        {
                            EditorGUILayout.PropertyField(_serializedProfile.FindProperty("borderGradientRadialCenter"), 
                                new GUIContent("Center", "Center point of the radial gradient (0-1)."));
                            EditorGUILayout.PropertyField(_serializedProfile.FindProperty("borderGradientRadialScale"), 
                                new GUIContent("Scale", "Zoom/scale of the radial gradient."));
                        }
                        else if (gradType == Runtime.UIEffectProfile.GradientParams.GradientType.Angular)
                        {
                            EditorGUILayout.PropertyField(_serializedProfile.FindProperty("borderGradientAngularRotation"), 
                                new GUIContent("Rotation", "Rotation offset for the angular gradient (degrees)."));
                        }
                        
                        // Gradient preview
                        EditorGUILayout.Space(2);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Preview:", GUILayout.Width(90));
                            Rect gradientRect = GUILayoutUtility.GetRect(100, EditorGUIUtility.singleLineHeight);
                            DrawGradientPreview(gradientRect, _workingProfile.borderColor, _workingProfile.borderColorB);
                        }
                        
                        EditorGUI.indentLevel--;
                    }

                    // ── Progress Border ──
                    EditorGUILayout.Space(8);
                    DrawSeparatorLine();
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Progress Border", EditorStyles.boldLabel);
                    
                    EditorGUILayout.PropertyField(_serializedProfile.FindProperty("useProgressBorder"), 
                        new GUIContent("Enable", "Enable progress border effect."));
                    
                    if (_workingProfile.useProgressBorder)
                    {
                        EditorGUI.indentLevel++;
                        
                        EditorGUILayout.PropertyField(_serializedProfile.FindProperty("progressValue"), 
                            LocalizedGUI.Content("PROGRESS_VALUE"));
                        EditorGUILayout.PropertyField(_serializedProfile.FindProperty("progressStartAngle"), 
                            LocalizedGUI.Content("PROGRESS_START_ANGLE"));
                        EditorGUILayout.PropertyField(_serializedProfile.FindProperty("progressDirection"), 
                            LocalizedGUI.Content("PROGRESS_DIRECTION"));
                        
                        // ── Progress Color Gradient (sub-section) ──
                        EditorGUILayout.Space(4);
                        EditorGUILayout.PropertyField(_serializedProfile.FindProperty("useProgressColorGradient"), 
                            new GUIContent("Color Gradient", "Interpolate border color based on progress value."));
                        
                        if (_workingProfile.useProgressColorGradient)
                        {
                            EditorGUI.indentLevel++;
                        
                            EditorGUILayout.PropertyField(_serializedProfile.FindProperty("progressColorStart"), 
                                LocalizedGUI.Content("PROGRESS_COLOR_START"));
                            EditorGUILayout.PropertyField(_serializedProfile.FindProperty("progressColorEnd"), 
                                LocalizedGUI.Content("PROGRESS_COLOR_END"));
                        
                            // Preview
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField("Preview:", GUILayout.Width(90));
                                Rect gradientRect = GUILayoutUtility.GetRect(100, EditorGUIUtility.singleLineHeight);
                                DrawGradientPreview(gradientRect, _workingProfile.progressColorStart, _workingProfile.progressColorEnd);
                            }
                        
                            EditorGUI.indentLevel--;
                        }
                        
                        EditorGUI.indentLevel--;
                    }
                    
                    EditorGUI.indentLevel--;
                }
            }
        }
        
        /// <summary>
        /// Draws a thin horizontal separator line in the editor.
        /// </summary>
        private void DrawSeparatorLine()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }
        
        /// <summary>
        /// Draws UI controls for the main fill color.
        /// </summary>
        private void DrawFillSettings()
        {
            _fillFoldout = EditorGUILayout.Foldout(_fillFoldout, LocalizedGUI.Text("FILL_SETTINGS"), true, EditorStyles.foldoutHeader);
            if (_fillFoldout)
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    EditorGUI.indentLevel++;
                    
                    // LÍNIA MODIFICADA
                    EditorGUILayout.PropertyField(_serializedProfile.FindProperty("fillColor"), 
                        LocalizedGUI.Content("FILL_COLOR"));
                    
                    EditorGUI.indentLevel--;
                }
            }
        }
        
        /// <summary>
        /// Draws UI controls for the blur effect.
        /// </summary>
        private void DrawBlurSettings()
        {
            _blurFoldout = EditorGUILayout.Foldout(_blurFoldout, LocalizedGUI.Text("BLUR_EFFECT"), true, EditorStyles.foldoutHeader);
            if (_blurFoldout)
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    EditorGUI.indentLevel++;
                    
                    EditorGUILayout.PropertyField(_serializedProfile.FindProperty("enableBlur"), 
                        LocalizedGUI.Content("ENABLE_BLUR"));
                    
                    bool blurEnabled = _serializedProfile.FindProperty("enableBlur").boolValue;
                    
                    EditorGUI.BeginDisabledGroup(!blurEnabled);
                    
                    SerializedProperty blurParams = _serializedProfile.FindProperty("blurParams");
                    if (blurParams != null)
                    {
                        EditorGUILayout.Space(3);
                        
                        // NEW SECTION: Blur Type Selector
                        SerializedProperty blurTypeProp = blurParams.FindPropertyRelative("blurType");
                        string[] blurTypeNames = new string[] 
                        { 
                            LocalizedGUI.Text("BLUR_TYPE_INTERNAL"), 
                            LocalizedGUI.Text("BLUR_TYPE_BACKGROUND") 
                        };
                        
                        // Use a normal popup with localized text
                        blurTypeProp.enumValueIndex = EditorGUILayout.Popup(
                            LocalizedGUI.Text("BLUR_TYPE"),
                            blurTypeProp.enumValueIndex,
                            blurTypeNames
                        );
                        
                        // Warning for Background Blur (Type 1)
                        if (blurTypeProp.enumValueIndex == 1 && blurEnabled)
                        {
                            EditorGUILayout.HelpBox(
                                LocalizedGUI.Text("BLUR_BACKGROUND_WARNING"),
                                MessageType.Warning
                            );
                        }
                        // END NEW SECTION
                        
                        EditorGUILayout.PropertyField(blurParams.FindPropertyRelative("radius"), 
                            LocalizedGUI.Content("RADIUS"));
                        EditorGUILayout.PropertyField(blurParams.FindPropertyRelative("iterations"), 
                            LocalizedGUI.Content("ITERATIONS"));
                        EditorGUILayout.PropertyField(blurParams.FindPropertyRelative("downsample"), 
                            LocalizedGUI.Content("DOWNSAMPLE"));
                        
                        if (blurEnabled)
                        {
                            EditorGUILayout.Space(5);
                            using (new EditorGUILayout.VerticalScope(_infoBoxStyle))
                            {
                                EditorGUILayout.LabelField(LocalizedGUI.Text("BLUR_PERFORMANCE_TIP"), _statusLabelStyle);
                            }
                        }
                    }
                    
                    EditorGUI.EndDisabledGroup();
                    
                    EditorGUI.indentLevel--;
                }
            }
        }
        
        /// <summary>
        /// Draws UI controls for the drop shadow effect.
        /// </summary>
        private void DrawShadowSettings()
        {
            _shadowFoldout = EditorGUILayout.Foldout(_shadowFoldout, LocalizedGUI.Text("DROP_SHADOW"), true, EditorStyles.foldoutHeader);
            if (_shadowFoldout)
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    EditorGUI.indentLevel++;
                    
                    EditorGUILayout.PropertyField(_serializedProfile.FindProperty("enableShadow"), 
                        LocalizedGUI.Content("ENABLE_SHADOW"));
                    
                    bool shadowEnabled = _serializedProfile.FindProperty("enableShadow").boolValue;
                    
                    EditorGUI.BeginDisabledGroup(!shadowEnabled);
                    
                    EditorGUILayout.PropertyField(_serializedProfile.FindProperty("shadowUnit"), LocalizedGUI.Content("UNIT"));
                    string shadowLabel = (_workingProfile.shadowUnit == Runtime.UIEffectProfile.Unit.Percent) ? "%" : "px";
                    
                    SerializedProperty shadowParams = _serializedProfile.FindProperty("shadowParams");
                    if (shadowParams != null)
                    {
                        EditorGUILayout.Space(3);
                        EditorGUILayout.PropertyField(shadowParams.FindPropertyRelative("color"), 
                            LocalizedGUI.Content("SHADOW_COLOR"));
                        EditorGUILayout.PropertyField(shadowParams.FindPropertyRelative("offset"), 
                            new GUIContent(LocalizedGUI.Text("OFFSET"), $"{LocalizedGUI.Text("OFFSET")} ({shadowLabel})"));
                        EditorGUILayout.PropertyField(shadowParams.FindPropertyRelative("blur"), 
                            new GUIContent(LocalizedGUI.Text("BLUR"), $"{LocalizedGUI.Text("BLUR")} ({shadowLabel})"));
                        EditorGUILayout.PropertyField(shadowParams.FindPropertyRelative("opacity"), 
                            LocalizedGUI.Content("OPACITY"));
                        
                        if (shadowEnabled)
                        {
                            EditorGUILayout.Space(3);
                            Vector2 offset = shadowParams.FindPropertyRelative("offset").vector2Value;
                            float distance = offset.magnitude;
                            
                            using (new EditorGUILayout.VerticalScope(_infoBoxStyle))
                            {
                                EditorGUILayout.LabelField($"{LocalizedGUI.Text("SHADOW_DISTANCE")}: {distance:F1}{shadowLabel}", _statusLabelStyle);
                            }
                        }
                    }
                    
                    EditorGUI.EndDisabledGroup();
                    
                    EditorGUI.indentLevel--;
                }
            }
        }
        
        /// <summary>
        /// Draws UI controls for the gradient fill effect.
        /// </summary>
        private void DrawGradientSettings()
        {
            _gradientFoldout = EditorGUILayout.Foldout(_gradientFoldout, LocalizedGUI.Text("GRADIENT_FILL"), true, EditorStyles.foldoutHeader);
            if (_gradientFoldout)
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    EditorGUI.indentLevel++;
                    
                    SerializedProperty enableGradientProp = _serializedProfile.FindProperty("enableGradient");
                    EditorGUILayout.PropertyField(enableGradientProp, 
                        LocalizedGUI.Content("ENABLE_GRADIENT"));
                    
                    bool gradientEnabled = enableGradientProp.boolValue;
                    
                    EditorGUI.BeginDisabledGroup(!gradientEnabled);
                    
                    SerializedProperty gradientParams = _serializedProfile.FindProperty("gradientParams");
                    if (gradientParams != null)
                    {
                        EditorGUILayout.Space(3);
                        EditorGUILayout.PropertyField(gradientParams.FindPropertyRelative("type"), 
                            LocalizedGUI.Content("TYPE"));
                        EditorGUILayout.PropertyField(gradientParams.FindPropertyRelative("colorA"), 
                            LocalizedGUI.Content("COLOR_A"));
                        EditorGUILayout.PropertyField(gradientParams.FindPropertyRelative("colorB"), 
                            LocalizedGUI.Content("COLOR_B"));

                        var gradType = (Runtime.UIEffectProfile.GradientParams.GradientType)gradientParams.FindPropertyRelative("type").enumValueIndex;
                        
                        // Controls specific to each gradient type
                        if (gradType == Runtime.UIEffectProfile.GradientParams.GradientType.Linear)
                        {
                            EditorGUILayout.PropertyField(gradientParams.FindPropertyRelative("angle"),
                                LocalizedGUI.Content("ANGLE", "Angle for the linear gradient"));
                        }
                        else if (gradType == Runtime.UIEffectProfile.GradientParams.GradientType.Radial)
                        {
                            // NEW: Controls for Radial
                            EditorGUILayout.PropertyField(gradientParams.FindPropertyRelative("radialCenter"),
                                LocalizedGUI.Content("GRADIENT_RADIAL_CENTER", "Center point of the radial gradient (0-1)"));
                            EditorGUILayout.PropertyField(gradientParams.FindPropertyRelative("radialScale"),
                                LocalizedGUI.Content("GRADIENT_RADIAL_SCALE", "Zoom/scale of the radial gradient"));
                        }
                        else if (gradType == Runtime.UIEffectProfile.GradientParams.GradientType.Angular)
                        {
                            // NEW: Controls for Angular
                            EditorGUILayout.PropertyField(gradientParams.FindPropertyRelative("angularRotation"),
                                LocalizedGUI.Content("GRADIENT_ANGULAR_ROTATION", "Rotation offset for the angular gradient (degrees)"));
                        }
                    }
                    
                    EditorGUI.EndDisabledGroup();

                    if(gradientEnabled)
                    {
                        EditorGUILayout.Space(3);
                        using (new EditorGUILayout.VerticalScope(_infoBoxStyle))
                        {
                            EditorGUILayout.LabelField(LocalizedGUI.Text("GRADIENT_OVERRIDE_TIP"), _statusLabelStyle);
                        }
                    }
                    
                    EditorGUI.indentLevel--;
                }
            }
        }
        
        /// <summary>
        /// Draws the main action buttons like "Apply" and "Reset", and preset management buttons.
        /// </summary>
        private void DrawActionButtons()
        {
            using (new EditorGUILayout.VerticalScope(_panelStyle))
            {
                EditorGUILayout.LabelField(LocalizedGUI.Text("ACTIONS"), _sectionHeaderStyle);
                EditorGUILayout.Space(8);
                
                EditorGUI.BeginDisabledGroup(_shaderMissing);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(LocalizedGUI.Text("APPLY_TO_SELECTED"), _primaryButtonStyle, GUILayout.Height(35)))
                    {
                        ApplyToSelected();
                    }
                    
                    if (GUILayout.Button(LocalizedGUI.Text("RESET_SETTINGS"), _actionButtonStyle, GUILayout.Height(35)))
                    {
                        ResetProfile();
                    }
                }
                
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.Space(15);
                
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(LocalizedGUI.Text("PRESET_MANAGEMENT"), EditorStyles.boldLabel);
                    EditorGUILayout.Space(3);
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button(LocalizedGUI.Text("SAVE_PRESET"), _secondaryButtonStyle))
                        {
                            SavePreset();
                        }
                        
                        if (GUILayout.Button(LocalizedGUI.Text("LOAD_PRESET"), _secondaryButtonStyle))
                        {
                            LoadPreset();
                        }
                    }
                }
            }
        }
        
        #endregion
        
        #region Sprite Atlas Handling
        /// <summary>
        /// Checks if an Image component is using a sprite from an atlas.
        /// </summary>
        private bool IsUsingSpriteAtlas(Image image)
        {
            if (image == null || image.sprite == null) return false;
                // Check if the sprite's texture is different from the sprite's associated texture
            // This indicates it's packed in an atlas
            Texture2D spriteTexture = image.sprite.texture;
            if (spriteTexture != null && spriteTexture.name.Contains("Atlas"))
            {
                return true;
            }
                // Alternative check: if sprite is packed
            if (image.sprite.packed)
            {
                return true;
            }
                return false;
        }
        /// <summary>
        /// Prepares an Image component for UI Effects, handling Sprite Atlas correctly.
        /// </summary>
        private void PrepareImageForEffect(Image image)
        {
            if (image == null) return;
                Undo.RecordObject(image, "Prepare Image for UI Effect");
                bool usesAtlas = IsUsingSpriteAtlas(image);
                if (usesAtlas)
            {
                // For atlas sprites, we keep the sprite but change the type
                // This preserves UV coordinates while allowing our effect to work
                // if (showDebugInfo)
                // {
                //     Debug.Log($"Image '{image.gameObject.name}' uses Sprite Atlas. Preserving sprite reference.");
                // }
                        // Keep sprite but ensure it's in Simple mode
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                        // Important: We DON'T set sprite to null for atlas sprites
            }
            else
            {
                // For non-atlas sprites, we can safely remove the sprite
                // if (showDebugInfo && image.sprite != null)
                // {
                //     Debug.Log($"Image '{image.gameObject.name}' uses standalone sprite. Removing sprite reference.");
                // }
                        image.sprite = null;
                image.type = Image.Type.Simple;
            }
        }
        #endregion

        #region Preview System
        
        /// <summary>
        /// Called when the user's selection changes in the Hierarchy or Project windows.
        /// Repaints the window to update the UI state.
        /// </summary>
        private void OnSelectionChanged()
        {
            Repaint();
        }

        /// <summary>
        /// Enables the live preview on the currently selected GameObject.
        /// </summary>
        private void EnablePreviewOnSelected()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog(LocalizedGUI.Text("NO_SELECTION"), LocalizedGUI.Text("SELECT_GAMEOBJECT_FIRST"), "OK");
                return;
            }
            
            var image = selected.GetComponent<Image>();
            var rawImage = selected.GetComponent<RawImage>();
            
            if (image == null && rawImage == null)
            {
                EditorUtility.DisplayDialog(LocalizedGUI.Text("INVALID_TARGET"), 
                    LocalizedGUI.Text("OBJECT_MUST_HAVE_IMAGE"), "OK");
                return;
            }
            
            if (_shaderMissing)
            {
                EditorUtility.DisplayDialog(LocalizedGUI.Text("SHADERS_MISSING"), 
                    LocalizedGUI.Text("CANNOT_PREVIEW_SHADERS"), "OK");
                return;
            }
            
            // Try to load existing settings from the selected object for a better user experience.
            bool settingsCopied = SmartCopySettingsFromSelected(selected);
            
            // If we are already previewing another object, clean it up first.
            if (_previewEnabled && _previewTarget != null && _previewTarget.gameObject != selected)
            {
                CleanupPreviousPreview();
            }
            
            // Use the new method instead of directly setting sprite to null
            if (image != null)
            {
                PrepareImageForEffect(image); // ← CANVI AQUÍ
            }
            
            // Get or add the UIEffectComponent.
            _previewTarget = selected.GetComponent<Runtime.UIEffectComponent>();
            if (_previewTarget == null)
            {
                _previewTarget = Undo.AddComponent<Runtime.UIEffectComponent>(selected);
            }
            
            // Store the original profile so it can be restored later.
            _originalPreviewProfile = _previewTarget.profile;
            
            Undo.RecordObject(_previewTarget, "Enable UI Effect Preview");
            
            // Assign the temporary working profile to the component.
            _previewTarget.SetProfile(_workingProfile);
            _previewTarget.ForceUpdate();
            
            EditorUtility.SetDirty(_previewTarget);
            SceneView.RepaintAll();
            
            _previewEnabled = true;
            
            string copyMessage = settingsCopied ? " (settings copied)" : "";
            Debug.Log($"Preview enabled on {selected.name} using working profile{copyMessage}. Original profile preserved.");
        }

        /// <summary>
        /// Tries to automatically copy settings from the selected object into the working profile.
        /// It checks for an existing UIEffectComponent or a compatible material.
        /// </summary>
        private bool SmartCopySettingsFromSelected(GameObject selected)
        {
            if (selected == null) return false;
            
            try
            {
                var effectComponent = selected.GetComponent<Runtime.UIEffectComponent>();
                if (effectComponent != null && effectComponent.profile != null)
                {
                    CopySettingsFromUIEffectComponent(effectComponent);
                    return true;
                }
                
                var image = selected.GetComponent<Image>();
                var rawImage = selected.GetComponent<RawImage>();
                Graphic targetGraphic = image != null ? (Graphic)image : (Graphic)rawImage;
                
                if (targetGraphic != null && targetGraphic.material != null && IsCompatibleMaterial(targetGraphic.material))
                {
                    CopyMaterialPropertiesToProfile(targetGraphic.material, _workingProfile);
                    UpdateSerializedObject();
                    return true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not copy settings from selected object: {e.Message}");
            }
            
            return false;
        }

        /// <summary>
        /// Copies settings from an existing UIEffectComponent's profile to the working profile.
        /// </summary>
        private void CopySettingsFromUIEffectComponent(Runtime.UIEffectComponent sourceComponent)
        {
            if (sourceComponent?.profile == null) return;
            
            var sourceProfile = sourceComponent.profile;
            
            EditorUtility.CopySerialized(sourceProfile, _workingProfile);
            _workingProfile.name = "Working Profile";
            
            UpdateSerializedObject();
            
            Debug.Log($"Settings copied from UIEffectComponent (Profile: {sourceProfile.name})");
        }

        /// <summary>
        /// Checks if a material is likely compatible with UIEffects shaders.
        /// </summary>
        private bool IsCompatibleMaterial(Material material)
        {
            if (material == null || material.shader == null) return false;
            
            string shaderName = material.shader.name;
            
            return shaderName.Contains("UIEffects") || 
                   shaderName.Contains("RoundedBorder") ||
                   HasUIEffectsProperties(material);
        }

        /// <summary>
        /// A helper function to check if a material has the common properties of a UIEffects shader.
        /// </summary>
        private bool HasUIEffectsProperties(Material material)
        {
            return material.HasProperty("_CornerRadii") ||
                   material.HasProperty("_BorderWidth") ||
                   material.HasProperty("_BorderColor") ||
                   material.HasProperty("_EnableShadow") ||
                   material.HasProperty("_EnableBlur");
        }

        /// <summary>
        /// Reads shader properties from a material and applies them to the working profile.
        /// This allows "importing" settings from a manually configured material.
        /// </summary>
        private void CopyMaterialPropertiesToProfile(Material sourceMaterial, Runtime.UIEffectProfile targetProfile)
        {
            if (sourceMaterial.HasProperty("_CornerRadii"))
            {
                Vector4 cornerRadii = sourceMaterial.GetVector("_CornerRadii");
                
                bool hasIndividualCorners = !Mathf.Approximately(cornerRadii.x, cornerRadii.y) ||
                                            !Mathf.Approximately(cornerRadii.y, cornerRadii.z) ||
                                            !Mathf.Approximately(cornerRadii.z, cornerRadii.w);
                
                targetProfile.useIndividualCorners = hasIndividualCorners;
                
                if (hasIndividualCorners)
                {
                    targetProfile.cornerRadiusTopLeft = cornerRadii.x;
                    targetProfile.cornerRadiusTopRight = cornerRadii.y;
                    targetProfile.cornerRadiusBottomRight = cornerRadii.z;
                    targetProfile.cornerRadiusBottomLeft = cornerRadii.w;
                }
                else
                {
                    targetProfile.globalCornerRadius = cornerRadii.x;
                }
                
                targetProfile.cornerRadiusUnit = Runtime.UIEffectProfile.Unit.Pixels;
            }
            
            if (sourceMaterial.HasProperty("_BorderWidth"))
            {
                targetProfile.borderWidth = sourceMaterial.GetFloat("_BorderWidth");
                targetProfile.borderWidthUnit = Runtime.UIEffectProfile.Unit.Pixels;
            }
            
            if (sourceMaterial.HasProperty("_BorderColor"))
            {
                targetProfile.borderColor = sourceMaterial.GetColor("_BorderColor");
            }
            
            if (sourceMaterial.HasProperty("_BorderColorB"))
            {
                targetProfile.borderColorB = sourceMaterial.GetColor("_BorderColorB");
            }
            
            if (sourceMaterial.HasProperty("_UseBorderGradient"))
            {
                targetProfile.useBorderGradient = sourceMaterial.GetFloat("_UseBorderGradient") > 0.5f;
            }
            
            if (sourceMaterial.HasProperty("_BorderGradientAngle"))
            {
                targetProfile.borderGradientAngle = sourceMaterial.GetFloat("_BorderGradientAngle") * Mathf.Rad2Deg;
            }
            
            if (sourceMaterial.HasProperty("_BorderGradientType"))
            {
                targetProfile.borderGradientType = (Runtime.UIEffectProfile.GradientParams.GradientType)
                    Mathf.RoundToInt(sourceMaterial.GetFloat("_BorderGradientType"));
            }
            
            if (sourceMaterial.HasProperty("_BorderGradientRadialCenter"))
            {
                Vector4 center = sourceMaterial.GetVector("_BorderGradientRadialCenter");
                targetProfile.borderGradientRadialCenter = new Vector2(center.x, center.y);
            }
            
            if (sourceMaterial.HasProperty("_BorderGradientRadialScale"))
            {
                targetProfile.borderGradientRadialScale = sourceMaterial.GetFloat("_BorderGradientRadialScale");
            }
            
            if (sourceMaterial.HasProperty("_BorderGradientAngularRotation"))
            {
                targetProfile.borderGradientAngularRotation = sourceMaterial.GetFloat("_BorderGradientAngularRotation") * Mathf.Rad2Deg;
            }
            
            if (sourceMaterial.HasProperty("_Color"))
            {
                targetProfile.fillColor = sourceMaterial.GetColor("_Color");
            }
            
            if (sourceMaterial.HasProperty("_EnableBlur"))
            {
                targetProfile.enableBlur = sourceMaterial.GetFloat("_EnableBlur") > 0.5f;
                
                if (targetProfile.enableBlur && targetProfile.blurParams != null)
                {
                    if (sourceMaterial.HasProperty("_BlurType"))
                        targetProfile.blurParams.blurType = (Runtime.UIEffectProfile.BlurParams.BlurType)
                            Mathf.RoundToInt(sourceMaterial.GetFloat("_BlurType"));
                    if (sourceMaterial.HasProperty("_BlurRadius"))
                        targetProfile.blurParams.radius = sourceMaterial.GetFloat("_BlurRadius");
                    if (sourceMaterial.HasProperty("_BlurIterations"))
                        targetProfile.blurParams.iterations = Mathf.RoundToInt(sourceMaterial.GetFloat("_BlurIterations"));
                    if (sourceMaterial.HasProperty("_BlurDownsample"))
                        targetProfile.blurParams.downsample = Mathf.RoundToInt(sourceMaterial.GetFloat("_BlurDownsample"));
                }
            }
            
            if (sourceMaterial.HasProperty("_EnableShadow"))
            {
                targetProfile.enableShadow = sourceMaterial.GetFloat("_EnableShadow") > 0.5f;
                
                if (targetProfile.enableShadow && targetProfile.shadowParams != null)
                {
                    if (sourceMaterial.HasProperty("_ShadowColor"))
                        targetProfile.shadowParams.color = sourceMaterial.GetColor("_ShadowColor");
                    if (sourceMaterial.HasProperty("_ShadowOffset"))
                    {
                        Vector4 offset = sourceMaterial.GetVector("_ShadowOffset");
                        targetProfile.shadowParams.offset = new Vector2(offset.x, offset.y);
                    }
                    if (sourceMaterial.HasProperty("_ShadowBlur"))
                        targetProfile.shadowParams.blur = sourceMaterial.GetFloat("_ShadowBlur");
                    if (sourceMaterial.HasProperty("_ShadowOpacity"))
                        targetProfile.shadowParams.opacity = sourceMaterial.GetFloat("_ShadowOpacity");
                        
                    targetProfile.shadowUnit = Runtime.UIEffectProfile.Unit.Pixels;
                }
            }
            
            if (sourceMaterial.HasProperty("_EnableGradient"))
            {
                targetProfile.enableGradient = sourceMaterial.GetFloat("_EnableGradient") > 0.5f;
                
                if (targetProfile.enableGradient && targetProfile.gradientParams != null)
                {
                    if (sourceMaterial.HasProperty("_GradientType"))
                        targetProfile.gradientParams.type = (Runtime.UIEffectProfile.GradientParams.GradientType)
                            Mathf.RoundToInt(sourceMaterial.GetFloat("_GradientType"));
                    if (sourceMaterial.HasProperty("_GradientColorA"))
                        targetProfile.gradientParams.colorA = sourceMaterial.GetColor("_GradientColorA");
                    if (sourceMaterial.HasProperty("_GradientColorB"))
                        targetProfile.gradientParams.colorB = sourceMaterial.GetColor("_GradientColorB");
                    if (sourceMaterial.HasProperty("_GradientAngle"))
                        targetProfile.gradientParams.angle = sourceMaterial.GetFloat("_GradientAngle") * Mathf.Rad2Deg;
                }
            }
            
            Debug.Log($"Settings copied from material '{sourceMaterial.name}' (Shader: {sourceMaterial.shader.name})");
        }

        /// <summary>
        /// Restores the previously previewed object to its original state before switching to a new one.
        /// </summary>
        private void CleanupPreviousPreview()
        {
            if (_previewTarget != null)
            {
                GameObject previousTargetObject = _previewTarget.gameObject;

                if (_previewTarget.profile == _workingProfile)
                {
                    if (_originalPreviewProfile != null)
                    {
                        Undo.RecordObject(_previewTarget, "Restore Original Profile");
                        _previewTarget.SetProfile(_originalPreviewProfile);
                        Debug.Log($"Preview restored on {previousTargetObject?.name} (restored original profile)");
                    }
                    else
                    {
                        Undo.DestroyObjectImmediate(_previewTarget);
                        Debug.Log($"Preview component removed from {previousTargetObject?.name} (no original profile)");
                    }
                }
                else
                {
                    Undo.RecordObject(_previewTarget, "Maintain Original State");
                    _previewTarget.enabled = true;
                    Debug.Log($"Preview maintained on {previousTargetObject?.name} (kept existing profile)");
                }
                
                if(previousTargetObject != null)
                {
                    EditorUtility.SetDirty(previousTargetObject);
                }
            }
            
            _originalPreviewProfile = null;
        }
        
        /// <summary>
        /// Disables the live preview, restoring the target object to its original state.
        /// </summary>
        private void DisablePreview()
        {
            if (_previewTarget != null)
            {
                try
                {
                    GameObject targetObject = _previewTarget.gameObject;

                    if (_previewTarget.profile == _workingProfile)
                    {
                        // If an original profile was saved, restore it.
                        if (_originalPreviewProfile != null)
                        {
                            Undo.RecordObject(_previewTarget, "Restore Original Profile");
                            _previewTarget.SetProfile(_originalPreviewProfile);
                            if(targetObject != null)
                                Debug.Log($"Preview disabled and original profile restored on {targetObject.name}");
                        }
                        // Otherwise, remove the component that was added for previewing.
                        else
                        {
                            Undo.DestroyObjectImmediate(_previewTarget);
                            if(targetObject != null)
                                Debug.Log($"Preview component removed from {targetObject.name}");
                        }
                    }
                    else if (_previewTarget.profile != null)
                    {
                        // If the user changed the profile while previewing, keep the component.
                        Undo.RecordObject(_previewTarget, "Keep UI Effect Component");
                        _previewTarget.enabled = true;
                        if(targetObject != null)
                           Debug.Log($"Preview disabled but component kept on {targetObject.name} (has own profile)");
                    }
                    
                    if(targetObject != null)
                    {
                        EditorUtility.SetDirty(targetObject);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Error disabling preview: {e.Message}");
                }
            }
            
            _previewEnabled = false;
            _previewTarget = null;
            _originalPreviewProfile = null;
            
            SceneView.RepaintAll();
        }

        /// <summary>
        /// Handles switching the preview target to a new selection without manual stop/start.
        /// </summary>
        private void SwitchPreviewTarget()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null || !_previewEnabled) return;
            
            if (_previewTarget != null && _previewTarget.gameObject == selected) return;
            
            if (_previewTarget != null)
            {
                CleanupPreviousPreview();
            }
            
            EnablePreviewOnSelected();
        }
        
        #endregion
        
        #region Apply and Preset Actions
        
        /// <summary>
        /// Applies the current settings to all selected GameObjects.
        /// This creates a new, independent profile asset for each object.
        /// </summary>
        private void ApplyToSelected()
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            
            if (selectedObjects.Length == 0)
            {
                EditorUtility.DisplayDialog(LocalizedGUI.Text("NO_SELECTION"), 
                    LocalizedGUI.Text("SELECT_GAMEOBJECT_FIRST"), "OK");
                return;
            }
            
            if (_shaderMissing)
            {
                EditorUtility.DisplayDialog(LocalizedGUI.Text("SHADERS_MISSING"), 
                    LocalizedGUI.Text("CANNOT_APPLY_SHADERS"), "OK");
                return;
            }
            
            int appliedCount = 0;
            
            foreach (GameObject obj in selectedObjects)
            {
                var image = obj.GetComponent<Image>();
                var rawImage = obj.GetComponent<RawImage>();
                
                if (image == null && rawImage == null) continue;

                // Use the new method instead of directly setting sprite to null
                if (image != null)
                {
                    PrepareImageForEffect(image); // ← CANVI AQUÍ
                }
                
                var effectComponent = obj.GetComponent<Runtime.UIEffectComponent>();
                if (effectComponent == null)
                {
                    effectComponent = Undo.AddComponent<Runtime.UIEffectComponent>(obj);
                }
                
                // Create a clone of the working profile to make it independent.
                var profileCopy = _workingProfile.Clone();
                profileCopy.name = $"UIEffect_{obj.name}_{System.DateTime.Now.Ticks}";
                
                // Ensure the target directory for presets exists.
                string directory = "Assets/UIEffectsPro/Presets";
                if (!AssetDatabase.IsValidFolder(directory))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/UIEffectsPro"))
                    {
                        AssetDatabase.CreateFolder("Assets", "UIEffectsPro");
                    }
                    AssetDatabase.CreateFolder("Assets/UIEffectsPro", "Presets");
                }
                
                // Create a new asset file for the cloned profile.
                string assetPath = $"{directory}/AppliedProfile_{obj.name}_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}.asset";
                AssetDatabase.CreateAsset(profileCopy, assetPath);
                
                Undo.RecordObject(effectComponent, "Apply UI Effect Profile");
                effectComponent.SetProfile(profileCopy);
                effectComponent.ForceUpdate();
                
                EditorUtility.SetDirty(effectComponent);
                appliedCount++;
            }
            
            AssetDatabase.SaveAssets();
            SceneView.RepaintAll();
            
            string effectsDescription = "";
            if (_workingProfile.enableBlur && _workingProfile.enableShadow)
                effectsDescription = LocalizedGUI.Text("WITH_BLUR_SHADOW");
            else if (_workingProfile.enableBlur)
                effectsDescription = LocalizedGUI.Text("WITH_BLUR");
            else if (_workingProfile.enableShadow)
                effectsDescription = LocalizedGUI.Text("WITH_SHADOW");
            
            EditorUtility.DisplayDialog(LocalizedGUI.Text("APPLY_COMPLETE"), 
                LocalizedGUI.Format("APPLIED_EFFECT_TO", appliedCount) + effectsDescription, "OK");
            
            Debug.Log($"Applied independent profiles to {appliedCount} objects. Each object now has its own profile copy.");
        }
        
        /// <summary>
        /// Saves the current working profile settings as a new preset asset file.
        /// </summary>
        private void SavePreset()
        {
            string defaultName = "NewUIEffectPreset";
            if (_workingProfile.enableBlur && _workingProfile.enableShadow)
                defaultName = "BlurShadowPreset";
            else if (_workingProfile.enableBlur)
                defaultName = "BlurPreset";
            else if (_workingProfile.enableShadow)
                defaultName = "ShadowPreset";
            
            string path = EditorUtility.SaveFilePanelInProject(
                LocalizedGUI.Text("SAVE_PRESET"),
                defaultName,
                "asset",
                "Choose location to save preset");
            
            if (!string.IsNullOrEmpty(path))
            {
                var presetCopy = _workingProfile.Clone();
                AssetDatabase.CreateAsset(presetCopy, path);
                AssetDatabase.SaveAssets();
                
                Debug.Log($"Preset saved to {path}");
                EditorUtility.DisplayDialog(LocalizedGUI.Text("PRESET_SAVED"), 
                    LocalizedGUI.Format("PRESET_SAVED_SUCCESS", path), "OK");
            }
        }
        
        /// <summary>
        /// Loads a UIEffectProfile preset asset from disk into the working profile.
        /// </summary>
        private void LoadPreset()
        {
            string path = EditorUtility.OpenFilePanel(
                LocalizedGUI.Text("LOAD_PRESET"),
                Application.dataPath,
                "asset");

            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }

                var preset = AssetDatabase.LoadAssetAtPath<Runtime.UIEffectProfile>(path);
                if (preset != null)
                {
                    Undo.RecordObject(_workingProfile, "Load UI Effect Preset");

                    EditorUtility.CopySerialized(preset, _workingProfile);
                    UpdateSerializedObject();

                    if (_previewEnabled && _previewTarget != null)
                    {
                        _previewTarget.ForceUpdate();
                        EditorUtility.SetDirty(_previewTarget);
                        SceneView.RepaintAll();
                    }

                    Debug.Log($"Preset loaded from {path}");
                }
                else
                {
                    EditorUtility.DisplayDialog(LocalizedGUI.Text("LOAD_ERROR"), LocalizedGUI.Text("COULD_NOT_LOAD"), "OK");
                }
            }
        }
        
        #endregion
        
        #region Shader Validation
        
        /// <summary>
        /// Checks if the required shaders are available in the project for the current render pipeline.
        /// </summary>
        private void CheckShaderAvailability()
        {
            bool urpFound = Shader.Find(SHADER_URP) != null;
            bool builtinFound = Shader.Find(SHADER_BUILTIN) != null;
            bool legacyFound = Shader.Find(SHADER_LEGACY) != null;
            
            _shaderMissing = !urpFound && !builtinFound && !legacyFound;
            
            if (_shaderMissing)
            {
                string pipeline = "Built-in";
                if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
                {
                    var pipelineName = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().Name;
                    if (pipelineName.Contains("Universal"))
                    {
                        pipeline = "URP";
                    }
                }
                
                _missingShaderMessage = $"{LocalizedGUI.Text("SHADER_NOT_FOUND")}\n" +
                    $"{LocalizedGUI.Format("CURRENT_PIPELINE", pipeline)}\n" +
                    $"{LocalizedGUI.Format("TRIED_SHADERS", $"{SHADER_URP}, {SHADER_BUILTIN}, {SHADER_LEGACY}")}\n" +
                    $"{LocalizedGUI.Text("ENSURE_SHADERS")}";
            }
        }
        
        #endregion
        
        #region Profile Management
        
        /// <summary>
        /// Creates a new, default UIEffectProfile instance to be used as the working profile.
        /// </summary>
        private void CreateDefaultWorkingProfile()
        {
            _workingProfile = ScriptableObject.CreateInstance<Runtime.UIEffectProfile>();
            _workingProfile.name = "Working Profile";
            _workingProfile.ResetToDefaults();
        }
        
        /// <summary>
        /// Updates the SerializedObject to match the current working profile.
        /// This is necessary after creating or loading a new profile.
        /// </summary>
        private void UpdateSerializedObject()
        {
            if (_workingProfile != null)
            {
                _serializedProfile = new SerializedObject(_workingProfile);
            }
        }
        
        /// <summary>
        /// Resets the working profile to its default values.
        /// </summary>
        private void ResetProfile()
        {
            if (_workingProfile == null) return;
            
            Undo.RecordObject(_workingProfile, "Reset UI Effect Profile");
            _workingProfile.ResetToDefaults();
            UpdateSerializedObject();
            
            if (_previewEnabled && _previewTarget != null)
            {
                _previewTarget.ForceUpdate();
                EditorUtility.SetDirty(_previewTarget);
                SceneView.RepaintAll();
            }
        }
        
        #endregion

        #region Texture Section (Modified)

        /// <summary>
        /// Draws the UI for the texture overlay effect settings.
        /// </summary>
        private void DrawTextureSettings()
        {
            _textureFoldout = EditorGUILayout.Foldout(_textureFoldout, LocalizedGUI.Text("TEXTURE_SETTINGS"), true, EditorStyles.foldoutHeader);
            if (_textureFoldout)
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    EditorGUI.indentLevel++;

                    SerializedProperty enableTextureProp = _serializedProfile.FindProperty("enableTexture");
                    EditorGUILayout.PropertyField(enableTextureProp,
                        LocalizedGUI.Content("ENABLE_TEXTURE"));

                    bool textureEnabled = enableTextureProp.boolValue;

                    EditorGUI.BeginDisabledGroup(!textureEnabled);

                    EditorGUILayout.Space(3);

                    SerializedProperty overlayTextureProp = _serializedProfile.FindProperty("overlayTexture");

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(overlayTextureProp,
                            LocalizedGUI.Content("OVERLAY_TEXTURE"));

                        // Button to open Unity's asset picker.
                        if (GUILayout.Button(LocalizedGUI.Text("SELECT_TEXTURE"), GUILayout.Width(100)))
                        {
                            ShowAssetTextureSelector(overlayTextureProp);
                        }
                        
                        // Alternative button to load from a file path.
                        if (GUILayout.Button("Load File", GUILayout.Width(70)))
                        {
                            ShowFileTextureSelector(overlayTextureProp);
                        }
                    }

                    // Show texture info if a texture is assigned.
                    if (overlayTextureProp.objectReferenceValue is Texture2D selectedTexture)
                    {
                        using (new EditorGUILayout.VerticalScope(_infoBoxStyle))
                        {
                            string textureInfo = LocalizedGUI.Format("TEXTURE_INFO",
                                selectedTexture.name, selectedTexture.width, selectedTexture.height);
                            EditorGUILayout.LabelField(textureInfo, EditorStyles.miniLabel);

                            EditorGUILayout.LabelField($"Format: {selectedTexture.format}", EditorStyles.miniLabel);
                            EditorGUILayout.LabelField($"Mipmap: {selectedTexture.mipmapCount}", EditorStyles.miniLabel);

                            // Performance warning for large textures.
                            int textureSize = selectedTexture.width * selectedTexture.height;
                            if (textureSize > 1024 * 1024)
                            {
                                EditorGUILayout.HelpBox(LocalizedGUI.Text("TEXTURE_PERFORMANCE_TIP"), MessageType.Warning);
                            }
                        }
                    }
                    else if (textureEnabled)
                    {
                        using (new EditorGUILayout.VerticalScope(_warningBoxStyle))
                        {
                            EditorGUILayout.LabelField(LocalizedGUI.Text("NO_TEXTURE_SELECTED"), EditorStyles.miniLabel);
                            EditorGUILayout.LabelField("Drag a texture here or use the buttons above", EditorStyles.miniLabel);
                        }
                    }

                    // Draw the texture parameters if the effect is enabled.
                    if (textureEnabled)
                    {
                        EditorGUILayout.Space(5);
                        SerializedProperty textureParams = _serializedProfile.FindProperty("textureParams");
                        if (textureParams != null)
                        {
                            DrawTextureParameters(textureParams);
                        }
                    }

                    EditorGUI.EndDisabledGroup();

                    EditorGUI.indentLevel--;
                }
            }
        }
        
        /// <summary>
        /// A helper method to draw the specific parameters for the texture overlay (tiling, offset, etc.).
        /// </summary>
        private void DrawTextureParameters(SerializedProperty textureParams)
        {
            EditorGUILayout.LabelField("Texture Parameters", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            EditorGUILayout.PropertyField(textureParams.FindPropertyRelative("tiling"),
                LocalizedGUI.Content("TEXTURE_TILING"));

            EditorGUILayout.PropertyField(textureParams.FindPropertyRelative("offset"),
                LocalizedGUI.Content("TEXTURE_OFFSET"));

            EditorGUILayout.PropertyField(textureParams.FindPropertyRelative("rotation"),
                new GUIContent(LocalizedGUI.Text("TEXTURE_ROTATION"), $"{LocalizedGUI.Text("TEXTURE_ROTATION")} (°)"));

            EditorGUILayout.PropertyField(textureParams.FindPropertyRelative("opacity"),
                LocalizedGUI.Content("TEXTURE_OPACITY"));

            EditorGUILayout.Space(3);

            SerializedProperty blendModeProp = textureParams.FindPropertyRelative("blendMode");
            blendModeProp.enumValueIndex = EditorGUILayout.Popup(
                LocalizedGUI.Text("TEXTURE_BLEND_MODE"),
                blendModeProp.enumValueIndex,
                GetBlendModeNames()
            );

            SerializedProperty uvModeProp = textureParams.FindPropertyRelative("uvMode");
            uvModeProp.enumValueIndex = EditorGUILayout.Popup(
                LocalizedGUI.Text("TEXTURE_UV_MODE"),
                uvModeProp.enumValueIndex,
                GetUVModeNames()
            );

            SerializedProperty aspectModeProp = textureParams.FindPropertyRelative("aspectMode");
            aspectModeProp.enumValueIndex = EditorGUILayout.Popup(
                LocalizedGUI.Text("TEXTURE_ASPECT_MODE"),
                aspectModeProp.enumValueIndex,
                GetAspectModeNames()
            );

            SerializedProperty filterModeProp = textureParams.FindPropertyRelative("filterMode");
            filterModeProp.enumValueIndex = EditorGUILayout.Popup(
                LocalizedGUI.Text("TEXTURE_FILTERING"),
                filterModeProp.enumValueIndex,
                System.Enum.GetNames(typeof(FilterMode))
            );
        }

        /// <summary>
        /// Opens Unity's built-in object picker to select a Texture2D from the project assets.
        /// </summary>
        private void ShowAssetTextureSelector(SerializedProperty textureProperty)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            EditorGUIUtility.ShowObjectPicker<Texture2D>(
                textureProperty.objectReferenceValue as Texture2D, 
                false, 
                "t:Texture2D", 
                controlID
            );
            
            // Store the control ID to later identify the result from the picker.
            _texturePickerControlID = controlID;
        }

        /// <summary>
        /// Opens the system's file browser to select a texture from the local disk.
        /// </summary>
        private void ShowFileTextureSelector(SerializedProperty textureProperty)
        {
            string path = EditorUtility.OpenFilePanel(
                LocalizedGUI.Text("SELECT_TEXTURE"),
                Application.dataPath,
                "png,jpg,jpeg,tga,psd,gif,bmp,tiff");
                
            if (!string.IsNullOrEmpty(path))
            {
                // If the path is already inside the project's Assets folder.
                if (path.StartsWith(Application.dataPath))
                {
                    string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                    
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath);
                    if (texture != null)
                    {
                        textureProperty.objectReferenceValue = texture;
                        EditorUtility.SetDirty(_workingProfile);
                        
                        Debug.Log($"Texture loaded successfully: {texture.name} ({texture.width}x{texture.height})");
                    }
                    else
                    {
                        // If it's not a valid asset, try to import it.
                        if (ImportTextureAsAsset(path, relativePath))
                        {
                            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath);
                            if (texture != null)
                            {
                                textureProperty.objectReferenceValue = texture;
                                EditorUtility.SetDirty(_workingProfile);
                                Debug.Log($"Texture imported and loaded: {texture.name}");
                            }
                        }
                    }
                }
                else
                {
                    // If the file is outside the project, copy it into the project.
                    CopyAndImportExternalTexture(path, textureProperty);
                }
            }
        }
        
        /// <summary>
        /// Listens for the result of the object picker and applies the selected texture.
        /// This should be called within OnGUI.
        /// </summary>
        private void HandleObjectPickerResult()
        {
            if (Event.current.commandName == "ObjectSelectorUpdated" && 
                EditorGUIUtility.GetObjectPickerControlID() == _texturePickerControlID)
            {
                var selectedTexture = EditorGUIUtility.GetObjectPickerObject() as Texture2D;
                if (selectedTexture != null)
                {
                    SerializedProperty overlayTextureProp = _serializedProfile.FindProperty("overlayTexture");
                    if (overlayTextureProp != null)
                    {
                        overlayTextureProp.objectReferenceValue = selectedTexture;
                        EditorUtility.SetDirty(_workingProfile);
                        Debug.Log($"Texture selected from assets: {selectedTexture.name}");
                    }
                }
            }
            
            if (Event.current.commandName == "ObjectSelectorClosed")
            {
                // Reset the control ID when the picker is closed.
                _texturePickerControlID = -1;
            }
        }

        /// <summary>
        /// Copies a texture file to a target path within the project and sets its import settings.
        /// </summary>
        private bool ImportTextureAsAsset(string sourcePath, string targetPath)
        {
            try
            {
                string targetDirectory = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }
                
                File.Copy(sourcePath, targetPath, true);
                
                AssetDatabase.Refresh();
                
                // Configure the texture importer for standard UI use.
                TextureImporter textureImporter = AssetImporter.GetAtPath(targetPath) as TextureImporter;
                if (textureImporter != null)
                {
                    textureImporter.textureType = TextureImporterType.Default; // Default is flexible for UI.
                    textureImporter.alphaSource = TextureImporterAlphaSource.FromInput;
                    textureImporter.mipmapEnabled = true;
                    textureImporter.wrapMode = TextureWrapMode.Repeat;
                    textureImporter.filterMode = FilterMode.Bilinear;
                    
                    AssetDatabase.ImportAsset(targetPath);
                    AssetDatabase.SaveAssets();
                    
                    return true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error importing texture: {e.Message}");
            }
            
            return false;
        }

        /// <summary>
        /// Handles importing a texture selected from outside the project folder.
        /// It copies the file to a designated folder within the project.
        /// </summary>
        private void CopyAndImportExternalTexture(string externalPath, SerializedProperty textureProperty)
        {
            try
            {
                string fileName = Path.GetFileName(externalPath);
                string targetDirectory = "Assets/UIEffectsPro/Textures";
                
                if (!AssetDatabase.IsValidFolder(targetDirectory))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/UIEffectsPro"))
                    {
                        AssetDatabase.CreateFolder("Assets", "UIEffectsPro");
                    }
                    AssetDatabase.CreateFolder("Assets/UIEffectsPro", "Textures");
                }
                
                string targetPath = $"{targetDirectory}/{fileName}";
                
                // Avoid overwriting existing files by appending a number.
                int counter = 1;
                string originalName = Path.GetFileNameWithoutExtension(fileName);
                string extension = Path.GetExtension(fileName);
                
                while (File.Exists(targetPath))
                {
                    fileName = $"{originalName}_{counter}{extension}";
                    targetPath = $"{targetDirectory}/{fileName}";
                    counter++;
                }
                
                if (ImportTextureAsAsset(externalPath, targetPath))
                {
                    Texture2D importedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(targetPath);
                    if (importedTexture != null)
                    {
                        textureProperty.objectReferenceValue = importedTexture;
                        EditorUtility.SetDirty(_workingProfile);
                        
                        EditorUtility.DisplayDialog("Texture Imported", 
                            $"Texture '{fileName}' has been copied to the project and loaded successfully.", "OK");
                            
                        Debug.Log($"External texture copied and imported: {fileName}");
                    }
                }
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Import Error", 
                    $"Could not import texture: {e.Message}", "OK");
                Debug.LogError($"Error copying external texture: {e.Message}");
            }
        }

        /// <summary>
        /// Returns localized names for the BlendMode enum to be used in a popup.
        /// </summary>
        private string[] GetBlendModeNames()
        {
            return new string[]
            {
                LocalizedGUI.Text("BLEND_MULTIPLY"),
                LocalizedGUI.Text("BLEND_ADD"),
                LocalizedGUI.Text("BLEND_SUBTRACT"),
                LocalizedGUI.Text("BLEND_OVERLAY"),
                LocalizedGUI.Text("BLEND_SCREEN"),
                LocalizedGUI.Text("BLEND_REPLACE")
            };
        }

        /// <summary>
        /// Returns localized names for the UVMode enum to be used in a popup.
        /// </summary>
        private string[] GetUVModeNames()
        {
            return new string[]
            {
                LocalizedGUI.Text("UV_LOCAL"),
                LocalizedGUI.Text("UV_WORLD"),
                LocalizedGUI.Text("UV_REPEAT")
            };
        }

        /// <summary>
        /// Returns localized names for the AspectMode enum to be used in a popup.
        /// </summary>
        private string[] GetAspectModeNames()
        {
            return new string[]
            {
                LocalizedGUI.Text("ASPECT_STRETCH"),
                LocalizedGUI.Text("ASPECT_FIT_WIDTH"),
                LocalizedGUI.Text("ASPECT_FIT_HEIGHT"),
                LocalizedGUI.Text("ASPECT_FILL")
            };
        }
        #endregion

        // --- [INICI NOU MÈTODE AFEGIT] ---
        /// <summary>
        /// Dibuixa una previsualització del gradient de colors.
        /// </summary>
        private void DrawGradientPreview(Rect rect, Color startColor, Color endColor)
        {
            Texture2D gradientTexture = new Texture2D(256, 1);
            for (int i = 0; i < 256; i++)
            {
                float t = i / 255f;
                gradientTexture.SetPixel(i, 0, Color.Lerp(startColor, endColor, t));
            }
            gradientTexture.Apply();
        
            GUI.DrawTexture(rect, gradientTexture, ScaleMode.StretchToFill);
            UnityEngine.Object.DestroyImmediate(gradientTexture);
        }
        // --- [FINAL NOU MÈTODE AFEGIT] ---
    }
}