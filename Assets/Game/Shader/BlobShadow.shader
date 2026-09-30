Shader "Game/BlobShadow"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 1)
        _ShadowOpacity ("Shadow Opacity", Range(0, 1)) = 0.4
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent-1"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ShadowColor;
            float _ShadowOpacity;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half4 tex = tex2D(_MainTex, i.uv);
                half alpha = tex.a * _ShadowOpacity;
                return half4(_ShadowColor.rgb, alpha);
            }
            ENDCG
        }
    }
}   