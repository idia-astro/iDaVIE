#include "../Shared/Vignette.cginc"

#define LINEAR 0
#define LOG 1
#define SQRT 2
#define SQUARED 3
#define EXP 4

#define X_INDEX 0
#define Y_INDEX 1
#define Z_INDEX 2
#define CMAP_INDEX 3
#define OPACITY_INDEX 4
#define POINT_SIZE_INDEX 5
#define POINT_SHAPE_INDEX 6

struct MappingConfig
{
    int Clamped;
    float MinVal;
    float MaxVal;
    float Offset;
    float Scale;
    int ScalingType;
    
    // Future use: Will filter points based on this range
    float FilterMinVal;
    float FilterMaxVal;
};

// Properties variables
uniform int useUniformColor;
uniform int useUniformOpacity;

// Uniform values;
uniform float opacity;
uniform float4 color;
uniform float4x4 datasetMatrix;

// Filtering
uniform float cutoffMin;
uniform float cutoffMax;
// Color maps
uniform sampler2D colorMap;
uniform float colorMapIndex;
uniform int numColorMaps;

// Data buffers for positions and values
Buffer<float> dataX;
Buffer<float> dataY;
Buffer<float> dataZ;
Buffer<float> dataCmap;
Buffer<float> dataOpacity;
StructuredBuffer<MappingConfig> mappingConfigs;

float applyScaling(float input, MappingConfig config)
{
    float scaledValue;
    switch (config.ScalingType)
    {
        case LOG:
            scaledValue = log(input);
            break;
        case SQRT:
            scaledValue = sqrt(input);
            break;
        case SQUARED:
            scaledValue = input * input;
            break;
        case EXP:
            scaledValue = exp(input);
            break;
        default:
            scaledValue = input * config.Scale + config.Offset;
            break;                        
    }
    
    if (config.Clamped)
    {
        scaledValue = clamp(scaledValue, config.MinVal, config.MaxVal);
    }
    return scaledValue;
}

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