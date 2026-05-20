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
Shader "IDIA/BasicVolume"
{
	Properties
	{
		_DataCube("Data Cube", 3D) = "white" {}
		_ColorMap("Color Map", 2D) = "white" {}
		_NumColorMaps("Number of Color Maps", Int) = 80
		_ColorMapIndex("Color Map Index", Range(0, 80)) = 0
		_SliceMin("Slice Min", Vector) = (0,0,0,1)
		_SliceMax("Slice Max", Vector) = (1,1,1,1)
		_ThresholdMin("Threshold Min", Range(0,1)) = 0
		_ThresholdMax("Threshold Max", Range(0,1)) = 1
		_ScaleMin("Data Scale Min" , Float) = 0
		_ScaleMax("Data Scale Max" , Float) = 1
		_Jitter("Jitter amount", Range(0,1)) = 0
		_MaxSteps("Maximum step count", Range(16,512)) = 128
	}
		SubShader
		{
			Tags { "Queue" = "Transparent-100" }

			Pass
			{
				Blend SrcAlpha OneMinusSrcAlpha
				Cull Front ZWrite Off ZTest Always

				CGPROGRAM
				#pragma multi_compile __ SHADER_AIP
				#pragma vertex vertexShaderVolume
				#pragma fragment fragmentShaderRayMarch

				#include "UnityCG.cginc"
				#include "BasicVolume.cginc"

				ENDCG
			}
		}
}