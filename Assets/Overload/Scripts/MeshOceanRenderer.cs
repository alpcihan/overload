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

        [Header("Internal References")]
        public ComputeShader m_resetInstancedIndirectDataComputeShader;
        public ComputeShader m_meshOceanCullComputeShader;
        public ComputeShader m_meshOceanComputeShader;

        #endregion

        #region monobehaviour

        private void OnEnable()
        {
            // init variables
            m_seed = 0;
            m_oceanWaveMaxHeight = oceanData.maxHeightMax;
            m_oceanFluxOffset = Vector2.zero;

            m_MPB = new MaterialPropertyBlock();

            _initBuffers();
        }

        private void Update()
        {
            _updateParameters();

            _resetIIDComputePass();
            _cullComputePass();
            _waveHeightComputePass();

            _drawIID();
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

        #region private

        private float m_seed;
        private float m_oceanWaveMaxHeight;
        private Vector2 m_oceanFluxOffset;

        private uint[] m_indirectArgs;
        private uint[] m_instanceCount = { 0 };
        private ComputeBuffer m_indirectArgsBuffer;
        private ComputeBuffer m_modelMatricesBuffer;

        private MaterialPropertyBlock m_MPB;
        private Bounds m_bounds;

        private void _initBuffers()
        {
            uint maxInstanceCount = oceanData.dimension * oceanData.dimension;

            // init args buffer
            m_indirectArgs = new uint[5] { oceanData.mesh.GetIndexCount(0), maxInstanceCount, oceanData.mesh.GetIndexStart(0), oceanData.mesh.GetBaseVertex(0), 0 };
            m_indirectArgsBuffer = new ComputeBuffer(m_indirectArgs.Length, sizeof(uint), ComputeBufferType.IndirectArguments);
            m_indirectArgsBuffer.SetData(m_indirectArgs);

            // model matrices buffer
            m_modelMatricesBuffer = new ComputeBuffer((int)maxInstanceCount, InstanceData.Size());

            // init ocean unit data (TODO: use compute shader)
            {
                InstanceData[] instanceData = new InstanceData[maxInstanceCount];
                int halfDim = (int)oceanData.dimension / 2;
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

                        instanceData[zIdx * oceanData.dimension + xIdx] = data;
                        zIdx++;
                    }
                    xIdx++;
                }
                m_modelMatricesBuffer.SetData(instanceData);
            }

            // assign shader buffers
            {
                // set material buffers
                oceanData.material.SetBuffer(ShaderID._perInstanceData, m_modelMatricesBuffer);

                // set instance indirect data reset pass buffers
                m_resetInstancedIndirectDataComputeShader.SetBuffer(0, ShaderID._indirectArgs, m_indirectArgsBuffer);

                // set mesh ocean cull compute pass buffers
                m_meshOceanCullComputeShader.SetBuffer(0, ShaderID._indirectArgs, m_indirectArgsBuffer);
                m_meshOceanCullComputeShader.SetBuffer(0, ShaderID._oceanModelMatrices, m_modelMatricesBuffer);

                // set mesh ocean compute pass buffers
                m_meshOceanComputeShader.SetBuffer(0, ShaderID._indirectArgs, m_indirectArgsBuffer);
                m_meshOceanComputeShader.SetBuffer(0, ShaderID._oceanModelMatrices, m_modelMatricesBuffer);
            }
        }

        private void _updateParameters()
        {
            float a = Mathf.PerlinNoise1D(Time.time * 0.01f);
            oceanData.maxHeightMin = Mathf.Min(oceanData.maxHeightMin, oceanData.maxHeightMax);
            m_oceanWaveMaxHeight = Mathf.Lerp(oceanData.maxHeightMin, oceanData.maxHeightMax, a);

            m_seed += oceanData.speed * Time.deltaTime;

            m_oceanFluxOffset += oceanData.flux * Time.deltaTime;
        }

        private void _resetIIDComputePass() => m_resetInstancedIndirectDataComputeShader.Dispatch(0, 1, 1, 1);

        private void _cullComputePass()
        {
            // TODO: load uniforms shared across multiple shaders to a single buffer
            m_meshOceanCullComputeShader.SetVector(ShaderID._oceanCenter, transform.position);
            m_meshOceanCullComputeShader.SetInt(ShaderID._oceanDimension, (int)oceanData.dimension);
            m_meshOceanCullComputeShader.SetFloat(ShaderID._oceanUnitSize, oceanData.unitSize);
            m_meshOceanCullComputeShader.SetFloat(ShaderID._oceanMaxHeight, m_oceanWaveMaxHeight);
            m_meshOceanCullComputeShader.SetVector(ShaderID._oceanFluxOffset, m_oceanFluxOffset);

            m_meshOceanCullComputeShader.SetMatrix(ShaderID._cameraVP, Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix);

            int threadGroups = (int)oceanData.dimension / 32;
            m_meshOceanCullComputeShader.Dispatch(0, threadGroups, threadGroups, 1);
        }

        private void _waveHeightComputePass()
        {
            // TODO: load uniforms shared across multiple shaders to a single buffer
            m_meshOceanComputeShader.SetVector(ShaderID._oceanCenter, transform.position);
            m_meshOceanComputeShader.SetInt(ShaderID._oceanDimension, (int)oceanData.dimension);
            m_meshOceanComputeShader.SetFloat(ShaderID._oceanUnitSize, oceanData.unitSize);
            m_meshOceanComputeShader.SetFloat(ShaderID._oceanMaxHeight, m_oceanWaveMaxHeight);
            m_meshOceanComputeShader.SetFloat(ShaderID._oceanWaveFrequency, oceanData.waveFrequency);
            m_meshOceanComputeShader.SetVector(ShaderID._oceanFluxOffset, m_oceanFluxOffset);

            m_meshOceanComputeShader.SetFloat(ShaderID._seed, m_seed);

            // TODO: dispatch indirect
            m_indirectArgsBuffer.GetData(m_instanceCount, 0, 1, 1);
            int threadGroups = (int)MathF.Ceiling((float)m_instanceCount[0] / (float)1024);
            m_meshOceanComputeShader.Dispatch(0, threadGroups, 1, 1);
        }

        private void _drawIID()
        {
            m_bounds = new Bounds(Vector3.zero, new Vector3(oceanData.unitSize * oceanData.dimension, m_oceanWaveMaxHeight, oceanData.unitSize * oceanData.dimension)); // TODO: calculate tighter bounds
            Graphics.DrawMeshInstancedIndirect(oceanData.mesh, 0, oceanData.material, m_bounds, m_indirectArgsBuffer, 0, m_MPB, shadowCasting, receiveShadows);
        }

        #endregion
    }
}
