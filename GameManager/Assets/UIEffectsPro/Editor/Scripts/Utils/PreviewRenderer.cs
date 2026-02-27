
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace UIEffectsPro.Editor
{
    /// <summary>
    /// Handles the rendering of a UI effect preview within the Unity Editor.
    /// This class sets up a dedicated scene with a camera and canvas to render
    /// a UI Image with an applied UIEffectsPro profile to a RenderTexture.
    /// It implements IDisposable to ensure proper cleanup of unmanaged resources.
    /// </summary>
    public class PreviewRenderer : System.IDisposable
    {
        // --- Constants ---
        private const int DEFAULT_PREVIEW_WIDTH = 256;
        private const int DEFAULT_PREVIEW_HEIGHT = 256;

        // --- Private Fields ---
        private RenderTexture _renderTexture;
        private Camera _previewCamera;
        private Canvas _previewCanvas;
        private GameObject _previewRoot;
        private Image _previewImage;
        private UIEffectsPro.Runtime.UIEffectComponent _effectComponent;

        private int _width;
        private int _height;
        private bool _isInitialized = false;
        private bool _disposed = false;

        // --- Public Properties ---
        
        /// <summary>
        /// Gets the RenderTexture used for the preview.
        /// </summary>
        public RenderTexture PreviewTexture => _renderTexture;

        /// <summary>
        /// Gets a value indicating whether the renderer has been initialized and not disposed.
        /// </summary>
        public bool IsInitialized => _isInitialized && !_disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewRenderer"/> class with specified dimensions.
        /// </summary>
        /// <param name="width">The width of the preview texture.</param>
        /// <param name="height">The height of the preview texture.</param>
        public PreviewRenderer(int width = DEFAULT_PREVIEW_WIDTH, int height = DEFAULT_PREVIEW_HEIGHT)
        {
            _width = width;
            _height = height;
        }

        /// <summary>
        /// Initializes the renderer if it hasn't been initialized yet.
        /// This sets up the render texture and the preview scene.
        /// </summary>
        public void InitializeIfNeeded()
        {
            if (_isInitialized && !_disposed) return;

            try
            {
                CreateRenderTexture();
                CreatePreviewScene();
                _isInitialized = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"PreviewRenderer: Failed to initialize. Error: {e.Message}");
                Dispose(); // Ensure cleanup on failure.
            }
        }

        /// <summary>
        /// Creates and configures the RenderTexture for capturing the preview.
        /// </summary>
        private void CreateRenderTexture()
        {
            _renderTexture = new RenderTexture(_width, _height, 0, RenderTextureFormat.ARGB32);
            _renderTexture.name = "UIEffects_PreviewRT";
            _renderTexture.antiAliasing = 4;
            _renderTexture.filterMode = FilterMode.Bilinear;
            _renderTexture.Create();
        }

        /// <summary>
        /// Sets up the hidden scene required for rendering the UI preview.
        /// This includes creating a root object, a dedicated camera, a world-space canvas,
        /// and an Image component to apply the effect to.
        /// </summary>
        private void CreatePreviewScene()
        {
            // Create a root object to hold all preview elements.
            _previewRoot = new GameObject("UIEffects_PreviewRoot");
            _previewRoot.hideFlags = HideFlags.HideAndDontSave;

            // Setup the camera for rendering.
            var cameraObject = new GameObject("PreviewCamera");
            cameraObject.transform.SetParent(_previewRoot.transform);
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            _previewCamera = cameraObject.AddComponent<Camera>();
            _previewCamera.clearFlags = CameraClearFlags.SolidColor;
            _previewCamera.backgroundColor = Color.clear;
            _previewCamera.cullingMask = 1 << 31; // Render only layer 31
            _previewCamera.orthographic = true;
            _previewCamera.orthographicSize = 1f;
            _previewCamera.nearClipPlane = 0.1f;
            _previewCamera.farClipPlane = 10f;
            _previewCamera.targetTexture = _renderTexture;
            _previewCamera.enabled = false; // Manually call Render().

            // Setup the canvas in world space.
            var canvasObject = new GameObject("PreviewCanvas");
            canvasObject.transform.SetParent(_previewRoot.transform);
            canvasObject.hideFlags = HideFlags.HideAndDontSave;
            canvasObject.layer = 31; // Assign to the preview layer.

            _previewCanvas = canvasObject.AddComponent<Canvas>();
            _previewCanvas.renderMode = RenderMode.WorldSpace;
            _previewCanvas.worldCamera = _previewCamera;
            
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            
            var canvasTransform = _previewCanvas.GetComponent<RectTransform>();
            canvasTransform.sizeDelta = new Vector2(200, 200);
            canvasTransform.position = new Vector3(0, 0, 1f);

            // Setup the image that will display the effect.
            var imageObject = new GameObject("PreviewImage");
            imageObject.transform.SetParent(canvasObject.transform, false);
            imageObject.hideFlags = HideFlags.HideAndDontSave;
            imageObject.layer = 31;

            _previewImage = imageObject.AddComponent<Image>();
            _previewImage.color = Color.white;
            
            var imageTransform = _previewImage.GetComponent<RectTransform>();
            imageTransform.anchorMin = Vector2.zero;
            imageTransform.anchorMax = Vector2.one;
            imageTransform.sizeDelta = Vector2.zero;
            imageTransform.anchoredPosition = Vector2.zero;

            // Add the effect component to the image.
            _effectComponent = imageObject.AddComponent<UIEffectsPro.Runtime.UIEffectComponent>();
            _effectComponent.autoUpdate = false; // Control updates manually.
        }

        /// <summary>
        /// Renders the preview of a given UIEffectProfile.
        /// </summary>
        /// <param name="profile">The effect profile to render.</param>
        /// <returns>The RenderTexture containing the rendered preview, or null on failure.</returns>
        public RenderTexture RenderPreview(UIEffectsPro.Runtime.UIEffectProfile profile)
        {
            if (profile == null)
            {
                return null;
            }

            // Ensure the renderer is ready.
            if (!_isInitialized)
            {
                InitializeIfNeeded();
                if (!_isInitialized) return null;
            }

            try
            {
                // Apply the profile and force an update of the effect.
                _effectComponent.SetProfile(profile);
                _effectComponent.ForceUpdate();

                // Manually trigger a render from the preview camera.
                RenderTexture previousActive = RenderTexture.active;
                _previewCamera.Render();
                RenderTexture.active = previousActive;

                return _renderTexture;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"PreviewRenderer: Failed to render preview. Error: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Updates the size of the preview render texture.
        /// Recreates the texture if dimensions have changed.
        /// </summary>
        /// <param name="width">The new width.</param>
        /// <param name="height">The new height.</param>
        public void UpdatePreviewSize(int width, int height)
        {
            if (width == _width && height == _height) return;

            _width = width;
            _height = height;

            if (_isInitialized)
            {
                if (_renderTexture != null)
                {
                    _renderTexture.Release();
                    Object.DestroyImmediate(_renderTexture);
                }

                CreateRenderTexture();

                if (_previewCamera != null)
                {
                    _previewCamera.targetTexture = _renderTexture;
                }
            }
        }

        /// <summary>
        /// Draws the rendered preview texture into a specific GUI rectangle.
        /// </summary>
        /// <param name="rect">The Rect to draw the preview in.</param>
        public void DrawPreview(Rect rect)
        {
            if (_renderTexture != null && _renderTexture.IsCreated())
            {
                GUI.DrawTexture(rect, _renderTexture, ScaleMode.ScaleToFit, true);
                DrawRectBorder(rect, Color.gray, 1f);
            }
            else
            {
                // Draw a placeholder if no preview is available.
                EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 1f));
                Rect labelRect = rect;
                labelRect.y += rect.height * 0.5f - 10f;
                labelRect.height = 20f;
                GUI.Label(labelRect, "No Preview", EditorStyles.centeredGreyMiniLabel);
            }
        }
        
        /// <summary>
        /// Draws a simple border around a given rectangle.
        /// </summary>
        private void DrawRectBorder(Rect rect, Color color, float width = 1f)
        {
            Color originalColor = GUI.color;
            GUI.color = color;

            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, width), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - width, rect.width, width), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, width, rect.height), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x + rect.width - width, rect.y, width, rect.height), EditorGUIUtility.whiteTexture);

            GUI.color = originalColor;
        }

        /// <summary>
        /// Gets a layout rectangle and draws the preview into it.
        /// </summary>
        /// <param name="maxWidth">The maximum width for the layout element.</param>
        /// <param name="maxHeight">The maximum height for the layout element.</param>
        public void DrawPreview(float maxWidth = 256f, float maxHeight = 256f)
        {
            Rect rect = GUILayoutUtility.GetRect(maxWidth, maxHeight, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            DrawPreview(rect);
        }

        /// <summary>
        /// Cleans up all resources used by the renderer.
        /// This includes the RenderTexture and all GameObjects created for the preview scene.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                if (_renderTexture != null)
                {
                    if (_renderTexture.IsCreated())
                    {
                        _renderTexture.Release();
                    }
                    Object.DestroyImmediate(_renderTexture);
                    _renderTexture = null;
                }

                if (_previewRoot != null)
                {
                    Object.DestroyImmediate(_previewRoot);
                    _previewRoot = null;
                }
                
                // Nullify references
                _previewCamera = null;
                _previewCanvas = null;
                _previewImage = null;
                _effectComponent = null;

                _isInitialized = false;
                _disposed = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"PreviewRenderer: Error during disposal: {e.Message}");
            }
        }

        /// <summary>
        /// Finalizer to ensure Dispose is called, warning about potential memory leaks
        /// if it was not called explicitly.
        /// </summary>
        ~PreviewRenderer()
        {
            if (!_disposed)
            {
                // This warning helps developers identify places where they forgot to call Dispose().
                Debug.LogWarning("PreviewRenderer: Finalizer called without explicit Dispose(). Always call Dispose() when done with PreviewRenderer.");
                Dispose();
            }
        }
    }
}