uniform float VignetteFadeStart;
uniform float VignetteFadeEnd;
uniform float VignetteIntensity;
uniform float4 VignetteColor;
uniform float ScreenWidth;
uniform float ScreenHeight;

float GetVignetteWeight(float2 position)
{
    bool leftEye = position.x < ScreenWidth;
    position = float2(position.x % ScreenWidth, position.y);       
    float2 center = float2(ScreenWidth, ScreenHeight) / 2.0;
    float2 delta = center - position;
    if (leftEye)
    {
        delta.x = max(delta.x, 0);
    }
    else
    {
        delta.x = min(delta.x, 0);
    }
    float radius = length(delta)/ ScreenWidth;    
    return VignetteIntensity * smoothstep(VignetteFadeStart, VignetteFadeEnd, radius);
}

float4 GetVignetteFromWeight(float weight, float4 inputColor)
{
    return lerp(inputColor, VignetteColor, weight);
}

float4 GetVignette(float2 position, float4 inputColor)
{
    float weight = GetVignetteWeight(position);
    return GetVignetteFromWeight(weight, inputColor);
}