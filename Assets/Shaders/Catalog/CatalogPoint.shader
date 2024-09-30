//
// iDaVIE (immersive Data Visualisation Interactive Explorer)
// Copyright (C) 2024 IDIA, INAF-OACT
//
// This file is part of the iDaVIE project.
//
// iDaVIE is free software: you can redistribute it and/or modify it under the terms 
// of the GNU Lesser General Public License (LGPL) as published by the Free Software 
// Foundation, either version 3 of the License, or (at your option) any later version.
//
// iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR 
// PURPOSE. See the GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License along with 
// iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
//
// Additional information and disclaimers regarding liability and third-party 
// components can be found in the DISCLAIMER and NOTICE files included with this project.
//
//
Shader "IDIA/CatalogPoint"
{
	Properties
	{
		_SpriteSheet("Sprite sheet", 2D) = "white" {}
		_NumSprites("Number of sprites", Int) = 1
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
			#pragma vertex vsPointBillboard
			#pragma geometry gsBillboard
			#pragma fragment fsSprite

			#include "UnityCG.cginc"
			#include "CatalogPoint.cginc"
	ENDCG
		}
		
		Pass
		{
			ZWrite Off
			Blend One One

			CGPROGRAM
			#pragma target 5.0
            
            // Identical to above, but the vertex shader uses spherical coordinates for position lookup
			#pragma vertex vsPointBillboardSpherical
			#pragma geometry gsBillboard
			#pragma fragment fsSprite

			#include "UnityCG.cginc"
			#include "CatalogPoint.cginc"			
	ENDCG
		}
	}
		Fallback Off
}