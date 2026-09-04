Shader "CoastRun/ToonLit"
{
    Properties
    {
        // [MainTexture]/[MainColor] route Material.mainTexture / .color / .mainTextureScale
        // to these properties. Without them Unity looks for _MainTex, which this shader
        // does not have — every caller that set a texture scale logged an error and the
        // texture never applied.
        [MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
        [MainColor]   _BaseColor ("Color", Color) = (1,1,1,1)
        _ShadowColor ("Shadow Tint", Color) = (0.35, 0.48, 0.62, 1)
        _ShadowThreshold ("Shadow Threshold", Range(0,1)) = 0.45
        _ShadowSoftness ("Shadow Softness", Range(0.001,0.3)) = 0.08
        _Smoothness ("Smoothness", Range(0,1)) = 0.05
        _CurveWeight ("Curved World Weight", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "CoastCurve.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _ShadowColor;
            half _ShadowThreshold;
            half _ShadowSoftness;
            half _Smoothness;
            half _CurveWeight;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 ws = CoastCurveWorld(TransformObjectToWorld(IN.positionOS.xyz), _CurveWeight);
                OUT.positionCS = TransformWorldToHClip(ws);
                OUT.positionWS = ws;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                float3 n = normalize(IN.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                half ndl = dot(n, mainLight.direction) * mainLight.shadowAttenuation;
                half shade = smoothstep(_ShadowThreshold - _ShadowSoftness, _ShadowThreshold + _ShadowSoftness, ndl);
                half3 lit = lerp(_ShadowColor.rgb * albedo.rgb, albedo.rgb * mainLight.color, shade);
                lit = MixFog(lit, IN.fogFactor);
                return half4(lit, albedo.a);
            }
            ENDHLSL
        }

        // Shadows must bend with the geometry, or a house that curves off to the left
        // still drops its shadow where the straight house would have stood.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vertShadow(Attributes IN)
            {
                Varyings OUT;
                float3 ws = CoastCurveWorld(TransformObjectToWorld(IN.positionOS.xyz), _CurveWeight);
                float3 n = TransformObjectToWorldNormal(IN.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDir = normalize(_LightPosition - ws);
            #else
                float3 lightDir = _LightDirection;
            #endif
                float4 cs = TransformWorldToHClip(ApplyShadowBias(ws, n, lightDir));
            #if UNITY_REVERSED_Z
                cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
            #else
                cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                OUT.positionCS = cs;
                return OUT;
            }

            half4 fragShadow(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vertDepth(Attributes IN)
            {
                Varyings OUT;
                float3 ws = CoastCurveWorld(TransformObjectToWorld(IN.positionOS.xyz), _CurveWeight);
                OUT.positionCS = TransformWorldToHClip(ws);
                return OUT;
            }

            half4 fragDepth(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack Off
}
