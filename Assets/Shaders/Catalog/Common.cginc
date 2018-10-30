#define NUM_COLOR_MAP_STEPS 256

#define LINEAR 0
#define LOG 1
#define SQRT 2
#define SQUARED 3
#define EXP 4

// Properties variables
uniform int useUniformColor;
uniform int useUniformOpacity;

// Uniform values;
uniform float opacity;
uniform float4 color;
uniform float4x4 datasetMatrix;

// Scaling types
uniform int scalingTypeX;
uniform int scalingTypeY;
uniform int scalingTypeZ;
uniform int scalingTypeColorMap;
uniform int scalingTypePointSize;
uniform int scalingTypeOpacity;
// Scaling sizes
uniform float scalingX;
uniform float scalingY;
uniform float scalingZ;            
uniform float scalingColorMap;
uniform float scalingPointSize;
uniform float scalingOpacity;        
// Scaling offsets
uniform float offsetX;
uniform float offsetY;
uniform float offsetZ;
uniform float offsetColorMap;
uniform float offsetPointSize;
uniform float offsetOpacity;

// Filtering
uniform float cutoffMin;
uniform float cutoffMax;
// Color maps
uniform float4 colorMapData[NUM_COLOR_MAP_STEPS];

// Data buffers for positions and values
Buffer<float> dataX;
Buffer<float> dataY;
Buffer<float> dataZ;
Buffer<float> dataCmap;

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