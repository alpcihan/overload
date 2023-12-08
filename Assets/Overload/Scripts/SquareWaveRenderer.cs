using UnityEngine;
using UnityEngine.Rendering;

namespace Overload
{
    public class SquareWaveRenderer : MonoBehaviour
    {
        #region public 

        [Header("Wave properties")]
        [Range(0.1f, 1.0f)] public float cubeSize = 0.5f;
        public uint dimension = 512;
        [Range(0.1f, 20)] public float maxHeightMax = 2.0f;
        [Range(0.1f, 20)] public float maxHeightMin = 2.0f;
        [Range(0f, 500)] public float frequency;
        [Range(0,10)] public float speed = 1.0f;

        [Header("Wave material properties")]
        [Range(-0.001f, 1)] public float borderTickness = 0.05f;

        [Header("Render properties")]
        public ShadowCastingMode shadowCasting = ShadowCastingMode.Off;
        public bool receiveShadows = true;

        [Header("References")]
        public Mesh mesh;
        public Material material;
        public ComputeShader m_squareWaveComputeShader;

        #endregion

        #region private
        private float m_seed;
        private float m_maxHeight;

        private ComputeBuffer m_instancesBuffer;
        private ComputeBuffer m_argsBuffer;
        private MaterialPropertyBlock m_MPB;
        private Bounds m_bounds;

        private struct InstanceData
        {
            public Matrix4x4 Matrix;
            public Matrix4x4 MatrixInverse;

            public static int Size()
            {
                return sizeof(float) * 4 * 4
                     + sizeof(float) * 4 * 4;
            }
        }

        private void _initBuffers()
        {
            // per instance data
            uint instanceCount = dimension * dimension; 
            InstanceData[] instanceData = new InstanceData[instanceCount];
            m_instancesBuffer = new ComputeBuffer((int)instanceCount, InstanceData.Size());

            int halfDim = (int)dimension/2;
            int xIdx = 0, zIdx = 0;
            for (int x = -halfDim; x < halfDim; x++)
            {
                zIdx = 0;
                for (int z = -halfDim; z < halfDim; z++)
                {
                    InstanceData data = new InstanceData();
                    
                    Vector3 position = new Vector3(x*cubeSize, 1.0f, z*cubeSize);
                    Vector3 scale = new Vector3(cubeSize, 0.5f, cubeSize);
                    data.Matrix = Matrix4x4.TRS(position, Quaternion.Euler(0, 0, 0), scale);

                    data.MatrixInverse = data.Matrix.inverse;
                    
                    instanceData[zIdx * dimension + xIdx] = data;
                    zIdx++;
                }
                xIdx++;
            }

            m_argsBuffer = _createArgsBuffer(instanceCount);
            m_instancesBuffer.SetData(instanceData);
            material.SetBuffer("_PerInstanceData", m_instancesBuffer);
        }

        #endregion

        private ComputeBuffer _createArgsBuffer(uint count)
        {
            uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
            args[0] = mesh.GetIndexCount(0);
            args[1] = count;
            args[2] = mesh.GetIndexStart(0);
            args[3] = mesh.GetBaseVertex(0);
            args[4] = 0;

            ComputeBuffer buffer = new ComputeBuffer(args.Length, sizeof(uint), ComputeBufferType.IndirectArguments);
            buffer.SetData(args);
            return buffer;
        }

        #region monobehaviour

        public void OnEnable()
        {
            m_seed = 0;
            m_maxHeight = maxHeightMax;

            m_MPB = new MaterialPropertyBlock();
            m_bounds = new Bounds(Vector3.zero, new Vector3(100000, 100000, 100000)); // TODO: calculate tight bounds
            _initBuffers();
        }

        public void Update()
        {   
            // update params
            float a = Mathf.PerlinNoise1D(Time.time * 0.01f);
            maxHeightMin = Mathf.Min(maxHeightMin, maxHeightMax);
            m_maxHeight = Mathf.Lerp(maxHeightMin, maxHeightMax, a);

            m_seed += Time.deltaTime * speed;

            // wave compute pass
            m_squareWaveComputeShader.SetFloat("_maxHeight", m_maxHeight);
            m_squareWaveComputeShader.SetFloat("_unitSize", cubeSize);
            m_squareWaveComputeShader.SetFloat("_frequency", frequency*cubeSize);
            m_squareWaveComputeShader.SetFloat("_seed", m_seed);
            m_squareWaveComputeShader.SetInt("_squareWaveDimension", (int)dimension);
            m_squareWaveComputeShader.SetBuffer(0, "_models", m_instancesBuffer);

            int threadGroups = (int)dimension / 32;
            m_squareWaveComputeShader.Dispatch(0, threadGroups, threadGroups, 1);

            // update wave material
            material.SetFloat("_BorderTickness", borderTickness);

            // wave draw pass
            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, m_bounds, m_argsBuffer, 0, m_MPB, shadowCasting, receiveShadows);
        }

        private void OnDisable()
        {
            if (m_instancesBuffer != null)
            {
                m_instancesBuffer.Release();
                m_instancesBuffer = null;
            }

            if (m_argsBuffer != null)
            {
                m_argsBuffer.Release();
                m_argsBuffer = null;
            }
        }

        #endregion
    }
}

