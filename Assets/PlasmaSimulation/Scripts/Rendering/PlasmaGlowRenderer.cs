using UnityEngine;

[ExecuteInEditMode]
public class PlasmaGlowRenderer : MonoBehaviour
{
    public PlasmaSimConfig config;
    public Material plasmaGlowMaterial;
    public MeshRenderer targetRenderer;

    private RenderTexture densityTemp;
    private RenderTexture temperatureTemp;

    public void Initialize(PlasmaSimConfig cfg)
    {
        config = cfg;
        if (plasmaGlowMaterial == null)
        {
            Shader glowShader = Shader.Find("PlasmaSim/PlasmaGlow");
            if (glowShader != null)
            {
                plasmaGlowMaterial = new Material(glowShader);
            }
        }
    }

    public void Render(RenderTexture densityRT, RenderTexture temperatureRT)
    {
        if (plasmaGlowMaterial == null || densityRT == null || temperatureRT == null) return;

        plasmaGlowMaterial.SetTexture("_DensityTex", densityRT);
        plasmaGlowMaterial.SetTexture("_TemperatureTex", temperatureRT);
        plasmaGlowMaterial.SetFloat("_GlowIntensity", config.glowIntensity);
        plasmaGlowMaterial.SetFloat("_GlowRadius", config.glowRadius);
        plasmaGlowMaterial.SetFloat("_MaxTemp", config.initialPlasmaTemperature * 2f);
        plasmaGlowMaterial.SetFloat("_MinDensity", config.ambientDensity * 2f);

        if (targetRenderer != null)
        {
            targetRenderer.material = plasmaGlowMaterial;
        }
    }

    public void RenderToQuad(RenderTexture densityRT, RenderTexture temperatureRT, Mesh quadMesh)
    {
        if (plasmaGlowMaterial == null) return;

        plasmaGlowMaterial.SetTexture("_DensityTex", densityRT);
        plasmaGlowMaterial.SetTexture("_TemperatureTex", temperatureRT);
        plasmaGlowMaterial.SetFloat("_GlowIntensity", config.glowIntensity);
        plasmaGlowMaterial.SetFloat("_GlowRadius", config.glowRadius);
        plasmaGlowMaterial.SetFloat("_MaxTemp", config.initialPlasmaTemperature * 2f);
        plasmaGlowMaterial.SetFloat("_MinDensity", config.ambientDensity * 2f);

        Graphics.DrawMesh(quadMesh, transform.localToWorldMatrix, plasmaGlowMaterial, gameObject.layer);
    }

    void OnDestroy()
    {
        if (densityTemp != null) densityTemp.Release();
        if (temperatureTemp != null) temperatureTemp.Release();
    }
}
