using UnityEngine;
using UnityEngine.VFX;

[ExecuteInEditMode]
public class FakeFogController : MonoBehaviour
{
    [Min(0)] public float fogDistance;
    public Vector3 fogCenter;
    public Material[] fogMaterials;
    public VisualEffect[] fogVFXs;

    void Update()
    {
        foreach(var material in fogMaterials)
        {
            material.SetFloat("_FogDistance", fogDistance);
            material.SetVector("_FogCenter", fogCenter);
        }

        foreach (var vfx in fogVFXs)
        {
            vfx.SetFloat("fogDistance", fogDistance);
            vfx.SetVector3("fogCenter", fogCenter);
        }
    }
}
