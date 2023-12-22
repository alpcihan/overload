using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace overload
{
    [Serializable]
    public struct MeshOceanProps
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
    }

    public class MeshOceanRenderer : MonoBehaviour
    {
        #region public 

        public MeshOceanProps oceanProps;

        [Header("Render properties")]
        public ShadowCastingMode shadowCasting = ShadowCastingMode.Off;
        public bool receiveShadows = true;

        [Header("Internal")]
        public ComputeShader m_resetInstancedIndirectDataComputeShader;
        public ComputeShader m_meshOceanCullComputeShader;
        public ComputeShader m_meshOceanComputeShader;

        #endregion

        #region private
        private struct MeshOceanRendererProps {

        };

        private float m_seed;
        private float m_maxHeight;

        uint[] m_indirectArgs;
        uint[] m_instanceCount = { 0 };
        private ComputeBuffer m_modelMatricesBuffer;
        private ComputeBuffer m_indirectArgsBuffer;

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
            uint maxInstanceCount = oceanProps.oceanDimension * oceanProps.oceanDimension;

            // model matrices buffer
            m_modelMatricesBuffer = new ComputeBuffer((int)maxInstanceCount, InstanceData.Size());

            // args buffer
            m_indirectArgs = new uint[5] { oceanProps.mesh.GetIndexCount(0), maxInstanceCount, oceanProps.mesh.GetIndexStart(0), oceanProps.mesh.GetBaseVertex(0), 0 };
            m_indirectArgsBuffer = new ComputeBuffer(m_indirectArgs.Length, sizeof(uint), ComputeBufferType.IndirectArguments);
            m_indirectArgsBuffer.SetData(m_indirectArgs);

            // init ocean unit data (TODO: use compute shader)
            {
                InstanceData[] instanceData = new InstanceData[maxInstanceCount];
                int halfDim = (int)oceanProps.oceanDimension / 2;
                int xIdx = 0, zIdx = 0;
                for (int x = -halfDim; x < halfDim; x++)
                {
                    zIdx = 0;
                    for (int z = -halfDim; z < halfDim; z++)
                    {
                        InstanceData data = new InstanceData();

                        Vector3 position = new Vector3(x * oceanProps.unitSize, 1.0f, z * oceanProps.unitSize);
                        Vector3 scale = new Vector3(oceanProps.unitSize, 0.5f, oceanProps.unitSize);
                        data.Matrix = Matrix4x4.TRS(position, Quaternion.Euler(0, 0, 0), scale);

                        data.MatrixInverse = data.Matrix.inverse;

                        instanceData[zIdx * oceanProps.oceanDimension + xIdx] = data;
                        zIdx++;
                    }
                    xIdx++;
                }
                m_modelMatricesBuffer.SetData(instanceData);
            }

            // assign shader params
            {
                m_resetInstancedIndirectDataComputeShader.SetBuffer(0, "_indirectArgs", m_indirectArgsBuffer);

                m_meshOceanCullComputeShader.SetBuffer(0, "_indirectArgs", m_indirectArgsBuffer);
                m_meshOceanCullComputeShader.SetBuffer(0, "_modelMatrices", m_modelMatricesBuffer);

                m_meshOceanComputeShader.SetBuffer(0, "_indirectArgs", m_indirectArgsBuffer);
                m_meshOceanComputeShader.SetBuffer(0, "_modelMatrices", m_modelMatricesBuffer);

                oceanProps.material.SetBuffer("_perInstanceData", m_modelMatricesBuffer);
            }
        }

        #endregion

        #region monobehaviour

        public void OnEnable()
        {
            m_seed = 0;
            m_maxHeight = oceanProps.maxHeightMax;

            m_MPB = new MaterialPropertyBlock();

            _initBuffers();
        }

        public void Update()
        {
            // update params
            {
                float a = Mathf.PerlinNoise1D(Time.time * 0.01f);
                oceanProps.maxHeightMin = Mathf.Min(oceanProps.maxHeightMin, oceanProps.maxHeightMax);
                m_maxHeight = Mathf.Lerp(oceanProps.maxHeightMin, oceanProps.maxHeightMax, a);

                m_seed += Time.deltaTime * oceanProps.speed;
            }
            
            // reset indirect args compute pass
            {
                m_instanceCount[0] = 0;
                m_indirectArgsBuffer.SetData(m_instanceCount, 0, 1, 1);
            }

            // wave cull compute pass
            {
                m_meshOceanCullComputeShader.SetInt("_oceanDimension", (int)oceanProps.oceanDimension);
                m_meshOceanCullComputeShader.SetFloat("_unitSize", oceanProps.unitSize);
                m_meshOceanCullComputeShader.SetFloat("_maxHeight", m_maxHeight);
                m_meshOceanCullComputeShader.SetMatrix("_cameraVP", Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix);

                // Camera.main.depthTextureMode = DepthTextureMode.Depth;
                // m_meshOceanCullComputeShader.SetTexture(0, "_sceneDepth", Shader.GetGlobalTexture("_CameraDepthTexture"));

                int threadGroups = (int)oceanProps.oceanDimension / 32;
                m_meshOceanCullComputeShader.Dispatch(0, threadGroups, threadGroups, 1);
                m_indirectArgsBuffer.GetData(m_instanceCount, 0, 1, 1);
            }
            
            
            // wave compute pass
            {
                m_meshOceanComputeShader.SetInt("_oceanDimension", (int)oceanProps.oceanDimension);
                m_meshOceanComputeShader.SetFloat("_unitSize", oceanProps.unitSize);
                m_meshOceanComputeShader.SetFloat("_maxHeight", m_maxHeight);
                m_meshOceanComputeShader.SetFloat("_waveFrequency", oceanProps.waveFrequency * oceanProps.unitSize);
                m_meshOceanComputeShader.SetFloat("_seed", m_seed);

                int threadGroups = (int)MathF.Ceiling((float)m_instanceCount[0] / (float)1024);
                m_meshOceanComputeShader.Dispatch(0, threadGroups, 1, 1);
            }

            // wave draw pass
            m_bounds = new Bounds(Vector3.zero, new Vector3(oceanProps.unitSize * oceanProps.oceanDimension, m_maxHeight, oceanProps.unitSize * oceanProps.oceanDimension)); // TODO: calculate tighter bounds
            Graphics.DrawMeshInstancedIndirect(oceanProps.mesh, 0, oceanProps.material, m_bounds, m_indirectArgsBuffer, 0, m_MPB, shadowCasting, receiveShadows);
            
        }

        private void OnDisable()
        {
            if (m_modelMatricesBuffer != null)
            {
                m_modelMatricesBuffer.Release();
                m_modelMatricesBuffer = null;
            }

            if (m_indirectArgsBuffer != null)
            {
                m_indirectArgsBuffer.Release();
                m_indirectArgsBuffer = null;
            }
        }

        #endregion
    }
}
