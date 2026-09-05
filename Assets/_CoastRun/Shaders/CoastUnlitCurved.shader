Shader "CoastRun/UnlitCurved"
{
    // Drop-in for "Universal Render Pipeline/Unlit" that follows the curved world.
    // Keeps the same property names CoastMaterials.CreateTransparent pokes
    // (_Surface/_SrcBlend/_DstBlend/_ZWrite) so the existing material code works.
    Properties
    {
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        [MainColor]   _BaseColor ("Color", Color) = (1,1,1,1)
        _CurveWeight ("Curved World Weight", Range(0,1)) = 1
        _FogWeight ("Fog Weight", Range(0,1)) = 1

        [HideInInspector] _Surface ("Surface", Float) = 0
        [HideInInspector] _Blend ("Blend", Float) = 0
        [HideInInspector] _SrcBlend ("Src", Float) = 1
        [HideInInspector] _DstBlend ("Dst", Float) = 0
        [HideInInspector] _ZWrite ("ZWrite", Float) = 1
        // 0 Off (billboards), 1 Front (ink outline shells), 2 Back
        [HideInInspector] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "CoastCurve.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _CurveWeight;
            half _FogWeight;
            half _Surface;
            half _Blend;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 ws = CoastCurveWorld(TransformObjectToWorld(IN.positionOS.xyz), _CurveWeight);
                OUT.positionCS = TransformWorldToHClip(ws);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                // Painted backdrops (far town, clouds) carry their own haze; fog on top
                // would dissolve them into the sky colour at 150 m.
                c.rgb = lerp(c.rgb, MixFog(c.rgb, IN.fogFactor), _FogWeight);
                return c;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            // Transparent surfaces (alpha billboards: clouds, far town) must not write
            // depth here: a full-quad depth stamp let the quad occlude the painted sky
            // behind it and the clear colour showed through as a flat rectangle.
            ZWrite [_ZWrite]
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vertDepth(Attributes IN)
            {
                Varyings OUT;
                float3 ws = CoastCurveWorld(TransformObjectToWorld(IN.positionOS.xyz), _CurveWeight);
                OUT.positionCS = TransformWorldToHClip(ws);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 fragDepth(Varyings IN) : SV_Target
            {
                half a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a;
                clip(a - 0.5);
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
