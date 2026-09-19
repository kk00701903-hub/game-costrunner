// 146차 radial curve (CoastCurve.hlsl 갱신 후 재임포트)
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
        // 143차: 동물의 숲풍 소프트 룩(마을) — 전역 _CoastSoft(0~1) 로 켠다. 하프 램버트 + 넓은 램프 + 그림자 바닥 + SH 앰비언트 + 림
        _RimColor ("Rim (soft look)", Color) = (0.92, 0.96, 1.0, 1)
        // 144차: 디테일 맵(회색 0.5 중심) — 큰 텍스처 위에 미세 결을 곱한다. _DetailStrength 0 이면 무시
        _DetailMap ("Detail (gray)", 2D) = "gray" {}
        _DetailScale ("Detail tiles per meter", Float) = 0.5
        _DetailStrength ("Detail strength", Range(0,1)) = 0
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
        TEXTURE2D(_DetailMap);
        SAMPLER(sampler_DetailMap);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _ShadowColor;
            half _ShadowThreshold;
            half _ShadowSoftness;
            half _Smoothness;
            half _CurveWeight;
            half4 _RimColor;
            half _DetailScale;
            half _DetailStrength;
        CBUFFER_END
        // 전역(코드에서 Shader.SetGlobal*): 마을에서만 1
        half _CoastSoft;            // 0 = 기존 러닝 룩, 1 = 소프트 룩
        half4 _CoastSoftShadowTint; // 그림자 곱색(따뜻한 라벤더)
        half _CoastSoftFloor;       // 그림자 바닥(0.55: 그늘도 밝게)
        half _CoastSoftAmbient;     // SH 앰비언트 가중치
        half _CoastSoftRim;         // 림 세기
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
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
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
                if (_DetailStrength > 0.001)
                {
                    // 월드 XZ 기준 타일(메시 UV 와 무관) — 두 스케일을 섞어 반복 무늬가 안 보이게
                    half d1 = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, IN.positionWS.xz * _DetailScale).r;
                    half d2 = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, IN.positionWS.xz * _DetailScale * 0.23 + 0.37).r;
                    half d = (d1 * 0.65 + d2 * 0.35) - 0.5;
                    albedo.rgb *= 1.0 + d * _DetailStrength * 2.0;
                }
                float3 n = normalize(IN.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                half soft = saturate(_CoastSoft);
                // 기존(러닝): 램버트 × 그림자, 좁은 램프
                half ndl = dot(n, mainLight.direction) * mainLight.shadowAttenuation;
                half shadeHard = smoothstep(_ShadowThreshold - _ShadowSoftness, _ShadowThreshold + _ShadowSoftness, ndl);
                // 소프트(마을): 하프 램버트(0.5·N·L+0.5) × 부드러운 그림자(바닥값) → 넓은 램프(±0.22)
                half halfL = dot(n, mainLight.direction) * 0.5 + 0.5;
                half shAtt = lerp(_CoastSoftFloor, 1.0, mainLight.shadowAttenuation);
                half shadeSoft = smoothstep(0.48 - 0.22, 0.48 + 0.22, halfL * shAtt);
                half shade = lerp(shadeHard, shadeSoft, soft);
                half3 shadowTint = lerp(_ShadowColor.rgb, _CoastSoftShadowTint.rgb, soft);
                // 소프트 룩은 앰비언트·보조광·림이 더해지므로 주광 에너지를 낮춰 총합이 1 을 넘지 않게
                // 146차: SSAO — 구석·맞닿는 곳을 살짝 어둡게(간접광은 그대로, 직사광은 DirectLightingStrength 만큼)
                half aoInd = 1.0, aoDir = 1.0;
                #if defined(_SCREEN_SPACE_OCCLUSION)
                {
                    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(IN.positionCS));
                    aoInd = ao.indirectAmbientOcclusion; aoDir = ao.directAmbientOcclusion;
                }
                #endif
                half3 lit = lerp(shadowTint * albedo.rgb * aoInd, albedo.rgb * mainLight.color * lerp(1.0, 0.82, soft) * aoDir, shade);
                // SH 앰비언트(하늘/지평/땅 3색) — 그늘도 뿌옇게 살아 있게
                lit += SampleSH(n) * albedo.rgb * (_CoastSoftAmbient * soft) * aoInd;
                // 보조광(남쪽 필 라이트 등) — 같은 하프 램버트
                #ifdef _ADDITIONAL_LIGHTS
                uint cnt = GetAdditionalLightsCount();
                for (uint li = 0u; li < cnt; li++)
                {
                    Light l = GetAdditionalLight(li, IN.positionWS);
                    half hl = saturate(dot(n, l.direction) * 0.5 + 0.5);
                    lit += albedo.rgb * l.color * l.distanceAttenuation * hl * lerp(0.35, 0.30, soft);
                }
                #endif
                // 림: 실루엣 가장자리에 하늘빛 — 검은 외곽선 대신 형태를 잡아 준다
                float3 v = normalize(_WorldSpaceCameraPos.xyz - IN.positionWS);
                half fres = pow(1.0 - saturate(dot(n, v)), 3.0);
                lit += _RimColor.rgb * fres * (_CoastSoftRim * soft) * (0.4 + 0.6 * shade);
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
