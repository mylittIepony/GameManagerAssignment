// UIEffectsPro/Runtime/Scripts/Core/UIEffectProfile.cs
using UnityEngine;

namespace UIEffectsPro.Runtime
{
    /// <summary>
    /// A ScriptableObject that defines a reusable profile for UI effects.
    /// It stores all the settings for shape, corners, borders, fills, and advanced effects like blur and shadow.
    /// This allows for consistent styling across multiple UI elements.
    /// </summary>
    [CreateAssetMenu(fileName = "New UI Effect Profile", menuName = "UIEffects/UI Effect Profile", order = 1)]
    public class UIEffectProfile : ScriptableObject
    {
        /// <summary>
        /// Defines the measurement unit for properties like radius and width.
        /// </summary>
        public enum Unit { Pixels, Percent }

        /// <summary>
        /// Defines the base shape of the UI element.
        /// </summary>
        public enum ShapeType
        {
            Rectangle = 0,
            Triangle = 3,
            Square = 4,
            Pentagon = 5,
            Hexagon = 6,
            Star = 7,
            Circle = 8
        }

        // [AFEGIT] Enumeració per a la direcció del progrés
        public enum ProgressDirection { Clockwise, CounterClockwise }

        [Header("Shape Type")]
        [Tooltip("Type of shape for proper border calculation.")]
        public ShapeType shapeType = ShapeType.Rectangle;

        [Tooltip("Number of points for the Star shape.")]
        [Range(3, 20)]
        public int starPoints = 5;

        [Tooltip("Inner radius ratio for the Star shape, controlling how sharp the points are.")]
        [Range(0.1f, 1.0f)]
        public float starInnerRatio = 0.5f;

        [Tooltip("Custom vertices for complex shapes, defined in normalized (0-1) coordinates.")]
        public Vector2[] customVertices = new Vector2[0];

        [Header("Texture Settings")]
        [Tooltip("If enabled, an overlay texture will be applied to the UI element.")]
        public bool enableTexture = false;

        [Tooltip("The texture to overlay on the shape.")]
        public Texture2D overlayTexture;

        [Tooltip("Parameters for controlling the appearance of the overlay texture.")]
        public TextureParams textureParams = new TextureParams();

        /// <summary>
        /// Contains all parameters related to texture rendering on the UI element.
        /// </summary>
        [System.Serializable]
        public class TextureParams
        {
            /// <summary>
            /// Specifies how the texture's color is combined with the element's base or gradient color.
            /// </summary>
            public enum BlendMode
            {
                Multiply = 0,
                Add = 1,
                Subtract = 2,
                Overlay = 3,
                Screen = 4,
                Replace = 5
            }

            /// <summary>
            /// Defines the coordinate system used for texture mapping.
            /// </summary>
            public enum UVMode
            {
                Local = 0,    // UVs are relative to the shape's bounds.
                World = 1,    // UVs are based on the object's world position.
                Repeat = 2    // A repeating UV pattern is applied.
            }

            /// <summary>
            /// Controls how the texture is scaled to fit the UI element's aspect ratio.
            /// </summary>
            public enum AspectMode
            {
                Stretch = 0,  // Stretches the texture to fit the shape completely.
                FitWidth = 1, // Fits the texture's width and adjusts its height proportionally.
                FitHeight = 2, // Fits the texture's height and adjusts its width proportionally.
                Fill = 3      // Fills the entire shape, cropping the texture if necessary.
            }

            [Tooltip("Texture tiling (repeat factor).")]
            public Vector2 tiling = Vector2.one;

            [Tooltip("Texture offset.")]
            public Vector2 offset = Vector2.zero;

            [Tooltip("Texture rotation in degrees.")]
            [Range(0f, 360f)]
            public float rotation = 0f;

            [Tooltip("Texture opacity.")]
            [Range(0f, 1f)]
            public float opacity = 1f;

            [Tooltip("How the texture blends with the base color.")]
            public BlendMode blendMode = BlendMode.Multiply;

            [Tooltip("The UV coordinate system to use.")]
            public UVMode uvMode = UVMode.Local;

            [Tooltip("Texture filtering mode.")]
            public FilterMode filterMode = FilterMode.Bilinear;

            [Tooltip("How the texture's aspect ratio is handled.")]
            public AspectMode aspectMode = AspectMode.Stretch;

            /// <summary>
            /// Default constructor for TextureParams.
            /// </summary>
            public TextureParams()
            {
                tiling = Vector2.one;
                offset = Vector2.zero;
                rotation = 0f;
                opacity = 1f;
                blendMode = BlendMode.Multiply;
                uvMode = UVMode.Local;
                filterMode = FilterMode.Bilinear;
                aspectMode = AspectMode.Stretch;
            }

            /// <summary>
            /// Calculates and returns a transformation matrix for texture UVs based on tiling, offset, and rotation.
            /// </summary>
            /// <returns>A 4x4 matrix to be used in the shader for UV transformation.</returns>
            public Matrix4x4 GetTextureMatrix()
            {
                Matrix4x4 matrix = Matrix4x4.identity;

                // Apply tiling
                matrix.m00 = tiling.x;
                matrix.m11 = tiling.y;

                // Apply rotation
                if (Mathf.Abs(rotation) > 0.01f)
                {
                    float rad = rotation * Mathf.Deg2Rad;
                    float cos = Mathf.Cos(rad);
                    float sin = Mathf.Sin(rad);

                    Matrix4x4 rotMatrix = Matrix4x4.identity;
                    rotMatrix.m00 = cos;
                    rotMatrix.m01 = -sin;
                    rotMatrix.m10 = sin;
                    rotMatrix.m11 = cos;

                    matrix = rotMatrix * matrix;
                }

                // Apply offset
                matrix.m03 = offset.x;
                matrix.m13 = offset.y;

                return matrix;
            }

            /// <summary>
            /// Adjusts the tiling values to respect the selected aspect mode.
            /// </summary>
            /// <param name="shapeSize">The size of the UI element's RectTransform.</param>
            /// <returns>The adjusted tiling vector.</returns>
            public Vector2 GetAdjustedTiling(Vector2 shapeSize)
            {
                if (aspectMode == AspectMode.Stretch) return tiling;

                Vector2 adjustedTiling = tiling;
                float aspectRatio = shapeSize.x / Mathf.Max(shapeSize.y, 0.001f);

                switch (aspectMode)
                {
                    case AspectMode.FitWidth:
                        adjustedTiling.y = tiling.y * aspectRatio;
                        break;
                    case AspectMode.FitHeight:
                        adjustedTiling.x = tiling.x / aspectRatio;
                        break;
                    case AspectMode.Fill:
                        float scale = Mathf.Max(1f / aspectRatio, aspectRatio);
                        adjustedTiling *= scale;
                        break;
                }

                return adjustedTiling;
            }
            
            /// <summary>
            /// Calculates a performance score for the current texture settings.
            /// </summary>
            /// <returns>A score from 0 (worst) to 10 (best).</returns>
            public float GetPerformanceScore()
            {
                float score = 10f;

                // High tiling values can be expensive due to texture sampling.
                float maxTiling = Mathf.Max(tiling.x, tiling.y);
                if (maxTiling > 4f) score -= (maxTiling - 4f) * 0.5f;

                // Complex blend modes require more shader instructions.
                if (blendMode != BlendMode.Multiply && blendMode != BlendMode.Replace)
                    score -= 0.5f;

                // World UV mode requires additional calculations in the shader.
                if (uvMode == UVMode.World) score -= 0.3f;

                return Mathf.Clamp(score, 0f, 10f);
            }
        }

        [Header("Corner Radius Settings")]
        [Tooltip("Unit for radius values: absolute pixels or a percentage of the smallest dimension.")]
        public Unit cornerRadiusUnit = Unit.Pixels;

        [Tooltip("If enabled, allows setting a different radius for each corner.")]
        public bool useIndividualCorners = false;

        [Tooltip("The global corner radius applied to all corners when 'Use Individual Corners' is disabled.")]
        [Range(0f, 100f)]
        public float globalCornerRadius = 10f;

        [Tooltip("Top-left corner radius.")]
        [Range(0f, 100f)]
        public float cornerRadiusTopLeft = 10f;

        [Tooltip("Top-right corner radius.")]
        [Range(0f, 100f)]
        public float cornerRadiusTopRight = 10f;

        [Tooltip("Bottom-left corner radius.")]
        [Range(0f, 100f)]
        public float cornerRadiusBottomLeft = 10f;

        [Tooltip("Bottom-right corner radius.")]
        [Range(0f, 100f)]
        public float cornerRadiusBottomRight = 10f;

        [Header("Corner Transition Settings")]
        [Tooltip("If enabled, allows setting a different transition smoothness for each corner.")]
        public bool useIndividualOffsets = false;

        [Tooltip("Controls the smoothness of the corner transition (0 = sharp, 1 = very smooth).")]
        [Range(0f, 1f)]
        public float globalCornerOffset = 0.2f;

        [Tooltip("Top-left corner transition smoothness.")]
        [Range(0f, 1f)]
        public float cornerOffsetTopLeft = 0.2f;

        [Tooltip("Top-right corner transition smoothness.")]
        [Range(0f, 1f)]
        public float cornerOffsetTopRight = 0.2f;

        [Tooltip("Bottom-left corner transition smoothness.")]
        [Range(0f, 1f)]
        public float cornerOffsetBottomLeft = 0.2f;

        [Tooltip("Bottom-right corner transition smoothness.")]
        [Range(0f, 1f)]
        public float cornerOffsetBottomRight = 0.2f;

        [Header("Border Settings")]
        [Tooltip("Unit for border width: absolute pixels or a percentage of the smallest dimension.")]
        public Unit borderWidthUnit = Unit.Pixels;

        [Tooltip("The width of the border.")]
        [Range(0f, 100f)]
        public float borderWidth = 2f;

        [Tooltip("The color of the border.")]
        public Color borderColor = Color.black;

        [Tooltip("Second color for border gradient.")]
        public Color borderColorB = Color.white;

        [Tooltip("Enable gradient on the border line.")]
        public bool useBorderGradient = false;

        [Tooltip("Type of border gradient.")]
        public GradientParams.GradientType borderGradientType = GradientParams.GradientType.Linear;

        [Tooltip("Angle of the border gradient in degrees (Linear).")]
        [Range(0f, 360f)]
        public float borderGradientAngle = 0f;

        [Tooltip("Center point for radial border gradient (0-1).")]
        public Vector2 borderGradientRadialCenter = new Vector2(0.5f, 0.5f);

        [Tooltip("Scale for radial border gradient.")]
        [Range(0.1f, 3f)]
        public float borderGradientRadialScale = 1f;

        [Tooltip("Rotation for angular border gradient (degrees).")]
        [Range(0f, 360f)]
        public float borderGradientAngularRotation = 0f;

        [Header("Fill Settings")]
        [Tooltip("The primary fill color of the shape.")]
        public Color fillColor = Color.white;

        [Header("Advanced Effects")]
        [Tooltip("If enabled, applies a blur effect.")]
        public bool enableBlur = false;

        [Tooltip("Parameters for the blur effect.")]
        public BlurParams blurParams = new BlurParams();

        [Tooltip("If enabled, applies a drop shadow effect.")]
        public bool enableShadow = false;

        [Tooltip("Unit for shadow parameters: absolute pixels or a percentage.")]
        public Unit shadowUnit = Unit.Pixels;

        [Tooltip("Parameters for the shadow effect.")]
        public ShadowParams shadowParams = new ShadowParams();

        [Header("Gradient Settings")]
        [Tooltip("If enabled, applies a gradient fill instead of a solid color.")]
        public bool enableGradient = false;

        [Tooltip("Parameters for the gradient effect.")]
        public GradientParams gradientParams = new GradientParams();

        // [AFEGIT] Nous camps per a la vora de progrés
        [Header("Progress Border Settings")]
        [Tooltip("Enables progress border mode (useful for loading bars, indicators, etc.).")]
        public bool useProgressBorder = false;

        [Tooltip("Progress value from 0 to 1 (0% to 100%).")]
        [Range(0f, 1f)]
        public float progressValue = 1f;

        [Tooltip("Starting angle for the progress border in degrees (0=right, 90=up).")]
        [Range(-360f, 360f)]
        public float progressStartAngle = -90f;

        [Tooltip("Direction in which the progress fills.")]
        public ProgressDirection progressDirection = ProgressDirection.Clockwise;

        [Header("Progress Border Color Gradient")]
        [Tooltip("Border color at 0% progress")]
        public Color progressColorStart = Color.black;
        [Tooltip("Border color at 100% progress")]
        public Color progressColorEnd = Color.green;
        [Tooltip("Use color gradient for progress border (interpolates between start and end colors)")]
        public bool useProgressColorGradient = false;


        /// <summary>
        /// Sets the shape data programmatically.
        /// </summary>
        /// <param name="type">The type of shape to use.</param>
        /// <param name="vertices">Optional array of custom vertices for complex shapes.</param>
        /// <param name="starPoints">Number of points if the shape is a star.</param>
        /// <param name="starInnerRatio">Inner radius ratio if the shape is a star.</param>
        public void SetShapeData(ShapeType type, Vector2[] vertices = null, int starPoints = 5, float starInnerRatio = 0.5f)
        {
            shapeType = type;
            this.starPoints = starPoints;
            this.starInnerRatio = starInnerRatio;

            if (vertices != null && vertices.Length > 0)
            {
                customVertices = new Vector2[vertices.Length];
                System.Array.Copy(vertices, customVertices, vertices.Length);
            }
            else
            {
                customVertices = new Vector2[0];
            }
        }

        /// <summary>
        /// Retrieves the vertices for the current shape, normalized to UV space (0-1).
        /// If custom vertices are defined, they are returned; otherwise, default vertices for the selected shape type are generated.
        /// </summary>
        /// <returns>An array of Vector2 representing the shape's vertices.</returns>
        public Vector2[] GetShapeVertices()
        {
            if (customVertices != null && customVertices.Length > 0)
            {
                return customVertices;
            }

            return GenerateDefaultVertices();
        }

        /// <summary>
        /// Generates the default vertices for the selected shape type.
        /// </summary>
        /// <returns>An array of vertices for the current shapeType.</returns>
        private Vector2[] GenerateDefaultVertices()
        {
            switch (shapeType)
            {
                case ShapeType.Triangle:
                    return new Vector2[]
                    {
                        new Vector2(0.5f, 1f),   // Top
                        new Vector2(0f, 0f),     // Bottom-left
                        new Vector2(1f, 0f)      // Bottom-right
                    };

                case ShapeType.Square:
                case ShapeType.Rectangle:
                    return new Vector2[]
                    {
                        new Vector2(0f, 0f),     // Bottom-left
                        new Vector2(0f, 1f),     // Top-left
                        new Vector2(1f, 1f),     // Top-right
                        new Vector2(1f, 0f)      // Bottom-right
                    };

                case ShapeType.Pentagon:
                    return GenerateRegularPolygonVertices(5);

                case ShapeType.Hexagon:
                    return GenerateRegularPolygonVertices(6);

                case ShapeType.Star:
                    return GenerateStarVertices(starPoints, starInnerRatio);

                case ShapeType.Circle:
                    return GenerateRegularPolygonVertices(32); // A 32-sided polygon is a good approximation for a circle.

                default:
                    return GenerateRegularPolygonVertices(4); // Default to a square if the shape is not recognized.
            }
        }

        /// <summary>
        /// Generates vertices for a regular polygon with a specified number of sides.
        /// </summary>
        /// <param name="sides">The number of sides for the polygon.</param>
        /// <returns>An array of vertices forming the polygon.</returns>
        private Vector2[] GenerateRegularPolygonVertices(int sides)
        {
            Vector2[] vertices = new Vector2[sides];
            float angleStep = 360f / sides;

            for (int i = 0; i < sides; i++)
            {
                float angle = (90f - i * angleStep) * Mathf.Deg2Rad; // Start from the top point
                vertices[i] = new Vector2(
                    0.5f + Mathf.Cos(angle) * 0.5f,
                    0.5f + Mathf.Sin(angle) * 0.5f
                );
            }

            return vertices;
        }

        /// <summary>
        /// Generates vertices for a star shape.
        /// </summary>
        /// <param name="points">The number of outer points of the star.</param>
        /// <param name="innerRatio">The ratio of the inner radius to the outer radius.</param>
        /// <returns>An array of vertices forming the star.</returns>
        private Vector2[] GenerateStarVertices(int points, float innerRatio)
        {
            Vector2[] vertices = new Vector2[points * 2];
            float angleStep = 360f / (points * 2);

            for (int i = 0; i < points * 2; i++)
            {
                // Alternate between outer and inner radius
                float radius = (i % 2 == 0) ? 0.5f : 0.5f * innerRatio;
                float angle = (90f - i * angleStep) * Mathf.Deg2Rad; // Start from the top point
                vertices[i] = new Vector2(
                    0.5f + Mathf.Cos(angle) * radius,
                    0.5f + Mathf.Sin(angle) * radius
                );
            }

            return vertices;
        }

        /// <summary>
        /// Contains parameters for the blur effect.
        /// </summary>
        [System.Serializable]
        public class BlurParams
        {
            /// <summary>
            /// Defines the type of blur to be rendered.
            /// </summary>
            public enum BlurType
            {
                Internal = 0,    // Blur of the element's content/fill
                Background = 1    // Blur of the scene behind the element (GrabPass)
            }
            
            [Tooltip("Type of blur: Internal (content) or Background (scene).")]
            public BlurType blurType = BlurType.Internal;

            [Tooltip("Offset added to the render queue for Background Blur (e.g., 3001). Higher means it renders on top of more UI.")]
            [Range(0, 100)]
            public int queueOffset = 1; // <--- NOVA PROPIETAT AFEGIDA

            [Tooltip("Downsample factor for blur performance. Higher values improve performance but reduce quality.")]
            [Range(1, 100)]
            public int downsample = 2;

            [Tooltip("Number of blur iterations. Higher values improve quality but decrease performance.")]
            [Range(1, 8)]
            public int iterations = 2;

            [Tooltip("Blur radius in pixels.")]
            [Range(0f, 1000f)]
            public float radius = 2f;

            /// <summary>
            /// Default constructor for BlurParams.
            /// </summary>
            public BlurParams()
            {
                blurType = BlurType.Internal;
                queueOffset = 1; // <--- AFEGIT AL CONSTRUCTOR
                downsample = 2;
                iterations = 2;
                radius = 2f;
            }

            /// <summary>
            /// Calculates a performance score for the current blur settings.
            /// </summary>
            /// <returns>A score from 0 (worst) to 10 (best).</returns>
            public float GetPerformanceScore()
            {
                float score = 10f;
                score -= (radius / 10f) * 3f;      // Larger radius is more expensive.
                score -= ((iterations - 1) / 7f) * 4f; // More iterations are very expensive.
                score += ((downsample - 1) / 3f) * 2f; // Downsampling improves performance.
                
                // Background blur is inherently more expensive due to GrabPass.
                if (blurType == BlurType.Background) score -= 1.5f;

                return Mathf.Clamp(score, 0f, 10f);
            }
        }

        /// <summary>
        /// Contains parameters for the drop shadow effect.
        /// </summary>
        [System.Serializable]
        public class ShadowParams
        {
            [Tooltip("The color of the shadow.")]
            public Color color = new Color(0f, 0f, 0f, 0.5f);

            [Tooltip("Shadow offset in pixels or percentage.")]
            public Vector2 offset = new Vector2(2f, -2f);

            [Tooltip("Shadow blur amount in pixels or percentage.")]
            [Range(0f, 100f)]
            public float blur = 3f;

            [Tooltip("An overall opacity multiplier for the shadow.")]
            [Range(0f, 1f)]
            public float opacity = 0.5f;

            /// <summary>
            /// Default constructor for ShadowParams.
            /// </summary>
            public ShadowParams()
            {
                color = new Color(0f, 0f, 0f, 0.5f);
                offset = new Vector2(2f, -2f);
                blur = 3f;
                opacity = 0.5f;
            }

            /// <summary>
            /// Calculates the distance of the shadow from the object.
            /// </summary>
            /// <returns>The magnitude of the offset vector.</returns>
            public float GetShadowDistance()
            {
                return offset.magnitude;
            }

            /// <summary>
            /// Calculates a performance score for the current shadow settings.
            /// </summary>
            /// <returns>A score from 0 (worst) to 10 (best).</returns>
            public float GetPerformanceScore()
            {
                float score = 10f;
                score -= (blur / 10f) * 2f; // More blur is more expensive.
                score -= (opacity * 0.5f);
                score -= (GetShadowDistance() / 20f) * 0.5f;
                return Mathf.Clamp(score, 0f, 10f);
            }
        }

        /// <summary>
        /// Contains parameters for the gradient fill effect.
        /// </summary>
        [System.Serializable]
        public class GradientParams
        {
            /// <summary>
            /// Defines the type of gradient to be rendered.
            /// </summary>
            public enum GradientType
            {
                Linear,
                Radial,
                Angular
            }

            [Tooltip("The type of gradient.")]
            public GradientType type = GradientType.Linear;

            [Tooltip("The starting color of the gradient.")]
            public Color colorA = Color.white;

            [Tooltip("The ending color of the gradient.")]
            public Color colorB = Color.gray;

            [Tooltip("The angle of the gradient in degrees (for Linear and Angular types).")]
            [Range(0f, 360f)]
            public float angle = 0f;

            [Tooltip("Center point for Radial gradients (0-1 normalized coordinates).")]
            public Vector2 radialCenter = new Vector2(0.5f, 0.5f);

            [Tooltip("Rotation angle for Angular gradients (degrees).")]
            [Range(0f, 360f)]
            public float angularRotation = 0f;

            [Tooltip("Scale/zoom for Radial gradients.")]
            [Range(0.1f, 3f)]
            public float radialScale = 1f;

            [Tooltip("Toggles the gradient effect on or off.")]
            public bool enabled = false;

            /// <summary>
            /// Default constructor for GradientParams.
            /// </summary>
            public GradientParams()
            {
                type = GradientType.Linear;
                colorA = Color.white;
                colorB = Color.gray;
                angle = 0f;
                enabled = false;
                // NOU: Valors per defecte pels nous paràmetres
                radialCenter = new Vector2(0.5f, 0.5f);
                angularRotation = 0f;
                radialScale = 1f;
            }
        }

        /// <summary>
        /// Gets the corner radii as a Vector4 (TL, TR, BR, BL), using either global or individual values.
        /// </summary>
        /// <returns>A Vector4 containing the radius for each corner.</returns>
        public Vector4 GetCornerRadii()
        {
            if (useIndividualCorners)
            {
                return new Vector4(
                    cornerRadiusTopLeft,
                    cornerRadiusTopRight,
                    cornerRadiusBottomRight,
                    cornerRadiusBottomLeft
                );
            }
            else
            {
                return new Vector4(
                    globalCornerRadius,
                    globalCornerRadius,
                    globalCornerRadius,
                    globalCornerRadius
                );
            }
        }

        /// <summary>
        /// Gets the corner offsets (smoothness) as a Vector4 (TL, TR, BR, BL), using either global or individual values.
        /// </summary>
        /// <returns>A Vector4 containing the clamped offset for each corner.</returns>
        public Vector4 GetCornerOffsets()
        {
            if (useIndividualOffsets)
            {
                return new Vector4(
                    Mathf.Clamp01(cornerOffsetTopLeft),
                    Mathf.Clamp01(cornerOffsetTopRight),
                    Mathf.Clamp01(cornerOffsetBottomRight),
                    Mathf.Clamp01(cornerOffsetBottomLeft)
                );
            }
            else
            {
                float clampedOffset = Mathf.Clamp01(globalCornerOffset);
                return new Vector4(
                    clampedOffset,
                    clampedOffset,
                    clampedOffset,
                    clampedOffset
                );
            }
        }

        /// <summary>
        /// Applies this profile's settings to a given UIEffectComponent.
        /// </summary>
        /// <param name="component">The component to apply the settings to.</param>
        public void ApplyTo(UIEffectComponent component)
        {
            if (component == null)
            {
                Debug.LogWarning("Cannot apply profile to a null UIEffectComponent.");
                return;
            }

            component.SetProfile(this);
        }

        /// <summary>
        /// Creates a deep copy of this profile instance.
        /// This is useful for creating runtime variations of a profile without modifying the original asset.
        /// </summary>
        /// <returns>A new UIEffectProfile instance with the same settings as this one.</returns>
        public UIEffectProfile Clone()
        {
            UIEffectProfile clone = CreateInstance<UIEffectProfile>();

            // Copy shape data
            clone.shapeType = shapeType;
            clone.starPoints = starPoints;
            clone.starInnerRatio = starInnerRatio;

            // Copy custom vertices array
            if (customVertices != null && customVertices.Length > 0)
            {
                clone.customVertices = new Vector2[customVertices.Length];
                System.Array.Copy(customVertices, clone.customVertices, customVertices.Length);
            }
            else
            {
                clone.customVertices = new Vector2[0];
            }
            
            // Copy value types
            clone.cornerRadiusUnit = cornerRadiusUnit;
            clone.borderWidthUnit = borderWidthUnit;
            clone.shadowUnit = shadowUnit;

            clone.useIndividualCorners = useIndividualCorners;
            clone.globalCornerRadius = globalCornerRadius;
            clone.cornerRadiusTopLeft = cornerRadiusTopLeft;
            clone.cornerRadiusTopRight = cornerRadiusTopRight;
            clone.cornerRadiusBottomLeft = cornerRadiusBottomLeft;
            clone.cornerRadiusBottomRight = cornerRadiusBottomRight;

            clone.useIndividualOffsets = useIndividualOffsets;
            clone.globalCornerOffset = globalCornerOffset;
            clone.cornerOffsetTopLeft = cornerOffsetTopLeft;
            clone.cornerOffsetTopRight = cornerOffsetTopRight;
            clone.cornerOffsetBottomLeft = cornerOffsetBottomLeft;
            clone.cornerOffsetBottomRight = cornerOffsetBottomRight;

            clone.borderWidth = borderWidth;
            clone.borderColor = borderColor;
            clone.borderColorB = borderColorB;
            clone.useBorderGradient = useBorderGradient;
            clone.borderGradientType = borderGradientType;
            clone.borderGradientAngle = borderGradientAngle;
            clone.borderGradientRadialCenter = borderGradientRadialCenter;
            clone.borderGradientRadialScale = borderGradientRadialScale;
            clone.borderGradientAngularRotation = borderGradientAngularRotation;
            clone.fillColor = fillColor;

            clone.enableBlur = enableBlur;
            clone.blurParams = new BlurParams
            {
                blurType = blurParams != null ? blurParams.blurType : BlurParams.BlurType.Internal,
                queueOffset = blurParams != null ? blurParams.queueOffset : 1, // <--- AFEGIT A CLONE
                downsample = blurParams != null ? blurParams.downsample : 2,
                iterations = blurParams != null ? blurParams.iterations : 2,
                radius = blurParams != null ? blurParams.radius : 2f
            };

            clone.enableShadow = enableShadow;
            clone.shadowParams = new ShadowParams
            {
                color = shadowParams != null ? shadowParams.color : new Color(0f, 0f, 0f, 0.5f),
                offset = shadowParams != null ? shadowParams.offset : new Vector2(2f, -2f),
                blur = shadowParams != null ? shadowParams.blur : 3f,
                opacity = shadowParams != null ? shadowParams.opacity : 0.5f
            };

            clone.enableGradient = enableGradient;
            clone.gradientParams = new GradientParams
            {
                type = gradientParams != null ? gradientParams.type : GradientParams.GradientType.Linear,
                colorA = gradientParams != null ? gradientParams.colorA : Color.white,
                colorB = gradientParams != null ? gradientParams.colorB : Color.gray,
                angle = gradientParams != null ? gradientParams.angle : 0f,
                enabled = gradientParams != null ? gradientParams.enabled : false,
                // NOU: Clonatge dels nous paràmetres
                radialCenter = gradientParams != null ? gradientParams.radialCenter : new Vector2(0.5f, 0.5f),
                angularRotation = gradientParams != null ? gradientParams.angularRotation : 0f,
                radialScale = gradientParams != null ? gradientParams.radialScale : 1f
            };
            
            clone.enableTexture = enableTexture;
            // Textures are shared by reference, not duplicated, to save memory.
            clone.overlayTexture = overlayTexture;
            
            // Clone texture parameters
            clone.textureParams = new TextureParams
            {
                tiling = textureParams != null ? textureParams.tiling : Vector2.one,
                offset = textureParams != null ? textureParams.offset : Vector2.zero,
                rotation = textureParams != null ? textureParams.rotation : 0f,
                opacity = textureParams != null ? textureParams.opacity : 1f,
                blendMode = textureParams != null ? textureParams.blendMode : TextureParams.BlendMode.Multiply,
                uvMode = textureParams != null ? textureParams.uvMode : TextureParams.UVMode.Local,
                filterMode = textureParams != null ? textureParams.filterMode : FilterMode.Bilinear,
                aspectMode = textureParams != null ? textureParams.aspectMode : TextureParams.AspectMode.Stretch
            };

            // [AFEGIT] Copiar els nous camps de progrés
            clone.useProgressBorder = useProgressBorder;
            clone.progressValue = progressValue;
            clone.progressStartAngle = progressStartAngle;
            clone.progressDirection = progressDirection;
            clone.progressColorStart = progressColorStart;
            clone.progressColorEnd = progressColorEnd;
            clone.useProgressColorGradient = useProgressColorGradient;


            // Give the clone a unique name to distinguish it from the original.
            clone.name = $"{name}_Clone_{System.DateTime.Now.Ticks % 10000}";

            return clone;
        }
        
        /// <summary>
        /// Checks if the provided profile is the exact same instance in memory.
        /// </summary>
        /// <param name="other">The other profile to compare against.</param>
        /// <returns>True if they are the same instance, false otherwise.</returns>
        public bool IsSameInstance(UIEffectProfile other)
        {
            return ReferenceEquals(this, other);
        }

        /// <summary>
        /// Clones this profile and gives the new instance a specific name.
        /// </summary>
        /// <param name="newName">The name for the new cloned profile.</param>
        /// <returns>A new UIEffectProfile instance.</returns>
        public UIEffectProfile CloneWithName(string newName)
        {
            var clone = Clone();
            clone.name = newName;
            return clone;
        }

        /// <summary>
        /// Resets all settings in this profile to their default values.
        /// </summary>
        public void ResetToDefaults()
        {
            shapeType = ShapeType.Rectangle;
            starPoints = 5;
            starInnerRatio = 0.5f;
            customVertices = new Vector2[0];

            cornerRadiusUnit = Unit.Pixels;
            borderWidthUnit = Unit.Pixels;
            shadowUnit = Unit.Pixels;

            useIndividualCorners = false;
            globalCornerRadius = 10f;
            cornerRadiusTopLeft = 10f;
            cornerRadiusTopRight = 10f;
            cornerRadiusBottomLeft = 10f;
            cornerRadiusBottomRight = 10f;

            useIndividualOffsets = false;
            globalCornerOffset = 0.2f;
            cornerOffsetTopLeft = 0.2f;
            cornerOffsetTopRight = 0.2f;
            cornerOffsetBottomLeft = 0.2f;
            cornerOffsetBottomRight = 0.2f;

            borderWidth = 2f;
            borderColor = Color.black;
            borderColorB = Color.white;
            useBorderGradient = false;
            borderGradientType = GradientParams.GradientType.Linear;
            borderGradientAngle = 0f;
            borderGradientRadialCenter = new Vector2(0.5f, 0.5f);
            borderGradientRadialScale = 1f;
            borderGradientAngularRotation = 0f;
            fillColor = Color.white;

            enableBlur = false;
            blurParams = new BlurParams();

            enableShadow = false;
            shadowParams = new ShadowParams();

            enableGradient = false;
            gradientParams = new GradientParams();

            enableTexture = false;
            overlayTexture = null;
            textureParams = new TextureParams();

            // [AFEGIT] Resetejar els nous camps
            useProgressBorder = false;
            progressValue = 1f;
            progressStartAngle = -90f;
            progressDirection = ProgressDirection.Clockwise;
            progressColorStart = Color.black;
            progressColorEnd = Color.green;
            useProgressColorGradient = false;
        }

        /// <summary>
        /// Applies a preset configuration suitable for a standard button style.
        /// </summary>
        public void SetButtonPreset()
        {
            ResetToDefaults();
            useIndividualCorners = false;
            cornerRadiusUnit = Unit.Percent;
            globalCornerRadius = 25f;
            useIndividualOffsets = false;
            globalCornerOffset = 0.15f;

            borderWidthUnit = Unit.Pixels;
            borderWidth = 1f;
            borderColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            fillColor = new Color(0.9f, 0.9f, 0.9f, 1f);

            enableBlur = false;
            enableShadow = true;
            shadowUnit = Unit.Pixels;
            shadowParams = new ShadowParams
            {
                color = new Color(0f, 0f, 0f, 0.3f),
                offset = new Vector2(0f, -2f),
                blur = 2f,
                opacity = 0.3f
            };
            enableGradient = false;
        }

        /// <summary>
        /// Applies a preset configuration suitable for a card or container style.
        /// </summary>
        public void SetCardPreset()
        {
            ResetToDefaults();
            useIndividualCorners = false;
            cornerRadiusUnit = Unit.Pixels;
            globalCornerRadius = 12f;
            useIndividualOffsets = false;
            globalCornerOffset = 0.1f;

            borderWidth = 0f;
            fillColor = Color.white;

            enableBlur = false;
            enableShadow = true;
            shadowUnit = Unit.Pixels;
            shadowParams = new ShadowParams
            {
                color = new Color(0f, 0f, 0f, 0.15f),
                offset = new Vector2(0f, 2f),
                blur = 4f,
                opacity = 0.8f
            };
            enableGradient = false;
        }

        /// <summary>
        /// Applies a preset configuration suitable for a background panel style.
        /// </summary>
        public void SetPanelPreset()
        {
            ResetToDefaults();
            useIndividualCorners = false;
            cornerRadiusUnit = Unit.Pixels;
            globalCornerRadius = 6f;
            useIndividualOffsets = false;
            globalCornerOffset = 0.05f;

            borderWidthUnit = Unit.Pixels;
            borderWidth = 1f;
            borderColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
            fillColor = new Color(0.95f, 0.95f, 0.95f, 0.9f);

            enableBlur = false;
            enableShadow = false;
            enableGradient = false;
        }

        /// <summary>
        /// Calculates an overall performance score for the current profile settings.
        /// A higher score indicates better performance.
        /// </summary>
        /// <returns>A score from 0 (worst) to 10 (best).</returns>
        public float GetPerformanceScore()
        {
            float score = 10f;

            // Minor costs for using individual settings due to more shader branching.
            if (useIndividualCorners) score -= 0.5f;
            if (useIndividualOffsets) score -= 0.5f;
            if (globalCornerOffset > 0.5f) score -= 0.5f;

            // Major effects have a significant performance impact.
            if (enableBlur) score -= (10f - blurParams.GetPerformanceScore()) * 0.4f;
            if (enableShadow) score -= (10f - shadowParams.GetPerformanceScore()) * 0.2f;

            // A thick border can be slightly more expensive.
            if (borderWidth > 5f && borderWidthUnit == Unit.Pixels) score -= 0.3f;
            
            // Texture effects have their own performance considerations.
            if (enableTexture && textureParams != null)
            {
                score -= (10f - textureParams.GetPerformanceScore()) * 0.15f;
                    
                // Large textures impact memory and sampling performance.
                if (overlayTexture != null)
                {
                    int textureSize = overlayTexture.width * overlayTexture.height;
                    if (textureSize > 1024 * 1024) score -= 0.5f; // Texture larger than 1 megapixel
                    else if (textureSize > 512 * 512) score -= 0.2f; // Texture larger than 0.25 megapixels
                }
            }

            return Mathf.Clamp(score, 0f, 10f);
        }

        /// <summary>
        /// Compares this profile to another to see if their visual settings are effectively the same.
        /// This is different from checking for the same instance.
        /// </summary>
        /// <param name="other">The other profile to compare against.</param>
        /// <returns>True if the profiles are visually equivalent, false otherwise.</returns>
        public bool IsEquivalentTo(UIEffectProfile other)
        {
            if (other == null) return false;

            if (useIndividualCorners != other.useIndividualCorners) return false;

            Vector4 thisRadii = GetCornerRadii();
            Vector4 otherRadii = other.GetCornerRadii();
            if (Vector4.Distance(thisRadii, otherRadii) > 0.01f) return false;

            if (useIndividualOffsets != other.useIndividualOffsets) return false;

            Vector4 thisOffsets = GetCornerOffsets();
            Vector4 otherOffsets = other.GetCornerOffsets();
            if (Vector4.Distance(thisOffsets, otherOffsets) > 0.001f) return false;

            if (Mathf.Abs(borderWidth - other.borderWidth) > 0.001f) return false;
            if (Vector4.Distance(borderColor, other.borderColor) > 0.01f) return false;
            if (Vector4.Distance(borderColorB, other.borderColorB) > 0.01f) return false;
            if (useBorderGradient != other.useBorderGradient) return false;
            if (borderGradientType != other.borderGradientType) return false;
            if (Mathf.Abs(borderGradientAngle - other.borderGradientAngle) > 0.01f) return false;
            if (Vector2.Distance(borderGradientRadialCenter, other.borderGradientRadialCenter) > 0.01f) return false;
            if (Mathf.Abs(borderGradientRadialScale - other.borderGradientRadialScale) > 0.01f) return false;
            if (Mathf.Abs(borderGradientAngularRotation - other.borderGradientAngularRotation) > 0.01f) return false;
            if (Vector4.Distance(fillColor, other.fillColor) > 0.01f) return false;

            if (enableBlur != other.enableBlur || enableShadow != other.enableShadow) return false;
            if (enableGradient != other.enableGradient) return false;

            if (cornerRadiusUnit != other.cornerRadiusUnit || borderWidthUnit != other.borderWidthUnit || shadowUnit != other.shadowUnit) return false;

            return true;
        }

        /// <summary>
        /// Provides a string summary of the profile's main settings for debugging purposes.
        /// </summary>
        /// <returns>A formatted string with key information about the profile.</returns>
        public override string ToString()
        {
            var radii = GetCornerRadii();
            var offsets = GetCornerOffsets();
            string effects = "";
            if (enableBlur) effects += "Blur ";
            if (enableShadow) effects += "Shadow ";

            return $"UIEffectProfile: Corners({radii.x:F0},{radii.y:F0},{radii.z:F0},{radii.w:F0}), " +
                   $"Offsets({offsets.x:F2},{offsets.y:F2},{offsets.z:F2},{offsets.w:F2}), " +
                   $"Border({borderWidth:F0}px), Effects({effects.Trim()}), " +
                   $"Performance({GetPerformanceScore():F1}/10)";
        }
        
        /// <summary>
        /// Ensures that all profile values are within their valid ranges.
        /// This is automatically called by Unity in the editor when a value is changed.
        /// </summary>
        public void ValidateAndCorrect()
        {
            // Ensure radii are non-negative
            globalCornerRadius = Mathf.Max(0f, globalCornerRadius);
            cornerRadiusTopLeft = Mathf.Max(0f, cornerRadiusTopLeft);
            cornerRadiusTopRight = Mathf.Max(0f, cornerRadiusTopRight);
            cornerRadiusBottomLeft = Mathf.Max(0f, cornerRadiusBottomLeft);
            cornerRadiusBottomRight = Mathf.Max(0f, cornerRadiusBottomRight);
            
            // Clamp offsets between 0 and 1
            globalCornerOffset = Mathf.Clamp01(globalCornerOffset);
            cornerOffsetTopLeft = Mathf.Clamp01(cornerOffsetTopLeft);
            cornerOffsetTopRight = Mathf.Clamp01(cornerOffsetTopRight);
            cornerOffsetBottomLeft = Mathf.Clamp01(cornerOffsetBottomLeft);
            cornerOffsetBottomRight = Mathf.Clamp01(cornerOffsetBottomRight);

            borderWidth = Mathf.Max(0f, borderWidth);
            
            // Validate nested parameter classes, ensuring they are not null
            if (blurParams != null)
            {
                blurParams.downsample = Mathf.Clamp(blurParams.downsample, 1, 100);
                blurParams.iterations = Mathf.Clamp(blurParams.iterations, 1, 8);
                blurParams.radius = Mathf.Max(0f, blurParams.radius);
                blurParams.queueOffset = Mathf.Clamp(blurParams.queueOffset, 0, 10); // <--- VALIDACIÓ AFEGIDA
            }
            else
            {
                blurParams = new BlurParams();
            }

            if (shadowParams != null)
            {
                shadowParams.blur = Mathf.Max(0f, shadowParams.blur);
                shadowParams.opacity = Mathf.Clamp01(shadowParams.opacity);
            }
            else
            {
                shadowParams = new ShadowParams();
            }

            if (gradientParams != null)
            {
                gradientParams.angle = gradientParams.angle % 360f;
                if (gradientParams.angle < 0f)
                    gradientParams.angle += 360f;
            }
            else
            {
                gradientParams = new GradientParams();
            }
            
            if (textureParams != null)
            {
                textureParams.tiling = new Vector2(
                    Mathf.Max(0.01f, textureParams.tiling.x),
                    Mathf.Max(0.01f, textureParams.tiling.y)
                );
                textureParams.rotation = textureParams.rotation % 360f;
                if (textureParams.rotation < 0f) textureParams.rotation += 360f;
                textureParams.opacity = Mathf.Clamp01(textureParams.opacity);
            }
            else
            {
                textureParams = new TextureParams();
            }

            // [AFEGIT] Validació per als nous camps
            progressValue = Mathf.Clamp01(progressValue);
            // (Opcional) Normalitzar l'angle, tot i que el shader ja ho fa
            // progressStartAngle = progressStartAngle % 360f;
        }
        
        /// <summary>
        /// Unity message called when the script is first added or reset.
        /// </summary>
        private void Reset()
        {
            ResetToDefaults();
        }

        /// <summary>
        /// Unity message called in the editor whenever the script is loaded or a value is changed in the Inspector.
        /// </summary>
        private void OnValidate()
        {
            ValidateAndCorrect();
        }

        #region Editor Utilities

        // This section contains methods that are only compiled in the Unity Editor.
        // They provide helpful context menus and asset creation options.
#if UNITY_EDITOR
        /// <summary>
        /// Editor-only method to create a default UIEffectProfile asset.
        /// </summary>
        [UnityEditor.MenuItem("Assets/Create/UI Effects Pro/Default Preset")]
        public static void CreateDefaultPreset()
        {
            UIEffectProfile preset = CreateInstance<UIEffectProfile>();
            preset.name = "Default UI Effect";
            preset.ResetToDefaults();

            string path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath("Assets/Default UI Effect.asset");
            UnityEditor.AssetDatabase.CreateAsset(preset, path);
            UnityEditor.AssetDatabase.SaveAssets();

            UnityEditor.Selection.activeObject = preset;
            UnityEditor.EditorUtility.FocusProjectWindow();

            Debug.Log($"Created default UI Effect preset at {path}");
        }

        /// <summary>
        /// Editor-only method to create a button-styled UIEffectProfile asset.
        /// </summary>
        [UnityEditor.MenuItem("Assets/Create/UI Effects Pro/Button Preset")]
        public static void CreateButtonPreset()
        {
            UIEffectProfile preset = CreateInstance<UIEffectProfile>();
            preset.name = "Button UI Effect";
            preset.SetButtonPreset();

            string path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath("Assets/Button UI Effect.asset");
            UnityEditor.AssetDatabase.CreateAsset(preset, path);
            UnityEditor.AssetDatabase.SaveAssets();

            UnityEditor.Selection.activeObject = preset;
            UnityEditor.EditorUtility.FocusProjectWindow();

            Debug.Log($"Created button UI Effect preset at {path}");
        }

        /// <summary>
        /// Editor-only method to create a card-styled UIEffectProfile asset.
        /// </summary>
        [UnityEditor.MenuItem("Assets/Create/UI Effects Pro/Card Preset")]
        public static void CreateCardPreset()
        {
            UIEffectProfile preset = CreateInstance<UIEffectProfile>();
            preset.name = "Card UI Effect";
            preset.SetCardPreset();

            string path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath("Assets/Card UI Effect.asset");
            UnityEditor.AssetDatabase.CreateAsset(preset, path);
            UnityEditor.AssetDatabase.SaveAssets();

            UnityEditor.Selection.activeObject = preset;
            UnityEditor.EditorUtility.FocusProjectWindow();

            Debug.Log($"Created card UI Effect preset at {path}");
        }

        /// <summary>
        /// Context menu item to apply this profile to all selected UIEffectComponents in the scene.
        /// </summary>
        [UnityEditor.MenuItem("CONTEXT/UIEffectProfile/Apply to Selected Components")]
        public static void ApplyToSelectedComponents(UnityEditor.MenuCommand command)
        {
            UIEffectProfile profile = command.context as UIEffectProfile;
            if (profile == null) return;

            var selectedComponents = UnityEditor.Selection.GetFiltered<UIEffectComponent>(UnityEditor.SelectionMode.Unfiltered);

            if (selectedComponents.Length == 0)
            {
                UnityEditor.EditorUtility.DisplayDialog("No Components Selected",
                    "Please select one or more UIEffectComponents in the scene.", "OK");
                return;
            }

            foreach (var component in selectedComponents)
            {
                UnityEditor.Undo.RecordObject(component, "Apply UI Effect Profile");
                profile.ApplyTo(component);
                UnityEditor.EditorUtility.SetDirty(component);
            }

            Debug.Log($"Applied profile '{profile.name}' to {selectedComponents.Length} component(s)");
        }
        
        /// <summary>
        /// Context menu item to show a performance analysis of this profile.
        /// </summary>
        [UnityEditor.MenuItem("CONTEXT/UIEffectProfile/Show Performance Analysis")]
        public static void ShowPerformanceAnalysis(UnityEditor.MenuCommand command)
        {
            UIEffectProfile profile = command.context as UIEffectProfile;
            if (profile == null) return;

            float score = profile.GetPerformanceScore();
            string analysis = $"Performance Analysis for '{profile.name}':\n\n";
            analysis += $"Overall Score: {score:F1}/10\n\n";

            if (score >= 8f)
                analysis += "✅ Excellent performance - suitable for all devices.\n";
            else if (score >= 6f)
                analysis += "⚠️ Good performance - suitable for most devices.\n";
            else if (score >= 4f)
                analysis += "⚠️ Moderate performance - may impact lower-end devices.\n";
            else
                analysis += "❌ Poor performance - optimize for better results.\n";

            analysis += "\nDetailed breakdown:\n";
            if (profile.enableBlur)
                analysis += $"• Blur: {profile.blurParams.GetPerformanceScore():F1}/10\n";
            if (profile.enableShadow)
                analysis += $"• Shadow: {profile.shadowParams.GetPerformanceScore():F1}/10\n";

            UnityEditor.EditorUtility.DisplayDialog("Performance Analysis", analysis, "OK");
        }
#endif

        #endregion
    }
}