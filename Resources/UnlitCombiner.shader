Shader "Unlit/TextureCombinerShader"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _AlphaTex ("Alpha Texture", 2D) = "white" {}
        _BackgroundTex ("Background Texture", 2D) = "black" {}
        _UseBackAlpha ("Flag to use background Alpha or not", Int) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100
        ZTest Always 
        Cull Off 
        ZWrite Off
        Blend Off
        Fog { Mode off }


        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _AlphaTex;
            sampler2D _BackgroundTex;
            float4 _MainTex_ST;
            float _UseBackAlpha;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed alpha = col.a * tex2D(_AlphaTex, i.uv).a;
                fixed4 bgTex = tex2D(_BackgroundTex, i.uv);
                bgTex.a = max(1.0 - _UseBackAlpha, bgTex.a);
                col = ((alpha) * col) + ((1.0 - alpha) * bgTex);
                return col;
            }
            ENDCG
        }
    }
}
