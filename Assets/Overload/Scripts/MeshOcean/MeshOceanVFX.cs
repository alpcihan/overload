using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

namespace overload
{
    [RequireComponent(typeof(VisualEffect))]
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
        [SerializeField] 
        private AudioSpectrumGPU m_audioSpectrumGPU;

        [Header("Internal References")]
        [SerializeField] 
        private ComputeShader m_oceanVFXCS;
        
        #endregion

        #region private members

        private float m_time;
        private Vector2 m_oceanFluxOffset;
        private uint m_instanceCount;

        private GraphicsBuffer m_positionsBuffer;
        private GraphicsBuffer m_scalesBuffer;

        private VisualEffect m_vfx;

        #endregion

        void _init()
        {
            // init member variables
            m_time = 0;
            m_oceanFluxOffset = Vector2.zero;
            m_instanceCount = oceanData.dimension * oceanData.dimension;
            
            m_vfx = GetComponent<VisualEffect>();

            // init buffers
            m_positionsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)m_instanceCount, sizeof(float) * 3);
            m_scalesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)m_instanceCount, sizeof(float) * 3);

            // set buffers
            m_oceanVFXCS.SetBuffer(0, "_positions", m_positionsBuffer);
            m_oceanVFXCS.SetBuffer(0, "_scales", m_scalesBuffer);

            // set vfx params
            m_vfx.SetGraphicsBuffer("positions", m_positionsBuffer);
            // m_vfx.SetGraphicsBuffer("positions", m_positionsBuffer);
        }

        
        void _update()
        {   
            // update data
            _updateParameters();
            _updateUniformBuffer(m_oceanVFXCS);

            // set vfx data
            m_vfx.SetUInt("dimension", oceanData.dimension);

            // ocean compute
            int threadGroups = Mathf.CeilToInt(oceanData.dimension / 32f);
            m_oceanVFXCS.Dispatch(0, threadGroups, threadGroups, 1);
        }

        void _updateParameters()
        {
            m_time += Time.deltaTime;
            m_oceanFluxOffset += oceanData.flux * Time.deltaTime;
        }

        void _updateUniformBuffer(ComputeShader cs)
        {
            // update uniforms (TODO: load uniforms shared across multiple shaders to a single buffer)
            cs.SetFloat(ShaderID._time, m_time);
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
