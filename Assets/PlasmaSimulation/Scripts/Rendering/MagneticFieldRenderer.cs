using UnityEngine;

[ExecuteInEditMode]
public class MagneticFieldRenderer : MonoBehaviour
{
    public PlasmaSimConfig config;
    public Material magneticMaterial;
    public MeshRenderer overlayRenderer;

    public void Initialize(PlasmaSimConfig cfg)
    {
        config = cfg;
        if (magneticMaterial == null)
        {
            Shader magneticShader = Shader.Find("PlasmaSim/MagneticFieldViz");
            if (magneticShader != null)
            {
                magneticMaterial = new Material(magneticShader);
            }
        }
    }

    public void RenderToQuad(RenderTexture magneticFieldRT, Mesh quadMesh)
    {
        if (magneticMaterial == null || magneticFieldRT == null) return;

        magneticMaterial.SetTexture("_MagneticFieldTex", magneticFieldRT);
        magneticMaterial.SetFloat("_MaxField", config.maxMagneticField);
        magneticMaterial.SetFloat("_Intensity", 1.0f);

        Graphics.DrawMesh(quadMesh, transform.localToWorldMatrix, magneticMaterial, gameObject.layer);
    }

    public void SetVisible(bool visible)
    {
        if (overlayRenderer != null)
        {
            overlayRenderer.enabled = visible;
        }
    }
}
