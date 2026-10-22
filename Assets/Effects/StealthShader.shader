// Sci-fi stealth field for sprites: camouflages the sprite into whatever is rendered behind it,
// overlaid with darkened hexagon seams, a noisy phase in/out and an imperfect, wobbling blend.
// The background comes from URP's 2D Camera Sorting Layers Texture (Renderer2D asset), so the sprite
// must sort above the layer that texture is captured up to. The texture is already lit, so this pass is unlit.
// Everything is procedural. The hex grid is anchored to the sprite's pivot and follows its rotation, but is measured
// in world units so the transform's scale (non-square player, crouch squash) never stretches the hexagons.
Shader "Custom/StealthShader"
{
    Properties
    {
        [MainTexture] _MainTex("Sprite Texture", 2D) = "white" {}
        [MainColor] _Color("Tint", Color) = (1, 1, 1, 1)

        [Header(Blend)]
        _Blend("Max Blend To Background", Range(0, 1)) = 0.92
        _BackgroundTint("Background Tint", Color) = (1, 1, 1, 1)
        [Toggle] _KeepEdges("Keep Sprite Shading", Float) = 0

        [Header(Phase)]
        _Phase("Phase (0 visible, 1 cloaked)", Range(0, 1)) = 0
        _PhaseNoiseScale("Phase Noise Scale", Float) = 6
        _PhaseCellWeight("Phase Per-Cell Weight (0 noise, 1 hexagons)", Range(0, 1)) = 0.6
        _PhaseEdgeWidth("Phase Edge Width", Range(0.01, 0.5)) = 0.15
        [HDR] _PhaseEdgeColor("Phase Edge Color", Color) = (0.4, 1.6, 2.4, 1)

        [Header(Hexagons)]
        _HexScale("Hex Cells Per Unit", Float) = 8
        _SeamWidth("Seam Width", Range(0.001, 0.2)) = 0.04
        _SeamTint("Seam Tint", Color) = (0, 0, 0, 1)
        _SeamTintAmount("Seam Tint Amount", Range(0, 1)) = 0.6
        _SeamPulseSpeed("Seam Pulse Speed", Float) = 2
        _CellFlickerRate("Cell Flicker Rate", Float) = 12
        _CellFlickerAmount("Cell Flicker Amount", Range(0, 1)) = 0.5

        [Header(Imperfection)]
        _DistortStrength("Noise Distortion (screen UV)", Range(0, 0.02)) = 0.004
        _DistortScale("Noise Distortion Scale", Float) = 3
        _DistortSpeed("Noise Distortion Speed", Float) = 1.5
        _WobbleStrength("Sine Wobble (screen UV)", Range(0, 0.02)) = 0.003
        _WobbleFrequency("Sine Wobble Frequency", Float) = 10
        _WobbleSpeed("Sine Wobble Speed", Float) = 4
        _ChromaticSplit("Chromatic Split (screen UV)", Range(0, 0.01)) = 0.0015
        _BlendJitter("Blend Jitter", Range(0, 1)) = 0.25
        _BlendJitterSpeed("Blend Jitter Speed", Float) = 3
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                // Object-space position with the transform's scale applied: sprite-anchored, world-sized.
                float2 fieldPos : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            // Written by the 2D Renderer when "Camera Sorting Layers Texture" is enabled.
            TEXTURE2D(_CameraSortingLayerTexture);
            SAMPLER(sampler_CameraSortingLayerTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _BackgroundTint;
                half _Blend;
                half _KeepEdges;

                half _Phase;
                float _PhaseNoiseScale;
                half _PhaseCellWeight;
                half _PhaseEdgeWidth;
                half4 _PhaseEdgeColor;

                float _HexScale;
                half _SeamWidth;
                half4 _SeamTint;
                half _SeamTintAmount;
                float _SeamPulseSpeed;
                float _CellFlickerRate;
                half _CellFlickerAmount;

                float _DistortStrength;
                float _DistortScale;
                float _DistortSpeed;
                float _WobbleStrength;
                float _WobbleFrequency;
                float _WobbleSpeed;
                float _ChromaticSplit;
                half _BlendJitter;
                float _BlendJitterSpeed;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Smooth value noise in [0, 1].
            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash21(i), Hash21(i + float2(1, 0)), u.x),
                            lerp(Hash21(i + float2(0, 1)), Hash21(i + float2(1, 1)), u.x), u.y);
            }

            // Pointy-top hex grid with unit spacing between neighbouring cell centres.
            // xy = offset from the cell centre, zw = cell id.
            static const float2 HexStep = float2(1.0, 1.7320508);

            float4 HexCell(float2 p)
            {
                float4 centres = floor(float4(p, p - float2(0.5, 1.0)) / HexStep.xyxy) + 0.5;
                float4 offsets = float4(p - centres.xy * HexStep, p - (centres.zw + 0.5) * HexStep);
                return dot(offsets.xy, offsets.xy) < dot(offsets.zw, offsets.zw)
                    ? float4(offsets.xy, centres.xy)
                    : float4(offsets.zw, centres.zw + 0.5);
            }

            // Distance from a cell-local offset to the nearest hexagon edge; 0 on the seam, 0.5 at the centre.
            float HexEdgeDistance(float2 offset)
            {
                offset = abs(offset);
                return 0.5 - max(dot(offset, normalize(HexStep)), offset.x);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = vertexInput.positionCS;
                OUT.screenPos = vertexInput.positionNDC;
                float4x4 objectToWorld = GetObjectToWorldMatrix();
                float2 objectScale = float2(length(objectToWorld._m00_m10_m20), length(objectToWorld._m01_m11_m21));
                OUT.fieldPos = IN.positionOS.xy * objectScale;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
                float time = _Time.y;

                // Hexagons anchored to the sprite.
                float2 hexPos = IN.fieldPos * _HexScale;
                float4 hex = HexCell(hexPos);
                float cellRandom = Hash21(hex.zw);
                float edgeDistance = HexEdgeDistance(hex.xy);

                // Phase: each pixel switches on once _Phase passes its noise threshold, so the cloak spreads in patches.
                float phaseNoise = lerp(ValueNoise(IN.fieldPos * _PhaseNoiseScale), cellRandom, _PhaseCellWeight);
                half cloak = smoothstep(phaseNoise, phaseNoise + _PhaseEdgeWidth, _Phase * (1.0 + _PhaseEdgeWidth));
                half phaseFront = saturate(cloak * (1.0 - cloak) * 4.0);

                // Imperfection: noisy and sinusoidal offsets on the background sample, only where cloaked.
                float2 distort = float2(
                    ValueNoise(IN.fieldPos * _DistortScale + float2(time * _DistortSpeed, 0.0)),
                    ValueNoise(IN.fieldPos * _DistortScale + float2(17.3, -time * _DistortSpeed))) * 2.0 - 1.0;
                float wobble = sin(IN.fieldPos.y * _WobbleFrequency + time * _WobbleSpeed);
                float2 uvOffset = (distort * _DistortStrength + float2(wobble * _WobbleStrength, 0.0)) * cloak;

                float2 screenUV = IN.screenPos.xy / IN.screenPos.w + uvOffset;
                float2 split = float2(_ChromaticSplit * cloak, 0.0);
                half3 background = half3(
                    SAMPLE_TEXTURE2D(_CameraSortingLayerTexture, sampler_CameraSortingLayerTexture, screenUV + split).r,
                    SAMPLE_TEXTURE2D(_CameraSortingLayerTexture, sampler_CameraSortingLayerTexture, screenUV).g,
                    SAMPLE_TEXTURE2D(_CameraSortingLayerTexture, sampler_CameraSortingLayerTexture, screenUV - split).b)
                    * _BackgroundTint.rgb;

                // Optionally keep the sprite's own brightness so its shape stays readable while camouflaged.
                half luma = dot(sprite.rgb, half3(0.299, 0.587, 0.114));
                background *= lerp(1.0h, luma * 2.0h, _KeepEdges);

                // Cells drift slightly out of sync, briefly letting the sprite show through.
                half jitter = ValueNoise(float2(time * _BlendJitterSpeed, cellRandom * 37.0));
                half blend = cloak * _Blend * (1.0h - _BlendJitter * jitter);
                half3 color = lerp(sprite.rgb, background, blend);

                // Seams are the same background, tinted (towards black by default), so they read as panel gaps.
                // Their strength pulses across cells and flickers per cell in steps.
                half seam = 1.0h - smoothstep(0.0, _SeamWidth, edgeDistance);
                half pulse = 0.75h + 0.25h * sin(time * _SeamPulseSpeed + cellRandom * TWO_PI);
                half flicker = lerp(1.0h, Hash21(hex.zw + floor(time * _CellFlickerRate)), _CellFlickerAmount);
                half3 seamColor = lerp(background, _SeamTint.rgb, _SeamTintAmount);
                color = lerp(color, seamColor, seam * pulse * flicker * cloak);

                color += _PhaseEdgeColor.rgb * phaseFront;

                return half4(color, sprite.a);
            }
            ENDHLSL
        }
    }
}
