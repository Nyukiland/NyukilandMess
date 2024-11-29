Shader "Custom/SimpleLight"
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
            Blend SrcAlpha OneMinusSrcAlpha

            // Queue tag to control render order
            Tags { "Queue"="Background" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            // Shader properties
            fixed4 _LightColor;
            float _LightRange;
            float4 _LightPosition; // Declare the light position as a uniform

            // Simple attenuation function based on distance from light
            float CalculateAttenuation(float3 worldPos, float3 lightPos, float lightRange)
            {
                float dist = distance(worldPos, lightPos);
                return saturate(1.0 - (dist / lightRange)); // Attenuation function
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz; // Get world position of vertex
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Calculate light intensity based on distance attenuation
                float attenuation = CalculateAttenuation(i.worldPos, _LightPosition.xyz, _LightRange);
                return _LightColor * attenuation; // Apply light color and attenuation
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
