// ============================================================
// Battery Dashes — Retro Robot Battery Indicator
// ============================================================
// ATTACHMENT:
//   1. Assign to a RawImage/Image on your HUD Canvas (120×32, bottom-left).
//   2. Add HealthBarDriver.cs to the same GO (pushes _Health 0→1).
//   3. No texture needed — fully procedural.
// ============================================================

Shader "Game/Battery Dashes"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Health ("Health (0-1)", Range(0.0, 1.0)) = 1.0
        _DashColor ("Dash Color (Full)", Color) = (0.2, 1.0, 0.4, 0.9)
        _LowColor ("Dash Color (Low)", Color) = (1.0, 0.2, 0.1, 0.9)
        _GlowIntensity ("Glow Intensity", Float) = 1.0
        _DashCount ("Dash Count", Range(4.0, 20.0)) = 8.0
        _BlinkSpeed ("Blink Speed (at low HP)", Float) = 4.0
        _LowThreshold ("Low HP Threshold", Range(0.0, 1.0)) = 0.3
        _BorderWidth ("Border Width", Range(0.0, 0.1)) = 0.03
        _BorderRadius ("Border Corner Radius", Range(0.0, 0.5)) = 0.08
        _BorderIntensity ("Border Intensity", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend One One

        Pass
        {
            Name "BatteryDashes"

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

            uniform float4 _DashColor;
            uniform float4 _LowColor;
            uniform float _GlowIntensity;
            uniform float _DashCount;
            uniform float _BlinkSpeed;
            uniform float _LowThreshold;
            uniform float _Health;
            uniform float _BorderWidth;
            uniform float _BorderRadius;
            uniform float _BorderIntensity;

            // ─── Hash / Noise ───
            float Hash(float n)
            {
                return frac(sin(n * 127.1) * 43758.5453);
            }

            float Noise1D(float x)
            {
                float i = floor(x);
                float f = frac(x);
                float u = f * f * (3.0 - 2.0 * f);
                return lerp(Hash(i), Hash(i + 1.0), u);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // ─── Determine which dashes are lit ───
                float dashIndex = floor(uv.x * _DashCount);
                float litCount = _Health * _DashCount;
                float lit = step(dashIndex + 0.5, litCount);

                // ─── Dash shape (vertical bar with gaps) ───
                float dashPhase = frac(uv.x * _DashCount);
                float dashMask = step(0.2, dashPhase) * step(dashPhase, 0.8);

                float vPhase = uv.y;
                float vMask = step(0.15, vPhase) * step(vPhase, 0.85);

                // ─── Blink effect at low health ───
                float isLow = step(_LowThreshold, 1.0 - _Health);
                float blink = 1.0;
                if (isLow > 0.5)
                {
                    float isLastDash = step(litCount - 1.0, dashIndex);
                    blink = isLastDash > 0.5
                        ? (0.5 + 0.5 * sin(_Time.z * _BlinkSpeed * 3.14159))
                        : 1.0;
                }

                // ─── Core intensity ───
                float core = lit * dashMask * vMask * blink;

                // ─── Glow (soft falloff around each dash) ───
                float glowDistX = abs(dashPhase - 0.5) * 2.0;
                float glowDistY = abs(vPhase - 0.5) * 2.0;
                float glowDist = max(glowDistX, glowDistY);
                float glow = (1.0 - smoothstep(0.0, 1.2, glowDist)) * _GlowIntensity * 0.4;
                glow *= lit * blink;

                // ─── Color: lerp from green to red at low HP ───
                float lowFactor = 1.0 - smoothstep(0.0, _LowThreshold, _Health);
                half3 color = lerp(_DashColor.rgb, _LowColor.rgb, lowFactor);

                // ─── CRT flicker ───
                float flicker = 0.95 + 0.05 * Noise1D(_Time.z * 60.0);

                // ─── Scanline effect ───
                float scanline = 0.9 + 0.1 * sin(uv.y * 80.0);

                // ─── Composite dashes ───
                float intensity = (core * 1.0 + glow) * flicker * scanline;

                // ─── Rounded Border ───
                float2 p = uv - 0.5;
                float2 halfExt = float2(0.5 - _BorderWidth, 0.5 - _BorderWidth) - _BorderRadius;
                float2 q = abs(p) - halfExt;
                float sdDist = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - _BorderRadius;
                float border = (1.0 - smoothstep(-_BorderWidth, _BorderWidth, sdDist)) * _BorderIntensity;

                half3 borderColor = half3(0.2, 0.8, 0.4);
                half3 finalColor = color * intensity + borderColor * border * flicker;

                return half4(finalColor, max(intensity, border * 0.5));
            }
            ENDCG
        }
    }

    Fallback Off
}   