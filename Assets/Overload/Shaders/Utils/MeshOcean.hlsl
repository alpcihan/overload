#ifndef OVERLOAD_UTILS_OCEAN_SHADER_INCLUDED
#define OVERLOAD_UTILS_OCEAN_SHADER_INCLUDED

//--------------------------------------------------------------------------------------
// Includes
//--------------------------------------------------------------------------------------
#include "Assets/Overload/Shaders/Utils/Math.hlsl"

//--------------------------------------------------------------------------------------
// Instance Indirect Data
//--------------------------------------------------------------------------------------
struct OceanInstanceData {
	float4x4 model;
	float4x4 modelInverse;
};

//--------------------------------------------------------------------------------------
// Ocean Data
//--------------------------------------------------------------------------------------
#define OCEAN_DATA  float3 _oceanCenter;        \
                    uint _oceanDimension;       \
                    float _oceanUnitSize;       \
                    float _oceanMaxHeight;      \
                    float _oceanWaveSpeed;      \
                    float _oceanWaveFrequency;  \
                    float2 _oceanFluxOffset;    \

#define OCEAN_MODEL_MATRICES_BUFFER RWStructuredBuffer<OceanInstanceData> _oceanModelMatrices;

//--------------------------------------------------------------------------------------
// Ocean Utils
//--------------------------------------------------------------------------------------
float2 oceanCalculatePos2DWorld(uint2 unitPos, int oceanDimension, float oceanUnitSize, float2 oceanFluxOffset, float3 oceanCenter) {
    const float oceanSize = oceanDimension * oceanUnitSize;
    const float halfOceanSize = oceanSize * 0.5;
    const float halfUnitSize = oceanUnitSize * 0.5;

    // ocean bound                      [0, OceanSize - 1]
    float2 xz = unitPos * oceanUnitSize;

    // add flux offset                  [0 + fluxOffset, OceanSize - 1 + fluxOffset]
    xz += oceanFluxOffset.xy;

    // mod by ocean size                [0, OceanSize - 1]
    xz %= oceanSize;

    // calculate center of the mesh     [0 + halfUnitSize, OceanSize - 1 + halfUnitSize] 
    xz += halfUnitSize;

    // center the ocean at local space
    xz -= halfOceanSize;

    // calculate the world space position
    xz += oceanCenter.xy;
    
    return xz;
}

inline float oceanCalculateHeightWorld(float heightLocal, float3 oceanCenter) {
    return heightLocal * 0.5 + oceanCenter.y;
}

uint2 oceanCalculateUnitPosition(uint2 idx, float oceanDimension, float oceanUnitSize, float2 oceanFluxOffset) {
    idx += (uint)(oceanFluxOffset / oceanUnitSize);
    idx %= oceanDimension;

    return idx;
}

// Warning: OCEAN_ENCODE_INSTANCE_UNIT_POSITION and OCEAN_DECODE_INSTANCE_UNIT_POSITION are
//          only used to transfer mesh unit position data from the cull compute pass to the
//          mesh ocean compute pass to reduce duplicated calculations.
//          Currently, it encodes the data into model matrices to avoid using a dedicated buffer,
//          which can easily become large for this purpose.
//          The data is read during the mesh ocean pass and then overridden with proper model data.
#define OCEAN_ENCODE_INSTANCE_UNIT_POSITION(in_instanceIDX, in_unitPos) \
        _oceanModelMatrices[in_instanceIDX].model[0][0] = in_unitPos.x; \
        _oceanModelMatrices[in_instanceIDX].model[2][2] = in_unitPos.y; \

#define OCEAN_DECODE_INSTANCE_UNIT_POSITION(in_instanceIDX, out_unitPos)      \
        out_unitPos = uint2(_oceanModelMatrices[in_instanceIDX].model[0][0],  \
                            _oceanModelMatrices[in_instanceIDX].model[2][2]); \

#endif
