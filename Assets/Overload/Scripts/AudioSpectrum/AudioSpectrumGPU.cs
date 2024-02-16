using UnityEngine;

namespace overload
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioSpectrumGPU : MonoBehaviour
    {
        #region public 

        public bool isActive = true;

        public ComputeBuffer audioSpectrumBuffer
        {
            get { return m_buffer; }
        }

        #endregion

        #region monobehaviour

        void Awake() {
            m_spectrum = new float[1024];
            m_buffer = new ComputeBuffer(1024, sizeof(float));
        }

        void Update()
        {
            if (!isActive) return;
            _processAudio();
        }

        void OnDestroy()
        {
            if (m_buffer != null)
            {
                m_buffer.Release();
                m_buffer = null;
            }
        }

        #endregion

        #region private

        float[] m_spectrum;
        ComputeBuffer m_buffer;

        void _processAudio()
        {
            AudioListener.GetSpectrumData(m_spectrum, 0, FFTWindow.Rectangular);
            m_buffer.SetData(m_spectrum);
        }

        #endregion
    }

}
