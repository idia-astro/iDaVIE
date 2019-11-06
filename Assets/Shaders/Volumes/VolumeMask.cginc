struct VoxelEntry 
{
    int index;
    int value;
};

Buffer<VoxelEntry> MaskEntries;

// Points -> <VS> -> VertexShaderOutput -> <GS> -> PixelShaderInput -> <PS> -> Pixels 
struct VertexShaderOutput
{
    float4 position : SV_POSITION;    
    int value: TEXCOORD0;
    float4 offsets[4]: TEXCOORD1;
};

struct FragmentShaderInput
{
    float4 position : SV_POSITION;
};

uniform float4 CubeDimensions;
uniform float4 RegionDimensions;
uniform float4 RegionOffset;
uniform float4x4 ModelMatrix;
uniform float MaskVoxelSize;
uniform float4 MaskVoxelOffsets[4];
uniform float4 MaskVoxelColor;

// Vertex shader
VertexShaderOutput vsMask(uint id : SV_VertexID)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;            
    VoxelEntry entry = MaskEntries[id];
    
    int value = entry.value % 32768;
    int activeEdges = entry.value / 32768;
        
    if (!activeEdges || !value)
    {
        return output;
    }
    
    output.value = entry.value;
    float i = entry.index;
    float3 voxelIndices;
    
    voxelIndices.x = i % RegionDimensions.x;
    float j = (i - voxelIndices.x) / RegionDimensions.x;
    voxelIndices.y = j % RegionDimensions.y;
    voxelIndices.z = (j - voxelIndices.y) / RegionDimensions.y;
    
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
[maxvertexcount(16)]
void gsMask(point VertexShaderOutput input[1], inout LineStream<FragmentShaderInput> outputStream) {
    int value = input[0].value % 32768;
    int activeEdges = input[0].value / 32768;
    
    if (!value) 
    {
        return;
    }

    FragmentShaderInput output;
    
    float4 corners[] = {
        input[0].position  + input[0].offsets[0],
        input[0].position  + input[0].offsets[1],
        input[0].position  + input[0].offsets[2],
        input[0].position  + input[0].offsets[3],
        input[0].position  - input[0].offsets[3],
        input[0].position  - input[0].offsets[2],
        input[0].position  - input[0].offsets[1],
        input[0].position  - input[0].offsets[0],
    };
       
    // generate cube edges line strip    
    output.position = corners[0];
    outputStream.Append(output);
    output.position = corners[1];    
    outputStream.Append(output);
    output.position = corners[3];
    outputStream.Append(output);
    output.position = corners[1];
    outputStream.Append(output);
    output.position = corners[5];
    outputStream.Append(output);
    output.position = corners[4];
    outputStream.Append(output);
    output.position = corners[0];    
    outputStream.Append(output);
    output.position = corners[2];
    outputStream.Append(output);
    output.position = corners[3];
    outputStream.Append(output);
    output.position = corners[7];
    outputStream.Append(output);
    output.position = corners[5];
    outputStream.Append(output);
    output.position = corners[4];    
    outputStream.Append(output);
    output.position = corners[6];
    outputStream.Append(output);
    output.position = corners[2];
    outputStream.Append(output);
    output.position = corners[6];
    outputStream.Append(output);
    output.position = corners[7];
    outputStream.Append(output);        
}

float4 fsMask(FragmentShaderInput input) : COLOR
{
    return MaskVoxelColor;
}