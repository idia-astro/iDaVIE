#include "Common.cginc"

// Sprites
uniform sampler2D _SpriteSheet;
uniform int _NumSprites;			
uniform int _ShapeIndex;

// Data transforms
uniform float _PointSize;
uniform float pointScale;

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

// Vertex shader
VertexShaderOutput vsPointBillboard(uint id : SV_VertexID)
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

// Same as above, wiith spherical coordinates
VertexShaderOutput vsPointBillboardSpherical(uint id : SV_VertexID)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float gLat = applyScaling(dataX[id], scalingTypeX, scalingX, offsetX);
    float gLong = applyScaling(dataY[id], scalingTypeY, scalingY, offsetY);
    float R = applyScaling(dataZ[id], scalingTypeZ, scalingZ, offsetZ);
    // Transform from spherical to cartesian coordinates
    float3 inputPos = R * float3(cos(gLong) * cos(gLat), sin(gLong) * cos(gLat), sin(gLat));
    
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
void gsBillboard(point VertexShaderOutput input[1], inout TriangleStream<FragmentShaderInput> outputStream) {
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

float4 fsSprite(FragmentShaderInput input) : COLOR
{
    float4 pointColor = input.color;
    float opacityFactor = tex2D(_SpriteSheet, input.uv).a * _Opacity; 				               
    return pointColor * opacityFactor;
}