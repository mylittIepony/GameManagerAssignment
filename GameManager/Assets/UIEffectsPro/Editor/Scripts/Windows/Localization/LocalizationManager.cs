// UIEffectsPro/Editor/Scripts/Windows/Localization/LocalizationManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UIEffectsPro.Editor.Localization
{
    /// <summary>
    /// Defines the languages supported by the editor extension.
    /// Each language is associated with an integer value for easy storage and retrieval.
    /// </summary>
    [System.Serializable]
    public enum SupportedLanguage
    {
        English = 0,
        Spanish = 1,
        German = 2,
        Chinese = 3
    }

    /// <summary>
    /// A container class for localization data. 
    /// While not directly used by the static LocalizationManager, it can be useful
    /// for serializing or managing language data in a different context (e.g., loading from files).
    /// </summary>
    [System.Serializable]
    public class LocalizationData
    {
        public SupportedLanguage language;
        public Dictionary<string, string> translations = new Dictionary<string, string>();
    }

    /// <summary>
    /// Manages the localization for the editor window. It handles loading translations,
    /// changing the current language, and retrieving translated strings.
    /// This is a static class, meaning it can be accessed from anywhere without an instance.
    /// </summary>
    public static class LocalizationManager
    {
        // Holds the currently selected language.
        private static SupportedLanguage _currentLanguage = SupportedLanguage.English;
        
        // A nested dictionary to store all translations. The structure is: Language -> (Key -> Translation).
        private static Dictionary<SupportedLanguage, Dictionary<string, string>> _translations;
        
        // A flag to ensure the initialization logic runs only once.
        private static bool _initialized = false;

        // The key used to save the user's language preference in Unity's EditorPrefs.
        private const string LANGUAGE_PREF_KEY = "UIEffectsPro_Language";

        /// <summary>
        /// Gets or sets the current language for the editor UI.
        /// When set, it saves the preference and invokes the OnLanguageChanged event.
        /// </summary>
        public static SupportedLanguage CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    // Save the selected language to EditorPrefs to persist between sessions.
                    EditorPrefs.SetInt(LANGUAGE_PREF_KEY, (int)value);
                    // Notify any subscribers that the language has changed.
                    OnLanguageChanged?.Invoke(value);
                }
            }
        }

        /// <summary>
        /// An event that is triggered whenever the current language is changed.
        /// Other parts of the editor can subscribe to this event to refresh their UI.
        /// </summary>
        public static event System.Action<SupportedLanguage> OnLanguageChanged;

        /// <summary>
        /// Static constructor. This is called automatically by the C# runtime before the
        /// class is accessed for the first time, ensuring initialization happens.
        /// </summary>
        static LocalizationManager()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the localization system. It loads the saved language preference
        /// and populates the translations dictionary.
        /// </summary>
        private static void Initialize()
        {
            // Prevent re-initialization.
            if (_initialized) return;

            // Load the saved language from EditorPrefs. Defaults to English (0) if not set.
            _currentLanguage = (SupportedLanguage)EditorPrefs.GetInt(LANGUAGE_PREF_KEY, 0);
            
            // Load all hardcoded translations into memory.
            InitializeTranslations();
            _initialized = true;
        }

        /// <summary>
        /// Retrieves the translated text for a given key in the current language.
        /// </summary>
        /// <param name="key">The unique identifier for the text to translate.</param>
        /// <returns>The translated string. If not found, it falls back to English, and then to the key itself.</returns>
        public static string GetText(string key)
        {
            // Ensure the system is initialized before trying to get text.
            if (!_initialized) Initialize();

            // Try to find the translation in the currently selected language.
            if (_translations != null && 
                _translations.ContainsKey(_currentLanguage) && 
                _translations[_currentLanguage].ContainsKey(key))
            {
                return _translations[_currentLanguage][key];
            }

            // If not found, fallback to the English translation as a default.
            if (_currentLanguage != SupportedLanguage.English && 
                _translations != null &&
                _translations.ContainsKey(SupportedLanguage.English) && 
                _translations[SupportedLanguage.English].ContainsKey(key))
            {
                return _translations[SupportedLanguage.English][key];
            }

            // If no translation is found in either language, return the key itself.
            // This helps in identifying missing translations during development.
            Debug.LogWarning($"Localization key not found: {key}");
            return key;
        }

        /// <summary>
        /// Gets an array of user-friendly names for all supported languages.
        /// The order must match the SupportedLanguage enum.
        /// </summary>
        /// <returns>An array of language names.</returns>
        public static string[] GetLanguageNames()
        {
            return new string[]
            {
                "English",
                "Español", 
                "Deutsch",
                "中文" // Chinese
            };
        }

        /// <summary>
        /// Gets the user-friendly name of the currently selected language.
        /// </summary>
        /// <returns>The name of the current language.</returns>
        public static string GetCurrentLanguageName()
        {
            return GetLanguageNames()[(int)_currentLanguage];
        }

        /// <summary>
        /// Initializes and populates the translations dictionary with all supported languages.
        /// In this implementation, translations are hardcoded directly in the source.
        /// </summary>
        private static void InitializeTranslations()
        {
            _translations = new Dictionary<SupportedLanguage, Dictionary<string, string>>();

            // English (base language) translations
            var english = new Dictionary<string, string>
            {
                {"WINDOW_TITLE", "UI Effects Pro"},
                {"WINDOW_SUBTITLE", "Professional UI Enhancement Tools"},
                {"SHADER_STATUS", "SHADER STATUS"},
                {"PERFORMANCE_IMPACT", "PERFORMANCE IMPACT"},
                {"LIVE_PREVIEW", "LIVE PREVIEW"},
                {"EFFECT_SETTINGS", "EFFECT SETTINGS"},
                {"ACTIONS", "ACTIONS"},
                {"PRESET_MANAGEMENT", "Preset Management"},
                {"LANGUAGE", "Language"},
                {"LANGUAGE_TOOLTIP", "Select interface language"},
                {"CORNER_RADIUS", "Corner Radius"},
                {"UNIT", "Unit"},
                {"INDIVIDUAL_CORNERS", "Individual Corners"},
                {"TOP_LEFT", "Top Left"},
                {"TOP_RIGHT", "Top Right"},
                {"BOTTOM_LEFT", "Bottom Left"},
                {"BOTTOM_RIGHT", "Bottom Right"},
                {"GLOBAL_RADIUS", "Global Radius"},
                {"BORDER_SETTINGS", "Border Settings"},
                {"WIDTH", "Width"},
                {"COLOR", "Color"},
                {"FILL_SETTINGS", "Fill Settings"},
                {"FILL_COLOR", "Fill Color"},
                {"BLUR_EFFECT", "Blur Effect"},
                {"ENABLE_BLUR", "Enable Blur"},
                {"RADIUS", "Radius"},
                {"ITERATIONS", "Iterations"},
                {"DOWNSAMPLE", "Downsample"},
                {"BLUR_PERFORMANCE_TIP", "Higher values = better quality but lower performance"},
                {"DROP_SHADOW", "Drop Shadow"},
                {"ENABLE_SHADOW", "Enable Shadow"},
                {"SHADOW_COLOR", "Color"},
                {"OFFSET", "Offset"},
                {"BLUR", "Blur"},
                {"OPACITY", "Opacity"},
                {"SHADOW_DISTANCE", "Shadow Distance"},
                {"GRADIENT_FILL", "Gradient Fill"},
                {"ENABLE_GRADIENT", "Enable Gradient"},
                {"TYPE", "Type"},
                {"COLOR_A", "Color A"},
                {"COLOR_B", "Color B"},
                {"ANGLE", "Angle"},
                {"GRADIENT_OVERRIDE_TIP", "Gradient overrides the solid Fill Color"},
                {"GRADIENT_RADIAL_CENTER", "Radial Center"},
                {"GRADIENT_ANGULAR_ROTATION", "Angular Rotation"},
                {"GRADIENT_RADIAL_SCALE", "Radial Scale"},
                {"PREVIEWING_ON", "Previewing on"},
                {"CHANGES_REALTIME", "Changes are applied in real-time"},
                {"START_PREVIEW", "Start Preview"},
                {"STOP_PREVIEW", "Stop Preview"},
                {"PREVIEW_UNAVAILABLE", "Preview unavailable: Required shaders are missing"},
                {"SELECTION_MUST_HAVE", "Selection must have an Image or RawImage component"},
                {"QUICK_START", "Quick Start:"},
                {"STEP_1", "1. Select a GameObject with Image/RawImage"},
                {"STEP_2", "2. Click 'Start Preview' to see effects"},
                {"STEP_3", "3. Adjust settings below in real-time"},
                {"APPLY_TO_SELECTED", "Apply to Selected"},
                {"RESET_SETTINGS", "Reset Settings"},
                {"SAVE_PRESET", "Save Preset"},
                {"LOAD_PRESET", "Load Preset"},
                {"REFRESH_SHADER_CHECK", "Refresh Shader Check"},
                {"HIDE_TIPS", "Hide Tips"},
                {"BLUR_SHADOW_WARNING", "Blur + Shadow effects enabled. Consider optimizing for mobile platforms."},
                {"BLUR_WARNING", "Blur effect enabled. May impact performance on lower-end devices."},
                {"SHADOW_WARNING", "Shadow effect enabled. Minimal performance impact."},
                {"NO_SELECTION", "No Selection"},
                {"SELECT_GAMEOBJECT_FIRST", "Please select a GameObject first."},
                {"INVALID_TARGET", "Invalid Target"},
                {"OBJECT_MUST_HAVE_IMAGE", "Selected object must have an Image or RawImage component."},
                {"SHADERS_MISSING", "Shaders Missing"},
                {"CANNOT_PREVIEW_SHADERS", "Cannot preview: UI Effects Pro shaders are not installed or available."},
                {"CANNOT_APPLY_SHADERS", "Cannot apply effects: UI Effects Pro shaders are not installed or available."},
                {"APPLY_COMPLETE", "Apply Complete"},
                {"APPLIED_EFFECT_TO", "Applied UI Effect to {0} object(s)."},
                {"WITH_BLUR_SHADOW", " (with Blur + Shadow)"},
                {"WITH_BLUR", " (with Blur)"},
                {"WITH_SHADOW", " (with Shadow)"},
                {"PRESET_SAVED", "Preset Saved"},
                {"PRESET_SAVED_SUCCESS", "Preset saved successfully to:\n{0}"},
                {"LOAD_ERROR", "Load Error"},
                {"COULD_NOT_LOAD", "Could not load preset file."},
                {"SHADER_NOT_FOUND", "UI Effects Pro shaders not found!"},
                {"CURRENT_PIPELINE", "Current pipeline: {0}"},
                {"TRIED_SHADERS", "Tried: {0}"},
                {"ENSURE_SHADERS", "Please ensure the shaders are installed and included in the build."},
                {"TEXTURE_SETTINGS", "Texture Settings"},
                {"ENABLE_TEXTURE", "Enable Texture"},
                {"OVERLAY_TEXTURE", "Overlay Texture"},
                {"TEXTURE_TILING", "Tiling"},
                {"TEXTURE_OFFSET", "Offset"},
                {"TEXTURE_ROTATION", "Rotation"},
                {"TEXTURE_OPACITY", "Opacity"},
                {"TEXTURE_BLEND_MODE", "Blend Mode"},
                {"TEXTURE_UV_MODE", "UV Mode"},
                {"TEXTURE_ASPECT_MODE", "Aspect Mode"},
                {"TEXTURE_FILTERING", "Filtering"},
                {"SELECT_TEXTURE", "Select Texture"},
                {"NO_TEXTURE_SELECTED", "No texture selected"},
                {"TEXTURE_INFO", "Texture: {0} ({1}x{2})"},
                {"TEXTURE_PERFORMANCE_TIP", "Large textures may impact performance. Consider using smaller textures."},
                {"BLEND_MULTIPLY", "Multiply"},
                {"BLEND_ADD", "Add"},
                {"BLEND_SUBTRACT", "Subtract"},
                {"BLEND_OVERLAY", "Overlay"},
                {"BLEND_SCREEN", "Screen"},
                {"BLEND_REPLACE", "Replace"},
                {"UV_LOCAL", "Local"},
                {"UV_WORLD", "World"},
                {"UV_REPEAT", "Repeat"},
                {"ASPECT_STRETCH", "Stretch"},
                {"ASPECT_FIT_WIDTH", "Fit Width"},
                {"ASPECT_FIT_HEIGHT", "Fit Height"},
                {"ASPECT_FILL", "Fill"},
                // NEW BLUR TYPE TRANSLATIONS
                {"BLUR_TYPE", "Blur Type"},
                {"BLUR_TYPE_INTERNAL", "Internal (Content)"},
                {"BLUR_TYPE_BACKGROUND", "Background (Scene)"},
                {"BLUR_BACKGROUND_WARNING", "⚠️ Background Blur uses GrabPass and has HIGH performance cost. Use sparingly and avoid on mobile devices."},
                // [AFEGIT] Traduccionions Progress Border
                {"PROGRESS_BORDER", "Progress Border"},
                {"ENABLE_PROGRESS_BORDER", "Enable Progress Border"},
                {"PROGRESS_VALUE", "Progress"},
                {"PROGRESS_START_ANGLE", "Start Angle"},
                {"PROGRESS_DIRECTION", "Direction"},
                // [AFEGIT] NOVETES TRADUCCIONS
                {"PROGRESS_COLOR_GRADIENT", "Progress Color Gradient"},
                {"USE_PROGRESS_COLOR_GRADIENT", "Use Color Gradient"},
                {"PROGRESS_COLOR_START", "Start Color (0%)"},
                {"PROGRESS_COLOR_END", "End Color (100%)"},
            };

            // Spanish translations
            var spanish = new Dictionary<string, string>
            {
                {"WINDOW_TITLE", "UI Effects Pro"},
                {"WINDOW_SUBTITLE", "Herramientas Profesionales de Mejora de UI"},
                {"SHADER_STATUS", "ESTADO DEL SHADER"},
                {"PERFORMANCE_IMPACT", "IMPACTO EN RENDIMIENTO"},
                {"LIVE_PREVIEW", "VISTA PREVIA EN TIEMPO REAL"},
                {"EFFECT_SETTINGS", "CONFIGURACIÓN DE EFECTOS"},
                {"ACTIONS", "ACCIONES"},
                {"PRESET_MANAGEMENT", "Gestión de Presets"},
                {"LANGUAGE", "Idioma"},
                {"LANGUAGE_TOOLTIP", "Seleccionar idioma de la interfaz"},
                {"CORNER_RADIUS", "Radio de Esquinas"},
                {"UNIT", "Unidad"},
                {"INDIVIDUAL_CORNERS", "Esquinas Individuales"},
                {"TOP_LEFT", "Arriba Izquierda"},
                {"TOP_RIGHT", "Arriba Derecha"},
                {"BOTTOM_LEFT", "Abajo Izquierda"},
                {"BOTTOM_RIGHT", "Abajo Derecha"},
                {"GLOBAL_RADIUS", "Radio Global"},
                {"BORDER_SETTINGS", "Configuración de Borde"},
                {"WIDTH", "Ancho"},
                {"COLOR", "Color"},
                {"FILL_SETTINGS", "Configuración de Relleno"},
                {"FILL_COLOR", "Color de Relleno"},
                {"BLUR_EFFECT", "Efecto de Desenfoque"},
                {"ENABLE_BLUR", "Activar Desenfoque"},
                {"RADIUS", "Radio"},
                {"ITERATIONS", "Iteraciones"},
                {"DOWNSAMPLE", "Submuestreo"},
                {"BLUR_PERFORMANCE_TIP", "Valores más altos = mejor calidad pero menor rendimiento"},
                {"DROP_SHADOW", "Sombra Proyectada"},
                {"ENABLE_SHADOW", "Activar Sombra"},
                {"SHADOW_COLOR", "Color"},
                {"OFFSET", "Desplazamiento"},
                {"BLUR", "Desenfoque"},
                {"OPACITY", "Opacidad"},
                {"SHADOW_DISTANCE", "Distancia de Sombra"},
                {"GRADIENT_FILL", "Relleno con Gradiente"},
                {"ENABLE_GRADIENT", "Activar Gradiente"},
                {"TYPE", "Tipo"},
                {"COLOR_A", "Color A"},
                {"COLOR_B", "Color B"},
                {"ANGLE", "Ángulo"},
                {"GRADIENT_OVERRIDE_TIP", "El gradiente anula el Color de Relleno sólido"},
                {"GRADIENT_RADIAL_CENTER", "Centro Radial"},
                {"GRADIENT_ANGULAR_ROTATION", "Rotación Angular"},
                {"GRADIENT_RADIAL_SCALE", "Escala Radial"},
                {"PREVIEWING_ON", "Vista previa en"},
                {"CHANGES_REALTIME", "Los cambios se aplican en tiempo real"},
                {"START_PREVIEW", "Iniciar Vista Previa"},
                {"STOP_PREVIEW", "Detener Vista Previa"},
                {"PREVIEW_UNAVAILABLE", "Vista previa no disponible: Faltan los shaders requeridos"},
                {"SELECTION_MUST_HAVE", "La selección debe tener un componente Image o RawImage"},
                {"QUICK_START", "Inicio Rápido:"},
                {"STEP_1", "1. Selecciona un GameObject con Image/RawImage"},
                {"STEP_2", "2. Haz clic en 'Iniciar Vista Previa' para ver efectos"},
                {"STEP_3", "3. Ajusta la configuración de abajo en tiempo real"},
                {"APPLY_TO_SELECTED", "Aplicar a Seleccionados"},
                {"RESET_SETTINGS", "Restablecer Configuración"},
                {"SAVE_PRESET", "Guardar Preset"},
                {"LOAD_PRESET", "Cargar Preset"},
                {"REFRESH_SHADER_CHECK", "Actualizar Verificación de Shader"},
                {"HIDE_TIPS", "Ocultar Consejos"},
                {"BLUR_SHADOW_WARNING", "Efectos de Desenfoque + Sombra activados. Considera optimizar para plataformas móviles."},
                {"BLUR_WARNING", "Efecto de desenfoque activado. Puede impactar el rendimiento en dispositivos de gama baja."},
                {"SHADOW_WARNING", "Efecto de sombra activado. Impacto mínimo en el rendimiento."},
                {"NO_SELECTION", "Sin Selección"},
                {"SELECT_GAMEOBJECT_FIRST", "Por favor selecciona un GameObject primero."},
                {"INVALID_TARGET", "Objetivo Inválido"},
                {"OBJECT_MUST_HAVE_IMAGE", "El objeto seleccionado debe tener un componente Image o RawImage."},
                {"SHADERS_MISSING", "Shaders Faltantes"},
                {"CANNOT_PREVIEW_SHADERS", "No se puede previsualizar: Los shaders de UI Effects Pro no están instalados o disponibles."},
                {"CANNOT_APPLY_SHADERS", "No se pueden aplicar efectos: Los shaders de UI Effects Pro no están instalados o disponibles."},
                {"APPLY_COMPLETE", "Aplicación Completa"},
                {"APPLIED_EFFECT_TO", "Efecto de UI aplicado a {0} objeto(s)."},
                {"WITH_BLUR_SHADOW", " (con Desenfoque + Sombra)"},
                {"WITH_BLUR", " (con Desenfoque)"},
                {"WITH_SHADOW", " (con Sombra)"},
                {"PRESET_SAVED", "Preset Guardado"},
                {"PRESET_SAVED_SUCCESS", "Preset guardado exitosamente en:\n{0}"},
                {"LOAD_ERROR", "Error de Carga"},
                {"COULD_NOT_LOAD", "No se pudo cargar el archivo de preset."},
                {"SHADER_NOT_FOUND", "¡Shaders de UI Effects Pro no encontrados!"},
                {"CURRENT_PIPELINE", "Pipeline actual: {0}"},
                {"TRIED_SHADERS", "Intentado: {0}"},
                {"ENSURE_SHADERS", "Por favor asegúrate de que los shaders estén instalados e incluidos en la build."},
                {"TEXTURE_SETTINGS", "Configuración de Textura"},
                {"ENABLE_TEXTURE", "Activar Textura"},
                {"OVERLAY_TEXTURE", "Textura Superpuesta"},
                {"TEXTURE_TILING", "Repetición"},
                {"TEXTURE_OFFSET", "Desplazamiento"},
                {"TEXTURE_ROTATION", "Rotación"},
                {"TEXTURE_OPACITY", "Opacidad"},
                {"TEXTURE_BLEND_MODE", "Modo de Mezcla"},
                {"TEXTURE_UV_MODE", "Modo UV"},
                {"TEXTURE_ASPECT_MODE", "Modo de Aspecto"},
                {"TEXTURE_FILTERING", "Filtrado"},
                {"SELECT_TEXTURE", "Seleccionar Textura"},
                {"NO_TEXTURE_SELECTED", "No hay textura seleccionada"},
                {"TEXTURE_INFO", "Textura: {0} ({1}x{2})"},
                {"TEXTURE_PERFORMANCE_TIP", "Las texturas grandes pueden afectar el rendimiento. Considera usar texturas más pequeñas."},
                {"BLEND_MULTIPLY", "Multiplicar"},
                {"BLEND_ADD", "Añadir"},
                {"BLEND_SUBTRACT", "Sustraer"},
                {"BLEND_OVERLAY", "Superponer"},
                {"BLEND_SCREEN", "Pantalla"},
                {"BLEND_REPLACE", "Reemplazar"},
                {"UV_LOCAL", "Local"},
                {"UV_WORLD", "Mundial"},
                {"UV_REPEAT", "Repetir"},
                {"ASPECT_STRETCH", "Estirar"},
                {"ASPECT_FIT_WIDTH", "Ajustar Ancho"},
                {"ASPECT_FIT_HEIGHT", "Ajustar Alto"},
                {"ASPECT_FILL", "Llenar"},
                // NEW BLUR TYPE TRANSLATIONS
                {"BLUR_TYPE", "Tipo de Blur"},
                {"BLUR_TYPE_INTERNAL", "Interno (Contenido)"},
                {"BLUR_TYPE_BACKGROUND", "Fondo (Escena)"},
                {"BLUR_BACKGROUND_WARNING", "⚠️ El Blur de Fondo usa GrabPass y tiene ALTO coste de rendimiento. Úsalo con moderación y evita en dispositivos móviles."},
                // [AFEGIT] Traduccionions Progress Border
                {"PROGRESS_BORDER", "Borde de Progreso"},
                {"ENABLE_PROGRESS_BORDER", "Activar Borde de Progreso"},
                {"PROGRESS_VALUE", "Progreso"},
                {"PROGRESS_START_ANGLE", "Ángulo de Inicio"},
                {"PROGRESS_DIRECTION", "Dirección"},
                // [AFEGIT] NOVETES TRADUCCIONS
                {"PROGRESS_COLOR_GRADIENT", "Gradiente de Color de Progreso"},
                {"USE_PROGRESS_COLOR_GRADIENT", "Usar Gradiente de Color"},
                {"PROGRESS_COLOR_START", "Color Inicial (0%)"},
                {"PROGRESS_COLOR_END", "Color Final (100%)"},
            };

            // German translations
            var german = new Dictionary<string, string>
            {
                {"WINDOW_TITLE", "UI Effects Pro"},
                {"WINDOW_SUBTITLE", "Professionelle UI-Verbesserungstools"},
                {"SHADER_STATUS", "SHADER-STATUS"},
                {"PERFORMANCE_IMPACT", "LEISTUNGSEINFLUSS"},
                {"LIVE_PREVIEW", "LIVE-VORSCHAU"},
                {"EFFECT_SETTINGS", "EFFEKT-EINSTELLUNGEN"},
                {"ACTIONS", "AKTIONEN"},
                {"PRESET_MANAGEMENT", "Preset-Verwaltung"},
                {"LANGUAGE", "Sprache"},
                {"LANGUAGE_TOOLTIP", "Schnittstellensprache auswählen"},
                {"CORNER_RADIUS", "Eckenradius"},
                {"UNIT", "Einheit"},
                {"INDIVIDUAL_CORNERS", "Einzelne Ecken"},
                {"TOP_LEFT", "Oben Links"},
                {"TOP_RIGHT", "Oben Rechts"},
                {"BOTTOM_LEFT", "Unten Links"},
                {"BOTTOM_RIGHT", "Unten Rechts"},
                {"GLOBAL_RADIUS", "Globaler Radius"},
                {"BORDER_SETTINGS", "Rahmen-Einstellungen"},
                {"WIDTH", "Breite"},
                {"COLOR", "Farbe"},
                {"FILL_SETTINGS", "Füll-Einstellungen"},
                {"FILL_COLOR", "Füllfarbe"},
                {"BLUR_EFFECT", "Unschärfe-Effekt"},
                {"ENABLE_BLUR", "Unschärfe Aktivieren"},
                {"RADIUS", "Radius"},
                {"ITERATIONS", "Iterationen"},
                {"DOWNSAMPLE", "Herunterskalieren"},
                {"BLUR_PERFORMANCE_TIP", "Höhere Werte = bessere Qualität aber geringere Leistung"},
                {"DROP_SHADOW", "Schlagschatten"},
                {"ENABLE_SHADOW", "Schatten Aktivieren"},
                {"SHADOW_COLOR", "Farbe"},
                {"OFFSET", "Versatz"},
                {"BLUR", "Unschärfe"},
                {"OPACITY", "Deckkraft"},
                {"SHADOW_DISTANCE", "Schattenentfernung"},
                {"GRADIENT_FILL", "Gradient-Füllung"},
                {"ENABLE_GRADIENT", "Gradient Aktivieren"},
                {"TYPE", "Typ"},
                {"COLOR_A", "Farbe A"},
                {"COLOR_B", "Farbe B"},
                {"ANGLE", "Winkel"},
                {"GRADIENT_OVERRIDE_TIP", "Gradient überschreibt die solide Füllfarbe"},
                {"GRADIENT_RADIAL_CENTER", "Radialzentrum"},
                {"GRADIENT_ANGULAR_ROTATION", "Winkelrotation"},
                {"GRADIENT_RADIAL_SCALE", "Radialskalierung"},
                {"PREVIEWING_ON", "Vorschau auf"},
                {"CHANGES_REALTIME", "Änderungen werden in Echtzeit angewendet"},
                {"START_PREVIEW", "Vorschau Starten"},
                {"STOP_PREVIEW", "Vorschau Stoppen"},
                {"PREVIEW_UNAVAILABLE", "Vorschau nicht verfügbar: Erforderliche Shader fehlen"},
                {"SELECTION_MUST_HAVE", "Auswahl muss eine Image- oder RawImage-Komponente haben"},
                {"QUICK_START", "Schnellstart:"},
                {"STEP_1", "1. Wähle ein GameObject mit Image/RawImage"},
                {"STEP_2", "2. Klicke 'Vorschau Starten' um Effekte zu sehen"},
                {"STEP_3", "3. Passe die Einstellungen unten in Echtzeit an"},
                {"APPLY_TO_SELECTED", "Auf Ausgewählte Anwenden"},
                {"RESET_SETTINGS", "Einstellungen Zurücksetzen"},
                {"SAVE_PRESET", "Preset Speichern"},
                {"LOAD_PRESET", "Preset Laden"},
                {"REFRESH_SHADER_CHECK", "Shader-Prüfung Aktualisieren"},
                {"HIDE_TIPS", "Tipps Verstecken"},
                {"BLUR_SHADOW_WARNING", "Unschärfe + Schatten-Effekte aktiviert. Erwäge Optimierung für mobile Plattformen."},
                {"BLUR_WARNING", "Unschärfe-Effekt aktiviert. Kann die Leistung auf schwächeren Geräten beeinträchtigen."},
                {"SHADOW_WARNING", "Schatten-Effekt aktiviert. Minimaler Leistungseinfluss."},
                {"NO_SELECTION", "Keine Auswahl"},
                {"SELECT_GAMEOBJECT_FIRST", "Bitte wähle zuerst ein GameObject aus."},
                {"INVALID_TARGET", "Ungültiges Ziel"},
                {"OBJECT_MUST_HAVE_IMAGE", "Ausgewähltes Objekt muss eine Image- oder RawImage-Komponente haben."},
                {"SHADERS_MISSING", "Shader Fehlen"},
                {"CANNOT_PREVIEW_SHADERS", "Vorschau nicht möglich: UI Effects Pro Shader sind nicht installiert oder verfügbar."},
                {"CANNOT_APPLY_SHADERS", "Effekte können nicht angewendet werden: UI Effects Pro Shader sind nicht installiert oder verfügbar."},
                {"APPLY_COMPLETE", "Anwendung Abgeschlossen"},
                {"APPLIED_EFFECT_TO", "UI-Effekt auf {0} Objekt(e) angewendet."},
                {"WITH_BLUR_SHADOW", " (mit Unschärfe + Schatten)"},
                {"WITH_BLUR", " (mit Unschärfe)"},
                {"WITH_SHADOW", " (mit Schatten)"},
                {"PRESET_SAVED", "Preset Gespeichert"},
                {"PRESET_SAVED_SUCCESS", "Preset erfolgreich gespeichert unter:\n{0}"},
                {"LOAD_ERROR", "Ladefehler"},
                {"COULD_NOT_LOAD", "Preset-Datei konnte nicht geladen werden."},
                {"SHADER_NOT_FOUND", "UI Effects Pro Shader nicht gefunden!"},
                {"CURRENT_PIPELINE", "Aktuelle Pipeline: {0}"},
                {"TRIED_SHADERS", "Versucht: {0}"},
                {"ENSURE_SHADERS", "Bitte stelle sicher, dass die Shader installiert und im Build enthalten sind."},
                {"TEXTURE_SETTINGS", "Textur-Einstellungen"},
                {"ENABLE_TEXTURE", "Textur Aktivieren"},
                {"OVERLAY_TEXTURE", "Overlay-Textur"},
                {"TEXTURE_TILING", "Kachelung"},
                {"TEXTURE_OFFSET", "Versatz"},
                {"TEXTURE_ROTATION", "Drehung"},
                {"TEXTURE_OPACITY", "Deckkraft"},
                {"TEXTURE_BLEND_MODE", "Mischmodus"},
                {"TEXTURE_UV_MODE", "UV-Modus"},
                {"TEXTURE_ASPECT_MODE", "Seitenverhältnis-Modus"},
                {"TEXTURE_FILTERING", "Filterung"},
                {"SELECT_TEXTURE", "Textur Auswählen"},
                {"NO_TEXTURE_SELECTED", "Keine Textur ausgewählt"},
                {"TEXTURE_INFO", "Textur: {0} ({1}x{2})"},
                {"TEXTURE_PERFORMANCE_TIP", "Große Texturen können die Leistung beeinträchtigen. Verwende kleinere Texturen."},
                {"BLEND_MULTIPLY", "Multiplizieren"},
                {"BLEND_ADD", "Hinzufügen"},
                {"BLEND_SUBTRACT", "Subtrahieren"},
                {"BLEND_OVERLAY", "Überlagern"},
                {"BLEND_SCREEN", "Negativ Multiplizieren"},
                {"BLEND_REPLACE", "Ersetzen"},
                {"UV_LOCAL", "Lokal"},
                {"UV_WORLD", "Welt"},
                {"UV_REPEAT", "Wiederholen"},
                {"ASPECT_STRETCH", "Strecken"},
                {"ASPECT_FIT_WIDTH", "Breite Anpassen"},
                {"ASPECT_FIT_HEIGHT", "Höhe Anpassen"},
                {"ASPECT_FILL", "Füllen"},
                // NEW BLUR TYPE TRANSLATIONS
                {"BLUR_TYPE", "Blur-Typ"},
                {"BLUR_TYPE_INTERNAL", "Intern (Inhalt)"},
                {"BLUR_TYPE_BACKGROUND", "Hintergrund (Szene)"},
                {"BLUR_BACKGROUND_WARNING", "⚠️ Hintergrund-Blur verwendet GrabPass und hat HOHE Leistungskosten. Sparsam verwenden und auf mobilen Geräten vermeiden."},
                // [AFEGIT] Traduccionions Progress Border
                {"PROGRESS_BORDER", "Fortschrittsrand"},
                {"ENABLE_PROGRESS_BORDER", "Fortschrittsrand aktivieren"},
                {"PROGRESS_VALUE", "Fortschritt"},
                {"PROGRESS_START_ANGLE", "Startwinkel"},
                {"PROGRESS_DIRECTION", "Richtung"},
                // [AFEGIT] NOVETES TRADUCCIONS
                {"PROGRESS_COLOR_GRADIENT", "Fortschritts-Farbverlauf"},
                {"USE_PROGRESS_COLOR_GRADIENT", "Farbverlauf Verwenden"},
                {"PROGRESS_COLOR_START", "Startfarbe (0%)"},
                {"PROGRESS_COLOR_END", "Endfarbe (100%)"},
            };
            
            // Chinese translations
            var chinese = new Dictionary<string, string>
            {
                {"WINDOW_TITLE", "UI Effects Pro"},
                {"WINDOW_SUBTITLE", "专业UI增强工具"},
                {"SHADER_STATUS", "着色器状态"},
                {"PERFORMANCE_IMPACT", "性能影响"},
                {"LIVE_PREVIEW", "实时预览"},
                {"EFFECT_SETTINGS", "效果设置"},
                {"ACTIONS", "操作"},
                {"PRESET_MANAGEMENT", "预设管理"},
                {"LANGUAGE", "语言"},
                {"LANGUAGE_TOOLTIP", "选择界面语言"},
                {"CORNER_RADIUS", "圆角半径"},
                {"UNIT", "单位"},
                {"INDIVIDUAL_CORNERS", "独立圆角"},
                {"TOP_LEFT", "左上"},
                {"TOP_RIGHT", "右上"},
                {"BOTTOM_LEFT", "左下"},
                {"BOTTOM_RIGHT", "右下"},
                {"GLOBAL_RADIUS", "全局半径"},
                {"BORDER_SETTINGS", "边框设置"},
                {"WIDTH", "宽度"},
                {"COLOR", "颜色"},
                {"FILL_SETTINGS", "填充设置"},
                {"FILL_COLOR", "填充颜色"},
                {"BLUR_EFFECT", "模糊效果"},
                {"ENABLE_BLUR", "启用模糊"},
                {"RADIUS", "半径"},
                {"ITERATIONS", "迭代次数"},
                {"DOWNSAMPLE", "降采样"},
                {"BLUR_PERFORMANCE_TIP", "数值越高 = 质量越好但性能越低"},
                {"DROP_SHADOW", "投影"},
                {"ENABLE_SHADOW", "启用阴影"},
                {"SHADOW_COLOR", "颜色"},
                {"OFFSET", "偏移"},
                {"BLUR", "模糊"},
                {"OPACITY", "不透明度"},
                {"SHADOW_DISTANCE", "阴影距离"},
                {"GRADIENT_FILL", "渐变填充"},
                {"ENABLE_GRADIENT", "启用渐变"},
                {"TYPE", "类型"},
                {"COLOR_A", "颜色A"},
                {"COLOR_B", "颜色B"},
                {"ANGLE", "角度"},
                {"GRADIENT_OVERRIDE_TIP", "渐变会覆盖纯色填充"},
                {"GRADIENT_RADIAL_CENTER", "径向中心"},
                {"GRADIENT_ANGULAR_ROTATION", "角向旋转"},
                {"GRADIENT_RADIAL_SCALE", "径向缩放"},
                {"PREVIEWING_ON", "预览对象"},
                {"CHANGES_REALTIME", "更改实时应用"},
                {"START_PREVIEW", "开始预览"},
                {"STOP_PREVIEW", "停止预览"},
                {"PREVIEW_UNAVAILABLE", "预览不可用：缺少所需着色器"},
                {"SELECTION_MUST_HAVE", "选择对象必须包含Image或RawImage组件"},
                {"QUICK_START", "快速开始："},
                {"STEP_1", "1. 选择带有Image/RawImage的GameObject"},
                {"STEP_2", "2. 点击开始预览查看效果"},
                {"STEP_3", "3. 实时调整下方设置"},
                {"APPLY_TO_SELECTED", "应用到选中对象"},
                {"RESET_SETTINGS", "重置设置"},
                {"SAVE_PRESET", "保存预设"},
                {"LOAD_PRESET", "加载预设"},
                {"REFRESH_SHADER_CHECK", "刷新着色器检查"},
                {"HIDE_TIPS", "隐藏提示"},
                {"BLUR_SHADOW_WARNING", "模糊+阴影效果已启用。建议针对移动平台进行优化。"},
                {"BLUR_WARNING", "模糊效果已启用。可能影响低端设备性能。"},
                {"SHADOW_WARNING", "阴影效果已启用。性能影响最小。"},
                {"NO_SELECTION", "未选择对象"},
                {"SELECT_GAMEOBJECT_FIRST", "请先选择一个GameObject。"},
                {"INVALID_TARGET", "无效目标"},
                {"OBJECT_MUST_HAVE_IMAGE", "选中对象必须包含Image或RawImage组件。"},
                {"SHADERS_MISSING", "着色器缺失"},
                {"CANNOT_PREVIEW_SHADERS", "无法预览：UI Effects Pro着色器未安装或不可用。"},
                {"CANNOT_APPLY_SHADERS", "无法应用效果：UI Effects Pro着色器未安装或不可用。"},
                {"APPLY_COMPLETE", "应用完成"},
                {"APPLIED_EFFECT_TO", "UI效果已应用到{0}个对象。"},
                {"WITH_BLUR_SHADOW", "（含模糊+阴影）"},
                {"WITH_BLUR", "（含模糊）"},
                {"WITH_SHADOW", "（含阴影）"},
                {"PRESET_SAVED", "预设已保存"},
                {"PRESET_SAVED_SUCCESS", "预设成功保存到：\n{0}"},
                {"LOAD_ERROR", "加载错误"},
                {"COULD_NOT_LOAD", "无法加载预设文件。"},
                {"SHADER_NOT_FOUND", "未找到UI Effects Pro着色器！"},
                {"CURRENT_PIPELINE", "当前管线：{0}"},
                {"TRIED_SHADERS", "尝试了：{0}"},
                {"ENSURE_SHADERS", "请确保着色器已安装并包含在构建中。"},
                {"TEXTURE_SETTINGS", "纹理设置"},
                {"ENABLE_TEXTURE", "启用纹理"},
                {"OVERLAY_TEXTURE", "覆盖纹理"},
                {"TEXTURE_TILING", "平铺"},
                {"TEXTURE_OFFSET", "偏移"},
                {"TEXTURE_ROTATION", "旋转"},
                {"TEXTURE_OPACITY", "不透明度"},
                {"TEXTURE_BLEND_MODE", "混合模式"},
                {"TEXTURE_UV_MODE", "UV模式"},
                {"TEXTURE_ASPECT_MODE", "纵横比模式"},
                {"TEXTURE_FILTERING", "过滤"},
                {"SELECT_TEXTURE", "选择纹理"},
                {"NO_TEXTURE_SELECTED", "未选择纹理"},
                {"TEXTURE_INFO", "纹理：{0} ({1}x{2})"},
                {"TEXTURE_PERFORMANCE_TIP", "大纹理可能影响性能。建议使用较小的纹理。"},
                {"BLEND_MULTIPLY", "相乘"},
                {"BLEND_ADD", "相加"},
                {"BLEND_SUBTRACT", "相减"},
                {"BLEND_OVERLAY", "叠加"},
                {"BLEND_SCREEN", "屏幕"},
                {"BLEND_REPLACE", "替换"},
                {"UV_LOCAL", "本地"},
                {"UV_WORLD", "世界"},
                {"UV_REPEAT", "重复"},
                {"ASPECT_STRETCH", "拉伸"},
                {"ASPECT_FIT_WIDTH", "适应宽度"},
                {"ASPECT_FIT_HEIGHT", "适应高度"},
                {"ASPECT_FILL", "填充"},
                // NEW BLUR TYPE TRANSLATIONS
                {"BLUR_TYPE", "模糊类型"},
                {"BLUR_TYPE_INTERNAL", "内部（内容）"},
                {"BLUR_TYPE_BACKGROUND", "背景（场景）"},
                {"BLUR_BACKGROUND_WARNING", "⚠️ 背景模糊使用 GrabPass，性能开销很高。请谨慎使用，并避免在移动设备上使用。"},
                // [AFEGIT] Traduccionions Progress Border
                {"PROGRESS_BORDER", "进度边框"},
                {"ENABLE_PROGRESS_BORDER", "启用进度边框"},
                {"PROGRESS_VALUE", "进度"},
                {"PROGRESS_START_ANGLE", "起始角度"},
                {"PROGRESS_DIRECTION", "方向"},
                // [AFEGIT] NOVETES TRADUCCIONS
                {"PROGRESS_COLOR_GRADIENT", "进度颜色渐变"},
                {"USE_PROGRESS_COLOR_GRADIENT", "使用颜色渐变"},
                {"PROGRESS_COLOR_START", "起始颜色 (0%)"},
                {"PROGRESS_COLOR_END", "结束颜色 (100%)"},
            };

            // Add the dictionaries for each language to the main translations dictionary.
            _translations[SupportedLanguage.English] = english;
            _translations[SupportedLanguage.Spanish] = spanish;
            _translations[SupportedLanguage.German] = german;
            _translations[SupportedLanguage.Chinese] = chinese;
        }
    }

    /// <summary>
    /// A helper class to simplify the creation of localized GUI content in editor scripts.
    /// It wraps calls to the LocalizationManager for convenience.
    /// </summary>
    public static class LocalizedGUI
    {
        /// <summary>
        /// Creates a GUIContent object with localized text and an optional localized tooltip.
        /// </summary>
        /// <param name="key">The key for the main text.</param>
        /// <param name="tooltipKey">The key for the tooltip text.</param>
        /// <returns>A new GUIContent object.</returns>
        public static GUIContent Content(string key, string tooltipKey = "")
        {
            string text = LocalizationManager.GetText(key);
            string tooltip = !string.IsNullOrEmpty(tooltipKey) ? LocalizationManager.GetText(tooltipKey) : "";
            return new GUIContent(text, tooltip);
        }

        /// <summary>
        /// Creates a GUIContent object with localized text, an image, and an optional localized tooltip.
        /// </summary>
        /// <param name="key">The key for the main text.</param>
        /// <param name="image">The image to display.</param>
        /// <param name="tooltipKey">The key for the tooltip text.</param>
        /// <returns>A new GUIContent object.</returns>
        public static GUIContent Content(string key, Texture2D image, string tooltipKey = "")
        {
            string text = LocalizationManager.GetText(key);
            string tooltip = !string.IsNullOrEmpty(tooltipKey) ? LocalizationManager.GetText(tooltipKey) : "";
            return new GUIContent(text, image, tooltip);
        }

        /// <summary>
        /// A direct wrapper to get a localized string.
        /// </summary>
        /// <param name="key">The key for the text.</param>
        /// <returns>The translated string.</returns>
        public static string Text(string key)
        {
            return LocalizationManager.GetText(key);
        }

        /// <summary>
        /// Retrieves a localized formatted string.
        /// Useful for translations that contain placeholders (e.g., "Applied to {0} objects.").
        /// </summary>
        /// <param name="key">The key for the format string.</param>
        /// <param name="args">The arguments to insert into the string.</param>
        /// <returns>The formatted, translated string.</returns>
        public static string Format(string key, params object[] args)
        {
            string formatString = LocalizationManager.GetText(key);
            return string.Format(formatString, args);
        }

        /// <summary>
        /// Renders a language selection dropdown (popup) using EditorGUILayout.
        /// This provides a reusable UI component for changing the language.
        /// </summary>
        public static void LanguageSelector()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Use the Content helper to create a localized label and tooltip.
            EditorGUILayout.LabelField(Content("LANGUAGE", "LANGUAGE_TOOLTIP"), GUILayout.Width(60));
            
            int currentIndex = (int)LocalizationManager.CurrentLanguage;
            string[] languageNames = LocalizationManager.GetLanguageNames();
            
            // Create the popup field.
            int newIndex = EditorGUILayout.Popup(currentIndex, languageNames, GUILayout.Width(100));
            
            // If the user selects a new language, update the manager.
            if (newIndex != currentIndex)
            {
                LocalizationManager.CurrentLanguage = (SupportedLanguage)newIndex;
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
}