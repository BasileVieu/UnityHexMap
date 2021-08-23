﻿Shader "Custom/River"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Specular ("Specular", Color) = (0.2, 0.2, 0.2)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+1"
        }

        LOD 200

        CGPROGRAM
        #pragma surface surf StandardSpecular alpha vertex:vert
        #pragma target 3.0
        
        #pragma multi_compile _ HEX_MAP_EDIT_MODE

        #include "Water.cginc"
        #include "HexCellData.cginc"

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float2 visibility;
        };

        half _Glossiness;
		fixed3 _Specular;
        fixed4 _Color;

		void vert (inout appdata_full v, out Input data)
		{
			UNITY_INITIALIZE_OUTPUT(Input, data);

			float4 cell0 = GetCellData(v, 0);
			float4 cell1 = GetCellData(v, 1);
            
            data.visibility.x = cell0.x * v.color.x + cell1.x * v.color.y;
            data.visibility.x = lerp(0.25, 1, data.visibility.x);
            data.visibility.y = cell0.y * v.color.x + cell1.y * v.color.y;
		}

        void surf (Input IN, inout SurfaceOutputStandardSpecular o)
        {
            float river = River(IN.uv_MainTex, _MainTex);

            fixed4 c = saturate(_Color + river);
            
            float explored = IN.visibility.y;

            o.Albedo = c.rgb * IN.visibility.x;
			o.Specular = _Specular * explored;
			o.Smoothness = _Glossiness;
			o.Occlusion = explored;
			o.Alpha = c.a * explored;
        }

        ENDCG
    }

    FallBack "Diffuse"
}