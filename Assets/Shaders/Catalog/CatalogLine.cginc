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
       // Apply color mapping
        float colorMapOffset = 1.0 - (0.5 + colorMapIndex) / numColorMaps;
        output.color = tex2Dlod(colorMap, float4(output.value, colorMapOffset, 0, 0));
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
        // Apply color mapping
        float colorMapOffset = 1.0 - (0.5 + colorMapIndex) / numColorMaps;
        output.color = tex2Dlod(colorMap, float4(output.value, colorMapOffset, 0, 0));
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
    return GetVignette(input.position.xy, input.color * input.opacity);
}