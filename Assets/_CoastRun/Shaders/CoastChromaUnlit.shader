Shader "CoastRun/ChromaUnlit"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        _KeyColor ("Chroma Key", Color) = (1,0,1,1)
        _Cutoff ("Key Cutoff", Range(0,1)) = 0.38
        _PinkKill ("Pink Kill (0 = keep pinks, e.g. hearts)", Float) = 1
        // 12차: 그림 소품도 도로와 같이 휜다. 전엔 이 셰이더만 곧게 그려서 멀리 있는 관광객·버스가
        // 도로가 오르막으로 휘면 바닥에 파묻히고 내리막이면 떠 보였다(하반신 클리핑의 원인).
        _CurveWeight ("Curved World Weight", Range(0,1)) = 1
        // 14차: 장애물 그림엔 흰 테두리(가독성 — 배경과 섞여 왜 죽었는지 모르는 걸 막는다)
        _OutlineOn ("Outline On", Float) = 0
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width (texels)", Range(0,12)) = 5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "CoastCurve.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float4 _KeyColor;
            float _PinkKill;
            float _Cutoff;
            float _CurveWeight;
            float _OutlineOn;
            float4 _OutlineColor;
            float _OutlineWidth;
            float4 _BaseMap_TexelSize;

            // 14차-7: 그림은 오프라인에서 진짜 알파로 바꿨다(Tools/Art/key_to_alpha.py).
            // 셰이더는 알파를 믿고, 남은 마젠타만 안전장치로 자른다 — 밉맵/필터링에서 분홍 테두리 없음.
            bool IsKeyed(float2 uv)
            {
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                half d = distance(c.rgb, _KeyColor.rgb);
                return c.a < 0.5 || d < 0.22;
            }

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 ws = CoastCurveWorld(TransformObjectToWorld(v.positionOS.xyz), _CurveWeight);
                o.positionCS = TransformWorldToHClip(ws);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                half d = distance(c.rgb, _KeyColor.rgb);
                if (c.a < 0.5 || d < 0.22)
                {
                    if (_OutlineOn > 0.5)
                    {
                        // 키 색 픽셀이지만 이웃에 본체가 있으면 테두리 색으로 칠한다.
                        float2 t = _BaseMap_TexelSize.xy * _OutlineWidth;
                        bool near = !IsKeyed(i.uv + float2( t.x, 0)) || !IsKeyed(i.uv + float2(-t.x, 0))
                                 || !IsKeyed(i.uv + float2(0,  t.y)) || !IsKeyed(i.uv + float2(0, -t.y))
                                 || !IsKeyed(i.uv + float2( t.x,  t.y) * 0.7) || !IsKeyed(i.uv + float2(-t.x,  t.y) * 0.7)
                                 || !IsKeyed(i.uv + float2( t.x, -t.y) * 0.7) || !IsKeyed(i.uv + float2(-t.x, -t.y) * 0.7);
                        // 텍스처 가장자리 밖(클램프)에서 본체가 잘린 경우는 테두리를 치지 않는다.
                        bool inside = i.uv.x > t.x && i.uv.x < 1 - t.x && i.uv.y > t.y && i.uv.y < 1 - t.y;
                        if (near && inside)
                            return half4(_OutlineColor.rgb, 1);
                    }
                    clip(-1);
                }
                // Flat "lit" sprite: every 3D thing on screen is toon-lit (sun × ramp,
                // well above albedo) and then tonemapped, so raw albedo read as a
                // silhouette. Scale by sun + sky like a face-on lit surface would get.
                Light sun = GetMainLight();
                half3 lit = sun.color * 0.9 + half3(unity_AmbientSky.rgb) * 0.6 + 0.35;
                c.rgb *= lit;
                // 알파 가장자리는 0.5 를 중심으로 짧게 섞는다(밉맵에서 부드러운 윤곽, 멀리서 도트 반짝임 없음)
                c.a = saturate((c.a - 0.5) * 4.0 + 0.5);
                return c;
            }
            ENDHLSL
        }
    }
}
