Shader "CoastRun/CoastSea"
{
    // 155차(사용자: 「바다는 파도치게」): 두 겹 스크롤 물 텍스처 + 버텍스 너울(3중 사인) + 마루 거품 + 프레넬 하늘빛 + 밤 어둡게(_CoastNight)
    Properties
    {
        [MainTexture] _BaseMap ("Water", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1,1,1,1)
        _DeepColor ("Deep tint", Color) = (0.25, 0.60, 0.90, 1)
        _FoamColor ("Foam", Color) = (1,1,1,1)
        _Amp ("Wave amplitude", Float) = 0.28
        _Speed ("Scroll speed", Float) = 0.035
        _Foam ("Foam strength", Range(0,1)) = 0.55
        _CurveWeight ("Curved World Weight", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "CoastCurve.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; half4 _BaseColor; half4 _DeepColor; half4 _FoamColor; float _Amp; float _Speed; half _Foam; half _CurveWeight;
            CBUFFER_END
            half _CoastNight;
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float2 uv2 : TEXCOORD1; };   // 161차: uv2.x = 너울 가중치(육지 밑 0)
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; float wave : TEXCOORD2; float fogFactor : TEXCOORD3; };
            float WaveH(float3 w, float t)
            {
                return sin(w.x * 0.09 + t * 1.1) * 1.0 + sin(w.z * 0.07 - t * 0.8) * 0.85 + sin((w.x + w.z) * 0.25 + t * 2.2) * 0.25 + sin(w.x * 0.31 - w.z * 0.22 + t * 1.7) * 0.18;
            }
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 ws = TransformObjectToWorld(IN.positionOS.xyz);
                float t = _Time.y;
                float h = WaveH(ws, t) * IN.uv2.x;   // 161차(사용자: 「바닷물이 육지 가운데서 생성」): 물가·육지 밑에서는 너울이 땅을 뚫지 않게 가중치
                ws.y += h * _Amp;
                ws = CoastCurveWorld(ws, _CurveWeight);
                OUT.positionCS = TransformWorldToHClip(ws); OUT.positionWS = ws; OUT.uv = IN.uv; OUT.wave = h;
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }
            half4 frag(Varyings IN) : SV_Target
            {
                float t = _Time.y * _Speed;
                float2 uv1 = IN.uv * _BaseMap_ST.xy + float2(t * 0.6, t);
                float2 uv2 = IN.uv * _BaseMap_ST.xy * 0.53 + float2(-t * 0.8, t * 0.45) + 0.37;
                half3 c1 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv1).rgb;
                half3 c2 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv2).rgb;
                half3 water = lerp(c1, c2, 0.45) * _BaseColor.rgb;
                // 골은 진하게, 마루는 밝게 + 거품
                half k = saturate(IN.wave * 0.5 + 0.5);
                water = lerp(water * _DeepColor.rgb * 1.15, water, k);
                half crest = smoothstep(0.62, 0.95, k) * _Foam;
                water = lerp(water, _FoamColor.rgb, crest * 0.8);
                // 프레넬 하늘빛
                float3 v = normalize(_WorldSpaceCameraPos.xyz - IN.positionWS);
                half fres = pow(1.0 - saturate(v.y), 3.0);
                water = lerp(water, half3(0.75, 0.88, 1.0), fres * 0.35);
                // 밤
                water *= lerp(1.0, 0.28, saturate(_CoastNight)); water = lerp(water, water * half3(0.6, 0.7, 1.1), saturate(_CoastNight) * 0.6);
                water = MixFog(water, IN.fogFactor);
                return half4(water, 1);
            }
            ENDHLSL
        }
    }
}
