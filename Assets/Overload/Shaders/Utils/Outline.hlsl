#ifndef OVERLOAD_UTILS_OUTLINE_INCLUDED
#define OVERLOAD_UTILS_OUTLINE_INCLUDED

// During the implementation phase, stencil writes are not supported in Shader Graph.
// As a quick workaround, values greater than one will be used to denote their usage in outline shaders.
// While using a renderer feature to filter layers would be a proper solution,
// it does not work seamlessly with VFX meshes right away.
// However, since the current approach is sufficient for the project,
// it is being used (even though it harms the accuracy of the color channel slightly).

void encodeFloat3WithOutlineStencil_float(float3 value, out float3 encoded) {
    encoded = value + 1;
};

bool isOutlineCalculateStencil(float3 value) {
    return value.x > 1;
}

void decodeFloat3WithOutlineStencil_float(float3 encoded, out float3 value) {
    value = encoded - 1;
};

#endif