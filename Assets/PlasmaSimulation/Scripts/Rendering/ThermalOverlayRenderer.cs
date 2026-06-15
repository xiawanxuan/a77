using UnityEngine;

[ExecuteInEditMode]
public class ThermalOverlayRenderer : MonoBehaviour
{
    public PlasmaSimConfig config;
    public Material thermalMaterial;
    public MeshRenderer overlayRenderer;

    public void Initialize(PlasmaSimConfig cfg)
    {
        config = cfg;
        if (thermalMaterial == null)
        {
            Shader thermalShader = Shader.Find("PlasmaSim/ThermalOverlay");
            if (thermalShader != null)
            {
                thermalMaterial = new Material(thermalShader);
            }
        }
    }

    public void Render(RenderTexture temperatureRT, RenderTexture densityRT)
    {
        if (thermalMaterial == null || temperatureRT == null || densityRT == null) return;

        thermalMaterial.SetTexture("_TemperatureTex", temperatureRT);
        thermalMaterial.SetTexture("_DensityTex", densityRT);
        thermalMaterial.SetFloat("_MaxTemp", config.initialPlasmaTemperature * 2f);
        thermalMaterial.SetFloat("_Alpha", config.thermalAlpha);
        thermalMaterial.SetFloat("_MinDensity", config.ambientDensity * 2f);

        if (overlayRenderer != null)
        {
            overlayRenderer.material = thermalMaterial;
        }
    }

    public void RenderToQuad(RenderTexture temperatureRT, RenderTexture densityRT, Mesh quadMesh)
    {
        if (thermalMaterial == null) return;

        thermalMaterial.SetTexture("_TemperatureTex", temperatureRT);
        thermalMaterial.SetTexture("_DensityTex", densityRT);
        thermalMaterial.SetFloat("_MaxTemp", config.initialPlasmaTemperature * 2f);
        thermalMaterial.SetFloat("_Alpha", config.thermalAlpha);
        thermalMaterial.SetFloat("_MinDensity", config.ambientDensity * 2f);

        Graphics.DrawMesh(quadMesh, transform.localToWorldMatrix, thermalMaterial, gameObject.layer);
    }

    public void SetVisible(bool visible)
    {
        if (overlayRenderer != null)
        {
            overlayRenderer.enabled = visible;
        }
    }
}
