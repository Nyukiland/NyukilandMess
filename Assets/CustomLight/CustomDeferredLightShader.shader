Shader "Custom/DeferredLight"
{
    Properties
    {
        _LightColor ("Light Color", Color) = (1, 1, 1, 1)
        _LightRange ("Light Range", Float) = 5.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Name "CustomLightPass"
            ZWrite Off
            ZTest LEqual
            Blend One One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            fixed4 _LightColor;
            float _LightRange;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Example color output, adjust for your light model
                return _LightColor * (_LightRange * 0.1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}