using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace overload
{
    public class MeshOceanRenderer : MonoBehaviour
    {
        #region public members

        public MeshOceanData oceanData;

        #endregion

        #region monobehaviour

        void Start() => _init();

        void Update() => _update();

        void OnDestroy() => _freeResources();

        #endregion

        #region internal references

        [Header("Audio")]
        [SerializeField] AudioSpectrumGPU m_audioSpectrumGPU;

        [Header("Internal References")]
        [SerializeField] ComputeShader m_resetInstancedIndirectDataComputeShader;
        [SerializeField] ComputeShader m_meshOceanCullComputeShader;
        [SerializeField] ComputeShader m_meshOceanComputeShader;

        #endregion

        #region members

        float m_seed;
        Vector2 m_oceanFluxOffset;

        #region instanced indirect properties

        uint[] m_indirectArgs;
        uint[] m_instanceCount = { 0 };
        ComputeBuffer m_indirectArgsBuffer;
        ComputeBuffer m_modelMatricesBuffer;
        MaterialPropertyBlock m_MPB;
        Bounds m_bounds;

        #endregion

        #endregion

        void _init()
        {
            // init member variables
            m_seed = 0;
            m_oceanFluxOffset = Vector2.zero;

            m_MPB = new MaterialPropertyBlock();
            m_bounds = new Bounds();

            _initBuffers();
        }

        void _initBuffers()
        {
            // init args buffer
            uint maxInstanceCount = oceanData.dimension * oceanData.dimension;
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
                m_meshOceanComputeShader.SetBuffer(0, ShaderID._audioSpectrum, m_audioSpectrumGPU.audioSpectrumBuffer); // TODO: add audio spectrum null check
            }
        }

        void _update()
        {
            _updateParameters();

            // passes
            _resetIIDComputePass();
            _cullComputePass();
            _waveHeightComputePass();
            _drawIID();
        }

        void _updateParameters()
        {
            m_seed += oceanData.speed * Time.deltaTime;

            m_oceanFluxOffset += oceanData.flux * Time.deltaTime;
        }

        void _resetIIDComputePass()
        {
            m_resetInstancedIndirectDataComputeShader.Dispatch(0, 1, 1, 1);
        }

        void _cullComputePass()
        {
            _updateOceanDataUniform(m_meshOceanCullComputeShader);

            m_meshOceanCullComputeShader.SetMatrix(ShaderID._cameraVP, Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix);

            // dispatch
            int threadGroups = (int)oceanData.dimension / 32;
            m_meshOceanCullComputeShader.Dispatch(0, threadGroups, threadGroups, 1);
        }

        void _waveHeightComputePass()
        {
            _updateOceanDataUniform(m_meshOceanComputeShader);

            m_meshOceanComputeShader.SetFloat(ShaderID._seed, m_seed);

            // dispatch (TODO: dispatch indirect)
            m_indirectArgsBuffer.GetData(m_instanceCount, 0, 1, 1);
            int threadGroups = (int)MathF.Ceiling((float)m_instanceCount[0] / (float)1024);
            m_meshOceanComputeShader.Dispatch(0, threadGroups, 1, 1);
        }

        void _updateOceanDataUniform(ComputeShader cs)
        {
            // update uniforms (TODO: load uniforms shared across multiple shaders to a single buffer)
            cs.SetVector(ShaderID._oceanCenter, transform.position);
            cs.SetInt(ShaderID._oceanDimension, (int)oceanData.dimension);
            cs.SetFloat(ShaderID._oceanUnitSize, oceanData.unitSize);
            cs.SetFloat(ShaderID._oceanMaxHeight, oceanData.maxHeight);
            cs.SetFloat(ShaderID._oceanWaveFrequency, oceanData.waveFrequency);
            cs.SetVector(ShaderID._oceanFluxOffset, m_oceanFluxOffset);
        }

        void _drawIID()
        {
            _updateOceanBounds();
            Graphics.DrawMeshInstancedIndirect(oceanData.mesh, 0, oceanData.material, m_bounds, m_indirectArgsBuffer, 0, m_MPB, ShadowCastingMode.Off, false);
        }

        void _updateOceanBounds()
        {
            m_bounds = new Bounds(transform.position,
                                 new Vector3(
                                    oceanData.unitSize * oceanData.dimension,
                                    oceanData.maxHeight,
                                    oceanData.unitSize * oceanData.dimension)
                                ); // TODO: calculate tighter bounds
        }

        void _freeResources()
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
    }
}
