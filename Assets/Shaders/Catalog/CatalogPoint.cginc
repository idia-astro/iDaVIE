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

// Sprites
uniform sampler2D _SpriteSheet;
uniform int _NumSprites;			

// Additional parameters for points
Buffer<float> dataPointSize;
Buffer<float> dataPointShape;
uniform int useUniformPointSize;
uniform int useUniformPointShape;
uniform float pointSize;
uniform float pointShape;
uniform float scalingFactor;

// Points -> <VS> -> VertexShaderOutput -> <GS> -> PixelShaderInput -> <PS> -> Pixels 
struct VertexShaderOutput
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float3 upVector: TEXCOORD0;
    float3 rightVector: TEXCOORD1;
    float pointSize: TEXCOORD2;
    float pointShape: TEXCOORD3;  
    float opacity: TEXCOORD4;
};

struct FragmentShaderInput
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float2 uv : TEXCOORD0;
    float opacity: TEXCOORD1;
};

// Vertex shader
VertexShaderOutput vsPointBillboard(uint id : SV_VertexID)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float3 inputPos = float3(applyScaling(dataX[id], mappingConfigs[X_INDEX]), applyScaling(dataY[id], mappingConfigs[Y_INDEX]), applyScaling(dataZ[id], mappingConfigs[Z_INDEX]));
    // Transform position from local space to world space
    float4 worldPos = mul(datasetMatrix, float4(inputPos, 1.0));
    output.position = worldPos;

    if (!useUniformPointSize) {                
        output.pointSize = applyScaling(dataPointSize[id], mappingConfigs[POINT_SIZE_INDEX]);
    }
    else {
        output.pointSize = pointSize;
    }
        
    // Get per-vertex camera direction
    float3 cameraDirection = normalize(worldPos.xyz - _WorldSpaceCameraPos);				
    float3 cameraUp = UNITY_MATRIX_IT_MV[1].xyz;
    // Find two basis vectors that are perpendicular to the camera direction 
    float3 basisX = normalize(cross(cameraUp, cameraDirection));
    float3 basisY = normalize(cross(basisX, cameraDirection));        
        
    output.upVector = basisY * output.pointSize * scalingFactor;
    output.rightVector = basisX * output.pointSize * scalingFactor;

    if (!useUniformColor) {        
        float value = clamp(applyScaling(dataCmap[id], mappingConfigs[CMAP_INDEX]), 0, 1);        
        // Apply color mapping
        float colorMapOffset = 1.0 - (0.5 + colorMapIndex) / numColorMaps;
        output.color = tex2Dlod(colorMap, float4(value, colorMapOffset, 0, 0));
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
    
    if (!useUniformPointShape) {                
        output.pointShape = applyScaling(dataPointShape[id], mappingConfigs[POINT_SHAPE_INDEX]);
    }
    else {
        output.pointShape = pointShape;
    }
    
    return output;
}

// Same as above, wiith spherical coordinates
VertexShaderOutput vsPointBillboardSpherical(uint id : SV_VertexID)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float gLat = applyScaling(dataX[id], mappingConfigs[X_INDEX]);
    float gLong = applyScaling(dataY[id], mappingConfigs[Y_INDEX]);
    float R = applyScaling(dataZ[id], mappingConfigs[Z_INDEX]);
    // Transform from spherical to cartesian coordinates
    float3 inputPos = R * float3(cos(gLong) * cos(gLat), sin(gLong) * cos(gLat), sin(gLat));
    
    // Transform position from local space to world space
    float4 worldPos = mul(datasetMatrix, float4(inputPos, 1.0));
    output.position = worldPos;

    if (!useUniformPointSize) {                
        output.pointSize = applyScaling(dataPointSize[id], mappingConfigs[POINT_SIZE_INDEX]);
    }
    else {
        output.pointSize = pointSize;
    }      
    
    // Get per-vertex camera direction
    float3 cameraDirection = normalize(worldPos.xyz - _WorldSpaceCameraPos);				
    float3 cameraUp = UNITY_MATRIX_IT_MV[1].xyz;
    // Find two basis vectors that are perpendicular to the camera direction 
    float3 basisX = normalize(cross(cameraUp, cameraDirection));
    float3 basisY = normalize(cross(basisX, cameraDirection));
    output.upVector = basisY * output.pointSize * scalingFactor;
    output.rightVector = basisX * output.pointSize * scalingFactor;
    
    if (!useUniformColor) {
        float value = clamp(applyScaling(dataCmap[id], mappingConfigs[CMAP_INDEX]), 0, 1);        
        // Apply color mapping
        float colorMapOffset = 1.0 - (0.5 + colorMapIndex) / numColorMaps;
        output.color = tex2Dlod(colorMap, float4(value, colorMapOffset, 0, 0));
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
    
    if (!useUniformPointShape) {                
        output.pointShape = applyScaling(dataPointShape[id], mappingConfigs[POINT_SHAPE_INDEX]);
    }
    else {
        output.pointShape = pointShape;
    }
    
    return output;
}

// Geometry shader is limited to a fixed output. In future, this stage could be used for culling
[maxvertexcount(4)]
void gsBillboard(point VertexShaderOutput input[1], inout TriangleStream<FragmentShaderInput> outputStream) {
    // Offsets for the four corners of the quad
    float2 offsets[4] = { float2(-0.5, 0.5), float2(-0.5,-0.5), float2(0.5,0.5), float2(0.5,-0.5) };
    // Shift UV coordinates based on shape index
    float uvOffset = clamp(round(input[0].pointShape), 0, _NumSprites -1) / _NumSprites;
    float uvRange = 1.0 / _NumSprites;
    
    float3 billboardOrigin = input[0].position.xyz;
    float3 up = input[0].upVector;
    float3 right = input[0].rightVector;
    
    FragmentShaderInput output;
    output.color = input[0].color;
    output.opacity = input[0].opacity;
    
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
    float opacityFactor = tex2D(_SpriteSheet, input.uv).a * input.opacity;    
    return GetVignette(input.position.xy, input.color * opacityFactor);
}