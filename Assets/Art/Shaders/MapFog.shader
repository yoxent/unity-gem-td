// URP sheet for the playfield fog. The blur matches AOS FogPlane:
// a clamped mask, a 3x3 gaussian, then a tint. The built-in FogPlane
// shader does not render in this project's Universal pipeline.
Shader "GemTD/MapFog"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (0, 0, 0, 1)
        _BlurOffset("BlurOffset", Range(0, 10)) = 1.25
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent-100"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "MapFog"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _BlurOffset;
            CBUFFER_END

            float4 _MainTex_TexelSize;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 SampleMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 texel = _BlurOffset * _MainTex_TexelSize.xy;
                half4 color =
                    SampleMask(input.uv + float2(-texel.x, -texel.y)) +
                    SampleMask(input.uv + float2(0, -texel.y)) * 2 +
                    SampleMask(input.uv + float2(texel.x, -texel.y)) +
                    SampleMask(input.uv + float2(-texel.x, 0)) * 2 +
                    SampleMask(input.uv) * 4 +
                    SampleMask(input.uv + float2(texel.x, 0)) * 2 +
                    SampleMask(input.uv + float2(-texel.x, texel.y)) +
                    SampleMask(input.uv + float2(0, texel.y)) * 2 +
                    SampleMask(input.uv + float2(texel.x, texel.y));
                color /= 16;
                return color * (half4)_Color;
            }
            ENDHLSL
        }
    }
}
