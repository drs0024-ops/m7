Shader "Game/CloakingShader"
{
    Properties
    {
        [MainTex] _MainTex ("Sprite Texture", 2D) = "white" {}
        _CloakFactor ("Cloak Factor", Range(0, 1)) = 0
        _GlowColor ("Glow Color", Color) = (0.3, 0.9, 1.0, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 1.5
        _DilateAmount ("Dilate Amount", Range(0, 0.1)) = 0.02
        _GlowSoftness ("Glow Softness", Range(0, 1)) = 0.5
    }

    SubShader
    {
        LOD 100
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        // ─── PASS 1: GLOW (Additive) ───
        Pass
        {
            Name "Glow"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Off
            Lighting Off
            ZWrite Off
            ZTest LEqual
            Blend One One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.5
            #pragma multi_compile_fog
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
                UNITY_FOG_COORDS(1)
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _CloakFactor;
            float4 _GlowColor;
            float _GlowIntensity;
            float _DilateAmount;
            float _GlowSoftness;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // Correct for UV tiling so dilate amount is consistent
                float2 stScale = _MainTex_ST.xy;
                float2 d = _DilateAmount / stScale;

                // Sample center + 4 dilated neighbors
                float a0 = tex2D(_MainTex, i.uv).a;
                float a1 = tex2D(_MainTex, i.uv + float2(d.x, 0)).a;
                float a2 = tex2D(_MainTex, i.uv - float2(d.x, 0)).a;
                float a3 = tex2D(_MainTex, i.uv + float2(0, d.y)).a;
                float a4 = tex2D(_MainTex, i.uv - float2(0, d.y)).a;

                // Dilated alpha = max of all samples
                float dilated = max(max(max(a0, a1), a2), max(a3, a4));

                // Halo ring only (where dilated extends beyond center)
                float halo = saturate(dilated - a0);

                // Soft falloff curve:
                //   Softness 0 → pow(halo, 2.0)  tight, harder edge
                //   Softness 1 → pow(halo, 0.3)  wide, very soft
                float falloffExp = lerp(2.0, 0.3, _GlowSoftness);
                halo = pow(halo, falloffExp);

                float glow = halo * _CloakFactor * _GlowIntensity;
                glow = saturate(glow);

                half4 col = half4(_GlowColor.rgb * glow, glow);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }

        // ─── PASS 2: BODY (Alpha blend) ───
        Pass
        {
            Name "Body"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Off
            Lighting Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.5
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_FOG_COORDS(1)
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _CloakFactor;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 tex = tex2D(_MainTex, i.uv);
                float alpha = tex.a * (1.0 - _CloakFactor);
                half4 col = half4(tex.rgb * i.color.rgb, alpha);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }

    Fallback "Sprite-Unlit-2D"
}   