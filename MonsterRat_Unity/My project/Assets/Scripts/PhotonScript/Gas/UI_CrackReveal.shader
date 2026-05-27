Shader "UI/CrackReveal"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Reveal ("Reveal", Range(0,1)) = 0
        _Center ("Center", Vector) = (0.5,0.5,0,0)
        _Softness ("Softness", Range(0.001,0.5)) = 0.05
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Reveal;
            float4 _Center;
            float _Softness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                float dist = distance(i.uv, _Center.xy);
                float mask = smoothstep(_Reveal, _Reveal - _Softness, dist);

                col.a *= mask;
                return col;
            }
            ENDCG
        }
    }
}