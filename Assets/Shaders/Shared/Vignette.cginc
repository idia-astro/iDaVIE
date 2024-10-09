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
uniform float VignetteFadeStart;
uniform float VignetteFadeEnd;
uniform float VignetteIntensity;
uniform float4 VignetteColor;

float GetVignetteWeight(float2 position)
{
    bool leftEye = position.x < _ScreenParams.x / 2.0;
    position = float2(position.x % _ScreenParams.x, position.y);       
    float2 center = _ScreenParams.xy / 2.0;
    float2 delta = center - position;
    if (leftEye)
    {
        delta.x = max(delta.x, 0);
    }
    else
    {
        delta.x = min(delta.x, 0);
    }
    float radius = length(delta)/ _ScreenParams.x;    
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