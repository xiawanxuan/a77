Shader "PlasmaSim/ThermalOverlay"
{
    Properties
    {
        _TemperatureTex ("Temperature", 2D) = "black" {}
        _DensityTex ("Density", 2D) = "black" {}
        _MaxTemp ("Max Temperature", Float) = 30000
        _Alpha ("Overlay Alpha", Range(0,1)) = 0.6
        _MinDensity ("Min Density Threshold", Float) = 0.01
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+1" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

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

            sampler2D _TemperatureTex;
            sampler2D _DensityTex;
            float _MaxTemp;
            float _Alpha;
            float _MinDensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 thermalColor(float t)
            {
                float3 c;
                if (t < 0.25)
                {
                    c = lerp(float3(0, 0, 0.2), float3(0, 0, 1), t / 0.25);
                }
                else if (t < 0.5)
                {
                    c = lerp(float3(0, 0, 1), float3(0, 1, 0), (t - 0.25) / 0.25);
                }
                else if (t < 0.75)
                {
                    c = lerp(float3(0, 1, 0), float3(1, 1, 0), (t - 0.5) / 0.25);
                }
                else
                {
                    c = lerp(float3(1, 1, 0), float3(1, 0, 0), (t - 0.75) / 0.25);
                }
                return c;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float temperature = tex2D(_TemperatureTex, i.uv).r;
                float density = tex2D(_DensityTex, i.uv).r;

                float normalizedTemp = saturate(temperature / _MaxTemp);
                float normalizedDensity = saturate(density / 2.0);

                if (normalizedDensity < _MinDensity && normalizedTemp < 0.01)
                    discard;

                float3 color = thermalColor(normalizedTemp);

                float alpha = _Alpha * saturate(normalizedTemp * 3.0);
                alpha *= smoothstep(_MinDensity * 0.5, _MinDensity * 2.0, normalizedDensity);

                return float4(color, alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
