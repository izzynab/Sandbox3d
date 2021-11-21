Shader "Custom/Particle"
{
   	SubShader {
		Pass {
		Tags{ "RenderType" = "Opaque" }
		LOD 200
		Blend SrcAlpha one

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma vertex vert
		#pragma fragment frag

		#include "UnityCG.cginc"

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 4.5


		struct v2f{
			float4 position : SV_POSITION;
			float4 color : COLOR;
		};
		
		
		struct Particle{
			float3 position;
			float3 velocity;
			float life;
		};
		
		StructuredBuffer<Particle> particleBuffer;
		

		v2f vert( uint instance_id : SV_InstanceID)
		{
			v2f o = (v2f)0;

			// Color
			float life = particleBuffer[instance_id].life;
			float lerpVal = life * 0.25f;
			o.color = fixed4(1.0f - lerpVal+0.1, lerpVal+0.1, 1.0f, lerpVal);

			// Position
			o.position = UnityObjectToClipPos(float4(particleBuffer[instance_id].position, 1.0f));

			return o;
		}

		float4 frag(v2f i) : COLOR
		{
			return i.color;
		}


		ENDCG
		}
	}
	FallBack Off
}