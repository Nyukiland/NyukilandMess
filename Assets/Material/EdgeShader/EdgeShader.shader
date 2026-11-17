Shader "Custom/BuiltIn/PureWireframeLit"
{
    Properties
    {
        [Toggle] _NormalColorMode ("Color Per Normal", Float) = 0
        [HDR] _EdgeColor ("Edge Color", Color) = (0, 1, 1, 1)
        _FillColor ("Fill Color", Color) = (0.15, 0.15, 0.15, 0)
        _EdgeWidth ("Edge Width (px)", Range(0.5, 8.0)) = 2.0
        _LitAmount ("Edge Lighting Influence", Range(0.0, 1.0)) = 1.0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ForwardBase"
            Tags { "LightMode" = "ForwardBase" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Back

            CGPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma multi_compile_fwdbase

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            float _NormalColorMode;
            half4 _EdgeColor;
            half4 _FillColor;
            float _EdgeWidth;
            float _LitAmount;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2g
            {
                float4 pos      : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 vertex   : TEXCOORD2;
                float3 normalOS : TEXCOORD3;
            };

            struct g2f
            {
                float4 pos      : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 bary     : TEXCOORD2;
                float3 normalOS : TEXCOORD3;
                SHADOW_COORDS(4)
            };

            v2g vert(appdata v)
            {
                v2g o;
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normalWS = UnityObjectToWorldNormal(v.normal);
                o.vertex   = v.vertex;
                o.normalOS = v.normal;
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream)
            {
                float3 baryCoords[3] = {
                    float3(1.0, 0.0, 0.0),
                    float3(0.0, 1.0, 0.0),
                    float3(0.0, 0.0, 1.0)
                };

                for (int i = 0; i < 3; i++)
                {
                    v2g v = IN[i];
                    g2f o;
                    UNITY_INITIALIZE_OUTPUT(g2f, o);
                    o.pos      = v.pos;
                    o.normalWS = v.normalWS;
                    o.worldPos = v.worldPos;
                    o.bary     = baryCoords[i];
                    o.normalOS = v.normalOS;
                    TRANSFER_SHADOW(o);
                    triStream.Append(o);
                }
            }

            half4 frag(g2f i) : SV_Target
            {
                float3 fw = max(fwidth(i.bary), 1e-5);
                float3 a = smoothstep(fw * max(0.0, _EdgeWidth - 0.5), fw * (_EdgeWidth + 0.5), i.bary);
                float edgeMask = 1.0 - min(min(a.x, a.y), a.z);

                if (_NormalColorMode > 0.5)
                {
                    half3 normalColor = normalize(i.normalOS) * 0.5 + 0.5;
                    half4 faceFill = half4(normalColor, 1.0);
                    return lerp(faceFill, _EdgeColor, edgeMask * _EdgeColor.a);
                }

                float3 n = normalize(i.normalWS);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

                float ndotl = saturate(dot(n, lightDir));
                float3 lighting = ShadeSH9(half4(n, 1.0)) + _LightColor0.rgb * (ndotl * atten);

                half4 litFill = half4(_FillColor.rgb * lighting, _FillColor.a);
                half4 litEdge = half4(_EdgeColor.rgb * lerp(1.0, lighting, _LitAmount), _EdgeColor.a);

                return lerp(litFill, litEdge, edgeMask);
            }
            ENDCG
        }
    }
}