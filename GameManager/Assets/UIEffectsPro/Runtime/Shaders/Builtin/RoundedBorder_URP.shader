// UIEffectsPro/Runtime/Shaders/URP/RoundedBorder_URP.shader
Shader "UIEffects/RoundedBorder_URP"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)

        // Shape Configuration
        [Header(Shape)]
        _ShapeType ("Shape Type", Float) = 0 
        _ShapeVertices ("Vertices A", Vector) = (0,0,0,0)
        _ShapeVerticesExt ("Vertices B", Vector) = (0,0,0,0)
        _VertexCount ("Vertex Count", Float) = 4 

        // Border and Corner Styling
        [Header(Corners and Border)]
        _SpriteUvs ("Sprite UVs", Vector) = (0,0,1,1)
        _CornerRadii ("Corner Radii (TL TR BR BL)", Vector) = (10,10,10,10)
        _CornerOffsets ("Corner Offsets (TL TR BR BL)", Vector) = (0.2,0.2,0.2,0.2)
        _BorderWidth ("Border Width", Float) = 2
        
        _BorderColor ("Border Color", Color) = (0,0,0,1)
        _BorderColorB ("Border Color B", Color) = (1,1,1,1)
        _UseBorderGradient ("Use Border Gradient", float) = 0
        _BorderGradientType ("Border Gradient Type", float) = 0
        _BorderGradientAngle ("Border Gradient Angle", float) = 0
        _BorderGradientRadialCenter ("Border Gradient Radial Center", Vector) = (0.5,0.5,0,0)
        _BorderGradientRadialScale ("Border Gradient Radial Scale", float) = 1
        _BorderGradientAngularRotation ("Border Gradient Angular Rotation", float) = 0
        
        /* [AFEGIT] Propietats de la vora de progrés */
        _UseProgressBorder ("Use Progress Border", float) = 0
        _ProgressValue ("Progress Value", Range(0,1)) = 1
        _ProgressStartAngle ("Progress Start Angle", float) = -90
        _ProgressDirection ("Progress Direction", float) = 0
        
        // --- MODIFICACIÓ A ---
        _ProgressColorStart ("Progress Color Start", Color) = (0,0,0,1)
        _ProgressColorEnd ("Progress Color End", Color) = (0,1,0,1)
        _UseProgressColorGradient ("Use Progress Color Gradient", float) = 0
        // --- FI MODIFICACIÓ A ---
        
        _UseIndividualCorners ("Use Individual Corners", Float) = 1
        _UseIndividualOffsets ("Use Individual Offsets", Float) = 0
        _GlobalCornerOffset ("Global Corner Offset", Float) = 0.2 
        _RectSize ("Element Size", Vector) = (100,100,0,0)

        // Blur Effect
        [Header(Blur)]
        _EnableBlur ("Enable Blur", Float) = 0
        _BlurType ("Blur Type", Float) = 0 // 0=Internal, 1=Background
        
        _BlurRadius ("Blur Radius", Range(0, 1000)) = 2.0
        _BlurIterations ("Blur Iterations", Range(1, 8)) = 2
        _BlurDownsample ("Blur Downsample", Range(1, 100)) = 2

        // Shadow Effect
        [Header(Shadow)]
        _EnableShadow ("Enable Shadow", Float) = 0 
        _ShadowColor ("Shadow Color", Color) = (0,0,0,0.5)
        _ShadowOffset ("Shadow Offset", Vector) = (2, -2, 0, 0)
        _ShadowBlur ("Shadow Blur", Range(0, 10)) = 3.0
        _ShadowOpacity ("Shadow Opacity", Range(0, 1)) = 0.5

        // Gradient Overlay
        [Header(Gradient)]
        _EnableGradient ("Enable Gradient", Float) = 0 
        _GradientType ("Gradient Type", Float) = 0
        _GradientColorA ("Gradient Color A", Color) = (1,1,1,1)
        _GradientColorB ("Gradient Color B", Color) = (0,0,0,1)
        _GradientAngle ("Gradient Angle Radians", Float) = 1.57
        _GradientRadialCenter ("Radial Center", Vector) = (0.5, 0.5, 0, 0)
        _GradientAngularRotation ("Angular Rotation", Float) = 0
        _GradientRadialScale ("Radial Scale", Float) = 1

        // Texture Overlay
        [Header(Texture)]
        _EnableTexture ("Enable Texture", Float) = 0
        _OverlayTexture ("Overlay Texture", 2D) = "white" {}
        _TextureTiling ("Texture Tiling", Vector) = (1,1,0,0) 
        _TextureOffset ("Texture Offset", Vector) = (0,0,0,0)
        _TextureRotation ("Texture Rotation", Float) = 0
        _TextureOpacity ("Texture Opacity", Range(0,1)) = 1
        _TextureBlendMode ("Texture Blend Mode", Float) = 0
        _TextureUVMode ("Texture UV Mode", Float) = 0

        // Stencil Operations
        [Header(Stencil)]
        _StencilComp ("Stencil Comparison", Float) = 8 
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent+1"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "UIEffects URP" 
            
            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                Pass [_StencilOp]
                ReadMask [_StencilReadMask] 
                WriteMask [_StencilWriteMask]
            }

            Cull Off
            Lighting Off
            ZWrite Off
            ZTest [unity_GUIZTestMode]
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask [_ColorMask] 
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            // Define PI if not already defined
            #ifndef PI
            #define PI 3.14159265359
            #endif

            // --- Variable Declarations ---
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            
            half4 _Color;
            float _ShapeType;
            float4 _ShapeVertices;
            float4 _ShapeVerticesExt;
            float _VertexCount;
            
            float4 _SpriteUvs;
            float4 _CornerRadii;
            float4 _CornerOffsets;
            float _BorderWidth;
            half4 _BorderColor;
            half4 _BorderColorB;
            float _UseBorderGradient;
            float _BorderGradientType;
            float _BorderGradientAngle;
            float4 _BorderGradientRadialCenter;
            float _BorderGradientRadialScale;
            float _BorderGradientAngularRotation;

            // [AFEGIT] Variables de progrés
            float _UseProgressBorder;
            float _ProgressValue;
            float _ProgressStartAngle;
            float _ProgressDirection;
            
            // --- MODIFICACIÓ B (URP) ---
            half4 _ProgressColorStart;
            half4 _ProgressColorEnd;
            float _UseProgressColorGradient;
            // --- FI MODIFICACIÓ B (URP) ---
            
            float _UseIndividualCorners;
            float _UseIndividualOffsets;
            float _GlobalCornerOffset;
            float4 _RectSize;

            float _EnableBlur; 
            float _BlurType;
            float _BlurRadius;
            float _BlurIterations;
            float _BlurDownsample;

            float _EnableShadow;
            half4 _ShadowColor;
            float4 _ShadowOffset;
            float _ShadowBlur;
            float _ShadowOpacity;
            
            float _EnableGradient;
            float _GradientType;
            half4 _GradientColorA;
            half4 _GradientColorB;
            float _GradientAngle;
            float4 _GradientRadialCenter;
            float _GradientAngularRotation;
            float _GradientRadialScale;
            
            float _EnableTexture;
            TEXTURE2D(_OverlayTexture);
            SAMPLER(sampler_OverlayTexture);
            float4 _OverlayTexture_ST;
            float4 _OverlayTexture_TexelSize;
            float4 _TextureTiling;
            float4 _TextureOffset;
            float _TextureRotation;
            float _TextureOpacity;
            float _TextureBlendMode;
            float _TextureUVMode;
            float4 _ClipRect;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                half4 color     : COLOR; 
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex       : SV_POSITION;
                half4 color         : COLOR;
                float2 texcoord     : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float2 rectPos      : TEXCOORD2;
                float4 screenPos    : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO 
            };

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v); 
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                OUT.worldPosition = v.vertex;
                OUT.vertex = TransformObjectToHClip(v.vertex.xyz);
                OUT.screenPos = ComputeScreenPos(OUT.vertex);
                
                OUT.texcoord = v.texcoord;
                
                float2 spriteUvRange = _SpriteUvs.zw - _SpriteUvs.xy;
                if (spriteUvRange.x < 0.0001) spriteUvRange.x = 1.0;
                if (spriteUvRange.y < 0.0001) spriteUvRange.y = 1.0;
                OUT.rectPos = (v.texcoord - _SpriteUvs.xy) / spriteUvRange;

                OUT.color = v.color * _Color;
                return OUT;
            }

            // --- Helper Functions ---

            float smoothAlpha(float distance)
            {
                float smoothing = fwidth(distance) * 0.707;
                return 1.0 - smoothstep(-smoothing, smoothing, distance);
            }
            
            float roundedRectSDF(float2 uv, float2 rectSize, float4 radii, float4 offsets)
            {
                float2 pos = (uv - 0.5) * rectSize;
                float2 halfSize = rectSize * 0.5;
                
                float radius;
                float offset;
                if (pos.x > 0 && pos.y > 0)      { radius = radii.y; offset = offsets.y; }
                else if (pos.x <= 0 && pos.y > 0) { radius = radii.x; offset = offsets.x; }
                else if (pos.x <= 0 && pos.y <= 0) { radius = radii.w; offset = offsets.w; }
                else                             { radius = radii.z; offset = offsets.z; }

                float maxRadius = min(halfSize.x, halfSize.y);
                radius = clamp(radius, 0.0, maxRadius);
                float radiusNormalized = radius / maxRadius;
                
                float offsetMultiplier = 1.0 - smoothstep(0.90, 1.0, radiusNormalized);
                offset = offset * offsetMultiplier;
                
                float2 d = abs(pos) - (halfSize - radius);
                float2 q = max(d, 0.0);
                float exponent = lerp(2.0, 8.0, offset);
                float outsideDistance;
                if (q.x < 0.001 && q.y < 0.001)
                {
                    outsideDistance = 0.0;
                }
                else
                {
                    outsideDistance = pow(pow(q.x, exponent) + pow(q.y, exponent), 1.0/exponent);
                }

                float insideDistance = min(max(d.x, d.y), 0.0);
                return outsideDistance + insideDistance - radius;
            }
            
            float triangleSDF(float2 uv, float2 rectSize)
            {
                float2 pos = (uv - 0.5) * rectSize;
                float2 p0 = float2(0, rectSize.y * 0.5);
                float2 p1 = float2(-rectSize.x * 0.5, -rectSize.y * 0.5);
                float2 p2 = float2(rectSize.x * 0.5, -rectSize.y * 0.5);
                float2 e0 = p1 - p0;
                float2 e1 = p2 - p1;
                float2 e2 = p0 - p2;
                
                float2 v0 = pos - p0;
                float2 v1 = pos - p1;
                float2 v2 = pos - p2;
                float2 pq0 = v0 - e0 * clamp(dot(v0, e0) / dot(e0, e0), 0.0, 1.0);
                float2 pq1 = v1 - e1 * clamp(dot(v1, e1) / dot(e1, e1), 0.0, 1.0);
                float2 pq2 = v2 - e2 * clamp(dot(v2, e2) / dot(e2, e2), 0.0, 1.0);
                float s = sign(e0.x * e2.y - e0.y * e2.x);
                float2 d = min(min(float2(dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x)),
                                     float2(dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x))),
                                     float2(dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x)));
                
                return -sqrt(d.x) * sign(d.y);
            }
            
            float regularPolygonSDF(float2 uv, float2 rectSize, int sides)
            {
                float2 pos = (uv - 0.5) * rectSize;
                float radius = min(rectSize.x, rectSize.y) * 0.5; 
                
                float angle = atan2(pos.y, pos.x);
                float sectorAngle = 6.28318530718 / float(sides);
                float sectorIndex = floor((angle + sectorAngle * 0.5) / sectorAngle); 
                float localAngle = angle - sectorIndex * sectorAngle;
                float distToCenter = length(pos); 
                float distToEdge = distToCenter * cos(abs(localAngle)) - radius * cos(sectorAngle * 0.5);
                
                return distToEdge;
            }
            
            float getShapeSDF(float2 uv, float2 rectSize)
            {
                int shapeType = (int)_ShapeType;
                if (shapeType == 3) { return triangleSDF(uv, rectSize); }
                else if (shapeType == 5) { return regularPolygonSDF(uv, rectSize, 5); }
                else if (shapeType == 6) { return regularPolygonSDF(uv, rectSize, 6); }
                else if (shapeType == 7) { return regularPolygonSDF(uv, rectSize, 6); }
                else if (shapeType == 8)
                {
                    float2 pos = (uv - 0.5) * rectSize;
                    float radius = min(rectSize.x, rectSize.y) * 0.5; 
                    return length(pos) - radius;
                }
                else
                {
                    float4 cornerRadii = _UseIndividualCorners > 0.5 ? _CornerRadii : _CornerRadii.xxxx; 
                    float4 cornerOffsets = _UseIndividualOffsets > 0.5 ? _CornerOffsets : _GlobalCornerOffset.xxxx;
                    return roundedRectSDF(uv, rectSize, cornerRadii, cornerOffsets);
                }
            }

            // [AFEGIDA] Funció auxiliar per a la màscara de progrés
            float getProgressMask(float2 uv, float2 rectSize)
            {
                if (_UseProgressBorder < 0.5) return 1.0;
                if (_ProgressValue >= 1.0) return 1.0; // Correction for 100%
                if (_ProgressValue <= 0.0) return 0.0;
                float2 center = float2(0.5, 0.5);
                float2 dir = uv - center;
                float currentAngle = atan2(dir.y, dir.x) * 57.2958; // rad to deg
                
                if (currentAngle < 0) currentAngle += 360.0;
                float startAngle = _ProgressStartAngle;
                while (startAngle < 0) startAngle += 360.0;
                while (startAngle > 360) startAngle -= 360.0;
                float progressAngle = _ProgressValue * 360.0;
                float targetAngle = startAngle + (_ProgressDirection > 0.5 ? -progressAngle : progressAngle);
                while (targetAngle < 0) targetAngle += 360.0;
                while (targetAngle > 360) targetAngle -= 360.0;
                
                if (_ProgressDirection > 0.5) // CounterClockwise
                {
                    if (startAngle < targetAngle)
                    {
                         return (currentAngle < startAngle || currentAngle > targetAngle) ? 1.0 : 0.0;
                    }
                    else
                    {
                        return (currentAngle < startAngle && currentAngle > targetAngle) ? 1.0 : 0.0;
                    }
                }
                else // Clockwise
                {
                    if (startAngle > targetAngle)
                    {
                         return (currentAngle > startAngle || currentAngle < targetAngle) ? 1.0 : 0.0;
                    }
                    else
                    {
                        return (currentAngle > startAngle && currentAngle < targetAngle) ? 1.0 : 0.0;
                    }
                }
            }
            
            half4 blendTexture(half4 base, half4 overlay, float blendMode)
            {
                half4 result = base;
                if (blendMode < 0.5) { result.rgb = base.rgb * overlay.rgb; }
                else if (blendMode < 1.5) { result.rgb = base.rgb + overlay.rgb; }
                else if (blendMode < 2.5) { result.rgb = base.rgb - overlay.rgb; }
                else if (blendMode < 3.5) 
                { 
                    result.rgb = base.rgb < 0.5 ? (2.0 * base.rgb * overlay.rgb) : (1.0 - 2.0 * (1.0 - base.rgb) * (1.0 - overlay.rgb));
                }
                else if (blendMode < 4.5) { result.rgb = 1.0 - (1.0 - base.rgb) * (1.0 - overlay.rgb); }
                else { result.rgb = overlay.rgb; }
                    
                result.a = base.a;
                return result; 
            }

            float2 transformTextureUV(float2 uv, float textureUVMode)
            {
                float2 transformedUV = uv;
                if (textureUVMode < 0.5) { transformedUV = uv; }
                else if (textureUVMode < 1.5) { transformedUV = uv; }
                else { transformedUV = frac(uv * _TextureTiling.xy); }
                
                transformedUV = transformedUV * _TextureTiling.xy + _TextureOffset.xy;
                if (abs(_TextureRotation) > 0.01)
                {
                    float cosTheta = cos(_TextureRotation);
                    float sinTheta = sin(_TextureRotation);
                    float2 center = float2(0.5, 0.5);
                    transformedUV -= center;
                    transformedUV = float2(
                        transformedUV.x * cosTheta - transformedUV.y * sinTheta,
                        transformedUV.x * sinTheta + transformedUV.y * cosTheta
                    );
                    transformedUV += center; 
                }
                
                return transformedUV;
            }
            
            // INTERNAL BLUR (5x5 Gaussian Kernel)
            half4 applyBoxBlur(TEXTURE2D_PARAM(sourceTex, samplerTex), float4 texelSize, float2 uv, float radius, int iterations)
            {
                if (radius < 0.1 || iterations < 1)
                {
                    return SAMPLE_TEXTURE2D(sourceTex, samplerTex, uv);
                }
                
                iterations = clamp(iterations, 1, 8);
                half4 result = half4(0, 0, 0, 0);
                float totalWeight = 0.0;
                float2 baseTexelSize = texelSize.xy;
                
                float3 offsets[25];
                offsets[0]  = float3(-2, -2, 0.003765); offsets[1]  = float3(-1, -2, 0.015019); offsets[2]  = float3( 0, -2, 0.023792);
                offsets[3]  = float3( 1, -2, 0.015019); offsets[4]  = float3( 2, -2, 0.003765);
                offsets[5]  = float3(-2, -1, 0.015019);
                offsets[6]  = float3(-1, -1, 0.059912); offsets[7]  = float3( 0, -1, 0.094907);
                offsets[8]  = float3( 1, -1, 0.059912);
                offsets[9]  = float3( 2, -1, 0.015019);
                offsets[10] = float3(-2,  0, 0.023792); offsets[11] = float3(-1,  0, 0.094907);
                offsets[12] = float3( 0,  0, 0.150342);
                offsets[13] = float3( 1,  0, 0.094907);
                offsets[14] = float3( 2,  0, 0.023792);
                offsets[15] = float3(-2,  1, 0.015019); offsets[16] = float3(-1,  1, 0.059912);
                offsets[17] = float3( 0,  1, 0.094907);
                offsets[18] = float3( 1,  1, 0.059912);
                offsets[19] = float3( 2,  1, 0.015019);
                offsets[20] = float3(-2,  2, 0.003765); offsets[21] = float3(-1,  2, 0.015019);
                offsets[22] = float3( 0,  2, 0.023792);
                offsets[23] = float3( 1,  2, 0.015019);
                offsets[24] = float3( 2,  2, 0.003765);
                
                [unroll]
                for (int iter = 1; iter <= 8; iter++)
                {
                    if (iter > iterations) break;
                    float iterationRadius = radius * float(iter) * 0.85;
                    float2 texelSize2 = baseTexelSize * iterationRadius;
                    float iterWeight = 1.0 / sqrt(float(iter));
                    [unroll]
                    for (int i = 0; i < 25; i++)
                    {
                        float2 sampleUV = uv + offsets[i].xy * texelSize2;
                        half4 sample = SAMPLE_TEXTURE2D(sourceTex, samplerTex, sampleUV);
                        float weight = offsets[i].z * iterWeight;
                        
                        result += sample * weight;
                        totalWeight += weight;
                    }
                }
                
                if (totalWeight > 0.0)
                {
                    result /= totalWeight;
                }
                
                return result;
            }

            // BACKGROUND BLUR (URP - usa _CameraOpaqueTexture)
            half4 applyBackgroundBlur(float2 screenUV, float radius, int iterations, int downsample)
            {
                if (radius < 0.1 || iterations < 1)
                {
                    return half4(SampleSceneColor(screenUV), 1.0);
                }
                
                iterations = clamp(iterations, 1, 8);
                float effectiveRadius = radius / max(float(downsample), 1.0);
                
                half4 result = half4(0, 0, 0, 0);
                float totalWeight = 0.0;
                float2 baseTexelSize = _ScreenParams.zw - 1.0;
                
                float3 offsets[25];
                offsets[0]  = float3(-2, -2, 0.003765); offsets[1]  = float3(-1, -2, 0.015019);
                offsets[2]  = float3( 0, -2, 0.023792);
                offsets[3]  = float3( 1, -2, 0.015019);
                offsets[4]  = float3( 2, -2, 0.003765);
                offsets[5]  = float3(-2, -1, 0.015019); offsets[6]  = float3(-1, -1, 0.059912);
                offsets[7]  = float3( 0, -1, 0.094907);
                offsets[8]  = float3( 1, -1, 0.059912);
                offsets[9]  = float3( 2, -1, 0.015019);
                offsets[10] = float3(-2,  0, 0.023792); offsets[11] = float3(-1,  0, 0.094907);
                offsets[12] = float3( 0,  0, 0.150342);
                offsets[13] = float3( 1,  0, 0.094907);
                offsets[14] = float3( 2,  0, 0.023792);
                offsets[15] = float3(-2,  1, 0.015019); offsets[16] = float3(-1,  1, 0.059912);
                offsets[17] = float3( 0,  1, 0.094907);
                offsets[18] = float3( 1,  1, 0.059912);
                offsets[19] = float3( 2,  1, 0.015019);
                offsets[20] = float3(-2,  2, 0.003765); offsets[21] = float3(-1,  2, 0.015019);
                offsets[22] = float3( 0,  2, 0.023792);
                offsets[23] = float3( 1,  2, 0.015019);
                offsets[24] = float3( 2,  2, 0.003765);
                
                [unroll]
                for (int iter = 1; iter <= 8; iter++)
                {
                    if (iter > iterations) break;
                    float iterationRadius = effectiveRadius * float(iter) * 0.85;
                    float2 texelSize = baseTexelSize * iterationRadius;
                    float iterWeight = 1.0 / sqrt(float(iter));
                    [unroll]
                    for (int i = 0; i < 25; i++)
                    {
                        float2 sampleUV = screenUV + offsets[i].xy * texelSize;
                        half3 sample = SampleSceneColor(sampleUV);
                        float weight = offsets[i].z * iterWeight;
                        
                        result.rgb += sample * weight;
                        totalWeight += weight;
                    }
                }
                
                if (totalWeight > 0.0)
                {
                    result.rgb /= totalWeight;
                }
                
                result.a = 1.0;
                return result;
            }

            float sampleSpriteAlphaWithBlur_AtlasSafe(float2 baseUV, float blurRadius)
            {
                if (blurRadius < 0.1)
                {
                    float2 inside = step(_SpriteUvs.xy, baseUV) * step(baseUV, _SpriteUvs.zw);
                    if (inside.x * inside.y < 1.0) return 0.0;
                    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, baseUV).a;
                }

                float totalAlpha = 0.0;
                float totalWeight = 0.0;
                float2 texelSize = _MainTex_TexelSize.xy * blurRadius * 0.35;
                [loop]
                for (int y = -5; y <= 5; y++)
                {
                    [loop]
                    for (int x = -5; x <= 5; x++)
                    {
                        float2 offset = float2(x, y);
                        float2 sampleUV = baseUV + offset * texelSize;
                        
                        float2 inside = step(_SpriteUvs.xy, sampleUV) * step(sampleUV, _SpriteUvs.zw);
                        float isInside = inside.x * inside.y;

                        float alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV).a * isInside;
                        
                        float distance = length(offset);
                        float weight = exp(-(distance * distance) / 25.0); 
                        
                        totalAlpha += alpha * weight;
                        totalWeight += weight;
                    }
                }
                
                if (totalWeight > 0.0)
                {
                    return totalAlpha / totalWeight;
                }
                
                return 0.0;
            }

            float getSpriteBorderMask(float2 uv, float width)
            {
                float centerAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
                if (centerAlpha < 0.01) return 0.0;
                
                float2 texelSize = _MainTex_TexelSize.xy;
                float n  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( 0,  1) * texelSize).a;
                float s  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( 0, -1) * texelSize).a;
                float e  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( 1,  0) * texelSize).a;
                float w  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-1,  0) * texelSize).a;
                float ne = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( 1,  1) * texelSize).a;
                float nw = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-1,  1) * texelSize).a;
                float se = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( 1, -1) * texelSize).a;
                float sw = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-1, -1) * texelSize).a;
                
                float edgeThreshold = 0.1;
                float minNeighbor = min(min(min(n, s), min(e, w)), min(min(ne, nw), min(se, sw)));
                float isEdge = step(minNeighbor, edgeThreshold);
                if (width > 1.0)
                {
                    float widthFactor = width * 0.5;
                    float2 wideTexelSize = texelSize * widthFactor;
                    float n2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, 1) * wideTexelSize).a;
                    float s2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, -1) * wideTexelSize).a;
                    float e2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(1, 0) * wideTexelSize).a;
                    float w2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-1, 0) * wideTexelSize).a;
                    float minNeighbor2 = min(min(n2, s2), min(e2, w2));
                    isEdge = max(isEdge, step(minNeighbor2, edgeThreshold));
                }
                    
                return isEdge * smoothstep(0.1, 0.9, centerAlpha);
            }

            half4 frag(v2f IN) : SV_Target
            {
                float2 rectSize = max(_RectSize.xy, float2(2, 2));
                float2 effectUV = IN.rectPos;
                float2 textureUV = IN.texcoord;

                bool hasSourceImage = !all(_SpriteUvs == float4(0, 0, 1, 1));
                half4 originalTexture = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, textureUV);
                float spriteAlpha = hasSourceImage ? originalTexture.a : 1.0;

                float mainDistance = getShapeSDF(effectUV, rectSize);
                float mainAlpha = smoothAlpha(mainDistance);
                half4 finalColor = half4(0, 0, 0, 0);
                // SHADOW RENDERING
                if (_EnableShadow > 0.5)
                {
                    float2 shadowOffsetUV = float2(_ShadowOffset.x, -_ShadowOffset.y) / rectSize;
                    float2 shadowUV = effectUV - shadowOffsetUV;
                    float shadowAlpha;

                    if (hasSourceImage)
                    {
                        float2 spriteUvRange = _SpriteUvs.zw - _SpriteUvs.xy;
                        float2 shadowOffsetTexture = shadowOffsetUV * spriteUvRange;
                        float2 shadowTextureUV = textureUV - shadowOffsetTexture;
                        shadowAlpha = sampleSpriteAlphaWithBlur_AtlasSafe(shadowTextureUV, _ShadowBlur);
                    }
                    else
                    {
                        float shadowDistance = getShapeSDF(shadowUV, rectSize);
                        float shadowSoftness = max(_ShadowBlur * 2.0, 1.0);
                        shadowAlpha = 1.0 - smoothstep(-shadowSoftness, shadowSoftness, shadowDistance);
                    }
                    
                    if (shadowAlpha > 0.001)
                    {
                        half4 shadowColor = _ShadowColor;
                        shadowColor.a *= _ShadowOpacity * shadowAlpha;
                        finalColor = shadowColor;
                    }
                }

                // MAIN CONTENT RENDERING
                if (mainAlpha > 0.001 && spriteAlpha > 0.01)
                {
                    half4 contentColor;
                    bool applyNormalFlow = true;
                    
                    // Background Blur (URP)
                    if (_EnableBlur > 0.5 && _BlurType >= 0.5) 
                    {
                        float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                        int iterations = (int)_BlurIterations;
                        int downsample = (int)_BlurDownsample;
                        
                        contentColor = applyBackgroundBlur(screenUV, _BlurRadius, iterations, downsample);
                        contentColor *= IN.color;
                        applyNormalFlow = false;
                    }
                    
                    // Normal flow: texture + gradient + overlay
                    if (applyNormalFlow)
                    {
                        // Internal Blur
                        if (_EnableBlur > 0.5 && _BlurType < 0.5) 
                        {
                            int iterations = (int)_BlurIterations;
                            float blurRadius = _BlurRadius / max(_BlurDownsample, 1.0);
                            
                            contentColor = applyBoxBlur(TEXTURE2D_ARGS(_MainTex, sampler_MainTex), _MainTex_TexelSize, textureUV, blurRadius, iterations);
                        }
                        else
                        {
                            contentColor = originalTexture;
                        }
                        
                        contentColor *= IN.color;
                        
                        // GRADIENT
                        if (_EnableGradient > 0.5)
                        {
                            float2 uv = effectUV;
                            float t = 0;
                                
                            if (_GradientType < 0.5)
                            {
                                float s = sin(_GradientAngle);
                                float c = cos(_GradientAngle);
                                float2x2 rotationMatrix = float2x2(c, -s, s, c);
                                float2 rotatedUV = mul(rotationMatrix, uv - 0.5);
                                t = rotatedUV.y + 0.5;
                            }
                            else if (_GradientType < 1.5)
                            {
                                float2 center = _GradientRadialCenter.xy;
                                float scale = _GradientRadialScale;
                                float2 delta = uv - center;
                                t = length(delta) * scale * 2.0;
                            }
                            else
                            {
                                float2 delta = uv - 0.5;
                                float angle = atan2(delta.y, delta.x);
                                angle -= _GradientAngularRotation;
                                t = (angle + PI) / (2.0 * PI);
                                t = frac(t);
                            }
                                
                            contentColor *= lerp(_GradientColorA, _GradientColorB, saturate(t));
                        }

                        // TEXTURE OVERLAY
                        if (_EnableTexture > 0.5)
                        {
                            float2 overlayUV = transformTextureUV(effectUV, _TextureUVMode);
                            half4 textureColor;
                            if (_EnableBlur > 0.5 && _BlurType < 0.5)
                            {
                                int iterations = (int)_BlurIterations;
                                float blurRadius = _BlurRadius / max(_BlurDownsample, 1.0);
                                textureColor = applyBoxBlur(TEXTURE2D_ARGS(_OverlayTexture, sampler_OverlayTexture), _OverlayTexture_TexelSize, overlayUV, blurRadius, iterations);
                            }
                            else
                            {
                                textureColor = SAMPLE_TEXTURE2D(_OverlayTexture, sampler_OverlayTexture, overlayUV);
                            }

                            if (textureColor.a > 0.001)
                            {
                                half4 originalColor = contentColor;
                                half4 fullEffectColor = blendTexture(originalColor, textureColor, _TextureBlendMode);
                                fullEffectColor.a = originalColor.a * textureColor.a;
                                contentColor = lerp(originalColor, fullEffectColor, _TextureOpacity);
                            }
                        }
                    }
                    
                    // SHAPE MASK AND BORDER (common for both flows)
                    half4 shapeColor = contentColor;
                    float finalCompositeAlpha = mainAlpha;
                    
                    if (_BorderWidth > 0.0)
                    {
                        float borderMask;
                        if (hasSourceImage)
                        {
                            borderMask = getSpriteBorderMask(textureUV, _BorderWidth);
                        }
                        else
                        {
                            float halfWidth = _BorderWidth * 0.5;
                            float contentMask = smoothAlpha(mainDistance + halfWidth);
                            finalCompositeAlpha = smoothAlpha(mainDistance - halfWidth);
                            borderMask = saturate(finalCompositeAlpha - contentMask);
                        }

                        // [AFEGIT] Aplicar la màscara de progrés
                        if (_UseProgressBorder > 0.5)
                        {
                            // El 'IN.rectPos' del shader de UIEffectsPro és el mateix que el del shader de ProceduralUITool
                            float progressMask = getProgressMask(IN.rectPos, rectSize);
                            borderMask *= progressMask;
                        }
                        
                        // --- Border color calculation with gradient + opacity fix ---
                        half4 finalBorderColor = _BorderColor;
                        
                        // Border gradient interpolation (Linear / Radial / Angular)
                        if (_UseBorderGradient > 0.5)
                        {
                            float gradT = 0;
                            if (_BorderGradientType < 0.5) // Linear
                            {
                                float s = sin(_BorderGradientAngle);
                                float c = cos(_BorderGradientAngle);
                                float2x2 rotMat = float2x2(c, -s, s, c);
                                float2 rotatedUV = mul(rotMat, effectUV - 0.5);
                                gradT = saturate(rotatedUV.y + 0.5);
                            }
                            else if (_BorderGradientType < 1.5) // Radial
                            {
                                float2 center = _BorderGradientRadialCenter.xy;
                                float scale = _BorderGradientRadialScale;
                                float2 delta = effectUV - center;
                                gradT = saturate(length(delta) * scale * 2.0);
                            }
                            else // Angular
                            {
                                float2 delta = effectUV - 0.5;
                                float angle = atan2(delta.y, delta.x);
                                angle -= _BorderGradientAngularRotation;
                                gradT = frac((angle + 3.14159265359) / (2.0 * 3.14159265359));
                            }
                            finalBorderColor = lerp(_BorderColor, _BorderColorB, gradT);
                        }
                        
                        // Progress color gradient overrides
                        if (_UseProgressBorder > 0.5 && _UseProgressColorGradient > 0.5){
                            finalBorderColor = lerp(_ProgressColorStart, _ProgressColorEnd, _ProgressValue);
                        }
                        
                        // Blend RGB and alpha (opacity fix)
                        shapeColor.rgb = lerp(contentColor.rgb, finalBorderColor.rgb, borderMask);
                        shapeColor.a = lerp(contentColor.a, finalBorderColor.a, borderMask) * finalCompositeAlpha;
                        // --- End border color ---
                    }
                    
                    // Alpha already set in border block above, only apply when no border
                    if (_BorderWidth <= 0.0)
                    {
                        shapeColor.a = contentColor.a * finalCompositeAlpha;
                    }
                    finalColor.rgb = lerp(finalColor.rgb, shapeColor.rgb, shapeColor.a);
                    finalColor.a = shapeColor.a + finalColor.a * (1.0 - shapeColor.a);
                }

                #ifdef UNITY_UI_CLIP_RECT
                    float2 clipPos = IN.worldPosition.xy;
                    float2 inside = step(_ClipRect.xy, clipPos) * step(clipPos, _ClipRect.zw);
                    finalColor.a *= inside.x * inside.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip(finalColor.a - 0.001);
                #endif

                return finalColor;
            }
            ENDHLSL
        }
    }
    
    Fallback "Universal Render Pipeline/Unlit"
}