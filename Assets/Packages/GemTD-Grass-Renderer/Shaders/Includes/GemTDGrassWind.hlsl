#ifndef GEMTD_GRASS_WIND_INCLUDED
#define GEMTD_GRASS_WIND_INCLUDED

float3 ApplyGemTDGrassWind(float3 positionWS, float bladeHeight01)
{
    float2 direction = _GrassWindDirection.xy;
    float maximumComponent = max(abs(direction.x), abs(direction.y));
    float lengthSquared = 0.0;
    if (maximumComponent <= 1e-4)
    {
        direction = float2(1.0, 0.0);
    }
    else
    {
        direction /= maximumComponent;
        lengthSquared = dot(direction, direction);
        direction *= rsqrt(max(lengthSquared, 1e-8));
    }

    float phase = dot(positionWS.xz, direction) / max(_GrassWindScale, 0.01);
    phase += _Time.y * _GrassWindSpeed;
    float gust = sin(phase) * 0.65 + sin(phase * 0.37 + 1.7) * 0.35;
    float weight = saturate(bladeHeight01);
    weight *= weight;
    positionWS.xz += direction * gust * _GrassWindStrength * weight;
    return positionWS;
}

#endif
