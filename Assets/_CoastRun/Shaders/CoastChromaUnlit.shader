Shader "CoastRun/ChromaUnlit"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        _KeyColor ("Chroma Key", Color) = (1,0,1,1)
        _Cutoff ("Key Cutoff", Range(0,1)) = 0.38
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

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float4 _KeyColor;
            float _Cutoff;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                half d = distance(c.rgb, _KeyColor.rgb);
                // Also drop anti-aliased edge texels that are part figure, part key:
                // anything clearly pink-purple (red and blue both well above green)
                // is a key blend — the palette has no such colour of its own.
                if (d < _Cutoff || (c.r > 0.75 && c.g < 0.35 && c.b > 0.75)
                    || (c.r > c.g + 0.30 && c.b > c.g + 0.22))
                    clip(-1);
                // Flat "lit" sprite: every 3D thing on screen is toon-lit (sun × ramp,
                // well above albedo) and then tonemapped, so raw albedo read as a
                // silhouette. Scale by sun + sky like a face-on lit surface would get.
                Light sun = GetMainLight();
                half3 lit = sun.color * 0.9 + half3(unity_AmbientSky.rgb) * 0.6 + 0.35;
                c.rgb *= lit;
                return c;
            }
            ENDHLSL
        }
    }
}
