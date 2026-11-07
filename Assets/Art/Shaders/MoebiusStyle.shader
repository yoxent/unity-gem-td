Shader "Hidden/GemTD/MoebiusStyle"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "Moebius"

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            TEXTURE2D(_HatchTex);
            SAMPLER(sampler_HatchTex);

            float4 _OutlineColor;
            float _OutlineThickness;
            float _DepthThreshold;
            float _NormalThreshold;
            float _HatchTiling;
            float _HatchIntensity;
            float _KeepBrightCutoff;
            float _Desaturate;

            float SobelDepth(float2 uv, float2 texel)
            {
                float tl = LinearEyeDepth(SampleSceneDepth(uv + texel * float2(-1, 1)), _ZBufferParams);
                float t  = LinearEyeDepth(SampleSceneDepth(uv + texel * float2( 0, 1)), _ZBufferParams);
                float tr = LinearEyeDepth(SampleSceneDepth(uv + texel * float2( 1, 1)), _ZBufferParams);
                float l  = LinearEyeDepth(SampleSceneDepth(uv + texel * float2(-1, 0)), _ZBufferParams);
                float r  = LinearEyeDepth(SampleSceneDepth(uv + texel * float2( 1, 0)), _ZBufferParams);
                float bl = LinearEyeDepth(SampleSceneDepth(uv + texel * float2(-1,-1)), _ZBufferParams);
                float b  = LinearEyeDepth(SampleSceneDepth(uv + texel * float2( 0,-1)), _ZBufferParams);
                float br = LinearEyeDepth(SampleSceneDepth(uv + texel * float2( 1,-1)), _ZBufferParams);

                float gx = -tl - 2.0 * l - bl + tr + 2.0 * r + br;
                float gy = -tl - 2.0 * t - tr + bl + 2.0 * b + br;
                return sqrt(gx * gx + gy * gy);
            }

            float SobelNormal(float2 uv, float2 texel)
            {
                float3 n = SampleSceneNormals(uv);
                float3 r = SampleSceneNormals(uv + texel * float2(1, 0));
                float3 u = SampleSceneNormals(uv + texel * float2(0, 1));
                float dx = 1.0 - saturate(dot(n, r));
                float dy = 1.0 - saturate(dot(n, u));
                return max(dx, dy);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float2 texel = float2(_CameraDepthTexture_TexelSize.x, _CameraDepthTexture_TexelSize.y);
                float depthEdge = saturate(SobelDepth(uv, texel) * _DepthThreshold);
                float normalEdge = saturate(SobelNormal(uv, texel) * _NormalThreshold);
                float edge = saturate(max(depthEdge, normalEdge) * _OutlineThickness);

                half luma = dot(color.rgb, half3(0.299, 0.587, 0.114));
                half3 grey = luma.xxx;
                color.rgb = lerp(color.rgb, grey, saturate(_Desaturate));
                luma = dot(color.rgb, half3(0.299, 0.587, 0.114));

                float2 hatchUv = uv * _HatchTiling * _ScreenParams.xy / 512.0;
                half3 hatchSample = SAMPLE_TEXTURE2D(_HatchTex, sampler_HatchTex, hatchUv).rgb;
                half hatch = 1.0;
                hatch = luma < 0.72 ? min(hatch, hatchSample.b) : hatch;
                hatch = luma < 0.48 ? min(hatch, hatchSample.g) : hatch;
                hatch = luma < 0.28 ? min(hatch, hatchSample.r) : hatch;
                half keepBright = step(_KeepBrightCutoff, luma);
                half hatchMix = (1.0 - keepBright) * _HatchIntensity;
                color.rgb *= lerp(1.0, hatch, hatchMix);

                color.rgb = lerp(color.rgb, _OutlineColor.rgb, edge);
                return color;
            }
            ENDHLSL
        }
    }
}
