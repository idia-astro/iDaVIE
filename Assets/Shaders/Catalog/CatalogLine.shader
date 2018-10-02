Shader "IDIA/CatalogLine"
{
	Properties
	{
		_Color("Color", Color) = (0, 0, 0.5, 0.3)
		_Opacity("Opacity", Range(0, 1)) = 0.2
	}

	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
		Pass
		{
		    // Basic additive blending
			ZWrite Off
			Blend One One

			CGPROGRAM
			#pragma target 5.0

            // Points to pixels pipeline is as follows: 
            // 1) Vertex shader pulls vertex data from the buffer, based on the verted id passed by the GPU.
            // 2) Vertex shader transforms point positions to world space, passes through color and opacity, as well as a line vector 
            // 3) Geometry shader extrudes a single point into a line
            // 4) fragment shader shades each pixel with the point color            
			#pragma vertex vsLine
			#pragma geometry gsLine
			#pragma fragment fsSimple

			#include "UnityCG.cginc"
			#include "CatalogLine.cginc"
	ENDCG
		}
		
		Pass
		{
			ZWrite Off
			Blend One One

			CGPROGRAM
			#pragma target 5.0
            
            // Identical to above, but the vertex shader uses spherical coordinates for position lookup
			#pragma vertex vsLineSpherical
			#pragma geometry gsLine
			#pragma fragment fsSimple

			#include "UnityCG.cginc"
			#include "CatalogLine.cginc"			
	ENDCG
		}
	}
		Fallback Off
}