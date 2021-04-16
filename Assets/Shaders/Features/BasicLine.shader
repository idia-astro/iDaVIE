Shader "IDIA/BasicLine"
{
    Properties
    {
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True"
        }
        Pass
        {
            // Basic additive blending
            ZWrite Off
            Blend One One

            CGPROGRAM
            #pragma target 5.0

            // Points to pixels pipeline is as follows: 
            // 1) Vertex shader pulls vertex data from the buffer, based on the verted id passed by the GPU.
            // 2) Vertex shader transforms point positions to world space, passes through color and opacity 
            // 4) fragment shader shades each pixel with the point color            
            #pragma vertex vsLine
            #pragma fragment fsSimple

            #include "UnityCG.cginc"
            #include "BasicLine.cginc"
            ENDCG
        }
    }
    Fallback Off
}