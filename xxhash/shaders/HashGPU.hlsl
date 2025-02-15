#if defined (UNITY_PROCEDURAL_INSTANCING_ENABLED)
    StructuredBuffer<uint> _Hashes;
#endif

float4 _Config;


/*
Then apply the configured vertical offset scale.
*/
void ConfigureProcedural()
{
#if defined (UNITY_PROCEDURAL_INSTANCING_ENABLED)
    // This is the 1D --> 2D array indexing technique
    // _Config.y = 1.0f/_resolution
    // _Config.x = _resolution
    float v = floor(_Config.y * unity_InstanceID + 0.00001);
    float u = unity_InstanceID - _Config.x * v;
    
    unity_ObjectToWorld = 0.0;
    // Translate to origin
    unity_ObjectToWorld._m03_m13_m23_m33 = float4(
        _Config.y * (u + 0.5) - 0.5,
        _Config.z * ((1.0 / 255.0) * (_Hashes[unity_InstanceID] >> 24) - 0.5),
        _Config.y * (v + 0.5) - 0.5,
        1.0
    );
    // Scale
    unity_ObjectToWorld._m00_m11_m22 = _Config.y;
#endif
}

float3 GetHashColor()
{
#if defined (UNITY_PROCEDURAL_INSTANCING_ENABLED)
    uint hash = _Hashes[unity_InstanceID];
    // Toggle the 8 least significant bits to scale in range [0, 255]
    // Then divide by 255 to scale int range [0, 1]
    return (1.0/255.0) * float3(hash & 0xFF, hash >> 8 & 0xFF, hash >> 16 & 0xFF);
#endif
    return 1.0;
}