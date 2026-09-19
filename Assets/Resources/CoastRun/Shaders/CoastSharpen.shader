Shader "CoastRun/CoastSharpen"
{
    // 151차: 언샤프 마스크 전체 화면 패스 — 소프트 룩·DoF 로 뭉개진 소품 윤곽을 또렷하게. _Amount 0.3~0.6
    Properties { _Amount ("Amount", Range(0, 2)) = 0.45 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always
        Pass
        {
            Name "CoastSharpen"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            float _Amount;
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 px = _BlitTexture_TexelSize.xy;
                half3 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
                half3 n = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(0, px.y)).rgb
                        + SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(0, px.y)).rgb
                        + SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(px.x, 0)).rgb
                        + SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - float2(px.x, 0)).rgb;
                half3 blur = n * 0.25;
                half3 hi = c - blur;
                // 헤일로 방지: 고주파를 눌러서 더함
                hi = clamp(hi, -0.12, 0.12);
                return half4(saturate(c + hi * _Amount * 2.0), 1);
            }
            ENDHLSL
        }
    }
}
