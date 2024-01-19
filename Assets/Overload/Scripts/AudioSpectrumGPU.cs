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

        void OnEnable()
        {
            m_spectrum = new float[1024];
            m_buffer = new ComputeBuffer(1024, sizeof(float));
        }

        void Update()
        {
            if (!isActive) return;
            _processAudio();
        }

        private void OnDisable()
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
            for (int i = 1; i < m_spectrum.Length - 1; i++)
            {
                Debug.DrawLine(new Vector3(i - 1, m_spectrum[i] + 10, 0), new Vector3(i, m_spectrum[i + 1] + 10, 0), Color.red);
                Debug.DrawLine(new Vector3(i - 1, Mathf.Log(m_spectrum[i - 1]) + 10, 2), new Vector3(i, Mathf.Log(m_spectrum[i]) + 10, 2), Color.cyan);
                Debug.DrawLine(new Vector3(Mathf.Log(i - 1), m_spectrum[i - 1] - 10, 1), new Vector3(Mathf.Log(i), m_spectrum[i] - 10, 1), Color.green);
                Debug.DrawLine(new Vector3(Mathf.Log(i - 1), Mathf.Log(m_spectrum[i - 1]), 3), new Vector3(Mathf.Log(i), Mathf.Log(m_spectrum[i]), 3), Color.blue);
            }
            m_buffer.SetData(m_spectrum);
        }

        #endregion
    }

}
