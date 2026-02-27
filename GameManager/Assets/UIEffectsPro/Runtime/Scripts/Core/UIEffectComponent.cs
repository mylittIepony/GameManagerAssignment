// UIEffectsPro/Runtime/Scripts/Core/UIEffectComponent.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace UIEffectsPro.Runtime
{
    /// <summary>
    /// Main component to apply advanced UI effects to a Graphic element (Image, RawImage).
    /// This component modifies the material and mesh of the target UI element to render effects
    /// like rounded corners, borders, shadows, blur, gradients, and textures.
    /// It is designed to be highly customizable through a UIEffectProfile asset.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("UI Effects Pro/UI Effect Component")]
    [ExecuteInEditMode]
    public class UIEffectComponent : MonoBehaviour, IMeshModifier
    {
        #region Serialized Fields

        [Header("Effect Settings")]
        [Tooltip("Profile containing all effect parameters.")]
        public UIEffectProfile profile;

        [Header("Runtime Settings")]
        [Tooltip("Update the effect automatically when its profile changes.")]
        public bool autoUpdate = true;

        [Tooltip("Force an update every frame. This can impact performance.")]
        public bool forceUpdateEveryFrame = false;

        [Header("Performance Settings")]
        [Tooltip("Use lower quality for blur to improve performance.")]
        public bool optimizeBlur = true;

        [Tooltip("Reduce shadow quality for better performance.")]
        public bool optimizeShadow = false;

        [Header("Debug Info")]
        [Tooltip("Show debug information in the console.")]
        public bool showDebugInfo = false;

        #endregion

        #region Private Fields

        // Component references
        private Image _image;
        private RawImage _rawImage;
        private Graphic _targetGraphic;
        private RectTransform _rectTransform;

        // Material management
        private Material _originalMaterial;
        private Material _effectMaterial;
        private bool _isInitialized = false;

        // State tracking for updates
        private ProfileCache _profileCache = new ProfileCache();
        private bool _hasChanges = false;
        private Vector2 _lastRectSize = Vector2.zero;
        private bool _requiresMeshModification = false;

        #endregion

        #region Shader Property IDs

        // Pre-caching shader property IDs for performance.
        private static readonly int _MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int _ColorId = Shader.PropertyToID("_Color");
        private static readonly int _SpriteUvsId = Shader.PropertyToID("_SpriteUvs"); // <-- NOU
        private static readonly int _CornerRadiiId = Shader.PropertyToID("_CornerRadii");
        private static readonly int _CornerOffsetsId = Shader.PropertyToID("_CornerOffsets");
        private static readonly int _BorderWidthId = Shader.PropertyToID("_BorderWidth");
        private static readonly int _BorderColorId = Shader.PropertyToID("_BorderColor");
        private static readonly int _BorderColorBId = Shader.PropertyToID("_BorderColorB");
        private static readonly int _UseBorderGradientId = Shader.PropertyToID("_UseBorderGradient");
        private static readonly int _BorderGradientTypeId = Shader.PropertyToID("_BorderGradientType");
        private static readonly int _BorderGradientAngleId = Shader.PropertyToID("_BorderGradientAngle");
        private static readonly int _BorderGradientRadialCenterId = Shader.PropertyToID("_BorderGradientRadialCenter");
        private static readonly int _BorderGradientRadialScaleId = Shader.PropertyToID("_BorderGradientRadialScale");
        private static readonly int _BorderGradientAngularRotationId = Shader.PropertyToID("_BorderGradientAngularRotation");
        private static readonly int _UseIndividualCornersId = Shader.PropertyToID("_UseIndividualCorners");
        private static readonly int _UseIndividualOffsetsId = Shader.PropertyToID("_UseIndividualOffsets");
        private static readonly int _GlobalCornerOffsetId = Shader.PropertyToID("_GlobalCornerOffset");
        private static readonly int _AAId = Shader.PropertyToID("_AA");
        private static readonly int _RectSizeId = Shader.PropertyToID("_RectSize");

        // Blur properties
        private static readonly int _EnableBlurId = Shader.PropertyToID("_EnableBlur");
        private static readonly int _BlurTypeId = Shader.PropertyToID("_BlurType"); // ADDED
        private static readonly int _BlurRadiusId = Shader.PropertyToID("_BlurRadius");
        private static readonly int _BlurIterationsId = Shader.PropertyToID("_BlurIterations");
        private static readonly int _BlurDownsampleId = Shader.PropertyToID("_BlurDownsample");

        // Shadow properties
        private static readonly int _EnableShadowId = Shader.PropertyToID("_EnableShadow");
        private static readonly int _ShadowColorId = Shader.PropertyToID("_ShadowColor");
        private static readonly int _ShadowOffsetId = Shader.PropertyToID("_ShadowOffset");
        private static readonly int _ShadowBlurId = Shader.PropertyToID("_ShadowBlur");
        private static readonly int _ShadowOpacityId = Shader.PropertyToID("_ShadowOpacity");

        // Gradient properties
        private static readonly int _EnableGradientId = Shader.PropertyToID("_EnableGradient");
        private static readonly int _GradientTypeId = Shader.PropertyToID("_GradientType");
        private static readonly int _GradientColorAId = Shader.PropertyToID("_GradientColorA");
        private static readonly int _GradientColorBId = Shader.PropertyToID("_GradientColorB");
        private static readonly int _GradientAngleId = Shader.PropertyToID("_GradientAngle");
        private static readonly int _GradientRadialCenterId = Shader.PropertyToID("_GradientRadialCenter");
        private static readonly int _GradientAngularRotationId = Shader.PropertyToID("_GradientAngularRotation");
        private static readonly int _GradientRadialScaleId = Shader.PropertyToID("_GradientRadialScale");
        
        // Procedural Shape properties
        private static readonly int _ShapeTypeId = Shader.PropertyToID("_ShapeType");
        private static readonly int _ShapeVerticesId = Shader.PropertyToID("_ShapeVertices");
        private static readonly int _ShapeVerticesExtId = Shader.PropertyToID("_ShapeVerticesExt");
        private static readonly int _VertexCountId = Shader.PropertyToID("_VertexCount");
        
        // [AFEGIT] IDs de propietats per a la vora de progrés
        private static readonly int _UseProgressBorderId = Shader.PropertyToID("_UseProgressBorder");
        private static readonly int _ProgressValueId = Shader.PropertyToID("_ProgressValue");
        private static readonly int _ProgressStartAngleId = Shader.PropertyToID("_ProgressStartAngle");
        private static readonly int _ProgressDirectionId = Shader.PropertyToID("_ProgressDirection");
        // MODIFICACIÓ A) Afegir Property IDs
        private static readonly int _ProgressColorStartId = Shader.PropertyToID("_ProgressColorStart");
        private static readonly int _ProgressColorEndId = Shader.PropertyToID("_ProgressColorEnd");
        private static readonly int _UseProgressColorGradientId = Shader.PropertyToID("_UseProgressColorGradient");

        // Texture properties
        private static readonly int _EnableTextureId = Shader.PropertyToID("_EnableTexture");
        private static readonly int _OverlayTextureId = Shader.PropertyToID("_OverlayTexture");
        private static readonly int _TextureTilingId = Shader.PropertyToID("_TextureTiling");
        private static readonly int _TextureOffsetId = Shader.PropertyToID("_TextureOffset");
        private static readonly int _TextureRotationId = Shader.PropertyToID("_TextureRotation");
        private static readonly int _TextureOpacityId = Shader.PropertyToID("_TextureOpacity");
        private static readonly int _TextureBlendModeId = Shader.PropertyToID("_TextureBlendMode");
        private static readonly int _TextureUVModeId = Shader.PropertyToID("_TextureUVMode");
        private static readonly int _TextureMatrixId = Shader.PropertyToID("_TextureMatrix");

        #endregion

        #region Shader Names

        // Defines the shader names for different rendering pipelines.
        private const string SHADER_URP = "UIEffects/RoundedBorder_URP";
        private const string SHADER_BUILTIN = "UIEffects/RoundedBorder_Builtin";
        private const string SHADER_LEGACY = "UIEffects/RoundedBorder";
        
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
        }

        private void OnEnable()
        {
            // Initialize if not already done.
            if (!_isInitialized)
            {
                Initialize();
            }

            // If initialization was successful, apply the effect profile.
            if (_isInitialized)
            {
                // --- FIX: Re-apply the effect material ---
                // This is required because OnDisable() restores the original material.
                // We must ensure the effect material is active when the component is re-enabled.
                if (_targetGraphic != null && _effectMaterial != null)
                {
                    // Re-copy properties in case the sprite/texture changed while disabled.
                    CopyMaterialProperties();
                    _targetGraphic.material = _effectMaterial;
                }
                // --- End of Fix ---

                ApplyProfile();

                // Mark the graphic as dirty to force a redraw.
                // Using SetAllDirty() is safer to ensure both mesh and material are updated.
                if (_targetGraphic != null)
                {
                    _targetGraphic.SetAllDirty();
                }
            }
        }

        private void OnDisable()
        {
            // Revert to the original material when the component is disabled.
            RestoreOriginalMaterial();

            // Forcem la regeneració del mesh estàndard per evitar que es quedi el padding de l'ombra
            if (_targetGraphic != null)
            {
                _targetGraphic.SetVerticesDirty();
            }
        }

        private void Update()
        {
            if (!_isInitialized) return;

            // Check if the RectTransform size has changed, which requires an update.
            CheckRectSizeChange();

            // Update the effect if forced or if changes have been detected.
            if (forceUpdateEveryFrame || (autoUpdate && _hasChanges))
            {
                UpdateEffect();
            }
        }

        private void OnDestroy()
        {
            // Clean up the created effect material to prevent memory leaks.
            CleanupMaterials();
        }

        private void OnRectTransformDimensionsChange()
        {
            // When the RectTransform changes, flag that an update is needed.
            if (enabled && gameObject.activeInHierarchy)
            {
                _hasChanges = true;

                if (_targetGraphic != null)
                {
                    _targetGraphic.SetAllDirty();
                }
            }
        }

        #endregion

        #region IMeshModifier Implementation

        /// <summary>
        /// Modifies the mesh using a Mesh object.
        /// </summary>
        public void ModifyMesh(Mesh mesh)
        {
            using (var vh = new VertexHelper(mesh))
            {
                ModifyMesh(vh);
                vh.FillMesh(mesh);
            }
        }

        /// <summary>
        /// Main mesh modification method. Called by the Canvas system.
        /// This determines whether to use a standard quad or a padded quad for effects like shadows.
        /// </summary>
        public void ModifyMesh(VertexHelper vh)
        {
            if (!isActiveAndEnabled)
            {
                GenerateStandardQuad(vh);
                return;
            }

            if (ShouldModifyMesh())
            {
                GeneratePaddedQuad(vh);
            }
            else
            {
                GenerateStandardQuad(vh);
            }
        }

        /// <summary>
        /// Determines if the mesh needs to be modified to accommodate effects like shadows.
        /// </summary>
        private bool ShouldModifyMesh()
        {
            return profile != null && profile.enableShadow;
        }
        
        /// <summary>
        /// Generates a standard quad mesh that fills the RectTransform's bounds.
        /// </summary>
        private void GenerateStandardQuad(VertexHelper vh)
        {
            vh.Clear();
            if (_rectTransform == null) return;
        
            Rect rect = _rectTransform.rect;
            Color32 color = _targetGraphic != null ? _targetGraphic.color : Color.white;
        
            Vector4 uv = GetSpriteUVs();
        
            UIVertex vert = UIVertex.simpleVert;
            vert.color = color;
        
            // Bottom-Left
            vert.position = new Vector3(rect.xMin, rect.yMin);
            vert.uv0 = new Vector2(uv.x, uv.y);
            vh.AddVert(vert);
        
            // Top-Left
            vert.position = new Vector3(rect.xMin, rect.yMax);
            vert.uv0 = new Vector2(uv.x, uv.w);
            vh.AddVert(vert);
        
            // Top-Right
            vert.position = new Vector3(rect.xMax, rect.yMax);
            vert.uv0 = new Vector2(uv.z, uv.w);
            vh.AddVert(vert);
        
            // Bottom-Right
            vert.position = new Vector3(rect.xMax, rect.yMin);
            vert.uv0 = new Vector2(uv.z, uv.y);
            vh.AddVert(vert);
        
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        /// <summary>
        /// Generates a padded quad for shadow effects.
        /// </summary>
        private void GeneratePaddedQuad(VertexHelper vh)
        {
            vh.Clear();
            if (_rectTransform == null || profile == null)
            {
                if (_rectTransform != null) GenerateStandardQuad(vh);
                return;
            }
        
            Rect rect = _rectTransform.rect;
            Color32 color = _targetGraphic != null ? _targetGraphic.color : Color.white;
        
            // Calcular padding
            float padding = 0f;
            if (profile.enableShadow && profile.shadowParams != null)
            {
                var shadowParams = profile.shadowParams;
                Vector2 rectSize = _rectTransform.rect.size;
                Vector2 offsetPx = ConvertVectorToPixels(shadowParams.offset, profile.shadowUnit, rectSize);
                float blurPx = ConvertValueToPixels(shadowParams.blur, profile.shadowUnit, rectSize, true);
                padding = Mathf.Max(Mathf.Abs(offsetPx.x), Mathf.Abs(offsetPx.y)) + blurPx * 2;
            }
        
            // Vèrtexs amplificats
            Vector3 v0 = new Vector3(rect.xMin - padding, rect.yMin - padding);
            Vector3 v1 = new Vector3(rect.xMin - padding, rect.yMax + padding);
            Vector3 v2 = new Vector3(rect.xMax + padding, rect.yMax + padding);
            Vector3 v3 = new Vector3(rect.xMax + padding, rect.yMin - padding);
            
            Vector4 uv = GetSpriteUVs();
            
            // UVs estesos per l'atlas
            float uvPaddingX = (uv.z - uv.x) * padding / rect.width;
            float uvPaddingY = (uv.w - uv.y) * padding / rect.height;

            Vector2 uv0_ext = new Vector2(uv.x - uvPaddingX, uv.y - uvPaddingY);
            Vector2 uv1_ext = new Vector2(uv.x - uvPaddingX, uv.w + uvPaddingY);
            Vector2 uv2_ext = new Vector2(uv.z + uvPaddingX, uv.w + uvPaddingY);
            Vector2 uv3_ext = new Vector2(uv.z + uvPaddingX, uv.y - uvPaddingY);
            
            UIVertex vert = UIVertex.simpleVert;
            vert.color = color;
            
            vert.position = v0;
            vert.uv0 = uv0_ext;
            vh.AddVert(vert);
            
            vert.position = v1;
            vert.uv0 = uv1_ext;
            vh.AddVert(vert);
            
            vert.position = v2;
            vert.uv0 = uv2_ext;
            vh.AddVert(vert);
            
            vert.position = v3;
            vert.uv0 = uv3_ext;
            vh.AddVert(vert);
        
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        private Vector4 GetSpriteUVs()
        {
            if (_image != null && _image.sprite != null && _image.type == Image.Type.Simple)
            {
                return UnityEngine.Sprites.DataUtility.GetOuterUV(_image.sprite);
            }
            return new Vector4(0, 0, 1, 1);
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Caches references to required components for performance.
        /// </summary>
        private void CacheComponents()
        {
            _image = GetComponent<Image>();
            _rawImage = GetComponent<RawImage>();
            _rectTransform = GetComponent<RectTransform>();
            _targetGraphic = _image != null ? (Graphic)_image : (Graphic)_rawImage;

            if (_targetGraphic == null)
            {
                Debug.LogWarning($"UIEffectComponent on {gameObject.name} requires an Image or RawImage component!", this);
            }

            if (_rectTransform != null)
            {
                _lastRectSize = _rectTransform.rect.size;
            }
        }

        /// <summary>
        /// Initializes the component, creates the effect material, and prepares it for rendering.
        /// </summary>
        private void Initialize()
        {
            if (_targetGraphic == null)
            {
                CacheComponents();
                if (_targetGraphic == null)
                {
                    Debug.LogError($"UIEffectComponent on {gameObject.name}: No valid Graphic component found!", this);
                    return;
                }
            }

            _originalMaterial = _targetGraphic.material;

            if (CreateEffectMaterial())
            {
                _isInitialized = true;
                LogDebugInfo();
            }
        }

        /// <summary>
        /// Creates a new instance of the effect material using the appropriate shader.
        /// </summary>
        private bool CreateEffectMaterial()
        {
            string shaderName = GetAvailableShaderName();
            if (string.IsNullOrEmpty(shaderName)) return false;

            Shader effectShader = Shader.Find(shaderName);
            if (effectShader == null)
            {
                Debug.LogError($"Could not find any suitable shader for UI Effects.", this);
                return false;
            }

            // Create a new material instance to avoid modifying the shared shader asset.
            _effectMaterial = new Material(effectShader)
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = $"UIEffect_Material_{GetInstanceID()}"
            };

            CopyMaterialProperties();
            // Assign the new effect material to the graphic component.
            _targetGraphic.material = _effectMaterial;

            if (showDebugInfo) Debug.Log($"Effect material created using shader: {shaderName}");
            return true;
        }

        /// <summary>
        /// Copies essential properties (like main texture and color) from the original material
        /// to the new effect material to maintain the base appearance.
        /// </summary>
        private void CopyMaterialProperties()
        {
            if (_originalMaterial != null && _originalMaterial != _targetGraphic.defaultMaterial)
            {
                if (_originalMaterial.HasProperty(_MainTexId)) 
                    _effectMaterial.SetTexture(_MainTexId, _originalMaterial.GetTexture(_MainTexId));
                if (_originalMaterial.HasProperty(_ColorId)) 
                    _effectMaterial.SetColor(_ColorId, _originalMaterial.GetColor(_ColorId));
            }
            else if (_image != null && _image.sprite != null)
            {
                // CRITICAL CHANGE: Always use the sprite's texture.
                // Unity automatically handles whether it's from an atlas or not.
                _effectMaterial.SetTexture(_MainTexId, _image.sprite.texture);
                
                // Configure the wrap mode to prevent bleeding between atlas sprites.
                if (_image.sprite.texture != null)
                {
                    _image.sprite.texture.wrapMode = TextureWrapMode.Clamp;
                }
                
                if (showDebugInfo)
                {
                    bool isAtlas = _image.sprite.packed || _image.sprite.texture.name.Contains("Atlas");
                    Debug.Log($"Texture set: {_image.sprite.texture.name}, IsAtlas: {isAtlas}");
                }
            }
        }

        /// <summary>
        /// Finds the most suitable shader based on the current render pipeline (Built-in, URP).
        /// </summary>
        private string GetAvailableShaderName()
        {
            // Llista de shaders per provar, en ordre de preferència.
            // Aquesta llista ara es construeix dinàmicament en funció de la pipeline.
            var preferredShaders = new System.Collections.Generic.List<string>();

#if UNITY_PIPELINE_URP
            // --- Som a URP ---
            // 1. Preferim el shader "Enhanced" de URP (si existeix)
            preferredShaders.Add("UIEffects/RoundedBorder_Enhanced_URP");
            // 2. Després el shader URP estàndard
            preferredShaders.Add(SHADER_URP);
            // 3. Fallback a "Enhanced" genèric
            preferredShaders.Add("UIEffects/RoundedBorder_Enhanced");
            // 4. Fallback a Built-in (per si de cas l'usuari té URP però volia aquest)
            preferredShaders.Add(SHADER_BUILTIN);
#else
            // --- Som a Built-in o una altra pipeline ---
            // 1. Preferim el shader "Enhanced" genèric (si existeix)
            preferredShaders.Add("UIEffects/RoundedBorder_Enhanced");
            // 2. Després el shader Built-in estàndard
            preferredShaders.Add(SHADER_BUILTIN);
            // 3. Fallback a URP (menys probable que funcioni, però per completesa si el Built-in falta)
            preferredShaders.Add(SHADER_URP);
#endif

            // Afegim els fallbacks comuns al final de la llista
            preferredShaders.Add(SHADER_LEGACY);
            preferredShaders.Add("UI/Default"); // Fallback final

            // Ara iterem la llista que hem construït
            foreach (string shaderName in preferredShaders)
            {
                // Comprovem només si el nom no és buit (per si de cas)
                if (string.IsNullOrEmpty(shaderName)) continue;
                
                if (Shader.Find(shaderName) != null)
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"Using shader: {shaderName} for rendering pipeline: {GetCurrentPipelineName()}");
                    }
                    return shaderName;
                }
            }

            // Aquest punt no s'hauria d'assolir mai perquè "UI/Default" hauria d'existir
            Debug.LogWarning("No suitable UI Effects shader found. Using UI/Default fallback.", this);
            return "UI/Default";
        }
        
        /// <summary>
        /// Returns the name of the currently active render pipeline.
        /// </summary>
        private string GetCurrentPipelineName()
        {
#if UNITY_PIPELINE_URP
            return "Universal Render Pipeline";
#elif UNITY_PIPELINE_HDRP
            return "High Definition Render Pipeline";
#else
            return "Built-in Render Pipeline";
#endif
        }

        #endregion

        #region Effect Management

        /// <summary>
        /// Applies all settings from the current profile to the effect material.
        /// </summary>
        public void ApplyProfile()
        {
            if (!_isInitialized || profile == null || _effectMaterial == null) return;

            try
            {
                // Check if mesh modification is required before applying properties.
                bool previousMeshState = _requiresMeshModification;
                _requiresMeshModification = ShouldModifyMesh();

                ApplyProfileToMaterial(profile, _effectMaterial);
                UpdateRectSize();
                
                ApplyRenderQueue(); // AFEGIT PER APLICAR LA CUA DE RENDER

                // Mark the graphic as dirty to ensure changes are rendered.
                if (_targetGraphic != null && (_hasChanges || previousMeshState != _requiresMeshModification))
                {
                    _targetGraphic.SetAllDirty();
                }

                // Update the cache with the new profile values.
                CacheProfileValues();
                _hasChanges = false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error applying profile: {e.Message}", this);
            }
        }

        /// <summary>
        /// Checks for changes in the profile and applies them if needed.
        /// </summary>
        public void UpdateEffect()
        {
            if (!_isInitialized) return;
            CheckForProfileChanges();
            if (_hasChanges) ApplyProfile();
        }

        /// <summary>
        /// Forces an immediate update of the effect, bypassing the auto-update check.
        /// </summary>
        public void ForceUpdate()
        {
            if (profile == null) return;
            if (!_isInitialized) Initialize();
            if (_isInitialized)
            {
                _hasChanges = true;
                ApplyProfile();
            }
        }

        /// <summary>
        /// Detects if the RectTransform's size has changed since the last update.
        /// </summary>
        private void CheckRectSizeChange()
        {
            if (_rectTransform != null)
            {
                if (Vector2.Distance(_rectTransform.rect.size, _lastRectSize) > 0.1f)
                {
                    _lastRectSize = _rectTransform.rect.size;
                    _hasChanges = true;
                }
            }
        }

        /// <summary>
        /// Sends the RectTransform's size to the shader. This is crucial for effects
        /// that depend on the element's dimensions (e.g., percentage-based units).
        /// Enhanced to trigger re-application of adaptive anti-aliasing.
        /// </summary>
        private void UpdateRectSize()
        {
            if (_effectMaterial == null || _rectTransform == null) return;
            Vector2 size = _rectTransform.rect.size;
            _effectMaterial.SetVector(_RectSizeId, new Vector4(size.x, size.y, 0, 0));
            
            // Anti-aliasing fix per a vores sempre definides
            _effectMaterial.SetFloat(_AAId, 0.5f);
        }

        #endregion

        #region Material Application

        /// <summary>
        /// Converts a value from a percentage unit to pixels based on the RectTransform's size.
        /// </summary>
        private float ConvertValueToPixels(float value, UIEffectProfile.Unit unit, Vector2 rectSize, bool useMinDimension)
        {
            if (unit == UIEffectProfile.Unit.Pixels) return value;
            if (rectSize.x <= 0 || rectSize.y <= 0) return value; // Avoid division by zero

            // For percentage, calculate based on the smaller or larger dimension.
            float referenceDim = useMinDimension ? Mathf.Min(rectSize.x, rectSize.y) : Mathf.Max(rectSize.x, rectSize.y);
            return (value / 100f) * (referenceDim * 0.5f);
        }

        /// <summary>
        /// Converts a Vector2 from a percentage unit to pixels.
        /// </summary>
        private Vector2 ConvertVectorToPixels(Vector2 value, UIEffectProfile.Unit unit, Vector2 rectSize)
        {
            if (unit == UIEffectProfile.Unit.Pixels) return value;
            if (rectSize.x <= 0 || rectSize.y <= 0) return value;

            // Convert each component of the vector based on the corresponding axis size.
            return new Vector2(
                (value.x / 100f) * rectSize.x,
                (value.y / 100f) * rectSize.y
            );
        }
        
        /// <summary>
        /// Aplica el render queue correcte segons el tipus de blur.
        /// Background Blur needs to render *after* other UI elements to composite correctly.
        /// </summary>
        private void ApplyRenderQueue()
        {
            if (_effectMaterial == null || profile == null) return;

            // Queue base per Transparent UI
            int baseQueue = 3000; // Transparent = 3000

            if (profile.enableBlur && profile.blurParams.blurType == UIEffectProfile.BlurParams.BlurType.Background)
            {
                // CRÍTIC: Renderitzar DESPRÉS d'altres UI elements
                int offset = profile.blurParams.queueOffset;
                _effectMaterial.renderQueue = baseQueue + offset;

                if (showDebugInfo)
                {
                    Debug.Log($"Render Queue ajustada a {_effectMaterial.renderQueue} (base {baseQueue} + offset {offset})");
                }
            }
            else
            {
                // Queue normal
                _effectMaterial.renderQueue = baseQueue;
            }
        }


        /// <summary>
        /// Applies all effect categories from the profile to the material.
        /// </summary>
        private void ApplyProfileToMaterial(UIEffectProfile prof, Material mat)
        {
            ApplyBasicProperties(prof, mat);
            ApplyBlurProperties(prof, mat);
            ApplyShadowProperties(prof, mat);
            ApplyGradientProperties(prof, mat);
            ApplyTextureProperties(prof, mat);
        }

        /// <summary>
        /// Applies basic properties like shape, corners, border, and fill color.
        /// </summary>
        private void ApplyBasicProperties(UIEffectProfile prof, Material mat)
        {
            Vector2 rectSize = _rectTransform != null ? _rectTransform.rect.size : Vector2.one * 100;

            mat.SetFloat(_ShapeTypeId, (float)prof.shapeType);
            mat.SetVector(_SpriteUvsId, GetSpriteUVs());

            Vector2[] vertices = prof.GetShapeVertices();

            if (vertices != null && vertices.Length > 0)
            {
                mat.SetFloat(_VertexCountId, vertices.Length);

                Vector4 vertices1 = Vector4.zero;
                Vector4 vertices2 = Vector4.zero;

                for (int i = 0; i < Mathf.Min(vertices.Length, 8); i++)
                {
                    Vector2 vertex = vertices[i];
                    if (i < 2) 
                    {
                        if (i == 0) { vertices1.x = vertex.x; vertices1.y = vertex.y; }
                        else { vertices1.z = vertex.x; vertices1.w = vertex.y; }
                    }
                    else if (i < 4) 
                    {
                        if (i == 2) { vertices2.x = vertex.x; vertices2.y = vertex.y; }
                        else { vertices2.z = vertex.x; vertices2.w = vertex.y; }
                    }
                }

                mat.SetVector(_ShapeVerticesId, vertices1);
                mat.SetVector(_ShapeVerticesExtId, vertices2);
            }
            else
            {
                mat.SetFloat(_VertexCountId, 4f);
                mat.SetVector(_ShapeVerticesId, Vector4.zero);
                mat.SetVector(_ShapeVerticesExtId, Vector4.zero);
            }

            Vector4 cornerRadiiRaw = prof.GetCornerRadii();
            Vector4 cornerRadiiPx = new Vector4(
                ConvertValueToPixels(cornerRadiiRaw.x, prof.cornerRadiusUnit, rectSize, true),
                ConvertValueToPixels(cornerRadiiRaw.y, prof.cornerRadiusUnit, rectSize, true),
                ConvertValueToPixels(cornerRadiiRaw.z, prof.cornerRadiusUnit, rectSize, true),
                ConvertValueToPixels(cornerRadiiRaw.w, prof.cornerRadiusUnit, rectSize, true)
            );
            mat.SetVector(_CornerRadiiId, cornerRadiiPx);

            // ================================
            // CORRECCIÓ CRÍTICA: Border Width
            // ================================
            float borderWidthPx;
            if (prof.borderWidthUnit == UIEffectProfile.Unit.Percent)
            {
                // NOVA LÒGICA LINEAL:
                // 0%   → 0 píxels (sense border)
                // 50%  → meitat de la dimensió més petita (arriba al centre)
                // 100% → dimensió completa més petita (omple TOTA la forma)
                
                float referenceDim = Mathf.Min(rectSize.x, rectSize.y);
                
                // Ara el 100% = referenceDim sencera, no la meitat!
                borderWidthPx = (prof.borderWidth / 100f) * referenceDim;
                
                // Clamp per seguretat: mai pot superar la dimensió de referència
                borderWidthPx = Mathf.Min(borderWidthPx, referenceDim);
            }
            else
            {
                // Mode píxels: directe
                borderWidthPx = prof.borderWidth;
            }
            
            mat.SetFloat(_BorderWidthId, borderWidthPx);
            // ================================

            mat.SetVector(_CornerOffsetsId, prof.GetCornerOffsets());
            mat.SetFloat(_UseIndividualCornersId, prof.useIndividualCorners ? 1f : 0f);
            mat.SetFloat(_UseIndividualOffsetsId, prof.useIndividualOffsets ? 1f : 0f);
            mat.SetFloat(_GlobalCornerOffsetId, prof.globalCornerOffset);
            mat.SetColor(_BorderColorId, prof.borderColor);
            mat.SetColor(_BorderColorBId, prof.borderColorB);
            mat.SetFloat(_UseBorderGradientId, prof.useBorderGradient ? 1f : 0f);
            mat.SetFloat(_BorderGradientTypeId, (float)prof.borderGradientType);
            mat.SetFloat(_BorderGradientAngleId, prof.borderGradientAngle * Mathf.Deg2Rad);
            mat.SetVector(_BorderGradientRadialCenterId, new Vector4(prof.borderGradientRadialCenter.x, prof.borderGradientRadialCenter.y, 0, 0));
            mat.SetFloat(_BorderGradientRadialScaleId, prof.borderGradientRadialScale);
            mat.SetFloat(_BorderGradientAngularRotationId, prof.borderGradientAngularRotation * Mathf.Deg2Rad);
            mat.SetColor(_ColorId, prof.fillColor);

            // [AFEGIT] Enviar dades de progrés al material
            mat.SetFloat(_UseProgressBorderId, prof.useProgressBorder ? 1f : 0f);
            mat.SetFloat(_ProgressValueId, Mathf.Clamp01(prof.progressValue));
            mat.SetFloat(_ProgressStartAngleId, prof.progressStartAngle);
            mat.SetFloat(_ProgressDirectionId, (float)prof.progressDirection);
            // MODIFICACIÓ B) Afegir al final del bloc de progrés
            mat.SetColor(_ProgressColorStartId, prof.progressColorStart);
            mat.SetColor(_ProgressColorEndId, prof.progressColorEnd);
            mat.SetFloat(_UseProgressColorGradientId, prof.useProgressColorGradient ? 1f : 0f);
        }

        /// <summary>
        /// Applies blur effect properties to the material.
        /// </summary>
        private void ApplyBlurProperties(UIEffectProfile prof, Material mat)
        {
            bool enable = prof.enableBlur && prof.blurParams != null;
            mat.SetFloat(_EnableBlurId, enable ? 1f : 0f);

            if (!enable) return;

            // NEW LINE: Set the blur type (Internal/Background)
            mat.SetFloat(_BlurTypeId, (float)prof.blurParams.blurType); // ADDED

            float radius = prof.blurParams.radius;
            // Reduce radius slightly if optimization is enabled.
            if (optimizeBlur) radius *= 0.75f;
            mat.SetFloat(_BlurRadiusId, radius);
            mat.SetFloat(_BlurIterationsId, prof.blurParams.iterations);
            mat.SetFloat(_BlurDownsampleId, prof.blurParams.downsample);
        }

        /// <summary>
        /// Applies shadow effect properties to the material.
        /// </summary>
        private void ApplyShadowProperties(UIEffectProfile prof, Material mat)
        {
            bool previousState = _requiresMeshModification;
            bool enable = prof.enableShadow && prof.shadowParams != null;
            _requiresMeshModification = enable;
            mat.SetFloat(_EnableShadowId, enable ? 1f : 0f);

            if (!enable)
            {
                // If disabled, reset shader properties to avoid artifacts.
                mat.SetColor(_ShadowColorId, Color.clear);
                mat.SetVector(_ShadowOffsetId, Vector4.zero);
                mat.SetFloat(_ShadowBlurId, 0f);
                mat.SetFloat(_ShadowOpacityId, 0f);
            }
            else
            {
                Vector2 rectSize = _rectTransform != null ? _rectTransform.rect.size : Vector2.one * 100;
                Color shadowColor = prof.shadowParams.color;
                float opacity = prof.shadowParams.opacity;

                // Convert shadow parameters to pixel units.
                Vector2 offsetPx = ConvertVectorToPixels(prof.shadowParams.offset, prof.shadowUnit, rectSize);
                float blurPx = ConvertValueToPixels(prof.shadowParams.blur, prof.shadowUnit, rectSize, true);

                // Apply performance optimizations if enabled.
                if (optimizeShadow)
                {
                    blurPx *= 0.8f;
                    opacity *= 0.9f;
                }

                // Ensure blur is at least a minimal value to avoid artifacts.
                blurPx = Mathf.Max(blurPx, 0.5f);

                mat.SetColor(_ShadowColorId, shadowColor);
                mat.SetVector(_ShadowOffsetId, new Vector4(offsetPx.x, offsetPx.y, 0, 0));
                mat.SetFloat(_ShadowBlurId, blurPx);
                mat.SetFloat(_ShadowOpacityId, opacity);

                if (showDebugInfo)
                {
                    Debug.Log($"Shadow applied: Color={shadowColor}, Offset={offsetPx}, Blur={blurPx}, Opacity={opacity}");
                }
            }
            
            // If the shadow state has changed, we must force a mesh rebuild.
            if (previousState != _requiresMeshModification && _targetGraphic != null)
            {
                _targetGraphic.SetVerticesDirty();
            }
        }

        /// <summary>
        /// Applies gradient effect properties to the material.
        /// </summary>
        private void ApplyGradientProperties(UIEffectProfile prof, Material mat)
        {
            bool enable = prof.enableGradient && prof.gradientParams != null;
            mat.SetFloat(_EnableGradientId, enable ? 1f : 0f);
            if (!enable)
            {
                // Reset gradient properties if disabled.
                mat.SetFloat(_GradientTypeId, 0f);
                mat.SetColor(_GradientColorAId, Color.white);
                mat.SetColor(_GradientColorBId, Color.white);
                mat.SetFloat(_GradientAngleId, 0f);
                // NOU: Reset nous paràmetres
                mat.SetVector(_GradientRadialCenterId, new Vector4(0.5f, 0.5f, 0f, 0f));
                mat.SetFloat(_GradientAngularRotationId, 0f);
                mat.SetFloat(_GradientRadialScaleId, 1f);
                return;
            }
            mat.SetFloat(_GradientTypeId, (float)prof.gradientParams.type);
            mat.SetColor(_GradientColorAId, prof.gradientParams.colorA);
            mat.SetColor(_GradientColorBId, prof.gradientParams.colorB);
            // Convert angle from degrees to radians for shader calculations.
            float angleRad = prof.gradientParams.angle * Mathf.Deg2Rad;
            mat.SetFloat(_GradientAngleId, angleRad);
        
            // NOU: Enviar nous paràmetres al shader
            mat.SetVector(_GradientRadialCenterId, new Vector4(
                prof.gradientParams.radialCenter.x, 
                prof.gradientParams.radialCenter.y, 
                0f, 0f
            ));
        
            float angularRotationRad = prof.gradientParams.angularRotation * Mathf.Deg2Rad;
            mat.SetFloat(_GradientAngularRotationId, angularRotationRad);
        
            mat.SetFloat(_GradientRadialScaleId, prof.gradientParams.radialScale);
            if (showDebugInfo)
            {
                Debug.Log($"Gradient applied: Enable={enable}, Type={prof.gradientParams.type}, " +
                          $"ColorA={prof.gradientParams.colorA}, ColorB={prof.gradientParams.colorB}, " +
                          $"Angle={prof.gradientParams.angle}°, RadialCenter={prof.gradientParams.radialCenter}, " +
                          $"AngularRotation={prof.gradientParams.angularRotation}°, RadialScale={prof.gradientParams.radialScale}");
            }
        }

        /// <summary>
        /// Applies texture overlay properties to the material.
        /// </summary>
        private void ApplyTextureProperties(UIEffectProfile prof, Material mat)
        {
            bool enable = prof.enableTexture && prof.overlayTexture != null && prof.textureParams != null;
            mat.SetFloat(_EnableTextureId, enable ? 1f : 0f);

            if (!enable)
            {
                // Reset texture properties if disabled.
                mat.SetTexture(_OverlayTextureId, null);
                mat.SetVector(_TextureTilingId, Vector4.one);
                mat.SetVector(_TextureOffsetId, Vector4.zero);
                mat.SetFloat(_TextureRotationId, 0f);
                mat.SetFloat(_TextureOpacityId, 1f);
                mat.SetFloat(_TextureBlendModeId, 0f);
                mat.SetFloat(_TextureUVModeId, 0f);
                mat.SetMatrix(_TextureMatrixId, Matrix4x4.identity);
                return;
            }

            mat.SetTexture(_OverlayTextureId, prof.overlayTexture);

            // Apply texture filtering mode.
            if (prof.overlayTexture.filterMode != prof.textureParams.filterMode)
            {
                prof.overlayTexture.filterMode = prof.textureParams.filterMode;
            }
            
            Vector2 rectSize = _rectTransform != null ? _rectTransform.rect.size : Vector2.one * 100;
            Vector2 adjustedTiling = prof.textureParams.GetAdjustedTiling(rectSize);

            // Set all texture parameters on the material.
            mat.SetVector(_TextureTilingId, new Vector4(adjustedTiling.x, adjustedTiling.y, 0, 0));
            mat.SetVector(_TextureOffsetId, new Vector4(prof.textureParams.offset.x, prof.textureParams.offset.y, 0, 0));
            mat.SetFloat(_TextureRotationId, prof.textureParams.rotation * Mathf.Deg2Rad);
            mat.SetFloat(_TextureOpacityId, prof.textureParams.opacity);
            mat.SetFloat(_TextureBlendModeId, (float)prof.textureParams.blendMode);
            mat.SetFloat(_TextureUVModeId, (float)prof.textureParams.uvMode);
            
            Matrix4x4 textureMatrix = prof.textureParams.GetTextureMatrix();
            mat.SetMatrix(_TextureMatrixId, textureMatrix);

            if (showDebugInfo)
            {
                Debug.Log($"Texture applied: {prof.overlayTexture.name}, " +
                          $"Tiling={adjustedTiling}, Offset={prof.textureParams.offset}, " +
                          $"Rotation={prof.textureParams.rotation}°, Opacity={prof.textureParams.opacity}, " +
                          $"Blend={prof.textureParams.blendMode}, UV={prof.textureParams.uvMode}");
            }
        }

        #endregion

        #region Change Detection

        /// <summary>
        /// Checks if any property in the profile has changed since the last update
        /// by comparing against the cached values.
        /// </summary>
        private void CheckForProfileChanges()
        {
            if (profile == null)
            {
                _hasChanges = false;
                return;
            }
            if (_profileCache.HasChanged(profile))
            {
                _hasChanges = true;
            }
        }

        /// <summary>
        /// Stores the current state of the profile in the cache for future comparisons.
        /// </summary>
        private void CacheProfileValues()
        {
            if (profile == null) return;
            _profileCache.Update(profile);
        }

        #endregion

        #region Public API
        
        /// <summary>
        /// Configures this component for a procedural shape.
        /// </summary>
        /// <param name="shapeType">Type of procedural shape (e.g., Triangle, Star).</param>
        /// <param name="vertices">Normalized vertices (0-1 space) for custom polygons.</param>
        /// <param name="starPoints">Number of points for star shapes.</param>
        /// <param name="starInnerRatio">Inner radius ratio for star shapes.</param>
        public void ConfigureProceduralShape(UIEffectProfile.ShapeType shapeType, Vector2[] vertices = null, int starPoints = 5, float starInnerRatio = 0.5f)
        {
            if (profile == null)
            {
                // Create a new profile instance if none exists.
                profile = ScriptableObject.CreateInstance<UIEffectProfile>();
            }

            profile.SetShapeData(shapeType, vertices, starPoints, starInnerRatio);
            
            // Reset corner radius for sharp procedural shapes.
            profile.globalCornerRadius = 0f;
            profile.useIndividualCorners = false;

            ForceUpdate();

            if (showDebugInfo)
            {
                Debug.Log($"Configured procedural shape: {shapeType} with {(vertices?.Length ?? 0)} vertices", this);
            }
        }

        /// <summary>
        /// Sets a new profile and forces an update.
        /// </summary>
        public void SetProfile(UIEffectProfile newProfile)
        {
            profile = newProfile;
            ForceUpdate();
        }

        /// <summary>
        /// Enables or disables the blur effect at runtime.
        /// </summary>
        public void SetBlurEnabled(bool enabled)
        {
            if (profile != null)
            {
                profile.enableBlur = enabled;
                _hasChanges = true;
            }
        }

        /// <summary>
        /// Sets blur parameters at runtime. Use -1 to keep a value unchanged.
        /// </summary>
        public void SetBlurParameters(float radius, int iterations = -1, int downsample = -1)
        {
            if (profile?.blurParams != null)
            {
                profile.blurParams.radius = Mathf.Max(0f, radius);
                if (iterations >= 1) profile.blurParams.iterations = Mathf.Clamp(iterations, 1, 80);
                if (downsample >= 1) profile.blurParams.downsample = Mathf.Clamp(downsample, 1, 100);
                _hasChanges = true;
            }
        }

        /// <summary>
        /// Enables or disables the shadow effect at runtime.
        /// </summary>
        public void SetShadowEnabled(bool enabled)
        {
            if (profile != null)
            {
                bool previousState = profile.enableShadow;
                profile.enableShadow = enabled;
                
                // If the state changed, force a mesh rebuild.
                if (previousState != enabled)
                {
                    _hasChanges = true;
                    if (_targetGraphic != null)
                    {
                        _targetGraphic.SetVerticesDirty();
                    }
                }
            }
        }

        /// <summary>
        /// Sets shadow parameters at runtime. Use -1f to keep blur or opacity unchanged.
        /// </summary>
        public void SetShadowParameters(Color color, Vector2 offset, float blur = -1f, float opacity = -1f)
        {
            if (profile?.shadowParams != null)
            {
                profile.shadowParams.color = color;
                profile.shadowParams.offset = offset;
                if (blur >= 0f) profile.shadowParams.blur = blur;
                if (opacity >= 0f) profile.shadowParams.opacity = Mathf.Clamp01(opacity);
                _hasChanges = true;
            }
        }

        /// <summary>
        /// Sets individual corner offsets (0-1 range) at runtime.
        /// </summary>
        public void SetCornerOffsets(float topLeft, float topRight, float bottomLeft, float bottomRight)
        {
            if (profile != null)
            {
                profile.useIndividualOffsets = true;
                profile.cornerOffsetTopLeft = Mathf.Clamp01(topLeft);
                profile.cornerOffsetTopRight = Mathf.Clamp01(topRight);
                profile.cornerOffsetBottomLeft = Mathf.Clamp01(bottomLeft);
                profile.cornerOffsetBottomRight = Mathf.Clamp01(bottomRight);
                _hasChanges = true;
            }
        }

        /// <summary>
        /// Sets a global corner offset (0-1 range) for all corners at runtime.
        /// </summary>
        public void SetGlobalCornerOffset(float offset)
        {
            if (profile != null)
            {
                profile.useIndividualOffsets = false;
                profile.globalCornerOffset = Mathf.Clamp01(offset);
                _hasChanges = true;
            }
        }

        /// <summary>
        /// Enables or disables the texture overlay effect at runtime.
        /// </summary>
        public void SetTextureEnabled(bool enabled)
        {
            if (profile != null)
            {
                profile.enableTexture = enabled;
                _hasChanges = true;
            }
        }

        /// <summary>
        /// Sets the texture for the overlay effect at runtime.
        /// </summary>
        public void SetTexture(Texture2D texture)
        {
            if (profile != null)
            {
                profile.overlayTexture = texture;
                _hasChanges = true;
            }
        }

        /// <summary>
        /// Sets texture parameters at runtime.
        /// </summary>
        public void SetTextureParameters(Vector2 tiling, Vector2 offset, float rotation = 0f, float opacity = 1f)
        {
            if (profile?.textureParams != null)
            {
                profile.textureParams.tiling = tiling;
                profile.textureParams.offset = offset;
                profile.textureParams.rotation = rotation % 360f;
                profile.textureParams.opacity = Mathf.Clamp01(opacity);
                _hasChanges = true;
            }
        }

        /// <summary>
        /// Sets the texture blend mode at runtime.
        /// </summary>
        public void SetTextureBlendMode(UIEffectProfile.TextureParams.BlendMode blendMode)
        {
            if (profile?.textureParams != null)
            {
                profile.textureParams.blendMode = blendMode;
                _hasChanges = true;
            }
        }

        /// <summary>
        /// Sets the texture UV mode at runtime.
        /// </summary>
        public void SetTextureUVMode(UIEffectProfile.TextureParams.UVMode uvMode)
        {
            if (profile?.textureParams != null)
            {
                profile.textureParams.uvMode = uvMode;
                _hasChanges = true;
            }
        }

        #endregion

        #region Profile Sharing Detection and Management

        /// <summary>
        /// Checks if this component is sharing its profile with other components in the scene.
        /// </summary>
        public bool IsUsingSharedProfile()
        {
            if (profile == null) return false;
            
            UIEffectComponent[] allComponents = FindObjectsByType<UIEffectComponent>(FindObjectsSortMode.None);
            int usageCount = 0;
            
            foreach (var component in allComponents)
            {
                // Check if another component uses the exact same profile asset instance.
                if (component != this && component.profile != null && ReferenceEquals(component.profile, profile))
                {
                    usageCount++;
                }
            }
            
            return usageCount > 0;
        }

        /// <summary>
        /// Creates an independent copy of the current profile for this component.
        /// This prevents changes from affecting other components that shared the original profile.
        /// </summary>
        public void MakeProfileIndependent()
        {
            if (profile == null) return;
            
            var independentProfile = profile.Clone();
            independentProfile.name = $"IndependentProfile_{gameObject.name}";
            
            #if UNITY_EDITOR
            // In the Unity Editor, create a new asset file for the independent profile.
            string directory = "Assets/UIEffectsPro/Presets";
            if (!UnityEditor.AssetDatabase.IsValidFolder(directory))
            {
                if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/UIEffectsPro"))
                {
                    UnityEditor.AssetDatabase.CreateFolder("Assets", "UIEffectsPro");
                }
                UnityEditor.AssetDatabase.CreateFolder("Assets/UIEffectsPro", "Presets");
            }
            
            // Unity 2020.3 compatible string manipulation instead of using System.Range/Index
            string assetPath = $"{directory}/IndependentProfile_{gameObject.name}_{System.Guid.NewGuid().ToString("N").Substring(0, 8)}.asset";
            UnityEditor.AssetDatabase.CreateAsset(independentProfile, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
            
            SetProfile(independentProfile);
            Debug.Log($"Created independent profile for {gameObject.name}");
        }

        /// <summary>
        /// Gets a string with information about the profile usage (shared or independent).
        /// </summary>
        public string GetProfileUsageInfo()
        {
            if (profile == null) return "No profile assigned";
            
            UIEffectComponent[] allComponents = FindObjectsByType<UIEffectComponent>(FindObjectsSortMode.None);
            var sharedWith = new System.Collections.Generic.List<string>();
            
            foreach (var component in allComponents)
            {
                if (component != this && component.profile != null && ReferenceEquals(component.profile, profile))
                {
                    sharedWith.Add(component.gameObject.name);
                }
            }
            
            if (sharedWith.Count == 0)
            {
                return $"Using independent profile: {profile.name}";
            }
            else
            {
                return $"Sharing profile '{profile.name}' with: {string.Join(", ", sharedWith.ToArray())}";
            }
        }

        #endregion

        #region Performance Monitoring

        /// <summary>
        /// Calculates a performance score (0-10) based on the cost of enabled effects.
        /// Higher scores indicate better performance.
        /// </summary>
        public float GetPerformanceScore()
        {
            if (profile == null) return 10f;
            float score = 10f;
            // Blur is expensive, so it heavily impacts the score.
            if (profile.enableBlur && profile.blurParams != null)
            {
                score -= profile.blurParams.radius * profile.blurParams.iterations / Mathf.Max(1f, profile.blurParams.downsample) * 0.5f;
            }
            // Shadow blur also has a performance cost.
            if (profile.enableShadow && profile.shadowParams != null)
            {
                score -= profile.shadowParams.blur * 0.2f;
            }
            return Mathf.Clamp(score, 0f, 10f);
        }

        /// <summary>
        /// Provides a human-readable description of the performance impact.
        /// </summary>
        public string GetPerformanceDescription()
        {
            float score = GetPerformanceScore();
            if (score >= 8f) return "Excellent Performance";
            if (score >= 6f) return "Good Performance";
            if (score >= 4f) return "Moderate Performance";
            if (score >= 2f) return "High Performance Impact";
            return "Very High Performance Impact";
        }

        /// <summary>
        /// A simple check to see if the current settings are generally suitable for mobile devices.
        /// </summary>
        public bool IsMobileFriendly() => GetPerformanceScore() >= 5f;

        #endregion

        #region Validation and Debug

        /// <summary>
        /// Logs detailed debug information about the component's state to the console.
        /// </summary>
        public void LogDebugInfo()
        {
            if (!showDebugInfo) return;
            var issues = ValidateConfiguration();
            Debug.Log($"[UIEffectComponent] Debug Info for {gameObject.name}:", this);
            Debug.Log($"- Profile: {(profile != null ? profile.name : "None")}");
            Debug.Log($"- Initialized: {_isInitialized}");
            Debug.Log($"- Material: {(_effectMaterial != null ? _effectMaterial.name : "None")}");
            Debug.Log($"- Shader: {(_effectMaterial != null ? _effectMaterial.shader.name : "None")}");
            Debug.Log($"- Render Queue: {(_effectMaterial != null ? _effectMaterial.renderQueue.ToString() : "N/A")}"); // Added Render Queue Info
            Debug.Log($"- Render Pipeline: {GetCurrentPipelineName()}");
            Debug.Log($"- Requires Mesh Modification: {_requiresMeshModification}");
            Debug.Log($"- Performance: {GetPerformanceDescription()}");
            if (issues.Count > 0)
            {
                Debug.LogWarning($"- Issues Found: {string.Join(", ", issues.ToArray())}");
            }
        }

        /// <summary>
        /// Logs the current shadow values from both the profile and the material for debugging.
        /// </summary>
        public void DebugShadowValues()
        {
            if (profile == null || _effectMaterial == null)
            {
                Debug.LogWarning("No profile or material to debug!");
                return;
            }

            Debug.Log("=== SHADOW DEBUG INFO ===");
            Debug.Log($"Enable Shadow (Profile): {profile.enableShadow}");
            if (profile.shadowParams != null)
            {
                Debug.Log($"Shadow Color (Profile): {profile.shadowParams.color}");
                Debug.Log($"Shadow Offset (Profile): {profile.shadowParams.offset}");
                Debug.Log($"Shadow Blur (Profile): {profile.shadowParams.blur}");
                Debug.Log($"Shadow Opacity (Profile): {profile.shadowParams.opacity}");
            }

            Debug.Log("--- MATERIAL PROPERTIES ---");
            Debug.Log($"_EnableShadow (Material): {_effectMaterial.GetFloat(_EnableShadowId)}");
            Debug.Log($"_ShadowColor (Material): {_effectMaterial.GetColor(_ShadowColorId)}");
            Debug.Log($"_ShadowOffset (Material): {_effectMaterial.GetVector(_ShadowOffsetId)}");
            Debug.Log($"_ShadowBlur (Material): {_effectMaterial.GetFloat(_ShadowBlurId)}");
            Debug.Log($"_ShadowOpacity (Material): {_effectMaterial.GetFloat(_ShadowOpacityId)}");
            Debug.Log($"Requires Mesh Modification: {_requiresMeshModification}");
        }

        /// <summary>
        /// Validates the component's configuration and returns a list of potential issues.
        /// </summary>
        public List<string> ValidateConfiguration()
        {
            var issues = new List<string>();
            if (profile == null) issues.Add("No profile assigned");
            if (_targetGraphic == null) issues.Add("Image or RawImage component not found");
            if (_effectMaterial == null) issues.Add("Effect material not created");
            if (!IsMobileFriendly()) issues.Add($"Performance warning: {GetPerformanceDescription()}");
            return issues;
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Restores the original material on the target graphic.
        /// </summary>
        private void RestoreOriginalMaterial()
        {
            if (_targetGraphic != null && _originalMaterial != null)
            {
                _targetGraphic.material = _originalMaterial;
            }
        }

        /// <summary>
        /// Destroys the instantiated effect material to prevent memory leaks.
        /// </summary>
        private void CleanupMaterials()
        {
            if (_effectMaterial != null)
            {
                if (Application.isPlaying) Destroy(_effectMaterial);
                else DestroyImmediate(_effectMaterial);
                _effectMaterial = null;
            }
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        /// <summary>
        /// Called in the editor when a script is loaded or a value is changed in the Inspector.
        /// Ensures the effect is updated in Edit Mode.
        /// </summary>
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                _hasChanges = true;
                return;
            }
            // Delay the call to avoid issues with Unity's serialization and update lifecycle.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null || gameObject == null) return;
                if (_targetGraphic == null) CacheComponents();
                if (_targetGraphic == null) return;
                
                // Re-initialize if the material or shader is missing or incorrect.
                string expectedShader = GetAvailableShaderName();
                if (_effectMaterial == null || _targetGraphic.material == _originalMaterial || (_effectMaterial.shader.name != expectedShader))
                {
                    Initialize();
                }
                if (_isInitialized) ForceUpdate();
            };
        }

        /// <summary>
        /// Resets the component's fields to their default values.
        /// </summary>
        private void Reset()
        {
            autoUpdate = true;
            forceUpdateEveryFrame = false;
            optimizeBlur = true;
            optimizeShadow = false;
        }

        // --- Context Menu Items for the Inspector ---

        [UnityEditor.MenuItem("CONTEXT/UIEffectComponent/Force Update")]
        private static void ForceUpdateContext(UnityEditor.MenuCommand command)
        {
            UIEffectComponent component = command.context as UIEffectComponent;
            if (component != null)
            {
                component.ForceUpdate();
                UnityEditor.EditorUtility.SetDirty(component);
            }
        }

        [UnityEditor.MenuItem("CONTEXT/UIEffectComponent/Reset to Defaults")]
        private static void ResetToDefaultsContext(UnityEditor.MenuCommand command)
        {
            UIEffectComponent component = command.context as UIEffectComponent;
            if (component != null && component.profile != null)
            {
                UnityEditor.Undo.RecordObject(component.profile, "Reset UI Effect Profile");
                component.profile.ResetToDefaults();
                component.ForceUpdate();
                UnityEditor.EditorUtility.SetDirty(component);
                UnityEditor.EditorUtility.SetDirty(component.profile);
            }
        }

        [UnityEditor.MenuItem("CONTEXT/UIEffectComponent/Show Debug Info")]
        private static void LogDebugInfoContext(UnityEditor.MenuCommand command)
        {
            UIEffectComponent component = command.context as UIEffectComponent;
            if (component != null)
            {
                component.LogDebugInfo();
            }
        }

        [UnityEditor.MenuItem("CONTEXT/UIEffectComponent/Make Profile Independent")]
        private static void MakeProfileIndependentContext(UnityEditor.MenuCommand command)
        {
            UIEffectComponent component = command.context as UIEffectComponent;
            if (component != null)
            {
                if (component.IsUsingSharedProfile())
                {
                    UnityEditor.Undo.RecordObject(component, "Make Profile Independent");
                    component.MakeProfileIndependent();
                    UnityEditor.EditorUtility.SetDirty(component);
                }
                else
                {
                    UnityEditor.EditorUtility.DisplayDialog("Profile Already Independent", 
                        "This component is already using an independent profile.", "OK");
                }
            }
        }

        [UnityEditor.MenuItem("CONTEXT/UIEffectComponent/Show Profile Usage")]
        private static void ShowProfileUsageContext(UnityEditor.MenuCommand command)
        {
            UIEffectComponent component = command.context as UIEffectComponent;
            if (component != null)
            {
                string info = component.GetProfileUsageInfo();
                UnityEditor.EditorUtility.DisplayDialog("Profile Usage Information", info, "OK");
            }
        }

        // Validation function to enable/disable the "Make Profile Independent" menu item.
        [UnityEditor.MenuItem("CONTEXT/UIEffectComponent/Make Profile Independent", true)]
        private static bool ValidateMakeProfileIndependentContext(UnityEditor.MenuCommand command)
        {
            UIEffectComponent component = command.context as UIEffectComponent;
            return component != null && component.IsUsingSharedProfile();
        }
#endif

        #endregion
    }

    #region Helper Classes

    /// <summary>
    /// A helper class to cache the state of a UIEffectProfile.
    /// This is used to efficiently detect when the profile has been modified,
    /// avoiding unnecessary material updates every frame.
    /// </summary>
    [System.Serializable]
    internal class ProfileCache
    {
        // Cache fields for most profile properties.
        public Vector4 cornerRadii;
        public Vector4 cornerOffsets;
        public Color borderColor;
        public Color borderColorB;
        public bool useBorderGradient;
        public int borderGradientType;
        public float borderGradientAngle;
        public Vector2 borderGradientRadialCenter;
        public float borderGradientRadialScale;
        public float borderGradientAngularRotation;
        public Color fillColor;
        public float borderWidth;
        public bool useIndividualCorners;
        public bool useIndividualOffsets;
        public float globalCornerOffset;
        public bool enableBlur;
        public int blurType; // ADDED
        public int blurQueueOffset; // ADDED TO CACHE
        public float blurRadius;
        public int blurIterations;
        public int blurDownsample;
        public bool enableShadow;
        public Color shadowColor;
        public Vector2 shadowOffset;
        public float shadowBlur;
        public float shadowOpacity;
        public UIEffectProfile.Unit cornerRadiusUnit;
        public UIEffectProfile.Unit borderWidthUnit;
        public UIEffectProfile.Unit shadowUnit;
        public bool enableGradient;
        public int gradientType;
        public Color gradientColorA;
        public Color gradientColorB;
        public float gradientAngle;
        public Vector2 gradientRadialCenter;
        public float gradientAngularRotation;
        public float gradientRadialScale;
        public bool enableTexture;
        public Texture2D overlayTexture;
        public Vector2 textureTiling;
        public Vector2 textureOffset;
        public float textureRotation;
        public float textureOpacity;
        public int textureBlendMode;
        public int textureUVMode;
        public int textureAspectMode;

        // [AFEGIT] Camps de cache per al progrés
        public bool useProgressBorder;
        public float progressValue;
        public float progressStartAngle;
        public UIEffectProfile.ProgressDirection progressDirection;
        // MODIFICACIÓ C) Afegir al final dels camps de progrés
        public Color progressColorStart;
        public Color progressColorEnd;
        public bool useProgressColorGradient;

        /// <summary>
        /// Compares the cached values with the current values of a profile
        /// to determine if anything has changed.
        /// </summary>
        public bool HasChanged(UIEffectProfile p)
        {
            if (p == null) return false;

            // Check basic properties
            if (cornerRadii != p.GetCornerRadii() || cornerRadiusUnit != p.cornerRadiusUnit ||
                cornerOffsets != p.GetCornerOffsets() || borderColor != p.borderColor ||
                borderColorB != p.borderColorB || useBorderGradient != p.useBorderGradient ||
                borderGradientType != (int)p.borderGradientType ||
                !Mathf.Approximately(borderGradientAngle, p.borderGradientAngle) ||
                borderGradientRadialCenter != p.borderGradientRadialCenter ||
                !Mathf.Approximately(borderGradientRadialScale, p.borderGradientRadialScale) ||
                !Mathf.Approximately(borderGradientAngularRotation, p.borderGradientAngularRotation) ||
                fillColor != p.fillColor || !Mathf.Approximately(borderWidth, p.borderWidth) ||
                useIndividualCorners != p.useIndividualCorners || useIndividualOffsets != p.useIndividualOffsets ||
                !Mathf.Approximately(globalCornerOffset, p.globalCornerOffset) || borderWidthUnit != p.borderWidthUnit || 
                shadowUnit != p.shadowUnit) return true;

            // Check blur properties
            if (enableBlur != p.enableBlur) return true;
            if (p.enableBlur && p.blurParams != null && (
                blurType != (int)p.blurParams.blurType || // MODIFIED
                blurQueueOffset != p.blurParams.queueOffset || // ADDED
                !Mathf.Approximately(blurRadius, p.blurParams.radius) ||
                blurIterations != p.blurParams.iterations || blurDownsample != p.blurParams.downsample)) return true;

            // Check shadow properties
            if (enableShadow != p.enableShadow) return true;
            if (p.enableShadow && p.shadowParams != null && (shadowColor != p.shadowParams.color ||
                shadowOffset != p.shadowParams.offset || !Mathf.Approximately(shadowBlur, p.shadowParams.blur) ||
                !Mathf.Approximately(shadowOpacity, p.shadowParams.opacity))) return true;

            // Check gradient properties
            if (enableGradient != p.enableGradient) return true;
            if (p.enableGradient && p.gradientParams != null && (
                gradientType != (int)p.gradientParams.type ||
                gradientColorA != p.gradientParams.colorA ||
                gradientColorB != p.gradientParams.colorB ||
                !Mathf.Approximately(gradientAngle, p.gradientParams.angle) ||
                gradientRadialCenter != p.gradientParams.radialCenter ||
                !Mathf.Approximately(gradientAngularRotation, p.gradientParams.angularRotation) ||
                !Mathf.Approximately(gradientRadialScale, p.gradientParams.radialScale))) return true;
                
            // Check texture properties
            if (enableTexture != p.enableTexture) return true;
            if (p.enableTexture && p.textureParams != null)
            {
                if (!ReferenceEquals(overlayTexture, p.overlayTexture)) return true;
                if (textureTiling != p.textureParams.tiling) return true;
                if (textureOffset != p.textureParams.offset) return true;
                if (!Mathf.Approximately(textureRotation, p.textureParams.rotation)) return true;
                if (!Mathf.Approximately(textureOpacity, p.textureParams.opacity)) return true;
                if (textureBlendMode != (int)p.textureParams.blendMode) return true;
                if (textureUVMode != (int)p.textureParams.uvMode) return true;
                if (textureAspectMode != (int)p.textureParams.aspectMode) return true;
            }

            // [AFEGIT] Comprovació dels camps de progrés
            if (useProgressBorder != p.useProgressBorder) return true;
            if (p.useProgressBorder && (
                !Mathf.Approximately(progressValue, p.progressValue) ||
                !Mathf.Approximately(progressStartAngle, p.progressStartAngle) ||
                progressDirection != p.progressDirection ||
                // MODIFICACIÓ D) Afegir dins del bloc de comprovació de progrés
                progressColorStart != p.progressColorStart ||
                progressColorEnd != p.progressColorEnd ||
                useProgressColorGradient != p.useProgressColorGradient)) return true;

            return false;
        }

        /// <summary>
        /// Updates the cache with the current values from a profile.
        /// </summary>
        public void Update(UIEffectProfile p)
        {
            if (p == null) return;
            cornerRadii = p.GetCornerRadii();
            cornerOffsets = p.GetCornerOffsets();
            borderColor = p.borderColor;
            borderColorB = p.borderColorB;
            useBorderGradient = p.useBorderGradient;
            borderGradientType = (int)p.borderGradientType;
            borderGradientAngle = p.borderGradientAngle;
            borderGradientRadialCenter = p.borderGradientRadialCenter;
            borderGradientRadialScale = p.borderGradientRadialScale;
            borderGradientAngularRotation = p.borderGradientAngularRotation;
            fillColor = p.fillColor;
            borderWidth = p.borderWidth;
            useIndividualCorners = p.useIndividualCorners;
            useIndividualOffsets = p.useIndividualOffsets;
            globalCornerOffset = p.globalCornerOffset;
            cornerRadiusUnit = p.cornerRadiusUnit;
            borderWidthUnit = p.borderWidthUnit;
            shadowUnit = p.shadowUnit;

            enableBlur = p.enableBlur;
            if (p.blurParams != null)
            {
                blurType = (int)p.blurParams.blurType; // ADDED
                blurQueueOffset = p.blurParams.queueOffset; // ADDED
                blurRadius = p.blurParams.radius;
                blurIterations = p.blurParams.iterations;
                blurDownsample = p.blurParams.downsample;
            }

            enableShadow = p.enableShadow;
            if (p.shadowParams != null)
            {
                shadowColor = p.shadowParams.color;
                shadowOffset = p.shadowParams.offset;
                shadowBlur = p.shadowParams.blur;
                shadowOpacity = p.shadowParams.opacity;
            }

            enableGradient = p.enableGradient;
            if (p.gradientParams != null)
            {
                gradientType = (int)p.gradientParams.type;
                gradientColorA = p.gradientParams.colorA;
                gradientColorB = p.gradientParams.colorB;
                gradientAngle = p.gradientParams.angle;
                gradientRadialCenter = p.gradientParams.radialCenter;
                gradientAngularRotation = p.gradientParams.angularRotation;
                gradientRadialScale = p.gradientParams.radialScale;
            }
            
            enableTexture = p.enableTexture;
            overlayTexture = p.overlayTexture;
            if (p.textureParams != null)
            {
                textureTiling = p.textureParams.tiling;
                textureOffset = p.textureParams.offset;
                textureRotation = p.textureParams.rotation;
                textureOpacity = p.textureParams.opacity;
                textureBlendMode = (int)p.textureParams.blendMode;
                textureUVMode = (int)p.textureParams.uvMode;
                textureAspectMode = (int)p.textureParams.aspectMode;
            }

            // [AFEGIT] Actualització del cache de progrés
            useProgressBorder = p.useProgressBorder;
            progressValue = p.progressValue;
            progressStartAngle = p.progressStartAngle;
            progressDirection = p.progressDirection;
            // MODIFICACIÓ E) Afegir al final del bloc de progrés
            progressColorStart = p.progressColorStart;
            progressColorEnd = p.progressColorEnd;
            useProgressColorGradient = p.useProgressColorGradient;
        }
    }
    #endregion
}