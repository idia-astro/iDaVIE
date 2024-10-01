// Ray marching fragment shader, partially adapted from NVIDIA OpenGL SDK sample "Render to 3D Texture"
// http://developer.download.nvidia.com/SDK/10/opengl/samples.html
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
#define SQUARE 3
#define POWER 4
#define GAMMA 5

#define MASK_DISABLED 0
#define MASK_ENABLED 1
#define MASK_INVERTED 2
#define MASK_ISOLATED 3

struct Ray
{
    float3 origin;
    float3 direction;
};

struct VertexShaderInput
{
    float4 vertex : POSITION;
};

struct VertexShaderOuput
{
    float4 vertex : SV_POSITION;
    Ray ray : TEXCOORD0;
    float4 projPos : TEXCOORD2;
};

uniform sampler3D _DataCube;
uniform sampler2D _ColorMap;
uniform int _NumColorMaps;
uniform float _ColorMapIndex;
uniform float3 _SliceMin, _SliceMax;
uniform float _ThresholdMin, _ThresholdMax;
uniform float _ScaleMin;
uniform float _ScaleMax;
uniform float _MaxSteps;
uniform float _Jitter;

// Foveated Rendering settings
uniform float FoveationStart;
uniform float FoveationEnd;
uniform float FoveationJitter;
uniform int FoveatedStepsLow, FoveatedStepsHigh;

// Depth buffer
UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

// Highlight selection
uniform float3 HighlightMin, HighlightMax;
uniform float HighlightSaturateFactor;

// Non-linear scaling
uniform int ScaleType;
uniform float ScaleAlpha;
uniform float ScaleGamma;
uniform float ScaleBias;
uniform float ScaleContrast;

// Mask
uniform int MaskMode;
uniform sampler3D MaskCube;

// Projection
uniform int ProjectionMode;

// Implementation: NVIDIA. Original algorithm : HyperGraph
// http://www.siggraph.org/education/materials/HyperGraph/raytrace/rtinter3.htm
bool IntersectBox(Ray r, float3 boxmin, float3 boxmax, out float tnear, out float tfar)
{
    // compute intersection of ray with all six bbox planes
    float3 invR = 1.0 / r.direction;
    float3 tbot = invR * (boxmin.xyz - r.origin);
    float3 ttop = invR * (boxmax.xyz - r.origin);

    // re-order intersections to find smallest and largest on each axis
    float3 tmin = min(ttop, tbot);
    float3 tmax = max(ttop, tbot);

    // find the largest tmin and the smallest tmax
    float2 t0 = max(tmin.xx, tmin.yz);
    float largest_tmin = max(t0.x, t0.y);
    t0 = min(tmax.xx, tmax.yz);
    float smallest_tmax = min(t0.x, t0.y);

    // check for hit
    bool hit;
    if ((largest_tmin > smallest_tmax)) 
        hit = false;
    else
        hit = true;

    tnear = largest_tmin;
    tfar = smallest_tmax;

    return hit;
}

VertexShaderOuput vertexShaderVolume(VertexShaderInput v)
{
    VertexShaderOuput v2f;
    float4 worldPos = mul(UNITY_MATRIX_M, v.vertex);
    v2f.vertex = mul(UNITY_MATRIX_VP, worldPos);
    v2f.ray.direction = -ObjSpaceViewDir(v.vertex);
    v2f.ray.origin = v.vertex.xyz - v2f.ray.direction;
    // Adapted from the Unity "Particles/Additive" built-in shader
    v2f.projPos = ComputeScreenPos(v2f.vertex);
    COMPUTE_EYEDEPTH(v2f.projPos.z);

    return v2f;
}

// Simple pseudo-random number generator from https://github.com/keijiro
float nrand(float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}

bool isnanSafe(float val)
{
  return ( val < 0.0 || 0.0 < val || val == 0.0 ) ? false : true;
}

float numSamples(float2 position)
{
    position = float2(position.x % _ScreenParams.x, position.y);
    float2 center = _ScreenParams.xy / 2.0;
    float2 delta = center - position;
    float radius = length(delta) / _ScreenParams.x;
    return floor(FoveatedStepsLow + (FoveatedStepsHigh - FoveatedStepsLow) * (1.0 - smoothstep(FoveationStart, FoveationEnd, radius)));
}

bool positionInBox(float3 position, float3 boxMin, float3 boxMax)
{
    float3 stepTest = step(boxMin, position) * step(position, boxMax);
    return stepTest.x * stepTest.y * stepTest.z > 0.0f;
}

void accumulateSample(float3 position, inout float currentValue, inout bool maxInHighlightBounds, inout bool hasValue)
{
    float stepValue = tex3Dlod(_DataCube, float4(position, 0)).r;
    if (!isnanSafe(stepValue) && stepValue >= currentValue)
    {
        bool stepTest = positionInBox(position, HighlightMin, HighlightMax);
        maxInHighlightBounds = stepTest || (maxInHighlightBounds && (stepValue == currentValue));
        currentValue = stepValue;
        hasValue = true;
    }
}

void accumulateSampleMasked(float3 position, inout float currentValue, inout bool maxInHighlightBounds, inout bool hasValue)
{
    float maskValue = tex3Dlod(MaskCube, float4(position, 0)).r;
    float stepValue = tex3Dlod(_DataCube, float4(position, 0)).r;
    if (!isnanSafe(stepValue) && maskValue > 0.0f && stepValue >= currentValue)
    {
        bool stepTest = positionInBox(position, HighlightMin, HighlightMax);
        maxInHighlightBounds = stepTest || (maxInHighlightBounds && (stepValue == currentValue));
        currentValue = stepValue;
        hasValue = true;
    }
}

void accumulateSampleInverseMasked(float3 position, inout float currentValue, inout bool maxInHighlightBounds, inout bool hasValue)
{
    float maskValue = tex3Dlod(MaskCube, float4(position, 0)).r;
    float stepValue = tex3Dlod(_DataCube, float4(position, 0)).r;
    if (!isnanSafe(stepValue) && maskValue == 0.0f && stepValue >= currentValue)
    {
        bool stepTest = positionInBox(position, HighlightMin, HighlightMax);
        maxInHighlightBounds = stepTest || (maxInHighlightBounds && (stepValue == currentValue));
        currentValue = stepValue;
        hasValue = true;
    }
}

void accumulateMaskIsolated(float3 position, inout float currentValue, inout bool maxInHighlightBounds)
{
    float maskValue = tex3Dlod(MaskCube, float4(position, 0)).r;
    currentValue = max(currentValue, maskValue > 0? 1: 0);    
}

fixed4 fragmentShaderRayMarch(VertexShaderOuput input) : SV_Target
{
    // Adapted from the Unity "Particles/Additive" built-in shader
    float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(input.projPos)));
    float opaqueDepthObjectSpace = length(sceneZ * input.ray.direction / input.projPos.z);
    float vignetteWeight = GetVignetteWeight(input.vertex.xy);
    
    // Early exit if vignette is fully opaque
    if (vignetteWeight >= 1.0)
    {
        return GetVignetteFromWeight(vignetteWeight, float4(0, 0, 0, 0));
    }
    
    float foveatedSamples = numSamples(input.vertex.xy);
        
    float tNear, tFar;
    input.ray.direction = normalize(input.ray.direction);
    bool hit = IntersectBox(input.ray, _SliceMin, _SliceMax, tNear, tFar);
    // Early exit of pixels missing the bounding box or occluded by opaque objects
    if (!hit || tFar < 0 || opaqueDepthObjectSpace < tNear)
    {
        return GetVignetteFromWeight(vignetteWeight, float4(0, 0, 0, 0));
    }
    
    // Clamp intersection depths between 0 and opaque object depth
    tNear = max(0, tNear);
    tFar = min(tFar, opaqueDepthObjectSpace);

    // calculate intersection points    
    float3 pNear = input.ray.origin + input.ray.direction * tNear;
    float3 pFar = input.ray.origin + input.ray.direction * tFar;
    // convert to texture space
    pNear = pNear + 0.5;
    pFar = pFar + 0.5;
        
    float3 currentRayPosition = pNear;
    float3 rayDelta = pFar - pNear;
    float totalLength = length(rayDelta);

    // The maximum possible distance through the cube is sqrt(3) for the full cube, but smaller if slice bounds are applied.
    float maxLength = length(_SliceMax - _SliceMin);
    float stepLength = sqrt(maxLength) / floor(foveatedSamples);
    float3 stepVector = normalize(rayDelta);
    // Calculate the required number of steps, based on the total path length through the object 
    int requiredSteps = clamp(totalLength / stepLength, 0, foveatedSamples);
    
    // Shift ray's starting point by a small temporal noise amount to reduce box artefacts
    // Based on code from Ryan Brucks: https://shaderbits.com/blog/creating-volumetric-ray-marcher
    float3 randVector = nrand(input.vertex.xy + _Time.xy) * stepVector * stepLength * _Jitter;
    currentRayPosition += randVector;
    
    float3 regionScale = 1.0f / (_SliceMax - _SliceMin);
    float3 regionOffset = -(_SliceMin + 0.5f);
    
    float rayValue = 0.0f;
    
    // For transforming from object space to region space
    currentRayPosition = (currentRayPosition + regionOffset) * regionScale;
    float3 adjustedStepVector = stepVector * stepLength * regionScale;
    
    // transform highlight bounds from object space to region space
    HighlightMin = (HighlightMin + regionOffset + 0.5f) * regionScale;
    HighlightMax = (HighlightMax + regionOffset + 0.5f) * regionScale;

    bool maxInHighlightBounds = false;
    bool hasValue = false;
    // TODO: this really needs to be improved using shader variants!
#ifdef SHADER_AIP
    float cumulativeLength = 0.0f;
    // TODO: make this a shader variant thing
    if (MaskMode == MASK_DISABLED)
    {
        for (int i = 0; i < requiredSteps; i++)
        {
            float stepValue = tex3Dlod(_DataCube, float4(currentRayPosition, 0)).r;                
            if (!isnanSafe(stepValue))
            {
                rayValue += stepValue * stepLength;
                cumulativeLength += stepLength;
                bool stepTest = positionInBox(currentRayPosition, HighlightMin, HighlightMax);
                maxInHighlightBounds = stepTest || maxInHighlightBounds;
                hasValue = true;
            }
            currentRayPosition += adjustedStepVector;
        }
        // After the loop, we're still in the volume, so calculate the last step length and apply the transfer function
        float remainingStepLength = totalLength - (requiredSteps + 1) * stepLength - length(randVector);
        currentRayPosition += stepVector * remainingStepLength * regionScale;        
        
        float stepValue = tex3Dlod(_DataCube, float4(currentRayPosition, 0)).r;
        if (!isnanSafe(stepValue))
        {
            cumulativeLength += remainingStepLength;
            rayValue += stepValue * remainingStepLength;
            bool stepTest = positionInBox(currentRayPosition, HighlightMin, HighlightMax);
            maxInHighlightBounds = stepTest || maxInHighlightBounds;
            hasValue = true;
        }            
        rayValue /= cumulativeLength;
    }
    else if (MaskMode == MASK_ENABLED)
    {
        for (int i = 0; i < requiredSteps; i++)
        {
            float stepValue = tex3Dlod(_DataCube, float4(currentRayPosition, 0)).r;
            float maskValue = tex3Dlod(MaskCube, float4(currentRayPosition, 0)).r;
            if (!isnanSafe(stepValue) && maskValue > 0.0f)
            {
                rayValue += stepValue * stepLength;
                cumulativeLength += stepLength;
                bool stepTest = positionInBox(currentRayPosition, HighlightMin, HighlightMax);
                maxInHighlightBounds = stepTest || maxInHighlightBounds;
                hasValue = true;
            }
            currentRayPosition += adjustedStepVector;
        }
        // After the loop, we're still in the volume, so calculate the last step length and apply the transfer function
        float remainingStepLength = totalLength - (requiredSteps + 1) * stepLength - length(randVector);
        currentRayPosition += stepVector * remainingStepLength * regionScale;        
        
        float stepValue = tex3Dlod(_DataCube, float4(currentRayPosition, 0)).r;
        float maskValue = tex3Dlod(MaskCube, float4(currentRayPosition, 0)).r;
        if (!isnanSafe(stepValue) && maskValue > 0.0f)
        {
            cumulativeLength += remainingStepLength;
            rayValue += stepValue * remainingStepLength;
            bool stepTest = positionInBox(currentRayPosition, HighlightMin, HighlightMax);
            maxInHighlightBounds = stepTest || maxInHighlightBounds;
            hasValue = true;
        }            
        rayValue /= cumulativeLength;
    }
    else if (MaskMode == MASK_ISOLATED)
    {
        // Masks don't have NaNs
        hasValue = true;
        for (int i = 0; i < requiredSteps; i++)
        {
            accumulateMaskIsolated(currentRayPosition, rayValue, maxInHighlightBounds);
            currentRayPosition += adjustedStepVector;
        }
        // After the loop, we're still in the volume, so calculate the last step length and apply the transfer function
        float remainingStepLength = totalLength - (requiredSteps + 1) * stepLength - length(randVector);
        currentRayPosition += stepVector * remainingStepLength * regionScale;
        accumulateMaskIsolated(currentRayPosition, rayValue, maxInHighlightBounds);
    }
    else
    {
        for (int i = 0; i < requiredSteps; i++)
        {
            float stepValue = tex3Dlod(_DataCube, float4(currentRayPosition, 0)).r;                
            float maskValue = tex3Dlod(MaskCube, float4(currentRayPosition, 0)).r;
            if (!isnanSafe(stepValue) && maskValue == 0.0f)
            {
                rayValue += stepValue * stepLength;
                cumulativeLength += stepLength;
                bool stepTest = positionInBox(currentRayPosition, HighlightMin, HighlightMax);
                maxInHighlightBounds = stepTest || maxInHighlightBounds;
                hasValue = true;
            }
            currentRayPosition += adjustedStepVector;
        }
        // After the loop, we're still in the volume, so calculate the last step length and apply the transfer function
        float remainingStepLength = totalLength - (requiredSteps + 1) * stepLength - length(randVector);
        currentRayPosition += stepVector * remainingStepLength * regionScale;        
        
        float stepValue = tex3Dlod(_DataCube, float4(currentRayPosition, 0)).r;
        float maskValue = tex3Dlod(MaskCube, float4(currentRayPosition, 0)).r;
        if (!isnanSafe(stepValue) && maskValue == 0.0f)
        {
            cumulativeLength += remainingStepLength;
            rayValue += stepValue * remainingStepLength;
            bool stepTest = positionInBox(currentRayPosition, HighlightMin, HighlightMax);
            maxInHighlightBounds = stepTest || maxInHighlightBounds;
            hasValue = true;
        }            
        rayValue /= cumulativeLength;
    }
#else
    rayValue = -3.402823466e+38F;
    // TODO: make this a shader variant thing
    if (MaskMode == MASK_DISABLED)
    {
        for (int i = 0; i < requiredSteps; i++)
        {
            accumulateSample(currentRayPosition, rayValue, maxInHighlightBounds, hasValue);
            currentRayPosition += adjustedStepVector;
        }
        // After the loop, we're still in the volume, so calculate the last step length and apply the transfer function
        float remainingStepLength = totalLength - (requiredSteps + 1) * stepLength - length(randVector);
        currentRayPosition += stepVector * remainingStepLength * regionScale;
        accumulateSample(currentRayPosition, rayValue, maxInHighlightBounds, hasValue);
    }
    else if (MaskMode == MASK_ENABLED)
    {
        for (int i = 0; i < requiredSteps; i++)
        {
            accumulateSampleMasked(currentRayPosition, rayValue, maxInHighlightBounds, hasValue);
            currentRayPosition += adjustedStepVector;
        }
        // After the loop, we're still in the volume, so calculate the last step length and apply the transfer function
        float remainingStepLength = totalLength - (requiredSteps + 1) * stepLength - length(randVector);
        currentRayPosition += stepVector * remainingStepLength * regionScale;
        accumulateSampleMasked(currentRayPosition, rayValue, maxInHighlightBounds, hasValue);
    }
    else if (MaskMode == MASK_ISOLATED)
    {
        // Masks don't have NaNs
        hasValue = true;
        for (int i = 0; i < requiredSteps; i++)
        {
            accumulateMaskIsolated(currentRayPosition, rayValue, maxInHighlightBounds);
            currentRayPosition += adjustedStepVector;
        }
        // After the loop, we're still in the volume, so calculate the last step length and apply the transfer function
        float remainingStepLength = totalLength - (requiredSteps + 1) * stepLength - length(randVector);
        currentRayPosition += stepVector * remainingStepLength * regionScale;
        accumulateMaskIsolated(currentRayPosition, rayValue, maxInHighlightBounds);
    }
    else
    {
        for (int i = 0; i < requiredSteps; i++)
        {
            accumulateSampleInverseMasked(currentRayPosition, rayValue, maxInHighlightBounds, hasValue);
            currentRayPosition += adjustedStepVector;
        }
        // After the loop, we're still in the volume, so calculate the last step length and apply the transfer function
        float remainingStepLength = totalLength - (requiredSteps + 1) * stepLength - length(randVector);
        currentRayPosition += stepVector * remainingStepLength * regionScale;
        accumulateSampleInverseMasked(currentRayPosition, rayValue, maxInHighlightBounds, hasValue);
    }
#endif
    
    if (!hasValue)
    {
        return GetVignetteFromWeight(vignetteWeight, float4(0, 0, 0, 0));
    }    
    
    // transform into threshold space
    if (MaskMode != MASK_ISOLATED)
    {
        rayValue = (rayValue - _ScaleMin) / (_ScaleMax - _ScaleMin);
        rayValue = clamp(rayValue, _ThresholdMin, _ThresholdMax);
    }
    // Apply linear color mapping after threshold adjustments
    float colorMapOffset = 1.0 - (0.5 + _ColorMapIndex) / _NumColorMaps;
    float thresholdRange = _ThresholdMax - _ThresholdMin;
    float x = (rayValue - _ThresholdMin) / thresholdRange;
    
    // Non-linear scaling
    if (ScaleType == SQUARE)
    {
        x = x * x;
    }
    else if (ScaleType == SQRT)
    {
        x = sqrt(x);
    }
    else if (ScaleType == LOG)
    {
        x = clamp(log(ScaleAlpha * x + 1.0) / log(ScaleAlpha), 0.0, 1.0);
    }
    else if (ScaleType == POWER)
    {
        x = (pow(ScaleAlpha, x) - 1.0) / ScaleAlpha;
    }
    else if (ScaleType == GAMMA)
    {
        x = pow(x, ScaleGamma);
    }

    // bias mod
    x = clamp(x - ScaleBias, 0.0, 1.0);
    // contrast mod
    x = clamp((x - 0.5) * ScaleContrast + 0.5, 0.0, 1.0);
    
    // Interpolate between greyscale output and colormapped output, depending on the highlight dim factor and whether the ray value is within the highlight region
    float colorFraction = maxInHighlightBounds ? 1.0f : HighlightSaturateFactor;
    float4 colorMapColor = float4(tex2D(_ColorMap, float2(x, colorMapOffset)).xyz, x);
    float4 greyscaleColor = x;
    float4 color = lerp(greyscaleColor, colorMapColor, colorFraction);

    return GetVignetteFromWeight(vignetteWeight, color);
}