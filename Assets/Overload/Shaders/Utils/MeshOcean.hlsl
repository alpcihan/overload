#ifndef OVERLOAD_UTILS_MESH_OCEAN_SHADER_INCLUDED
#define OVERLOAD_UTILS_MESH_OCEAN_SHADER_INCLUDED

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
#define MESH_OCEAN_DATA float4 _oceanCenter;      \
                        int _oceanDimension;      \
                        float _oceanUnitSize;     \
                        float _oceanMaxHeight;    \
                        float _oceanWaveFrequency;
#endif

#endif