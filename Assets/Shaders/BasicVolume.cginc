// Ray marching fragment shader, partially adapted from NVIDIA OpenGL SDK sample "Render to 3D Texture"
// http://developer.download.nvidia.com/SDK/10/opengl/samples.html

#include "./Shared/Vignette.cginc"

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
uniform sampler2D _CameraDepthTexture;

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

// Data lookup from 3D texture. Slice and value thresholds are applied
float dataLookup(float3 uvw, float scaleMin, float scaleFactor)
{
    float data = tex3Dlod(_DataCube, float4(uvw, 0)).r;
    data = (data - scaleMin) * scaleFactor;
    // apply value threshold    
    return (data >= _ThresholdMin && data <= _ThresholdMax) ? data : 0.0;
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

float numSamples(float2 position)
{
    position = float2(position.x % _ScreenParams.x, position.y);
    float2 center = _ScreenParams.xy / 2.0;
    float2 delta = center - position;
    float radius = length(delta) / _ScreenParams.x;
    return floor(FoveatedStepsLow + (FoveatedStepsHigh - FoveatedStepsLow) * (1.0 - smoothstep(FoveationStart, FoveationEnd, radius)));
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
    
    float rayValue = 0;
    
    // For transforming from object space to region space
    currentRayPosition += regionOffset;
    currentRayPosition *= regionScale;
    float3 adjustedStepVector = stepVector * stepLength * regionScale;
    // For transforming from raw values to scaled values in range [0, 1]
    float scaleFactor = 1.0 / (_ScaleMax - _ScaleMin);
    
    for (int i = 0; i < requiredSteps; i++)
    {
        float stepValue = dataLookup(currentRayPosition, _ScaleMin, scaleFactor);
        rayValue = max(stepValue, rayValue);
        currentRayPosition += adjustedStepVector;
    }
    
    // After the loop, we're still in the volume, so calculate the last step length and apply the transfer function
    float remainingStepLength = totalLength - (requiredSteps + 1) * stepLength - length(randVector);
    currentRayPosition += stepVector * remainingStepLength * regionScale;
    float stepValue = dataLookup(currentRayPosition, _ScaleMin, scaleFactor);
    rayValue = max(stepValue, rayValue);
    rayValue = clamp(rayValue, 0, 1);

    // Apply linear color mapping after threshold adjustments
    float colorMapOffset = 1.0 - (0.5 + _ColorMapIndex) / _NumColorMaps;
    float thresholdRange = _ThresholdMax - _ThresholdMin;
    float colorMapValue = (rayValue - _ThresholdMin) / thresholdRange;
    float4 color = tex2D(_ColorMap, float2(colorMapValue, colorMapOffset));
        
    // Set the ouptut color's opacity to the ray value
    return GetVignetteFromWeight(vignetteWeight, float4(color.xyz, rayValue));
}