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
Shader "IDIA/VolumeMaskLines"
{
	Properties
	{
	}

	SubShader
	{
		Tags { "Queue" = "Transparent-99" "RenderType" = "Transparent" }
		Pass
		{
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
			CGPROGRAM
			#pragma target 5.0

            // Points to pixels pipeline is as follows: 
            // 1) Vertex shader pulls vertex data from the buffer, based on the verted id passed by the GPU.
            // 2) Vertex shader transforms index position to voxel coordinates, and then to world space, passes through color, as well as a line vector 
            // 3) Geometry shader extrudes a single point into a box
            // 4) fragment shader shades each pixel with the point color            
			#pragma vertex vsMask
			#pragma geometry gsMask
			#pragma fragment fsMask

			#include "UnityCG.cginc"
			#include "VolumeMask.cginc"
	ENDCG
		}				
	}
		Fallback Off
}