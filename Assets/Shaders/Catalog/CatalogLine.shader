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
Shader "IDIA/CatalogLine"
{
	Properties
	{
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