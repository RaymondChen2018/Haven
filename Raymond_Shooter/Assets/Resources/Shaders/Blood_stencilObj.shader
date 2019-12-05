Shader "Custom/Blood_stencilObj" {
	Properties{
		_Color("Color", Color) = (0.2,0,0,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
	_Glossiness("Smoothness", Range(0,1)) = 0.9
		//_Metallic ("Metallic", Range(0,1)) = 0.0
		_Specular("Specular", Color) = (0.3,0,0,1)
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		LOD 200

		Stencil{
		Ref 2
		Comp Equal
	}

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
#pragma surface surf StandardSpecular fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
#pragma target 3.0

		sampler2D _MainTex;

	struct Input {
		float2 uv_MainTex;
	};

	half _Glossiness;
	half _Metallic;
	fixed4 _Color;
	fixed4 _Specular;

	void surf(Input IN, inout SurfaceOutputStandardSpecular o) {
		// Albedo comes from a texture tinted by color
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
		fixed4 s = _Specular;
		o.Albedo = c.rgb;
		o.Specular = s.rgb;
		// Metallic and smoothness come from slider variables
		//o.Metallic = _Metallic;
		o.Smoothness = _Glossiness;
		o.Alpha = c.a;


	}
	ENDCG
	}
		FallBack "Diffuse"
}

