// 68차: 시네마틱 채도 곡선용 UI 셰이더 — 스틸을 회색으로 죽이거나(_Sat) 세피아로(_Sepia) 만든다. uGUI Image.material 에 붙인다.
// 240차(그래픽 업그레이드): 스토리 그림을 또렷하게(_Sharp 언샤프) · 밝은 곳 번짐(_Glow) · 필름 톤(_Grade) ·
//   가장자리 부드럽게(_Feather) · 뒤 배경용 흐림(_Blur). 값이 모두 0 이면 예전과 똑같이 나온다. 키워드는 쓰지 않는다(빌드에서 변형이 빠지지 않게).
Shader "CoastRun/UIDesaturate"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Sat ("Saturation", Range(0,1)) = 1
        _Sepia ("Sepia", Range(0,1)) = 0
        _Keep ("Keep warm spot colors", Range(0,1)) = 0
        _Sharp ("Sharpen", Range(0,2)) = 0
        _Glow ("Highlight glow", Range(0,1)) = 0
        _Grade ("Film grade", Range(0,1)) = 0
        _Feather ("Edge feather (uv)", Range(0,0.2)) = 0
        _Blur ("Backdrop blur radius (uv)", Range(0,0.1)) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 texcoord : TEXCOORD0; };
            sampler2D _MainTex; float4 _MainTex_ST; float4 _MainTex_TexelSize; fixed4 _Color; float _Sat; float _Sepia; float _Keep;
            float _Sharp; float _Glow; float _Grade; float _Feather; float _Blur;
            v2f vert(appdata_t v)
            {
                v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex); o.color = v.color * _Color; return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord;
                float4 c;
                if (_Blur > 0.0005)
                {
                    // 뒤 배경: 12탭 원판 흐림(세로 그림이라 가로 반경을 조금 넓게)
                    float2 r = float2(_Blur * 1.6, _Blur);
                    float3 acc = tex2D(_MainTex, uv).rgb;
                    acc += tex2D(_MainTex, uv + r * float2( 0.326, 0.000)).rgb;
                    acc += tex2D(_MainTex, uv + r * float2(-0.240, 0.221)).rgb;
                    acc += tex2D(_MainTex, uv + r * float2( 0.038,-0.474)).rgb;
                    acc += tex2D(_MainTex, uv + r * float2( 0.338, 0.494)).rgb;
                    acc += tex2D(_MainTex, uv + r * float2(-0.664,-0.107)).rgb;
                    acc += tex2D(_MainTex, uv + r * float2( 0.616,-0.413)).rgb;
                    acc += tex2D(_MainTex, uv + r * float2(-0.207, 0.778)).rgb;
                    acc += tex2D(_MainTex, uv + r * float2(-0.394,-0.767)).rgb;
                    acc += tex2D(_MainTex, uv + r * float2( 0.869, 0.292)).rgb;
                    acc += tex2D(_MainTex, uv + r * float2(-0.918, 0.287)).rgb;
                    acc += tex2D(_MainTex, uv + r * float2( 0.493,-0.870)).rgb;
                    acc += tex2D(_MainTex, uv + r * float2( 0.060, 1.000)).rgb;
                    c = float4(acc / 13.0, 1.0);
                }
                else
                {
                    c = tex2D(_MainTex, uv);
                    if (_Sharp + _Glow > 0.001)
                    {
                        float2 t = _MainTex_TexelSize.xy;
                        float3 n1 = tex2D(_MainTex, uv + t * float2( 1.0,  1.0)).rgb + tex2D(_MainTex, uv + t * float2(-1.0,  1.0)).rgb
                                  + tex2D(_MainTex, uv + t * float2( 1.0, -1.0)).rgb + tex2D(_MainTex, uv + t * float2(-1.0, -1.0)).rgb;
                        float3 near = n1 * 0.25;
                        // 언샤프: 흐린 값과의 차이를 더한다(차이를 ±0.12 로 묶어 선 주변 테가 생기지 않게)
                        c.rgb += clamp(c.rgb - near, -0.12, 0.12) * _Sharp;
                        if (_Glow > 0.001)
                        {
                            float3 w1 = tex2D(_MainTex, uv + t * float2( 7.0,  0.0)).rgb + tex2D(_MainTex, uv + t * float2(-7.0,  0.0)).rgb
                                      + tex2D(_MainTex, uv + t * float2( 0.0,  7.0)).rgb + tex2D(_MainTex, uv + t * float2( 0.0, -7.0)).rgb;
                            float3 soft = (w1 * 0.25 + near) * 0.5;
                            float sl = dot(soft, float3(0.299, 0.587, 0.114));
                            // 밝은 곳 자체는 더 밝히지 않는다(얼굴·하늘이 하얗게 날아갔다) — 밝은 곳 옆의 어두운 자리로만 빛이 번진다
                            c.rgb += max(soft - c.rgb, 0.0) * saturate((sl - 0.60) * 2.5) * _Glow;
                        }
                        c.rgb = saturate(c.rgb);
                    }
                }
                c *= i.color;
                float g = dot(c.rgb, float3(0.299, 0.587, 0.114));
                float3 gray = float3(g, g, g);
                float3 sep = float3(min(1.0, g * 1.10 + 0.05), g * 0.94 + 0.02, g * 0.74);
                float3 col = lerp(gray, c.rgb, _Sat);
                col = lerp(col, sep, _Sepia);
                // 스폿 컬러: 빨강/주황(하트·우비·부표)만 원색으로 남긴다
                float warm = saturate((c.r - max(c.g, c.b)) * 3.0) * saturate((c.r - 0.35) * 4.0);
                col = lerp(col, c.rgb, warm * _Keep);
                if (_Grade > 0.001)
                {
                    // 필름 톤: 부드러운 S 곡선 · 옅은 색만 살짝 진하게 · 어두운 곳은 푸르게, 밝은 곳은 따뜻하게
                    float l = dot(col, float3(0.299, 0.587, 0.114));
                    float3 s = col * col * (3.0 - 2.0 * col);
                    col = lerp(col, s, 0.22 * _Grade);
                    float mx = max(col.r, max(col.g, col.b)), mn = min(col.r, min(col.g, col.b));
                    float l2 = dot(col, float3(0.299, 0.587, 0.114));
                    col = lerp(float3(l2, l2, l2), col, 1.0 + 0.16 * _Grade * (1.0 - (mx - mn)) * step(0.02, _Sat));
                    col += _Grade * lerp(float3(-0.012, 0.000, 0.022), float3(0.012, 0.004, -0.014), smoothstep(0.15, 0.85, l));
                    col = saturate(col);
                }
                float a = c.a;
                if (_Feather > 0.0005)
                {
                    float f = _Feather;
                    a *= smoothstep(0.0, f, uv.y) * smoothstep(0.0, f, 1.0 - uv.y);
                    a *= smoothstep(0.0, f * 0.6, uv.x) * smoothstep(0.0, f * 0.6, 1.0 - uv.x);
                }
                return fixed4(col, a);
            }
            ENDCG
        }
    }
    Fallback "UI/Default"
}
