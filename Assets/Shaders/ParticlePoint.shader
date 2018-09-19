Shader "VRTest/Data point billboard"
{
	Properties
	{
		_Color("Color", Color) = (0, 0, 0.5, 0.3)
		_PointSize("Point Size", Range(0, 0.1)) = 0.001
		_Opacity("Opacity", Range(0, 1)) = 0.2
		_SpriteSheet("Sprite sheet", 2D) = "white" {}
		_NumSprites("Number of sprites", Int) = 1
		_ShapeIndex("Shape index", Int) = 0
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
            // 2) Vertex shader transforms point positions to world space, passes through color and opacity, as well as per-vertex camera-facing axes 
            // 3) Geometry shader explodes a single point to a billboarded 4-vertex triangle strip, based on the camera-facing axes, assigns texture coordinates based on sprite sheet and selected sprite index
            // 4) fragment shader shades each pixel after looking up the shape texture            
			#pragma vertex vertexShaderBufferLookup
			#pragma geometry geometryShaderBillboard
			#pragma fragment fragmentShaderSprite

			#include "UnityCG.cginc"
			#include "ParticlePoint.cginc"
	ENDCG
		}
		
		Pass
		{
			ZWrite Off
			Blend One One

			CGPROGRAM
			#pragma target 5.0
            
            // Identical to above, but the vertex shader uses spherical coordinates for position lookup
			#pragma vertex vertexShaderBufferLookupSpherical
			#pragma geometry geometryShaderBillboard
			#pragma fragment fragmentShaderSprite

			#include "UnityCG.cginc"
			#include "ParticlePoint.cginc"			
	ENDCG
		}
	}
		Fallback Off
}