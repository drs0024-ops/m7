// ============================================================
// EKG Trace Shader — Vintage Hospital Monitor
// ============================================================
// ATTACHMENT:
//   1. Create a RawImage on your HUD Canvas (Screen Space - Overlay).
//   2. Set its RectTransform to ~120×32 px (anchored bottom-left).
//   3. Assign this material to the RawImage.
//   4. Add the EKGDriver.cs component to the same GameObject
//      (or any parent) and assign the material in the Inspector.
//   5. No texture needed on the RawImage — the shader is fully procedural.
// ============================================================

Shader "Game/EKG Trace"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}  // ← ADD THIS LINE
        _LineColor ("Line Color", Color) = (0.2, 1.0, 0.4, 0.7)
        _GlowIntensity ("Glow Intensity", Float) = 1.0
        _ScrollSpeed ("Scroll Speed", Float) = 0.5
        _NoiseAmount ("Noise Amount", Float) = 0.15
        _PulseSpeed ("Pulse Speed", Range(0.1, 3.0)) = 1.0
        _PulseAmplitude ("Pulse Amplitude", Range(0.0, 2.0)) = 1.0 
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
        Blend One One // Additive for phosphor glow

        Pass
        {
            Name "EKGTrace"

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

            // ─── Shader Properties ───
            uniform float4 _LineColor;
            uniform float _GlowIntensity;
            uniform float _ScrollSpeed;
            uniform float _NoiseAmount;
            uniform float _PulseSpeed;
            uniform float _PulseAmplitude;


            // ─── Hash / Noise ───
            float Hash(float n)
            {
                return frac(sin(n * 127.1) * 43758.5453);
            }

            float Noise1D(float x)
            {
                float i = floor(x);
                float f = frac(x);
                float u = f * f * (3.0 - 2.0 * f); // smoothstep
                return lerp(Hash(i), Hash(i + 1.0), u);
            }

            // ─── Gaussian bump ───
            float Gaussian(float x, float center, float width)
            {
                float d = (x - center) / width;
                return exp(-d * d);
            }

            // ─── EKG Waveform ───
            // Returns the Y value (0..1) of the EKG signal at a given phase (0..1)
            float EKGSignal(float phase)
            {
                // Wrap phase to [0, 1)
                phase = frac(phase);

                float signal = 0.0;

                // P wave (small bump before QRS)
                signal += 0.08 * Gaussian(phase, 0.10, 0.025);

                // Q dip (small negative)
                signal -= 0.12 * Gaussian(phase, 0.18, 0.012);

                // R spike (sharp positive peak)
                signal += 0.85 * Gaussian(phase, 0.20, 0.008);

                // S dip (small negative after R)
                signal -= 0.15 * Gaussian(phase, 0.22, 0.012);

                // T wave (broad recovery bump)
                signal += 0.15 * Gaussian(phase, 0.38, 0.04);

                return signal;
            }

            // ─── Main EKG value at a given x position ───
            float GetEKGValue(float x)
            {
                // x is in [0, 1] across the quad width
                // Map to time: each "beat" takes (1.0 / _PulseSpeed) units of x
                float time = x * _PulseSpeed * 2.0; // 2 beats visible across the width

                float phase = frac(time);
                float baseSignal = EKGSignal(phase) * _PulseAmplitude;

                // Add baseline noise (jagged, old-monitor feel)
                float noise = (Noise1D(x * 80.0 + _Time.z * 3.0) - 0.5) * _NoiseAmount;

                // Slight drift/wander for vintage feel
                float wander = (Noise1D(x * 5.0 + _Time.z * 0.3) - 0.5) * 0.04;

                // Center the signal around 0.5 (middle of the quad)
                return 0.5 + baseSignal * 0.35 + noise + wander;
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

                // Scroll offset: newest data on the RIGHT, scrolls LEFT
                float scrollOffset = _Time.z * _ScrollSpeed;
                float x = frac(uv.x + scrollOffset);

                // Compute the EKG Y value at this x
                float ekgY = GetEKGValue(x);

                // Distance from the line (in UV space)
                float lineDist = abs(uv.y - ekgY);

                // Core line (thin, bright)
                float lineWidth = 0.012;
                float core = 1.0 - smoothstep(0.0, lineWidth, lineDist);

                // Glow (wider, softer falloff)
                float glowWidth = 0.06;
                float glow = 1.0 - smoothstep(0.0, glowWidth, lineDist);
                glow = glow * glow * _GlowIntensity; // Quadratic falloff

                // Phosphor bloom (even wider, very faint)
                float bloomWidth = 0.15;
                float bloom = 1.0 - smoothstep(0.0, bloomWidth, lineDist);
                bloom = bloom * bloom * bloom * _GlowIntensity * 0.3;

                // ─── Sweep Line (erase beam) ───
                // A bright vertical bar that moves right-to-left periodically
                float sweepPeriod = 4.0; // seconds per sweep
                float sweepPhase = frac(_Time.z / sweepPeriod);
                float sweepX = 1.0 - sweepPhase; // moves right to left
                float sweepWidth = 0.015;
                float sweep = 1.0 - smoothstep(0.0, sweepWidth, abs(uv.x - sweepX));
                sweep *= 0.6; // subtle

                // ─── Fade out on the left (old data fades) ───
                float leftFade = smoothstep(0.0, 0.25, uv.x);

                // ─── Composite ───
                float intensity = (core * 1.0 + glow * 0.5 + bloom * 0.2) * leftFade;
                intensity += sweep * leftFade;

                // Slight flicker (CRT feel)
                float flicker = 0.95 + 0.05 * Noise1D(_Time.z * 60.0);
                intensity *= flicker;

                half4 color = half4(_LineColor.rgb, _LineColor.a) * intensity;

                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}   