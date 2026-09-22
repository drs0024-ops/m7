// ============================================================
// CRT Roll Bar — Brightness Band
// ============================================================
// Usage:
//   Full-screen quad, child of camera, renderQueue 3001.
//   Driven by CRTTransitionController.
//   Disabled when idle (zero GPU cost).
// ============================================================

Shader "Game/CRT Roll Bar"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {} 
        _Tint ("Tint", Color) = (1.0, 1.0, 1.0, 1.0) 
        _Y ("Y Position", Range(0.0, 1.0)) = 0.5
        _Width ("Band Width", Range(0.01, 0.5)) = 0.08
        _Intensity ("Intensity", Range(0.0, 2.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "RollBar"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            uniform float4 _Tint;
            uniform float _Y;
            uniform float _Width;
            uniform float _Intensity;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                if (_Intensity < 0.001)
                    return half4(0, 0, 0, 0);

                float dist = abs(input.uv.y - _Y);
                float inner = _Width * 0.3;
                float outer = _Width * 0.5;
                float mask = 1.0 - smoothstep(inner, outer, dist);

                half3 color = _Tint.rgb * _Intensity;
                return half4(color, mask * _Intensity);
            }
            ENDCG
        }
    }

    Fallback Off
}   