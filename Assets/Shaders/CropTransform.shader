Shader "Unlit/CropTransform"
{
	Properties{
		_MainTex("Source", 2D) = "black" {}
		_Crop("XYWH 0..1", Vector) = (0,0,1,1)
		_RotationDeg("Rotation", Float) = 0
		_FlipX("FlipX", Float) = 0
		_FlipY("FlipY", Float) = 0
	}
		SubShader{
			Tags{"Queue" = "Geometry"} ZWrite Off Cull Off
			Pass{
				HLSLPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"
				sampler2D _MainTex;
				float4 _Crop; float _RotationDeg,_FlipX,_FlipY;
				struct V { float4 pos:POSITION; float2 uv:TEXCOORD0; };
				struct F { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
				F vert(V v) { F o; o.pos = UnityObjectToClipPos(v.pos); o.uv = v.uv; return o; }
				float2 rot(float2 p, float2 c, float a) { float s = sin(a),c0 = cos(a); p -= c; float2 r = float2(c0*p.x - s * p.y, s*p.x + c0 * p.y); return r + c; }
				fixed4 frag(F i) :SV_Target{
					float2 uv = i.uv;
					if (_FlipX > 0.5) uv.x = 1 - uv.x;
					if (_FlipY > 0.5) uv.y = 1 - uv.y;
					float2 sub = _Crop.xy, size = _Crop.zw, center = sub + size * 0.5;
					uv = sub + uv * size;
					uv = rot(uv, center, radians(_RotationDeg));
					return tex2D(_MainTex, uv);
				}
				ENDHLSL
			}
		}
}
