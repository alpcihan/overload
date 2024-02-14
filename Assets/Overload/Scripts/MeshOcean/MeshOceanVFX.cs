using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace overload
{
    public class MeshOceanVFX : MonoBehaviour
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
        [SerializeField] ComputeShader m_oceanVFXCS;
        
        #endregion

        #region members

        float m_time;
        Vector2 m_oceanFluxOffset;
        uint m_instanceCount;

        #region instanced indirect properties

        ComputeBuffer m_positionsBuffer;

        #endregion

        #endregion

        void _init()
        {
            // init member variables
            m_time = 0;
            m_oceanFluxOffset = Vector2.zero;
            m_instanceCount = oceanData.dimension * oceanData.dimension;

            _initBuffers();
        }

        void _initBuffers()
        {
            m_oceanVFXCS.SetBuffer(0, "_positions", m_positionsBuffer);
        }

        void _update()
        {
            _updateParameters();
            _updateOceanDataUniform(m_oceanVFXCS);

            m_oceanVFXCS.SetFloat(ShaderID._time, m_time);
            int threadGroups = (int)MathF.Ceiling(m_instanceCount / (float)1024);
            m_oceanVFXCS.Dispatch(0, threadGroups, 1, 1);
        }

        void _updateParameters()
        {
            m_time += Time.deltaTime;
            m_oceanFluxOffset += oceanData.flux * Time.deltaTime;
        }

        void _updateOceanDataUniform(ComputeShader cs)
        {
            // update uniforms (TODO: load uniforms shared across multiple shaders to a single buffer)
            cs.SetVector(ShaderID._oceanCenter, transform.position);
            cs.SetInt(ShaderID._oceanDimension, (int)oceanData.dimension);
            cs.SetFloat(ShaderID._oceanUnitSize, oceanData.unitSize);
            cs.SetFloat(ShaderID._oceanMaxHeight, oceanData.maxHeight);
            cs.SetFloat(ShaderID._oceanWaveSpeed, oceanData.speed);
            cs.SetFloat(ShaderID._oceanWaveFrequency, oceanData.waveFrequency);
            cs.SetVector(ShaderID._oceanFluxOffset, m_oceanFluxOffset);
        }

        void _freeResources()
        {
            if (m_positionsBuffer != null)
            {
                m_positionsBuffer.Release();
                m_positionsBuffer = null;
            }
        }
    }
}
