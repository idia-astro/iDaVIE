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
    float opacity: TEXCOORD2;
};

struct FragmentShaderInput
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float opacity: TEXCOORD1;
};

// Vertex shader
VertexShaderOutput vsLine(uint id : SV_VertexID)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float3 startPointLocalSpace = float3(applyScaling(dataX[id], mappingConfigs[X_INDEX]), applyScaling(dataY[id], mappingConfigs[Y_INDEX]), applyScaling(dataZ[id], mappingConfigs[Z_INDEX]));    
    float3 endPointLocalSpace = float3(applyScaling(dataX2[id], mappingConfigs[X_INDEX]), applyScaling(dataY2[id], mappingConfigs[Y_INDEX]), applyScaling(dataZ2[id], mappingConfigs[Z_INDEX]));    
    // Transform positions from local space to screen space    
    output.startPoint = mul(UNITY_MATRIX_VP, mul(datasetMatrix, float4(startPointLocalSpace, 1.0)));
    output.endPoint =  mul(UNITY_MATRIX_VP, mul(datasetMatrix, float4(endPointLocalSpace, 1.0)));
    output.value = clamp(applyScaling(dataCmap[id], mappingConfigs[CMAP_INDEX]), 0, 1);
    
    if (!useUniformColor) {
        uint colorMapIndex = clamp(output.value * NUM_COLOR_MAP_STEPS, 0, NUM_COLOR_MAP_STEPS-1);
        output.color = colorMapData[colorMapIndex];
    }
    else {
        output.color = color;
    }
    
    if (!useUniformOpacity) {                
        output.opacity = applyScaling(dataOpacity[id], mappingConfigs[OPACITY_INDEX]);
    }
    else {
        output.opacity = opacity;
    }
    
    return output;
}

// Vertex shader
VertexShaderOutput vsLineSpherical(uint id : SV_VertexID)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float gLat = applyScaling(dataX[id], mappingConfigs[X_INDEX]);
    float gLong = applyScaling(dataY[id], mappingConfigs[Y_INDEX]);
    float R = applyScaling(dataZ[id], mappingConfigs[Z_INDEX]);
    // Transform from spherical to cartesian coordinates
    float3 startPointLocalSpace = R * float3(cos(gLong) * cos(gLat), sin(gLong) * cos(gLat), sin(gLat));
    
    // Radial line for now
    float3 endPointLocalSpace = startPointLocalSpace * 1.1;    
    // Transform positions from local space to world space    
    output.startPoint = mul(UNITY_MATRIX_VP, mul(datasetMatrix, float4(startPointLocalSpace, 1.0)));
    output.endPoint =  mul(UNITY_MATRIX_VP, mul(datasetMatrix, float4(endPointLocalSpace, 1.0)));            
    output.value = clamp(applyScaling(dataCmap[id], mappingConfigs[CMAP_INDEX]), 0, 1);
    
    if (useUniformColor == 0) {
        uint colorMapIndex = clamp(output.value * NUM_COLOR_MAP_STEPS, 0, NUM_COLOR_MAP_STEPS-1);
        output.color = colorMapData[colorMapIndex];
    }
    else {
        output.color = color;
    }
    
    if (!useUniformOpacity) {                
        output.opacity = applyScaling(dataOpacity[id], mappingConfigs[OPACITY_INDEX]);
    }
    else {
        output.opacity = opacity;
    }
    
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
    output.opacity = input[0].opacity;
    outputStream.Append(output);
    // Second vertex
    output.position = input[0].endPoint;
    outputStream.Append(output);
}

float4 fsSimple(FragmentShaderInput input) : COLOR
{
    return input.color * input.opacity;
}