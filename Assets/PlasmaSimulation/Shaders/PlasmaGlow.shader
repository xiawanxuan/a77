Shader "PlasmaSim/PlasmaGlow"
{
    Properties
    {
        _DensityTex ("Density", 2D) = "black" {}
        _TemperatureTex ("Temperature", 2D) = "black" {}
        _GlowIntensity ("Glow Intensity", Float) = 2.0
        _GlowRadius ("Glow Radius", Float) = 1.5
        _MaxTemp ("Max Temperature", Float) = 30000
        _MinDensity ("Min Density", Float) = 0.01
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
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

            sampler2D _DensityTex;
            sampler2D _TemperatureTex;
            float _GlowIntensity;
            float _GlowRadius;
            float _MaxTemp;
            float _MinDensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 plasmaColor(float normalizedTemp)
            {
                float3 c;
                if (normalizedTemp < 0.15)
                {
                    c = lerp(float3(0, 0, 0), float3(0.2, 0, 0.4), normalizedTemp / 0.15);
                }
                else if (normalizedTemp < 0.3)
                {
                    c = lerp(float3(0.2, 0, 0.4), float3(0.6, 0, 0.2), (normalizedTemp - 0.15) / 0.15);
                }
                else if (normalizedTemp < 0.5)
                {
                    c = lerp(float3(0.6, 0, 0.2), float3(1, 0.3, 0), (normalizedTemp - 0.3) / 0.2);
                }
                else if (normalizedTemp < 0.7)
                {
                    c = lerp(float3(1, 0.3, 0), float3(1, 0.8, 0.2), (normalizedTemp - 0.5) / 0.2);
                }
                else
                {
                    c = lerp(float3(1, 0.8, 0.2), float3(1, 1, 1), (normalizedTemp - 0.7) / 0.3);
                }
                return saturate(c);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float density = tex2D(_DensityTex, i.uv).r;
                float temperature = tex2D(_TemperatureTex, i.uv).r;

                float normalizedTemp = saturate(temperature / _MaxTemp);
                float normalizedDensity = saturate(density / 2.0);

                if (normalizedDensity < _MinDensity) discard;

                float3 color = plasmaColor(normalizedTemp);

                float alpha = normalizedDensity * _GlowIntensity;
                alpha *= smoothstep(_MinDensity, 0.3, normalizedDensity);

                float coreGlow = exp(-normalizedDensity * 2.0) * normalizedTemp;
                color += float3(0.1, 0.1, 0.2) * coreGlow;

                float edgeFalloff = smoothstep(0.0, 0.5, i.uv.x) * smoothstep(0.0, 0.5, i.uv.y)
                                  * smoothstep(0.0, 0.5, 1.0 - i.uv.x) * smoothstep(0.0, 0.5, 1.0 - i.uv.y);

                return float4(color * alpha * edgeFalloff, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
