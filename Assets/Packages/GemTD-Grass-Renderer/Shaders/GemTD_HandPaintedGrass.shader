Shader "GemTD/Hand Painted Grass"
{
    Properties
    {
        _GrassRootColor("Grass Root Color", Color) = (0.10, 0.24, 0.06, 1)
        _GrassBodyColor("Grass Body Color", Color) = (0.22, 0.45, 0.10, 1)
        _GrassTipColor("Grass Tip Color", Color) = (0.42, 0.68, 0.18, 1)
        _GrassColorNoiseScale("Grass Color Noise Scale", Float) = 3.0
        _GrassColorNoiseStrength("Grass Color Noise Strength", Range(0, 1)) = 0.12
        _GrassWindDirection("Grass Wind Direction", Vector) = (1, 0.35, 0, 0)
        _GrassWindScale("Grass Wind Scale", Float) = 2.5
        _GrassWindSpeed("Grass Wind Speed", Float) = 0.35
        _GrassWindStrength("Grass Wind Strength", Float) = 0.08
        _GrassReceiveShadows("Grass Receive Shadows", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

        float3 _LightDirection;
        float3 _LightPosition;

        CBUFFER_START(UnityPerMaterial)
            half4 _GrassRootColor;
            half4 _GrassBodyColor;
            half4 _GrassTipColor;
            float _GrassColorNoiseScale;
            float _GrassColorNoiseStrength;
            float4 _GrassWindDirection;
            float _GrassWindScale;
            float _GrassWindSpeed;
            float _GrassWindStrength;
            float _GrassReceiveShadows;
        CBUFFER_END

        #include "Assets/Packages/GemTD-Grass-Renderer/Shaders/Includes/GemTDGrassWind.hlsl"

        struct GrassAttributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct GrassVaryings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float3 positionWS : TEXCOORD1;
            float3 clumpOriginWS : TEXCOORD2;
            float3 normalWS : TEXCOORD3;
            float fogFactor : TEXCOORD4;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        struct GrassForwardFragmentInput
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float3 positionWS : TEXCOORD1;
            float3 clumpOriginWS : TEXCOORD2;
            float3 normalWS : TEXCOORD3;
            float fogFactor : TEXCOORD4;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
            FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC;
        };

        struct GrassDepthVaryings
        {
            float4 positionCS : SV_POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        float GemTDGrassHash(float2 value)
        {
            value = frac(value * float2(0.1031, 0.1030));
            value += dot(value, value.yx + 33.33);
            return frac((value.x + value.y) * value.x);
        }

        float GemTDGrassWorldNoise(float2 positionXZ)
        {
            float scale = max(_GrassColorNoiseScale, 0.01);
            float2 noisePosition = positionXZ / scale;
            float2 cell = floor(noisePosition);
            float2 blend = smoothstep(0.0, 1.0, frac(noisePosition));
            float bottomLeft = GemTDGrassHash(cell);
            float bottomRight = GemTDGrassHash(cell + float2(1.0, 0.0));
            float topLeft = GemTDGrassHash(cell + float2(0.0, 1.0));
            float topRight = GemTDGrassHash(cell + float2(1.0, 1.0));
            float bottom = lerp(bottomLeft, bottomRight, blend.x);
            float top = lerp(topLeft, topRight, blend.x);
            return lerp(bottom, top, blend.y);
        }

        half3 GemTDGrassGradient(float height01)
        {
            float height = saturate(height01);
            if (height < 0.5)
                return lerp(_GrassRootColor.rgb, _GrassBodyColor.rgb, height * 2.0);

            return lerp(_GrassBodyColor.rgb, _GrassTipColor.rgb, (height - 0.5) * 2.0);
        }

        GrassVaryings GemTDGrassVertex(GrassAttributes input)
        {
            GrassVaryings output = (GrassVaryings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            float3 preWindPositionWS = TransformObjectToWorld(input.positionOS.xyz);
            float3 positionWS = ApplyGemTDGrassWind(preWindPositionWS, saturate(input.uv.y));
            output.positionCS = TransformWorldToHClip(positionWS);
            output.positionWS = positionWS;
            output.clumpOriginWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.uv = input.uv;
            output.fogFactor = ComputeFogFactor(output.positionCS.z);
            return output;
        }

        half4 GemTDGrassFragment(GrassForwardFragmentInput input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float3 normalWS = normalize(input.normalWS);
            normalWS *= IS_FRONT_VFACE(input.frontFace, 1.0, -1.0);

            float colorNoise = GemTDGrassWorldNoise(input.positionWS.xz);
            colorNoise = lerp(1.0, colorNoise * 2.0, saturate(_GrassColorNoiseStrength));
            float clumpBrightness = lerp(
                1.0 - 0.08,
                1.0 + 0.08,
                GemTDGrassHash(input.clumpOriginWS.xz));
            half3 albedo = GemTDGrassGradient(input.uv.y) * colorNoise * clumpBrightness;

            Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
            half receivesShadows = saturate(_GrassReceiveShadows);
            half shadowAttenuation = lerp(1.0h, mainLight.shadowAttenuation, receivesShadows);
            half halfLambert = saturate(dot(normalWS, mainLight.direction) * 0.5h + 0.5h);
            half3 ambient = SampleSH(normalWS);
            half3 lighting = ambient * 0.45h +
                             mainLight.color * (0.55h + halfLambert * 0.25h) * shadowAttenuation;
            half3 color = albedo * max(lighting, half3(0.18h, 0.18h, 0.18h));
            color = MixFog(color, input.fogFactor);
            return half4(color, 1.0h);
        }

        GrassDepthVaryings GemTDGrassDepthVertex(GrassAttributes input)
        {
            GrassDepthVaryings output = (GrassDepthVaryings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
            positionWS = ApplyGemTDGrassWind(positionWS, saturate(input.uv.y));
            output.positionCS = TransformWorldToHClip(positionWS);
            return output;
        }

        half4 GemTDGrassDepthFragment(GrassDepthVaryings input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);
            return 0.0h;
        }

        float4 GemTDGrassShadowPositionHClip(GrassAttributes input)
        {
            UNITY_SETUP_INSTANCE_ID(input);
            float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
            positionWS = ApplyGemTDGrassWind(positionWS, saturate(input.uv.y));
            float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

            float4 positionCS = TransformWorldToHClip(
                ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
            return positionCS;
        }

        GrassDepthVaryings GemTDGrassShadowVertex(GrassAttributes input)
        {
            GrassDepthVaryings output = (GrassDepthVaryings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = GemTDGrassShadowPositionHClip(input);
            return output;
        }
        ENDHLSL

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex GemTDGrassVertex
            #pragma fragment GemTDGrassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            Cull Off
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex GemTDGrassShadowVertex
            #pragma fragment GemTDGrassDepthFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            Cull Off
            ZWrite On
            ZTest LEqual
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex GemTDGrassDepthVertex
            #pragma fragment GemTDGrassDepthFragment
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
