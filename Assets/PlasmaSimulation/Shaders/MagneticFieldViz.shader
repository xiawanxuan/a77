Shader "PlasmaSim/MagneticFieldViz"
{
    Properties
    {
        _MagneticFieldTex ("Magnetic Field", 2D) = "black" {}
        _MaxField ("Max Field", Float) = 100.0
        _Intensity ("Intensity", Float) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always

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

            sampler2D _MagneticFieldTex;
            float _MaxField;
            float _Intensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 HSVToRGB(float3 hsv)
            {
                float h = hsv.x * 6.0;
                float s = hsv.y;
                float v = hsv.z;
                float c = v * s;
                float x = c * (1.0 - abs(fmod(h, 2.0) - 1.0));
                float m = v - c;
                float3 col;
                if (h < 1.0) col = float3(c, x, 0);
                else if (h < 2.0) col = float3(x, c, 0);
                else if (h < 3.0) col = float3(0, c, x);
                else if (h < 4.0) col = float3(0, x, c);
                else if (h < 5.0) col = float3(x, 0, c);
                else col = float3(c, 0, x);
                return col + m;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float Bz = tex2D(_MagneticFieldTex, i.uv).r;
                float absB = abs(Bz);

                float normalized = clamp(absB / _MaxField, 0.0, 1.0);

                float signB = sign(Bz);

                float3 col;
                if (signB > 0)
                {
                    float3 hsv = float3(0.66, 1.0, normalized * _Intensity);
                    col = HSVToRGB(hsv);
                }
                else if (signB < 0)
                {
                    float3 hsv = float3(0.0, 1.0, normalized * _Intensity);
                    col = HSVToRGB(hsv);
                }
                else
                {
                    col = float3(0.2, 0.2, 0.2);
                }

                float alpha = smoothstep(0.0, 0.3, normalized) * 0.6;

                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}
