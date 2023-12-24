using System;
using UnityEngine;

namespace overload
{
    [Serializable]
    public struct MeshOceanData
    {
        public uint oceanDimension;

        [Range(0.1f, 1.0f)]
        public float unitSize;

        [Range(0.1f, 20)]
        public float maxHeightMax;

        [Range(0.1f, 20)]
        public float maxHeightMin;

        [Range(0f, 500)]
        public float waveFrequency;

        [Range(0, 10)]
        public float speed;

        public Mesh mesh;

        public Material material;
    };

    public struct MeshOceanShaderData {
        public Vector4 oceanCenter; // TODO: use vec3 with alignment handle
        public int oceanDimension;
        public float unitSize;
        public float maxHeight;
    };
}