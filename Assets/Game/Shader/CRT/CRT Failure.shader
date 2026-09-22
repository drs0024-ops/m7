Shader "Game/CRT Failure"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _PhosphorColor ("Phosphor Color", Color) = (0.0, 0.8, 0.3, 1.0)
        _FailureAmount ("Failure Amount", Range(0.0, 2.0)) = 0.0
        _RetraceY ("Retrace Line Y", Range(-1.0, 2.0)) = 0.9
        _RetraceY2 ("Retrace Line 2 Y", Range(-1.0, 2.0)) = 0.7
        _RetraceIntensity ("Retrace Intensity", Range(0.0, 2.0)) = 0.6
        _RetraceOpacity ("Retrace Opacity", Range(0.0, 1.0)) = 1.0
        _RetraceWidth ("Retrace Line Width", Range(0.0001, 0.1)) = 0.001
        _Collapse ("Collapse", Range(0.0, 1.0)) = 0.0
        _TearY ("Tear Y", Range(-1.0, 2.0)) = 0.0
        _TearHeight ("Tear Height", Range(0.001, 0.2)) = 0.022
        _TearOpacity ("Tear Opacity", Range(0.0, 1.0)) = 1.0
        _TearColor ("Tear Color", Color) = (0.0, 0.8, 0.3, 1.0)
        _TearDirection ("Tear Direction", Float) = 1.0
        _TearLeadingOpacity ("Tear Leading Opacity", Range(0.0, 1.0)) = 1.0
        _TearTrailingOpacity ("Tear Trailing Opacity", Range(0.0, 1.0)) = 1.0
        _RGBSplit ("RGB Split", Range(0.0, 0.05)) = 0.0
        _ConsciencePulse ("Conscience Pulse", Range(0.0, 1.0)) = 0.0
        _TransitionOpacity ("Transition Opacity", Range(0.0, 1.0)) = 1.0
        _ScreenSize ("Screen Size", Vector) = (1920, 1080, 0, 0)
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
            Name "CRTFailure"

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

            uniform float4 _PhosphorColor;
            uniform float _FailureAmount;
            uniform float _RetraceY;
            uniform float _RetraceY2;
            uniform float _RetraceIntensity;
            uniform float _RetraceOpacity;
            uniform float _RetraceWidth;
            uniform float _Collapse;
            uniform float _TearY;
            uniform float _TearHeight;
            uniform float _TearOpacity;
            uniform float4 _TearColor;
            uniform float _TearDirection;
            uniform float _TearLeadingOpacity;
            uniform float _TearTrailingOpacity;
            uniform float _RGBSplit;
            uniform float _ConsciencePulse;
            uniform float _TransitionOpacity;
            uniform float4 _ScreenSize;

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
                float failure = _FailureAmount;

                if (failure < 0.001
                    && _RetraceIntensity < 0.001
                    && _TearOpacity < 0.001
                    && _Collapse < 0.001
                    && _ConsciencePulse < 0.001
                    && _RGBSplit < 0.0001)
                    return half4(0, 0, 0, 0);

                half4 result = half4(0, 0, 0, 0);

                // ─── 1. SCANLINES ───
                float scanlineFreq = 120.0;
                float scanline = sin(uv.y * scanlineFreq * 3.14159);
                scanline = step(0.0, scanline) * 0.5 + 0.5;
                float scanlineIntensity = 0.05 + failure * 0.15;
                half3 scanlineColor = _PhosphorColor.rgb * scanlineIntensity;
                result.rgb += scanlineColor * (1.0 - scanline * 0.5);
                result.a = max(result.a, scanlineIntensity * 0.3);

                // ─── 2. SYNC TEAR ───
                {
                    float dir = _TearDirection;
                    float halfH = _TearHeight * 0.5;
                    float d = (uv.y - _TearY) * dir;

                    float t = (d + halfH) / _TearHeight;
                    float bandMask = step(0.0, t) * step(t, 1.0);
                    float tearMask = lerp(_TearTrailingOpacity, _TearLeadingOpacity, t) * bandMask * _TearOpacity;

                    if (tearMask > 0.0)
                    {
                        float tearIntensity = tearMask * (0.5 + failure * 0.5);
                        result.rgb += _TearColor.rgb * tearIntensity;
                        result.a = max(result.a, tearIntensity * 0.6);
                    }
                }

                // ─── 3. RETRACE LINE ───
                if (_RetraceY >= 0.0 && _RetraceY <= 1.0 && _RetraceIntensity > 0.001)
                {
                    float retraceDist = abs(uv.y - _RetraceY);
                    float effectiveWidth = max(_RetraceWidth, 0.5 / _ScreenSize.y);
                    float coreMask = 1.0 - step(effectiveWidth, retraceDist);
                    float glowMask = 1.0 - smoothstep(effectiveWidth, effectiveWidth * 3.0, retraceDist);

                    half3 retraceColor = lerp(_PhosphorColor.rgb, half3(1, 1, 1), 0.5);
                    float retraceBrightness = (coreMask * 1.0 + glowMask * 0.3) * _RetraceIntensity;
                    float retraceOpacity = (coreMask * 0.9 + glowMask * 0.2) * _RetraceOpacity;

                    result.rgb += retraceColor * retraceBrightness;
                    result.a = max(result.a, retraceOpacity);
                }

                // ─── 3b. RETRACE LINE 2 (GHOST) ───
                if (_RetraceY2 >= 0.0 && _RetraceY2 <= 1.0 && _RetraceIntensity > 0.001)
                {
                    float retraceDist2 = abs(uv.y - _RetraceY2);
                    float effectiveWidth2 = max(_RetraceWidth, 0.5 / _ScreenSize.y);
                    float coreMask2 = 1.0 - step(effectiveWidth2, retraceDist2);
                    float glowMask2 = 1.0 - smoothstep(effectiveWidth2, effectiveWidth2 * 3.0, retraceDist2);

                    half3 retraceColor2 = lerp(_PhosphorColor.rgb, half3(1, 1, 1), 0.5);
                    float retraceBrightness2 = (coreMask2 * 1.0 + glowMask2 * 0.3) * _RetraceIntensity * 0.5;
                    float retraceOpacity2 = (coreMask2 * 0.9 + glowMask2 * 0.2) * _RetraceOpacity;

                    result.rgb += retraceColor2 * retraceBrightness2;
                    result.a = max(result.a, retraceOpacity2);
                }

                // ─── 4. PHOSPHOR FLICKER ───
                float flickerFreq = 8.0 + failure * 20.0;
                float flicker = sin(_Time.y * flickerFreq * 3.14159);
                float flickerAmount = failure * 0.15;
                result.rgb *= 1.0 - flicker * flickerAmount;
                result.a *= 1.0 - flicker * flickerAmount * 0.5;

                // ─── 5. UV JITTER ───
                float jitter = sin(uv.y * 40.0 + _Time.y * 30.0) * failure * 0.05;
                result.rgb *= 1.0 + jitter;

                // ─── 6. COLLAPSE ───
                if (_Collapse > 0.001)
                {
                    float lineHalfHeight = (1.0 - _Collapse) * 0.5;
                    float distFromCenter = abs(uv.y - 0.5);

                    float surviveMask = 1.0 - smoothstep(
                        lineHalfHeight - 0.01,
                        lineHalfHeight + 0.01,
                        distFromCenter
                    );

                    float dotThreshold = smoothstep(0.7, 1.0, _Collapse);
                    float distFromCenterX = abs(uv.x - 0.5);
                    float hSurviveMask = 1.0 - smoothstep(
                        (1.0 - _Collapse) * 0.5 * (1.0 - dotThreshold),
                        (1.0 - _Collapse) * 0.5 * (1.0 - dotThreshold) + 0.01,
                        distFromCenterX
                    );
                    hSurviveMask = lerp(1.0, hSurviveMask, dotThreshold);

                    float totalSurvive = surviveMask * hSurviveMask;

                    float darken = 1.0 - totalSurvive;
                    result.rgb *= 1.0 - darken * 0.9;
                    result.a *= 1.0 - darken * 0.7;

                    float glowBoost = _Collapse * 0.5;
                    result.rgb += _PhosphorColor.rgb * glowBoost * totalSurvive;
                    result.a = max(result.a, totalSurvive * (0.3 + _Collapse * 0.5));
                }

                // ─── 7. RGB SPLIT ───
                if (_RGBSplit > 0.0001)
                {
                    result.r += _RGBSplit * 2.0;
                    result.b -= _RGBSplit * 2.0;
                }

                // ─── 8. CONSCIENCE PULSE ───
                if (_ConsciencePulse > 0.001)
                {
                    float heartbeat = sin(_Time.y * 7.536) * 0.5 + 0.5;
                    heartbeat = pow(heartbeat, 3.0);
                    float pulse = heartbeat * _ConsciencePulse;
                    result.rgb += _PhosphorColor.rgb * pulse * 0.3;
                    result.a = max(result.a, pulse * 0.2);
                }

                // ─── TRANSITION OPACITY ───
                result.a *= _TransitionOpacity;

                // ─── FINAL CLAMP ───
                result.a = clamp(result.a, 0.0, 1.0);
                result.rgb = max(result.rgb, 0.0);

                return result;
            }
            ENDCG
        }
    }

    Fallback Off
}   