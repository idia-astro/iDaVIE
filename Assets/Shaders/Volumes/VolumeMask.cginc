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
#define X_FACE_FLAG 1
#define X_FACE_FLAG_NEG 2
#define Y_FACE_FLAG 4
#define Y_FACE_FLAG_NEG 8
#define Z_FACE_FLAG 16
#define Z_FACE_FLAG_NEG 32

struct VoxelEntry 
{
    int index;
    int encodedValue;
};

StructuredBuffer<VoxelEntry> MaskEntries;

// Points -> <VS> -> VertexShaderOutput -> <GS> -> PixelShaderInput -> <PS> -> Pixels 
struct VertexShaderOutput
{
    float4 position : SV_POSITION;    
    int value: TEXCOORD0;
    int activeEdges : TEXCOORD1;
    float4 offsets[4]: TEXCOORD2;
};

struct FragmentShaderInput
{
    float4 position : SV_POSITION;
    int value: TEXCOORD0;
};

uniform float4 CubeDimensions;
uniform float4 RegionDimensions;
uniform float4 RegionOffset;
uniform float4x4 ModelMatrix;
uniform float MaskVoxelSize;
uniform float4 MaskVoxelOffsets[4];
uniform float4 MaskVoxelColor;
uniform int HighlightedSource;

// Vertex shader
VertexShaderOutput vsMask(uint id : SV_VertexID)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;            
    VoxelEntry entry = MaskEntries[id];
    
    int value = entry.encodedValue % 32768;
    int activeEdges = (entry.encodedValue - value) / 32768;
        
    if (!activeEdges || !value)
    {
        return output;
    }
    
    output.value = value;
    output.activeEdges = activeEdges;
    
    uint3 regDims = RegionDimensions;
    uint3 voxelIndices;
    uint i = entry.index; 
    voxelIndices.x = i % regDims.x;    
    uint j = (i - voxelIndices.x) / regDims.x;
    voxelIndices.y = j % regDims.y;    
    voxelIndices.z = (j - voxelIndices.y) / regDims.y;
    float3 voxelLocation = (RegionOffset + voxelIndices - 0.5) / CubeDimensions.xyz - 0.5;   
         
    // Transform position from local space to screen space    
    output.position = mul(UNITY_MATRIX_VP, mul(ModelMatrix, float4(voxelLocation, 1.0)));
    // Transform offsets from world space to screen space
    output.offsets[0] =  mul(UNITY_MATRIX_VP, MaskVoxelOffsets[0]);
    output.offsets[1] =  mul(UNITY_MATRIX_VP, MaskVoxelOffsets[1]);
    output.offsets[2] =  mul(UNITY_MATRIX_VP, MaskVoxelOffsets[2]);            
    output.offsets[3] =  mul(UNITY_MATRIX_VP, MaskVoxelOffsets[3]);            
    return output;
}

// Geometry shader is limited to a fixed output.
[maxvertexcount(30)]
void gsMask(point VertexShaderOutput input[1], inout LineStream<FragmentShaderInput> outputStream) {
    int value = input[0].value;
    int activeEdges = input[0].activeEdges;
    
    if (!value) 
    {
        return;
    }

    FragmentShaderInput output;

    output.value = value;
    
    float4 corners[] = {
        input[0].position + input[0].offsets[0],
        input[0].position + input[0].offsets[1],
        input[0].position + input[0].offsets[2],
        input[0].position + input[0].offsets[3],
        input[0].position - input[0].offsets[3],
        input[0].position - input[0].offsets[2],
        input[0].position - input[0].offsets[1],
        input[0].position - input[0].offsets[0],
    };
   
    // -x face
    if (activeEdges & X_FACE_FLAG) 
    {
        output.position = corners[0];
        outputStream.Append(output);
        output.position = corners[1];    
        outputStream.Append(output);
        output.position = corners[3];
        outputStream.Append(output);
        output.position = corners[2];
        outputStream.Append(output);
        output.position = corners[0];
        outputStream.Append(output);    
        outputStream.RestartStrip();
    }
    // +x face
    if (activeEdges & X_FACE_FLAG_NEG)
    {
        output.position = corners[4];
        outputStream.Append(output);
        output.position = corners[6];    
        outputStream.Append(output);
        output.position = corners[7];
        outputStream.Append(output);
        output.position = corners[5];
        outputStream.Append(output);
        output.position = corners[4];
        outputStream.Append(output);
        outputStream.RestartStrip();
    }  
 
    // -y face
    if (activeEdges & Y_FACE_FLAG)
    {
        output.position = corners[0];
        outputStream.Append(output);
        output.position = corners[4];    
        outputStream.Append(output);
        output.position = corners[5];
        outputStream.Append(output);
        output.position = corners[1];
        outputStream.Append(output);
        output.position = corners[0];
        outputStream.Append(output);    
        outputStream.RestartStrip();
    }
    
    // +y face
    if (activeEdges & Y_FACE_FLAG_NEG)
    {
        output.position = corners[2];
        outputStream.Append(output);
        output.position = corners[3];    
        outputStream.Append(output);
        output.position = corners[7];
        outputStream.Append(output);
        output.position = corners[6];
        outputStream.Append(output);
        output.position = corners[2];
        outputStream.Append(output);    
        outputStream.RestartStrip();
    }
    
    // -z face
    if (activeEdges & Z_FACE_FLAG)
    {
        output.position = corners[0];
        outputStream.Append(output);
        output.position = corners[2];    
        outputStream.Append(output);
        output.position = corners[6];
        outputStream.Append(output);
        output.position = corners[4];
        outputStream.Append(output);
        output.position = corners[0];
        outputStream.Append(output);    
        outputStream.RestartStrip();
    }
    
    // +z face
    if (activeEdges & Z_FACE_FLAG_NEG)
    {
        output.position = corners[1];
        outputStream.Append(output);
        output.position = corners[5];    
        outputStream.Append(output);
        output.position = corners[7];
        outputStream.Append(output);
        output.position = corners[3];
        outputStream.Append(output);
        output.position = corners[1];
        outputStream.Append(output);    
        outputStream.RestartStrip();
    }    
}

float4 fsMask(FragmentShaderInput input) : COLOR
{
    float alphaWeighting =  (input.value == HighlightedSource ? 1.0f : 0.25f);
    return float4(MaskVoxelColor.rgb, alphaWeighting * MaskVoxelColor.a);
}