struct FeatureVertex
{
    float3 position;
    float4 color;
    int visibility;
};

StructuredBuffer<FeatureVertex> inputData;
uniform float4x4 datasetMatrix;

// Lines -> <VS> -> VertexShaderOutput -> <PS> -> Pixels 
struct VertexShaderOutput
{
    float4 position : SV_POSITION;
    float4 color : COLOR;    
};

struct FragmentShaderInput
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
};

// Vertex shader
VertexShaderOutput vsLine(uint id : SV_VertexID)
{
    FeatureVertex input = inputData[id];
    VertexShaderOutput output = (VertexShaderOutput) 0;
    if (input.visibility <= 0)
    {
        return output;
    }
    
    output.position = mul(UNITY_MATRIX_VP, mul(datasetMatrix, float4(input.position, 1.0)));
    output.color = input.color;    
    return output;
}

float4 fsSimple(FragmentShaderInput input) : COLOR
{
    return input.color;
}