Shader "PlasmaSim/ElectricFieldViz"
{
    Properties
    {
        _EFieldTex ("Electric Field", 2D) = "black" {}
        _PotentialTex ("Potential", 2D) = "black" {}
        _DensityTex ("Density", 2D) = "black" {}
        _FieldStrength ("Field Strength", Float) = 1.0
        _ArrowSpacing ("Arrow Spacing", Float) = 0.05
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+2" }
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

            sampler2D _EFieldTex;
            sampler2D _PotentialTex;
            sampler2D _DensityTex;
            float _FieldStrength;
            float _ArrowSpacing;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float arrow(float2 p, float2 dir)
            {
                float len = length(dir);
                if (len < 0.001) return 0;

                float2 nd = dir / len;
                float2 np = float2(-nd.y, nd.x);

                float along = dot(p, nd);
                float perp = abs(dot(p, np));

                float shaft = smoothstep(0.02, 0.01, perp) * step(0, along) * step(along, len * 0.7);
                float head = smoothstep(len * 0.5, len * 0.7, along) * smoothstep(0.04, 0.01, perp + (along - len * 0.7) * 0.5) * step(0, along);

                return max(shaft, head);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 eField = tex2D(_EFieldTex, i.uv).rg;
                float fieldMag = length(eField);
                float density = tex2D(_DensityTex, i.uv).r;

                if (fieldMag < 0.01) discard;

                float2 gridUv = fmod(i.uv, _ArrowSpacing);
                gridUv = gridUv - _ArrowSpacing * 0.5;

                float arr = arrow(gridUv, normalize(eField) * _ArrowSpacing * 0.4);

                float3 color = float3(0.2, 0.6, 1.0) * min(fieldMag * _FieldStrength, 1.0);
                float alpha = arr * 0.8;

                alpha *= smoothstep(0.001, 0.01, density);

                return float4(color, alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
