using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessingController : MonoBehaviour
{
    // public float intensity = 0;
    public Volume volume; // Reference to the Volume component containing chromatic aberration effect
    private ChromaticAberration chromaticAberration; // Reference to the ChromaticAberration component

    private bool m_isFound = false;
    void Start()
    {
        // Check if the volume is assigned
        if (volume == null)
        {
            Debug.LogError("Volume is not assigned!");
            return;
        }

        // Check if the ChromaticAberration effect is present in the volume
        if (!volume.profile.TryGet(out chromaticAberration))
        {
            Debug.LogError("Chromatic Aberration effect is not found in the volume!");
            return;
        }

        m_isFound = true;
    }

    void Update()
    {
        if (!m_isFound) return;

        // chromaticAberration.intensity.value = intensity;
    }

    void EnableChronaticAberration() {
        if (!m_isFound) return;
        
        chromaticAberration.active = true;
    }

    void DisableChronaticAberration()
    {
        if (!m_isFound) return;

        chromaticAberration.active = false;
    }
}