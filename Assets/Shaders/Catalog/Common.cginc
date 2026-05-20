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