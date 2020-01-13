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