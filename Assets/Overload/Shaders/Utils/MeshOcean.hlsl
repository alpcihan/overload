#ifndef OVERLOAD_UTILS_MESH_OCEAN_SHADER_INCLUDED
#define OVERLOAD_UTILS_MESH_OCEAN_SHADER_INCLUDED

//--------------------------------------------------------------------------------------
// Includes
//--------------------------------------------------------------------------------------
#include "Assets/Overload/Shaders/Utils/Math.hlsl"

//--------------------------------------------------------------------------------------
// Instance Indirect Data
//--------------------------------------------------------------------------------------
struct InstanceData {
	float4x4 model;
	float4x4 modelInverse;
};

//--------------------------------------------------------------------------------------
// Mesh Ocean Data
//--------------------------------------------------------------------------------------
#ifndef MESH_OCEAN_DATA
#define MESH_OCEAN_DATA float3 _oceanCenter;      \
                        int _oceanDimension;      \
                        float _oceanUnitSize;     \
                        float _oceanMaxHeight;    \
                        float _oceanWaveFrequency;\
                        float2 _oceanFluxOffset;
#endif

//--------------------------------------------------------------------------------------
// Mesh Ocean Utils
//--------------------------------------------------------------------------------------
float2 calculateOceanMeshPos2DWorld(uint2 idx, int oceanDimension, float oceanUnitSize, float2 oceanFluxOffset, float3 oceanCenter) {
    const float oceanSize = oceanDimension * oceanUnitSize;
    const float halfOceanSize = oceanSize * 0.5;
    const float halfUnitSize = oceanUnitSize * 0.5;

    // ocean bound                      [0, OceanSize - 1]
    float2 xz = idx * oceanUnitSize;

    // add flux offset                  [0 + fluxOffset, OceanSize - 1 + fluxOffset]
    xz += oceanFluxOffset.xy;

    // mod by ocean size                [0, OceanSize - 1]
    xz %= float2(oceanSize, oceanSize);

    // calculate center of the mesh     [0 + halfUnitSize, OceanSize - 1 + halfUnitSize] 
    xz += float2(halfUnitSize, halfUnitSize);

    // center the ocean at local space
    xz -= float2(halfOceanSize, halfOceanSize);

    // calculate the world space position
    xz += oceanCenter.xy;
    
    return xz;
}

inline float calculateOceanMeshHeightWorld(float heightLocal, float3 oceanCenter) {
    return heightLocal * 0.5 + oceanCenter.y;;
}

#endif