#ifndef OVERLOAD_UTILS_CUBE_OUTLINE_INCLUDED
#define OVERLOAD_UTILS_CUBE_OUTLINE_INCLUDED

// Helper function to create the object space position of a fragment in a cube mesh using UV and normal information.
//
// This is particularly useful in VFX shader graphs. At the time of implementation,
// VFX shader graphs provide local space position (relative to the particle system root) instead of object position.
// TODO: replace if statements
void cubeNormalAndUVToObjectSpacePosition_half(float2 uv, float3 normal, out float3 positionObject) {
    float3 position = float3(0,0,0);

    positionObject = position;
};

#endif