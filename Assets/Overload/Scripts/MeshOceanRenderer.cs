using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace overload
{
    public class MeshOceanRenderer : MonoBehaviour
    {
        #region public 

        public MeshOceanData oceanData;

        [Header("Render properties")]
        public ShadowCastingMode shadowCasting = ShadowCastingMode.Off;
        public bool receiveShadows = true;

        [Header("Internal")]
        public ComputeShader m_resetInstancedIndirectDataComputeShader;
        public ComputeShader m_meshOceanCullComputeShader;
        public ComputeShader m_meshOceanComputeShader;

        #endregion

        #region private

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
            uint maxInstanceCount = oceanData.oceanDimension * oceanData.oceanDimension;

            // model matrices buffer
            m_modelMatricesBuffer = new ComputeBuffer((int)maxInstanceCount, InstanceData.Size());

            // init args buffer
            m_indirectArgs = new uint[5] { oceanData.mesh.GetIndexCount(0), maxInstanceCount, oceanData.mesh.GetIndexStart(0), oceanData.mesh.GetBaseVertex(0), 0 };
            m_indirectArgsBuffer = new ComputeBuffer(m_indirectArgs.Length, sizeof(uint), ComputeBufferType.IndirectArguments);
            m_indirectArgsBuffer.SetData(m_indirectArgs);

            // init ocean unit data (TODO: use compute shader)
            {
                InstanceData[] instanceData = new InstanceData[maxInstanceCount];
                int halfDim = (int)oceanData.oceanDimension / 2;
                int xIdx = 0, zIdx = 0;
                for (int x = -halfDim; x < halfDim; x++)
                {
                    zIdx = 0;
                    for (int z = -halfDim; z < halfDim; z++)
                    {
                        InstanceData data = new InstanceData();

                        Vector3 position = new Vector3(x * oceanData.unitSize, 1.0f, z * oceanData.unitSize);
                        Vector3 scale = new Vector3(oceanData.unitSize, 0.5f, oceanData.unitSize);
                        data.Matrix = Matrix4x4.TRS(position, Quaternion.Euler(0, 0, 0), scale);

                        data.MatrixInverse = data.Matrix.inverse;

                        instanceData[zIdx * oceanData.oceanDimension + xIdx] = data;
                        zIdx++;
                    }
                    xIdx++;
                }
                m_modelMatricesBuffer.SetData(instanceData);
            }

            // assign shader buffers
            {
                // set material buffers
                oceanData.material.SetBuffer("_perInstanceData", m_modelMatricesBuffer);

                // set instance indirect data reset pass buffers
                m_resetInstancedIndirectDataComputeShader.SetBuffer(0, "_indirectArgs", m_indirectArgsBuffer);

                // set mesh ocean cull pass buffers
                m_meshOceanCullComputeShader.SetBuffer(0, "_indirectArgs", m_indirectArgsBuffer);
                m_meshOceanCullComputeShader.SetBuffer(0, "_modelMatrices", m_modelMatricesBuffer);

                // set mesh ocean pass buffers
                m_meshOceanComputeShader.SetBuffer(0, "_indirectArgs", m_indirectArgsBuffer);
                m_meshOceanComputeShader.SetBuffer(0, "_modelMatrices", m_modelMatricesBuffer); 
            }
        }

        #endregion

        #region monobehaviour

        public void OnEnable()
        {
            m_seed = 0;
            m_maxHeight = oceanData.maxHeightMax;

            m_MPB = new MaterialPropertyBlock();

            _initBuffers();
        }

        public void Update()
        {
            // update params
            {
                float a = Mathf.PerlinNoise1D(Time.time * 0.01f);
                oceanData.maxHeightMin = Mathf.Min(oceanData.maxHeightMin, oceanData.maxHeightMax);
                m_maxHeight = Mathf.Lerp(oceanData.maxHeightMin, oceanData.maxHeightMax, a);

                m_seed += Time.deltaTime * oceanData.speed;
            }
            
            // reset indirect args compute pass
            {
                m_resetInstancedIndirectDataComputeShader.Dispatch(0, 1, 1, 1);
            }

            // wave cull compute pass
            {
                m_meshOceanCullComputeShader.SetVector(ShaderID._oceanCenter, transform.position);
                m_meshOceanCullComputeShader.SetInt(ShaderID._oceanDimension, (int)oceanData.oceanDimension);
                m_meshOceanCullComputeShader.SetFloat(ShaderID._oceanUnitSize, oceanData.unitSize);
                m_meshOceanCullComputeShader.SetFloat(ShaderID._oceanMaxHeight, m_maxHeight);

                m_meshOceanCullComputeShader.SetMatrix(ShaderID._cameraVP, Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix);

                int threadGroups = (int)oceanData.oceanDimension / 32;
                m_meshOceanCullComputeShader.Dispatch(0, threadGroups, threadGroups, 1);
                m_indirectArgsBuffer.GetData(m_instanceCount, 0, 1, 1);
            }
            
            // wave compute pass
            {
                m_meshOceanComputeShader.SetVector(ShaderID._oceanCenter, transform.position);
                m_meshOceanComputeShader.SetInt(ShaderID._oceanDimension, (int)oceanData.oceanDimension);
                m_meshOceanComputeShader.SetFloat(ShaderID._oceanUnitSize, oceanData.unitSize);
                m_meshOceanComputeShader.SetFloat(ShaderID._oceanMaxHeight, m_maxHeight);
                m_meshOceanComputeShader.SetFloat(ShaderID._oceanWaveFrequency, oceanData.waveFrequency * oceanData.unitSize);

                m_meshOceanComputeShader.SetFloat(ShaderID._seed, m_seed);

                int threadGroups = (int)MathF.Ceiling((float)m_instanceCount[0] / (float)1024);
                m_meshOceanComputeShader.Dispatch(0, threadGroups, 1, 1);
            }

            // wave draw pass
            m_bounds = new Bounds(Vector3.zero, new Vector3(oceanData.unitSize * oceanData.oceanDimension, m_maxHeight, oceanData.unitSize * oceanData.oceanDimension)); // TODO: calculate tighter bounds
            Graphics.DrawMeshInstancedIndirect(oceanData.mesh, 0, oceanData.material, m_bounds, m_indirectArgsBuffer, 0, m_MPB, shadowCasting, receiveShadows);
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
