// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/InstancedShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
          
            #pragma target 4.5

            #include "UnityCG.cginc"


            void rotate2D(inout float2 v, float r)
            {
                float s, c;
                sincos(r, s, c);
                v = float2(v.x * c - v.y * s, v.x * s + v.y * c);
            }

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv_MainTex : TEXCOORD0;
            };

            sampler2D _MainTex;

             #if SHADER_TARGET >= 45
            StructuredBuffer<float4> positionBuffer;
        #endif

            v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
            {
            #if SHADER_TARGET >= 45
                float4 data = positionBuffer[instanceID];
            #else
                float4 data = 0;
            #endif

                //float rotation = data.w * data.w * _Time.x * 0.5f;
                //rotate2D(data.xz, rotation);

                float3 localPosition = v.vertex.xyz * data.w;
                float3 worldPosition = data.xyz + localPosition;
                float3 worldNormal = v.normal;

                v2f o;
                o.pos = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0f));
                //o.pos = UnityObjectToClipPos(v.vertex);
                o.uv_MainTex = v.texcoord;

                return o;
            }

             // color from the material
            fixed4 _Color;

            fixed4 frag (v2f i) : SV_Target
            {
                //fixed shadow = SHADOW_ATTENUATION(i);
                //fixed4 albedo = tex2D(_MainTex, i.uv_MainTex);
                //float3 lighting = i.diffuse * shadow + i.ambient;
                fixed4 output = _Color;
                //UNITY_APPLY_FOG(i.fogCoord, output);
                return output;
            }
            ENDCG
        }
    }
}
