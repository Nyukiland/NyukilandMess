Shader "Unlit/WaterSideWithCircleLightColor"
{
    Properties
    {
        _WaterColor1 ("Water Color (Dark)", Color) = (0, 0.5, 1, 1)
        _WaterColor2 ("Water Color (Light)", Color) = (0, 1, 0.5, 1)
        _Speed ("Movement Speed", Float) = 1.0
        _Scale ("Noise Scale", Float) = 10.0
        _Transparency ("Transparency", Range(0, 1)) = 0.5
        _DeformFrequency ("Deformation Frequency", Float) = 10.0
        _DeformAmplitude ("Deformation Amplitude", Float) = 0.1
        _NoiseOffset ("Noise Offset", Float) = 0.0
        _NoiseScale ("Noise Scale", Float) = 1.0

        // Properties for the circle effect
        _LightPosition ("Light Position", Vector) = (0, 5, 0, 0) // Position of the center of the light circle
        _LightRadius ("Light Radius", Float) = 2.0 // Radius of the light effect (how large the light's influence is)
        _LightIntensity ("Light Intensity", Float) = 1.0 // Intensity of the light effect

        // New property for the light color
        _LightColor ("Light Color", Color) = (1, 1, 1, 1) // Default white light color
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // Water properties
            float4 _WaterColor1;
            float4 _WaterColor2;
            float _Speed;
            float _Scale;
            float _Transparency;
            float _DeformFrequency;
            float _DeformAmplitude;
            float _NoiseOffset;
            float _NoiseScale;

            // Light properties
            float4 _LightPosition; // Position of the light in world space
            float _LightRadius;    // Radius of the light effect (how large the light's influence is)
            float _LightIntensity; // Intensity of the light effect
            float4 _LightColor;    // Light color (user-controlled)

            // Function for pseudo-random gradient generation
            float2 random2(float2 p)
            {
                float2 randomVec = frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453);
                return randomVec * 2.0 - 1.0; // Return a vector of values in the range [-1, 1]
            }

            // Perlin noise function with deformation
            float perlin(float2 p)
            {
                float time = _Time.y * _Speed; // Apply time-based speed
                p.x += sin(time + p.y * _DeformFrequency) * _DeformAmplitude; // Deformation in X
                p.y += cos(time + p.x * _DeformFrequency) * _DeformAmplitude; // Deformation in Y

                p *= _NoiseScale;
                p += _NoiseOffset; // Offset for more variation

                // Calculate integer and fractional parts
                float2 i = floor(p);
                float2 f = frac(p);

                // Smooth interpolation
                float2 u = f * f * (3.0 - 2.0 * f);

                // Generate gradients at the corners
                float n00 = dot(random2(i + float2(0.0, 0.0)), f - float2(0.0, 0.0));
                float n01 = dot(random2(i + float2(0.0, 1.0)), f - float2(0.0, 1.0));
                float n10 = dot(random2(i + float2(1.0, 0.0)), f - float2(1.0, 0.0));
                float n11 = dot(random2(i + float2(1.0, 1.0)), f - float2(1.0, 1.0));

                return lerp(lerp(n00, n10, u.x), lerp(n01, n11, u.x), u.y);
            }

            // Function to calculate the circle effect (light effect)
            float computeLightEffect(float2 uv)
            {
                // Calculate the distance from the light position (world space position)
                float2 lightPos = _LightPosition.xy; // Get the light position from the properties
                float dist = distance(uv, lightPos);

                // Return a light effect factor: high intensity near the center, lower intensity at the edge
                // The light effect fades out as it gets farther from the light's center
                return smoothstep(_LightRadius, _LightRadius - 0.1, dist) * _LightIntensity;
            }

            // Vertex shader
            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Fragment shader
            fixed4 frag(v2f i) : SV_Target
            {
                // Generate Perlin noise for water deformation
                float2 uv = i.uv * _Scale; // Apply noise scale to the UVs
                float noise = perlin(uv);

                // Interpolate between two water colors based on the Perlin noise
                fixed4 waterColor = lerp(_WaterColor1, _WaterColor2, noise);

                // Compute the light effect factor from the circle's light area
                float lightEffect = computeLightEffect(uv);

                // Apply the light color only inside the circle area
                waterColor.rgb = lerp(waterColor.rgb, _LightColor.rgb, lightEffect);

                // Apply transparency to the water color
                waterColor.a *= _Transparency;

                return waterColor;
            }

            ENDCG
        }
    }

    Fallback "Diffuse"
}