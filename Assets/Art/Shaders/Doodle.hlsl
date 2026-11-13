#ifndef GEMTD_DOODLE_INCLUDED
#define GEMTD_DOODLE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _OutlineColor;
    float _OutlineThickness;
    float _WobbleAmount;
    float _WobbleSpeed;
    float _Desaturate;
    float _EdgeDarkening;
CBUFFER_END

float DoodleHash(float3 p)
{
    p = frac(p * 0.3183099 + float3(0.1, 0.13, 0.17));
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float3 ApplyDoodleWobble(float3 positionOS, float3 normalOS)
{
    float3 positionWS = TransformObjectToWorld(positionOS);
    float t = _Time.y * _WobbleSpeed;
    float n = DoodleHash(positionWS * 8.0 + t) * 2.0 - 1.0;
    return positionOS + normalOS * (n * _WobbleAmount);
}

float DoodleScribble(float3 positionWS)
{
    float t = _Time.y * _WobbleSpeed;
    return DoodleHash(positionWS * 20.0 + t);
}

half3 DoodleDesaturate(half3 color)
{
    half luma = dot(color, half3(0.299, 0.587, 0.114));
    return lerp(color, luma.xxx, saturate(_Desaturate));
}

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float3 positionWS : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings DoodleFillVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 posOS = ApplyDoodleWobble(input.positionOS.xyz, input.normalOS);
    VertexPositionInputs posInputs = GetVertexPositionInputs(posOS);
    VertexNormalInputs nrmInputs = GetVertexNormalInputs(input.normalOS);

    output.positionCS = posInputs.positionCS;
    output.positionWS = posInputs.positionWS;
    output.normalWS = nrmInputs.normalWS;
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    return output;
}

Varyings DoodleOutlineVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 posOS = ApplyDoodleWobble(input.positionOS.xyz, input.normalOS);
    float3 positionWS = TransformObjectToWorld(posOS);
    float scribble = DoodleScribble(positionWS);
    float thickness = _OutlineThickness * (0.65 + 0.35 * scribble);
    posOS += normalize(input.normalOS) * thickness;

    VertexPositionInputs posInputs = GetVertexPositionInputs(posOS);
    output.positionCS = posInputs.positionCS;
    output.positionWS = posInputs.positionWS;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    return output;
}

half4 DoodleOutlineFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    return _OutlineColor;
}

half4 DoodleFillFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
    float3 normalWS = normalize(input.normalWS);
    float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

    Light light = GetMainLight();
    float wrap = saturate(dot(normalWS, light.direction) * 0.5 + 0.5);
    half3 lit = albedo.rgb * (wrap * light.color + half3(0.35, 0.35, 0.35));

    float2 uvEdge = abs(input.uv * 2.0 - 1.0);
    float uvRim = saturate(pow(max(uvEdge.x, uvEdge.y), 6.0));
    float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), 3.0);
    lit *= 1.0 - saturate(_EdgeDarkening) * max(uvRim, fresnel * 0.35);

    lit = DoodleDesaturate(lit);
    return half4(lit, albedo.a);
}

Varyings DoodleDepthVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 posOS = ApplyDoodleWobble(input.positionOS.xyz, input.normalOS);
    VertexPositionInputs posInputs = GetVertexPositionInputs(posOS);
    output.positionCS = posInputs.positionCS;
    output.positionWS = posInputs.positionWS;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    return output;
}

half DoodleDepthFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    return input.positionCS.z;
}

half4 DoodleDepthNormalsFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
    return half4(normalWS * 0.5 + 0.5, 0.0);
}

float3 _LightDirection;
float3 _LightPosition;

float4 DoodleShadowPositionHClip(Attributes input)
{
    float3 posOS = ApplyDoodleWobble(input.positionOS.xyz, input.normalOS);
    float3 positionWS = TransformObjectToWorld(posOS);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif
    return positionCS;
}

Varyings DoodleShadowVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = DoodleShadowPositionHClip(input);
    return output;
}

half4 DoodleShadowFragment(Varyings input) : SV_Target
{
    return 0;
}

#endif
