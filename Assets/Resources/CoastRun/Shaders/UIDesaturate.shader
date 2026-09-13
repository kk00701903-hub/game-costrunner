// 68차: 시네마틱 채도 곡선용 UI 셰이더 — 스틸을 회색으로 죽이거나(_Sat) 세피아로(_Sepia) 만든다. uGUI Image.material 에 붙인다.
Shader "CoastRun/UIDesaturate"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Sat ("Saturation", Range(0,1)) = 1
        _Sepia ("Sepia", Range(0,1)) = 0
        _Keep ("Keep warm spot colors", Range(0,1)) = 0
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
            #include "UnityCG.cginc"
            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 texcoord : TEXCOORD0; };
            sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color; float _Sat; float _Sepia; float _Keep;
            v2f vert(appdata_t v)
            {
                v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex); o.color = v.color * _Color; return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.texcoord) * i.color;
                float g = dot(c.rgb, float3(0.299, 0.587, 0.114));
                float3 gray = float3(g, g, g);
                float3 sep = float3(min(1.0, g * 1.10 + 0.05), g * 0.94 + 0.02, g * 0.74);
                float3 col = lerp(gray, c.rgb, _Sat);
                col = lerp(col, sep, _Sepia);
                // 스폿 컬러: 빨강/주황(하트·우비·부표)만 원색으로 남긴다
                float warm = saturate((c.r - max(c.g, c.b)) * 3.0) * saturate((c.r - 0.35) * 4.0);
                col = lerp(col, c.rgb, warm * _Keep);
                return fixed4(col, c.a);
            }
            ENDCG
        }
    }
    Fallback "UI/Default"
}
