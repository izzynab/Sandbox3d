// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Particle"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _Size ("Mesh Size",float)= 1
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


            struct Particle{
			    float3 position;
			    float3 velocity;
			    float life;
		    };
		
		    StructuredBuffer<Particle> particleBuffer;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

            float _Size;

            v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
            {
                Particle data = particleBuffer[instanceID];

                float3 localPosition = v.vertex.xyz * _Size;
                float3 worldPosition = data.position + localPosition;

                v2f o;
                o.pos = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0f));
                // Color
			    float life = particleBuffer[instanceID].life;
			    float lerpVal = life * 0.25f;
			    o.color = fixed4(1.0f - lerpVal+0.1, lerpVal+0.1, 1.0f, lerpVal);

                return o;
            }

             // color from the material
            fixed4 _Color;

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}
