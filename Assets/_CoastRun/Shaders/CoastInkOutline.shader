Shader "CoastRun/InkOutline"
{
    // 14차-3: 스킨드 메시(주인공 리그)용 잉크 테두리. 인버티드 헐을 트랜스폼 스케일로 만들 수 없는
    // 스킨드 메시는 정점을 노멀 방향으로 밀어낸 셸을 같은 렌더러의 추가 머티리얼 슬롯으로 그린다.
    Properties
    {
        _OutlineColor ("Ink", Color) = (0.10, 0.08, 0.10, 1)
        _Width ("Width (m)", Range(0, 0.05)) = 0.014
        _CurveWeight ("Curved World Weight", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Ink"
            Tags { "LightMode"="UniversalForward" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "CoastCurve.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                half _Width;
                half _CurveWeight;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 ws = TransformObjectToWorld(IN.positionOS.xyz);
                float3 nw = normalize(TransformObjectToWorldNormal(IN.normalOS));
                // 25차-1: 손가락·팔뚝처럼 얇은 부위는 고정 폭(1.7 cm) 셸이 표면을 뒤덮어 팔 전체가 남색으로 보였다.
                // 카메라 거리에 비례시켜(4.4 m 기준 _Width) 가까울수록 얇게 — 화면에서 보이는 선 굵기는 그대로.
                float dist = distance(ws, _WorldSpaceCameraPos.xyz);
                ws += nw * _Width * clamp(dist / 4.4, 0.35, 1.6);
                ws = CoastCurveWorld(ws, _CurveWeight);
                OUT.positionCS = TransformWorldToHClip(ws);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target { return _OutlineColor; }
            ENDHLSL
        }
    }
}
