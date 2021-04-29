#define LINE_HIDDEN 0
#define LINE_VISIBLE 1
#define LINE_SELECTED 2

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
    VertexShaderOutput output = (VertexShaderOutput)0;
    if (input.visibility == LINE_HIDDEN)
    {
        return output;
    }
    if (input.visibility == LINE_SELECTED)
    {
        // Fade alpha between [1/3, 1]
        output.color = input.color;
        output.color.a *= (2.0 + sin(4.0 * _Time.y)) / 3.0;
    }
    else
    {
        output.color = input.color;
    }
    output.position = mul(UNITY_MATRIX_VP, mul(datasetMatrix, float4(input.position, 1.0)));
    return output;
}

float4 fsSimple(FragmentShaderInput input) : COLOR
{
    return input.color * input.color.a;
}
