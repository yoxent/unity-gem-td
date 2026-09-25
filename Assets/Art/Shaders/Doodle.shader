Shader "GemTD/Doodle"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1, 1, 1, 1)
        _OutlineColor("Outline Color", Color) = (0.05, 0.05, 0.05, 1)
        _OutlineThickness("Outline Thickness", Float) = 0.03
        _WobbleAmount("Wobble Amount", Float) = 0.012
        _WobbleSpeed("Wobble Speed", Float) = 0
        _Desaturate("Desaturate", Range(0, 1)) = 0
        _EdgeDarkening("Edge Darkening", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "DoodleOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DoodleOutlineVertex
            #pragma fragment DoodleOutlineFragment
            #pragma multi_compile_instancing
            #include "Doodle.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DoodleFill"
            Tags { "LightMode" = "UniversalForwardOnly" }
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DoodleFillVertex
            #pragma fragment DoodleFillFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #include "Doodle.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DoodleShadowVertex
            #pragma fragment DoodleShadowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Doodle.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DoodleDepthVertex
            #pragma fragment DoodleDepthFragment
            #pragma multi_compile_instancing
            #include "Doodle.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DoodleDepthVertex
            #pragma fragment DoodleDepthNormalsFragment
            #pragma multi_compile_instancing
            #include "Doodle.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode" = "DepthNormalsOnly" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DoodleDepthVertex
            #pragma fragment DoodleDepthNormalsFragment
            #pragma multi_compile_instancing
            #include "Doodle.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
