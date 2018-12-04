// Ray marching fragment shader, partially adapted from NVIDIA OpenGL SDK sample "Render to 3D Texture"
// http://developer.download.nvidia.com/SDK/10/opengl/samples.html

struct Ray
{
    float3 origin;
    float3 direction;  
};

struct VertexShaderInput
{
    float4 position : POSITION;
};

struct VertexShaderOuput
{
    float4 position : SV_POSITION;
    Ray ray: TEXCOORD0;
};

uniform sampler3D _DataCube;
uniform sampler2D _ColorMap;
uniform int _NumColorMaps;
uniform float _ColorMapIndex;
uniform float3 _SliceMin, _SliceMax;
uniform float _ThresholdMin, _ThresholdMax;
uniform float _ScaleMin;
uniform float _ScaleMax;
uniform float _PostScaling;
uniform float _MaxSteps;
uniform float _Jitter;

// Foveated Rendering settings
uniform float FoveationStart;
uniform float FoveationEnd;
uniform float FoveationJitter;
uniform int FoveatedStepsLow, FoveatedStepsHigh;

// Implementation: NVIDIA. Original algorithm : HyperGraph
// http://www.siggraph.org/education/materials/HyperGraph/raytrace/rtinter3.htm
bool IntersectBox(Ray r, float3 boxmin, float3 boxmax, out float tnear, out float tfar)
{
    // compute intersection of ray with all six bbox planes
    float3 invR = 1.0 / r.direction;
    float3 tbot = invR * (boxmin.xyz - r.origin);
    float3 ttop = invR * (boxmax.xyz - r.origin);

    // re-order intersections to find smallest and largest on each axis
    float3 tmin = min (ttop, tbot);
    float3 tmax = max (ttop, tbot);

    // find the largest tmin and the smallest tmax
    float2 t0 = max (tmin.xx, tmin.yz);
    float largest_tmin = max (t0.x, t0.y);
    t0 = min (tmax.xx, tmax.yz);
    float smallest_tmax = min (t0.x, t0.y);

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
float dataLookup(float3 uvw)
{
    float data = tex3Dlod(_DataCube, float4(uvw, 0)).r;
    float scaleRange = _ScaleMax - _ScaleMin;
    // transform from texture values to 0 -> 1
    data = (data - _ScaleMin) / scaleRange;
    // apply value threshold
    float thresholdStep = step(_ThresholdMin, data) * step(data, _ThresholdMax);
    data *= thresholdStep;
    return data;
}

VertexShaderOuput vertexShaderVolume (VertexShaderInput input)
{
    VertexShaderOuput output;
    output.position = UnityObjectToClipPos(input.position);
    output.ray.direction = -ObjSpaceViewDir(input.position);
    output.ray.origin = input.position.xyz - output.ray.direction;
    return output;
}

// Simple pseudo-random number generator from https://github.com/keijiro
float nrand(float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}

float numSamples(float2 position, float2 screenSize)
{
    position = float2(position.x % screenSize.x, position.y);       
    float2 center = screenSize / 2.0;
    float2 delta = center - position;
    float radius = length(delta)/ screenSize.x;
    return floor(FoveatedStepsLow + (FoveatedStepsHigh - FoveatedStepsLow) * (1.0 - smoothstep(FoveationStart, FoveationEnd, radius)));   
}

fixed4 fragmentShaderRayMarch (VertexShaderOuput input) : SV_Target
{
    // TODO: Make this dynamic
    float2 screenSize = float2(1901.0, 2263.0);
    float foveatedSamples = numSamples(input.position.xy, screenSize);
    //return float4(foveatedSamples / FoveatedStepsHigh, 0, 0, 1);
    
    input.ray.direction = normalize(input.ray.direction);
    float3 boxMin = _SliceMin;
    float3 boxMax = _SliceMax;    
        
    float tNear, tFar;
    bool hit = IntersectBox(input.ray, boxMin, boxMax, tNear, tFar);
    // Early exit of pixels missing the bounding box
    if (!hit)
    {
        return float4(0, 0, 0, 0);
    }
    
    tNear = max(0, tNear);    
    // calculate intersection points
    float3 pNear = input.ray.origin + input.ray.direction * tNear;
    float3 pFar  = input.ray.origin + input.ray.direction * tFar;
    // convert to texture space
    pNear = pNear + 0.5;
    pFar  = pFar + 0.5;
        
    float3 currentRayPosition = pNear;
    float3 rayDelta = pFar - pNear;
    float totalLength = length(rayDelta);
    // The maximum possible distance through the cube is sqrt(3).
    float stepLength = sqrt(3) / floor(foveatedSamples);
    float3 stepVector = normalize(rayDelta);
    // Calculate the required number of steps, based on the total path length through the object 
    int requiredSteps = clamp(totalLength / stepLength, 0, foveatedSamples);
    
    // Shift ray's starting point by a small temporal noise amount to reduce box artefacts
    // Based on code from Ryan Brucks: https://shaderbits.com/blog/creating-volumetric-ray-marcher
    float3 randVector = nrand(input.position.xy +_Time.xy) * stepVector * stepLength * _Jitter;
    currentRayPosition += randVector;
    
    // Maximum Value transfer function (MIP)
    float rayValue = 0;
    for(int i = 0; i < requiredSteps; i++)
    {        
        float stepValue = dataLookup(currentRayPosition);
        rayValue = max (stepValue, rayValue);
        // For an accumulating transfer function (AIP), we would need the step length as well: 
        // float stepValue = dataLookup(currentRayPosition) * stepLength;
        // rayValue += stepValue;
        currentRayPosition += stepVector * stepLength;
    }
    
    // After the loop, we're still in the volume, so calculate the last step length and apply the transfer function
    float remainingStepLength = totalLength - (requiredSteps + 1) * stepLength - length(randVector);
    currentRayPosition += stepVector * remainingStepLength;
    float stepValue = dataLookup(currentRayPosition);
    rayValue = max (stepValue, rayValue);
      
    // For AIP, we would normalize based on the total ray length  
    // rayValue /= totalLength;
    rayValue *= _PostScaling;
    // Apply color mapping
    float colorMapOffset = 1.0 - (0.5 + _ColorMapIndex) / _NumColorMaps;
    
    float thresholdRange = _ThresholdMax - _ThresholdMin;
    // transform from texture values to 0 -> 1
    rayValue = clamp(rayValue, 0, 1);
    float colorMapValue = (rayValue - _ThresholdMin) / thresholdRange;   
    float4 color = tex2D(_ColorMap, float2(colorMapValue, colorMapOffset));
        
    // Pre-multiply the output color
    return float4(color.xyz, rayValue);
}