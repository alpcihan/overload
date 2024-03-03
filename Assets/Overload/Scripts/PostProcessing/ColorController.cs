using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

public class ColorController : MonoBehaviour
{
    #region public 

    public Color primary;
    public Color secondary;

    public Material[] materials;
    public VisualEffect[] vfxs;
    public VisualEffect[] soundvfxs;

    public void switchColors() => m_isSwitched = !m_isSwitched;

    #endregion

    #region mono behaviour

    void Update()
    {
        if(m_isSwitched)
        {
            setColors(secondary, primary);
            foreach(var vfx in soundvfxs)
            {
                vfx.SetVector4("_color", primary);
            }
        }   
        else
        {
            setColors(primary, secondary);
            Vector4 col = new Vector4(secondary.r * 20, secondary.g * 20, secondary.b * 20, 20);
            
            foreach (var vfx in soundvfxs)
            {
                vfx.SetVector4("_color", col);
            }
        }
    }

    #endregion

    #region private

    private bool m_isSwitched = false; 

    private void setColors(Color primary, Color secondary)
    {
        foreach (var material in materials)
        {
            material.SetColor("_primaryColor", primary);
            material.SetColor("_secondaryColor", secondary);

        }

        foreach (var vfx in vfxs)
        {
            vfx.SetVector4("_primaryColor", primary.linear);
            vfx.SetVector4("_secondaryColor", secondary.linear);
        }
    }

    #endregion
}
