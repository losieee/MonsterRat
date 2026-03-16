Shader "Custom/Tutorial_Pollution_Outline"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)

        _OutlineColor("Outline Color", Color) = (1,0.25,0.05,1)
        _OutlineSize("Outline Size (UV)", Range(0.0005, 0.02)) = 0.003
        _AlphaCutoff("Alpha Cutoff", Range(0,1)) = 0.1
        _Softness("Outline Softness", Range(0.001,1)) = 0.05
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardUnlit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _OutlineColor;
                float _OutlineSize;
                float _AlphaCutoff;
                float _Softness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half GetAlpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                half4 texCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
                half centerA = texCol.a;

                float s = _OutlineSize;

                // 8방향 샘플
                half a1 = GetAlpha(uv + float2( s,  0));
                half a2 = GetAlpha(uv + float2(-s,  0));
                half a3 = GetAlpha(uv + float2( 0,  s));
                half a4 = GetAlpha(uv + float2( 0, -s));
                half a5 = GetAlpha(uv + float2( s,  s));
                half a6 = GetAlpha(uv + float2(-s,  s));
                half a7 = GetAlpha(uv + float2( s, -s));
                half a8 = GetAlpha(uv + float2(-s, -s));

                half neighborA = max(max(max(a1, a2), max(a3, a4)), max(max(a5, a6), max(a7, a8)));

                // 알파 판정
                half fillMask = step(_AlphaCutoff, centerA);
                half neighborMask = step(_AlphaCutoff, neighborA);

                // 바깥이면서 주변에 본체가 있으면 outline
                half outlineMask = saturate(neighborMask - fillMask);

                // outline 부드럽게
                half rawOutline = saturate((neighborA - centerA) / max(_Softness, 0.0001));
                outlineMask *= rawOutline;

                half4 fillCol = texCol;
                half4 outCol = _OutlineColor;

                // 내부는 원래 피색, 외부 테두리는 outline 색
                half4 finalCol = lerp(outCol, fillCol, fillMask);

                // 최종 알파
                finalCol.a = max(fillCol.a, outlineMask * _OutlineColor.a);

                // 완전 투명 제거
                clip(finalCol.a - 0.001);

                return finalCol;
            }
            ENDHLSL
        }
    }
}
