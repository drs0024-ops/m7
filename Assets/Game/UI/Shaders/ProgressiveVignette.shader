// ============================================================
// Progressive Vignette — Vintage Horror Damage Overlay
// ============================================================
// ATTACHMENT:
//   1. Create a RawImage on your HUD Canvas (Screen Space - Overlay).
//   2. Stretch it to fill the entire screen (anchored stretch, 0 margins).
//   3. Assign this material to the RawImage.
//   4. Add VignetteDriver.cs to the same GameObject.
//   5. No texture needed — fully procedural.
//   6. Set the RawImage's Sorting Order ABOVE the game camera
//      (e.g., Canvas Order in Layer = 100) so it renders on top.
// ============================================================

Shader "Game/Progressive Vignette"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}  // ← ADD THIS LINE (required by UI)
        _VignetteColor ("Vignette Color", Color) = (0, 0, 0, 1)
        _Damage ("Damage (0-1)", Range(0.0, 1.0)) = 0.0
        _MinBrightness ("Min Brightness (center floor)", Range(0.0, 1.0)) = 0.15
        _InnerRadius ("Inner Radius (at full damage)", Range(0.0, 0.8)) = 0.2
        _Softness ("Softness (transition width)", Range(0.01, 0.5)) = 0.15
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
        Blend DstColor Zero // Multiplicative: output * source

        Pass
        {
            Name "Vignette"

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

            uniform float4 _VignetteColor;
            uniform float _Damage;
            uniform float _MinBrightness;
            uniform float _InnerRadius;
            uniform float _Softness;

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

                // Distance from center (0 at center, ~0.707 at corners)
                float2 centered = uv - 0.5;
                float dist = length(centered * float2(1.0, 1.0)); // uniform circular

                // ─── Vignette calculation ───
                // At damage=0: very faint static vignette (5% at extreme corners)
                // At damage=1: reaches _InnerRadius from edge, full vignette color

                // The "edge" in UV space is at dist ≈ 0.5 (midpoint to edge)
                // The vignette starts at (0.5 - _InnerRadius * _Damage) and
                // transitions over _Softness

                float edgeDist = 0.5; // distance to screen edge (normalized)
                float vignetteStart = edgeDist - _InnerRadius * _Damage;
                float vignetteEnd = vignetteStart - _Softness;

                // smoothstep: 1 at center, 0 at edges
                float mask = 1.0 - smoothstep(vignetteEnd, vignetteStart, dist);

                // At damage=0, add a very faint static corner darkening (5%)
                float staticVignette = 1.0 - smoothstep(0.3, 0.5, dist);
                staticVignette *= 0.05; // very subtle

                // Combine: dynamic damage vignette + static vintage vignette
                float totalVignette = max(mask * _Damage, staticVignette);

                // The output is a MULTIPLIER (because Blend DstColor Zero)
                // 1.0 = no change, 0.0 = fully black
                // We want: center stays at _MinBrightness at full damage
                // Edges go to _VignetteColor

                // Brightness multiplier (how much of the original we keep)
                float brightness = 1.0 - totalVignette * (1.0 - _MinBrightness);

                // Tint toward vignette color at the edges
                // At full vignette, the color should be _VignetteColor
                // We blend: result = lerp(original, vignetteColor, vignetteAmount)
                // With multiplicative blend, output = lerp(1, vignetteColor/brightness, vignette)
                // Simplified: output the tinted brightness

                half3 tint = lerp(half3(1, 1, 1), _VignetteColor.rgb, totalVignette);
                half alpha = brightness;

                return half4(tint * alpha, 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}   