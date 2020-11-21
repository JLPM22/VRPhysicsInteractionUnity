Shader "VRMovement/ArcVRShader"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_GapPer("Gap Percentage", Range(0,1)) = 0.2
		_Speed("Speed", Float) = 1.0
		_NumberSegments("Number Segments", Float) = 1.0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "PreviewType" = "Plane" "IgnoreProjector" = "True" }

		Cull Off
		Lighting Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"

				struct appdata_t
				{
					float4 vertex   : POSITION;
					float2 texCoord : TEXCOORD0;
				};

				struct v2f
				{
					float4 vertex   : SV_POSITION;
					float2 texCoord : TEXCOORD0;
				};

				fixed4 _Color;
				float _GapPer;
				float _Speed;
				float _NumberSegments;

				v2f vert(appdata_t i)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(i.vertex);
					o.texCoord = i.texCoord;
					return o;
				}
				
				fixed4 frag(v2f IN) : SV_Target
				{
					IN.texCoord.x = fmod((IN.texCoord.x * _NumberSegments), 1.0f);
					float start = frac(_Time.y * _Speed);
					float end = start + _GapPer;
					float excess = saturate(end - 1.0f);
					return fixed4(_Color.xyz, _Color.a * !((IN.texCoord.x >= start && IN.texCoord.x < end) || IN.texCoord.x < excess));
				}
			ENDCG
		}
    }
    FallBack "Diffuse"
}
