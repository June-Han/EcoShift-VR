Shader "EcoShift/UnlitNoFog"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0.8, 1, 0.85)
        _EmissionStrength ("Emission Strength", Range(0, 5)) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        LOD 100

        // Render on top of everything, blend transparency
        ZTest Always
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            // nofog is the key — this makes the arrow immune to scene fog
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color;
            float _EmissionStrength;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Bright emissive color — no fog applied
                fixed4 col = _Color * _EmissionStrength;
                col.a = _Color.a;
                return col;
            }
            ENDCG
        }
    }
}
