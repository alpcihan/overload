using System;
using UnityEngine;

namespace overload
{
    [Serializable]
    public struct MeshOceanData
    {
        [Header("Properties")]
        public uint dimension;

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

        public Vector2 flux;

        [Header("References")]
        public Mesh mesh;

        public Material material;
    };
}