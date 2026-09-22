Shader "Game/CRT/Boot"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _BootProgress ("Boot Progress", Range(0, 1)) = 1.0
        _LineBrightness ("Line Brightness", Float) = 3.0
        _GlowWidth ("Glow Width", Range(0.001, 0.2)) = 0.03
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.3
        _ScanlineFrequency ("Scanline Frequency", Float) = 200.0
        _FlashColor ("Flash Color", Color) = (1, 0.95, 0.9, 1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ UNITY_UI_CLIP_RECT
            #pragma multi_compile _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 positionNDC : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _Color;
            float _BootProgress;
            float _LineBrightness;
            float _GlowWidth;
            float _ScanlineIntensity;
            float _ScanlineFrequency;
            float4 _FlashColor;
            float4 _ClipRect;
            float4 _CameraPixelCoordScale;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                o.positionNDC = ComputeScreenPos(o.pos);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 tex = tex2D(_MainTex, i.uv) * i.color;

                float distFromCenter = abs(i.uv.y - 0.5);
                float rasterHeight = _BootProgress * 0.5;
                float insideRaster = 1.0 - step(distFromCenter, rasterHeight);

                float edgeDist = distFromCenter - (rasterHeight - _GlowWidth);
                float edgeGlow = (1.0 - saturate(edgeDist / _GlowWidth)) * insideRaster;

                float lineFlash = (1.0 - _BootProgress) * _LineBrightness;
                float lineMask = 1.0 - saturate(distFromCenter / (0.01 + _GlowWidth));
                float flash = lineMask * lineFlash;

                float scan = sin(i.uv.y * _ScanlineFrequency * 3.14159) * 0.5 + 0.5;
                float scanlineEffect = lerp(1.0, scan, _ScanlineIntensity * _BootProgress);

                float overshoot = smoothstep(0.9, 1.0, _BootProgress) * 0.15;

                float4 color = tex * insideRaster * scanlineEffect;
                color.rgb += _FlashColor.rgb * (flash + edgeGlow * 0.5 + overshoot);
                color.a = max(insideRaster, flash * 0.8);

                #ifdef UNITY_UI_CLIP_RECT
                    color.a *= UnityClipRect(i.positionNDC, _ClipRect, _CameraPixelCoordScale);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                    clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}   