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
			
			#define NUM_COLOR_MAP_STEPS 256
			
			#define LINEAR 0
			#define LOG 1
			#define SQRT 2
			#define SQUARED 3
			#define EXP 4
			

            // This matches the structure defined on the CPU. Structured buffers are awesome
			struct DataPoint
			{
				float3 position;
				float value;
			};

            // Points -> <VS> -> VertexShaderOutput -> <GS> -> PixelShaderInput -> <PS> -> Pixels 
			struct VertexShaderOutput
			{
				float4 position : SV_POSITION;
				float4 color : COLOR;
				float3 upVector: TEXCOORD0;
				float3 rightVector: TEXCOORD1;
			};

			struct FragmentShaderInput
			{
				float4 position : SV_POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
			};

			// Properties variables
			uniform float4 _Color;			
			uniform float _Opacity;

			// Sprites
			uniform sampler2D _SpriteSheet;
			uniform int _NumSprites;			
			uniform int _ShapeIndex;
			
            // Data transforms
            uniform float _PointSize;
			uniform float pointScale;
			uniform float4x4 datasetMatrix;
			uniform int numDataPoints;
			// Scaling types
			uniform int scalingTypeX;
			uniform int scalingTypeY;
			uniform int scalingTypeZ;
            uniform int scalingTypeColorMap;
            // Scaling sizes
            uniform float scalingX;
            uniform float scalingY;
            uniform float scalingZ;            
            uniform float scaleColorMap;           
            // Scaling offsets
            uniform float offsetX;
            uniform float offsetY;
            uniform float offsetZ;
            uniform float offsetColorMap;

			// Color maps
			uniform float4 colorMapData[NUM_COLOR_MAP_STEPS];
			
			// Actual data buffer for positions and values
			Buffer<float> dataX;
			Buffer<float> dataY;
			Buffer<float> dataZ;
            Buffer<float> dataVal;
            
            
            float applyScaling(float input, int type, float scale, float offset)
            {
                switch (type)
				{
				    case LOG:
				        return log(input);
                    case SQRT:
                        return sqrt(input);
                    case SQUARED:
                        return input * input;
                    case EXP:
                        return exp(input);
                    default:
                        return input * scale + offset;                        
				}
            }
            
			// Vertex shader
			VertexShaderOutput vertexShaderBufferLookup(uint id : SV_VertexID)
			{
				VertexShaderOutput output = (VertexShaderOutput) 0;
				float3 inputPos = float3(applyScaling(dataX[id], scalingTypeX, scalingX, offsetX), applyScaling(dataY[id], scalingTypeY, scalingY, offsetY), applyScaling(dataZ[id], scalingTypeZ, scalingZ, offsetZ));
				// Transform position from local space to world space
				float4 worldPos = mul(datasetMatrix, float4(inputPos, 1.0));
				// Get per-vertex camera direction
				float3 cameraDirection = normalize(worldPos.xyz - _WorldSpaceCameraPos);				
				float3 cameraUp = UNITY_MATRIX_IT_MV[1].xyz;
				// Find two basis vectors that are perpendicular to the camera direction 
				float3 basisX = normalize(cross(cameraUp, cameraDirection));
				float3 basisY = normalize(cross(basisX, cameraDirection));
				output.upVector = basisY * _PointSize * pointScale;
				output.rightVector = basisX * _PointSize * pointScale;
				output.position = worldPos;
				
				// Look up color from the uniform
				uint colorMapIndex = clamp(applyScaling(dataVal[id], scalingTypeColorMap, scaleColorMap, offsetColorMap) * NUM_COLOR_MAP_STEPS, 0, NUM_COLOR_MAP_STEPS-1);
				output.color = colorMapData[colorMapIndex];
				return output;
			}

            // Geometry shader is limited to a fixed output. In future, this stage could be used for culling
			[maxvertexcount(4)]
			void geometryShaderBillboard(point VertexShaderOutput input[1], inout TriangleStream<FragmentShaderInput> outputStream) {
                // Offsets for the four corners of the quad
				float2 offsets[4] = { float2(-0.5, 0.5), float2(-0.5,-0.5), float2(0.5,0.5), float2(0.5,-0.5) };
                // Shift UV coordinates based on shape index
				float uvOffset = ((float)_ShapeIndex) / _NumSprites;
				float uvRange = 1.0 / _NumSprites;
				
				float3 billboardOrigin = input[0].position.xyz;
				float3 up = input[0].upVector;
				float3 right = input[0].rightVector;
				
                FragmentShaderInput output;
				output.color = input[0].color;
                
				for (int i = 0; i < 4; i++)
				{
					float3 worldPos = billboardOrigin + right * offsets[i].x + up * offsets[i].y;
					// Transform position from world space to screen space
					output.position = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
					output.uv = offsets[i] + 0.5;					
					output.uv.x = uvOffset + uvRange * output.uv.x;
					outputStream.Append(output);
				}
			}
			
			float4 fragmentShaderSprite(FragmentShaderInput input) : COLOR
			{
				float4 pointColor = input.color;
				float opacityFactor = tex2D(_SpriteSheet, input.uv).a * _Opacity; 				               
				return pointColor * opacityFactor;
			}

	ENDCG
		}
	}
		Fallback Off
}