#include "Common.cginc"

// Data buffers for end points
Buffer<float> dataX2;
Buffer<float> dataY2;
Buffer<float> dataZ2;

// Points -> <VS> -> VertexShaderOutput -> <GS> -> PixelShaderInput -> <PS> -> Pixels 
struct VertexShaderOutput
{
    float4 startPoint : SV_POSITION;
    float4 color : COLOR;
    float4 endPoint: TEXCOORD0;
    float value: TEXCOORD1;
};

struct FragmentShaderInput
{
    float4 position : SV_POSITION;
    float4 color : COLOR;    
};

// Vertex shader
VertexShaderOutput vsLine(uint id : SV_VertexID)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float3 startPointLocalSpace = float3(applyScaling(dataX[id], scalingTypeX, scalingX, offsetX), applyScaling(dataY[id], scalingTypeY, scalingY, offsetY), applyScaling(dataZ[id], scalingTypeZ, scalingZ, offsetZ));    
    float3 endPointLocalSpace = float3(applyScaling(dataX2[id], scalingTypeX, scalingX, offsetX), applyScaling(dataY2[id], scalingTypeY, scalingY, offsetY), applyScaling(dataZ2[id], scalingTypeZ, scalingZ, offsetZ));    
    // Transform positions from local space to screen space    
    output.startPoint = mul(UNITY_MATRIX_VP, mul(datasetMatrix, float4(startPointLocalSpace, 1.0)));
    output.endPoint =  mul(UNITY_MATRIX_VP, mul(datasetMatrix, float4(endPointLocalSpace, 1.0)));      
    output.value = applyScaling(dataVal[id], scalingTypeColorMap, scaleColorMap, offsetColorMap);
    // Look up color from the uniform
    uint colorMapIndex = clamp(output.value * NUM_COLOR_MAP_STEPS, 0, NUM_COLOR_MAP_STEPS-1);
    output.color = colorMapData[colorMapIndex];
    return output;
}

// Vertex shader
VertexShaderOutput vsLineSpherical(uint id : SV_VertexID)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float gLat = applyScaling(dataX[id], scalingTypeX, scalingX, offsetX);
    float gLong = applyScaling(dataY[id], scalingTypeY, scalingY, offsetY);
    float R = applyScaling(dataZ[id], scalingTypeZ, scalingZ, offsetZ);
    // Transform from spherical to cartesian coordinates
    float3 startPointLocalSpace = R * float3(cos(gLong) * cos(gLat), sin(gLong) * cos(gLat), sin(gLat));
    
    // Radial line for now
    float3 endPointLocalSpace = startPointLocalSpace * 1.1;    
    // Transform positions from local space to world space    
    output.startPoint = mul(UNITY_MATRIX_VP, mul(datasetMatrix, float4(startPointLocalSpace, 1.0)));
    output.endPoint =  mul(UNITY_MATRIX_VP, mul(datasetMatrix, float4(endPointLocalSpace, 1.0)));            
    output.value = applyScaling(dataVal[id], scalingTypeColorMap, scaleColorMap, offsetColorMap);
    // Look up color from the uniform
    uint colorMapIndex = clamp(output.value * NUM_COLOR_MAP_STEPS, 0, NUM_COLOR_MAP_STEPS-1);
    output.color = colorMapData[colorMapIndex];
    return output;
}

// Geometry shader is limited to a fixed output. In future, this stage could be used for culling
[maxvertexcount(2)]
void gsLine(point VertexShaderOutput input[1], inout LineStream<FragmentShaderInput> outputStream) {
    // Filtering based on min/max value    
    if (input[0].value < cutoffMin || input[0].value > cutoffMax)
    {
        return;
    }
    
    FragmentShaderInput output;
    // First vertex and common properties
    output.position = input[0].startPoint;
    output.color = input[0].color;
    outputStream.Append(output);
    // Second vertex
    output.position = input[0].endPoint;
    outputStream.Append(output);
}

float4 fsSimple(FragmentShaderInput input) : COLOR
{
    float4 pointColor = input.color;
    return pointColor * _Opacity;
}