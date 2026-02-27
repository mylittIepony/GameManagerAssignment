// --------------------------------------------------------------------------------
// This script defines a custom editor window for Unity to manage the language
// settings of the "UI Effects Pro" asset. It allows users to switch between
// supported languages, and also integrates these settings into Unity's main
// Preferences window for easier access.

using UnityEngine;
using UnityEditor;
using UIEffectsPro.Editor.Localization; // Custom namespace for localization logic.

namespace UIEffectsPro.Editor
{
    /// <summary>
    /// Creates a custom editor window to manage language preferences for the asset.
    /// This window provides a user-friendly interface to view and change the current language.
    /// </summary>
    public class LocalizationPreferences : EditorWindow
    {
        // Defines the path in the Unity top menu to open this window.
        private const string MENU_PATH = "Window/UI Effects Pro/Language Settings";
        
        // Stores the current position of the scroll view.
        private Vector2 _scrollPosition;
        
        // Private fields for custom GUI styles to ensure a consistent look and feel.
        private GUIStyle _titleStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _infoBoxStyle;
        
        // A flag to ensure that styles are initialized only once to improve performance.
        private bool _stylesInitialized = false;

        /// <summary>
        /// Creates and shows the language settings window. 
        /// This method is triggered by the user clicking the menu item defined in MENU_PATH.
        /// </summary>
        [MenuItem(MENU_PATH)]
        public static void ShowWindow()
        {
            // Get existing open window or if none, make a new one.
            var window = GetWindow<LocalizationPreferences>("Language Settings");
            // Set size constraints for the window.
            window.minSize = new Vector2(350, 200);
            window.maxSize = new Vector2(500, 300);
            window.Show();
        }

        /// <summary>
        /// Called when the window is enabled. Subscribes to the language changed event
        /// to automatically update the UI when the language is changed from another source.
        /// </summary>
        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += OnLanguageChanged;
        }

        /// <summary>
        /// Called when the window is disabled. Unsubscribes from the event to prevent
        /// memory leaks and errors.
        /// </summary>
        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
        }

        /// <summary>
        /// A callback method that is invoked when the language changes.
        /// It forces the window to redraw itself to reflect the new language.
        /// </summary>
        /// <param name="newLanguage">The new language that has been set.</param>
        private void OnLanguageChanged(SupportedLanguage newLanguage)
        {
            Repaint();
        }

        /// <summary>
        /// Initializes the custom GUIStyles used for rendering the window.
        /// This method is called once before drawing the GUI.
        /// </summary>
        private void InitializeStyles()
        {
            // Exit if styles have already been created.
            if (_stylesInitialized) return;

            // Style for the main window title.
            _titleStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 10, 15)
            };

            // Style for container boxes that group UI elements.
            _sectionStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(15, 15, 10, 10),
                margin = new RectOffset(10, 10, 5, 10)
            };

            // Style for the informational help box.
            _infoBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(0, 0, 5, 5),
                fontSize = 11
            };

            // Mark styles as initialized.
            _stylesInitialized = true;
        }

        /// <summary>
        /// This is the main Unity IMGUI method, called for rendering and handling GUI events.
        /// It draws the entire content of the editor window.
        /// </summary>
        private void OnGUI()
        {
            // Ensure all custom styles are ready before drawing.
            InitializeStyles();

            // Use a scroll view to ensure content is accessible on smaller window sizes.
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scrollScope.scrollPosition;

                // --- Window Title ---
                EditorGUILayout.LabelField(LocalizedGUI.Text("LANGUAGE"), _titleStyle);

                GUILayout.Space(10);

                // --- Language Selection Section ---
                // 'using' statement ensures proper begin/end calls for the vertical layout group.
                using (new EditorGUILayout.VerticalScope(_sectionStyle))
                {
                    EditorGUILayout.LabelField("🌐 " + LocalizedGUI.Text("LANGUAGE"), EditorStyles.boldLabel);
                    GUILayout.Space(5);

                    // Display the current language.
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Current / Actual / Aktuell / 当前:", GUILayout.Width(180));
                        EditorGUILayout.LabelField(LocalizationManager.GetCurrentLanguageName(), EditorStyles.boldLabel);
                    }

                    GUILayout.Space(10);

                    // Draw the language selection dropdown menu.
                    LocalizedGUI.LanguageSelector();

                    GUILayout.Space(10);

                    // Display an informational text box that changes with the selected language.
                    using (new EditorGUILayout.VerticalScope(_infoBoxStyle))
                    {
                        EditorGUILayout.LabelField("💡 " + GetLanguageInfoText(), EditorStyles.wordWrappedLabel);
                    }
                }

                GUILayout.Space(15);

                // --- Supported Languages List Section ---
                using (new EditorGUILayout.VerticalScope(_sectionStyle))
                {
                    EditorGUILayout.LabelField("📋 Supported Languages / Idiomas Soportados", EditorStyles.boldLabel);
                    GUILayout.Space(5);
                    
                    // Draw the detailed list of available languages.
                    DrawLanguageList();
                }

                GUILayout.Space(10);

                // --- Reset Button ---
                // This section centers the reset button horizontally.
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace(); // Pushes the button to the center.
                    if (GUILayout.Button("🔄 Reset to English", GUILayout.Width(140)))
                    {
                        // Set the language back to English when clicked.
                        LocalizationManager.CurrentLanguage = SupportedLanguage.English;
                    }
                    GUILayout.FlexibleSpace(); // Balances the layout.
                }
            }
        }

        /// <summary>
        /// Returns a localized informational string based on the currently selected language.
        /// </summary>
        /// <returns>The localized help text.</returns>
        private string GetLanguageInfoText()
        {
            switch (LocalizationManager.CurrentLanguage)
            {
                case SupportedLanguage.English:
                    return "Language settings will affect all UI Effects Pro windows. Changes are saved automatically.";
                case SupportedLanguage.Spanish:
                    return "La configuración de idioma afectará todas las ventanas de UI Effects Pro. Los cambios se guardan automáticamente.";
                case SupportedLanguage.German:
                    return "Spracheinstellungen wirken sich auf alle UI Effects Pro Fenster aus. Änderungen werden automatisch gespeichert.";
                case SupportedLanguage.Chinese:
                    return "语言设置将影响所有UI Effects Pro窗口。更改会自动保存。";
                default:
                    return "Language settings will affect all UI Effects Pro windows. Changes are saved automatically.";
            }
        }

        /// <summary>
        /// Draws a list of all supported languages, highlighting the currently active one
        /// and providing buttons to switch to others.
        /// </summary>
        private void DrawLanguageList()
        {
            // Define the list of languages with their details. Using an anonymous type is convenient here.
            var languages = new[]
            {
                new { code = SupportedLanguage.English, name = "English", native = "English", flag = "🇺🇸" },
                new { code = SupportedLanguage.Spanish, name = "Spanish", native = "Español", flag = "🇪🇸" },
                new { code = SupportedLanguage.German, name = "German", native = "Deutsch", flag = "🇩🇪" },
                new { code = SupportedLanguage.Chinese, name = "Chinese", native = "中文", flag = "🇨🇳" }
            };

            // Iterate through the languages and draw a row for each one.
            foreach (var lang in languages)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool isSelected = LocalizationManager.CurrentLanguage == lang.code;
                    
                    // If this language is the selected one, apply a subtle highlight color.
                    if (isSelected)
                    {
                        GUI.backgroundColor = new Color(0.7f, 1f, 0.7f, 0.3f);
                    }

                    // Use a box style for each language entry for better visual grouping.
                    using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
                    {
                        // Display flag and native name.
                        EditorGUILayout.LabelField($"{lang.flag} {lang.native}", GUILayout.Width(120));
                        // Display English name in a smaller font.
                        EditorGUILayout.LabelField($"({lang.name})", EditorStyles.miniLabel, GUILayout.Width(80));
                        
                        GUILayout.FlexibleSpace();
                        
                        // Show a checkmark for the selected language.
                        if (isSelected)
                        {
                            EditorGUILayout.LabelField("✓", EditorStyles.boldLabel, GUILayout.Width(20));
                        }
                        // Otherwise, show a "Select" button.
                        else if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            LocalizationManager.CurrentLanguage = lang.code;
                        }
                    }

                    // Reset the background color to default for subsequent GUI elements.
                    if (isSelected)
                    {
                        GUI.backgroundColor = Color.white;
                    }
                }
                GUILayout.Space(2); // Add a small vertical space between entries.
            }
        }
    }

    /// <summary>
    /// Integrates the localization settings directly into Unity's Preferences window
    /// (Edit -> Preferences). This provides a more native and accessible way for users
    /// to find and change the language.
    /// </summary>
    public static class UIEffectsProLocalizationSettings
    {
        /// <summary>
        /// Creates a new SettingsProvider for the Preferences window.
        /// The [SettingsProvider] attribute automatically registers this method with Unity.
        /// </summary>
        /// <returns>The configured SettingsProvider instance.</returns>
        [SettingsProvider]
        public static SettingsProvider CreateUIEffectsProLocalizationProvider()
        {
            var provider = new SettingsProvider("Preferences/UI Effects Pro/Localization", SettingsScope.User)
            {
                // The label displayed in the Preferences window list.
                label = "UI Effects Pro - Language",
                // The handler that draws the GUI for this settings page.
                guiHandler = (searchContext) =>
                {
                    GUILayout.Space(10);
                    
                    EditorGUILayout.LabelField("Language Settings", EditorStyles.boldLabel);
                    GUILayout.Space(5);
                    
                    // Re-use the same language selector dropdown for consistency.
                    LocalizedGUI.LanguageSelector();
                    
                    GUILayout.Space(10);
                    
                    // A help box to explain the purpose of this settings page.
                    EditorGUILayout.HelpBox(
                        "Select your preferred language for the UI Effects Pro interface. " +
                        "Changes will apply to all UI Effects Pro windows immediately.", 
                        MessageType.Info);
                        
                    GUILayout.Space(10);
                    
                    // A button to open the more detailed standalone window.
                    if (GUILayout.Button("Open Language Settings Window", GUILayout.Width(200)))
                    {
                        LocalizationPreferences.ShowWindow();
                    }
                },
                
                // Keywords to help users find this settings page via the search bar.
                keywords = new[] { "language", "localization", "idioma", "sprache", "语言", "ui effects pro" }
            };

            return provider;
        }
    }
}