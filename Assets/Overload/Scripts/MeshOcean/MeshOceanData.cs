using System;
using UnityEngine;

namespace overload
{
    [Serializable]
    public struct MeshOceanData
    {
        public enum MeshOceanMode: int { Wave = 0, WaveAudio = 1, WaveCubes = 2, WaveCubesAudio = 3};
        
        [Header("Properties")]
        public MeshOceanMode mode;

        public uint dimension;

        [Min(0.001f)]
        public float unitSize;

        [Min(0.1f)]
        public float maxHeight;

        [Min(0f)]
        public float waveFrequency;

        [Min(0)]
        public float speed;

        [Min(0)]
        public float audioScale;

        public Vector2 flux;

        [Header("References")]
        public Mesh mesh;
        public Material material;
    };

}