using UnityEngine;

public class PlasmaFluidSolver : MonoBehaviour
{
    public PlasmaSimConfig config;

    public RenderTexture densityRT;
    public RenderTexture velocityRT;
    public RenderTexture pressureRT;
    public RenderTexture temperatureRT;
    public RenderTexture divergenceRT;

    public RenderTexture densityRT2;
    public RenderTexture velocityRT2;
    public RenderTexture pressureRT2;
    public RenderTexture temperatureRT2;
    public RenderTexture divergenceRT2;

    public RenderTexture chargeDensityRT;
    public RenderTexture electricFieldRT;

    public ComputeShader fluidCompute;
    private int kernelAdvection;
    private int kernelDiffusion;
    private int kernelPressureJacobi;
    private int kernelPressureGradientSubtract;
    private int kernelInjectSource;
    private int kernelTemperatureUpdate;
    private int kernelDensityBoundary;
    private int kernelVelocityBoundary;
    private int kernelClearField;

    private bool initialized;

    public bool IsInitialized => initialized;

    public void Initialize(PlasmaSimConfig cfg)
    {
        config = cfg;
        if (fluidCompute == null)
        {
            fluidCompute = Resources.Load<ComputeShader>("FluidSolver");
        }
        if (fluidCompute == null)
        {
            Debug.LogError("FluidSolver compute shader not assigned!");
            return;
        }

        FindKernels();
        CreateRenderTextures();
        ClearAllFields();
        initialized = true;
    }

    void FindKernels()
    {
        kernelAdvection = fluidCompute.FindKernel("CSAdvection");
        kernelDiffusion = fluidCompute.FindKernel("CSDiffusion");
        kernelPressureJacobi = fluidCompute.FindKernel("CSPressureJacobi");
        kernelPressureGradientSubtract = fluidCompute.FindKernel("CSPressureGradientSubtract");
        kernelInjectSource = fluidCompute.FindKernel("CSInjectSource");
        kernelTemperatureUpdate = fluidCompute.FindKernel("CSTemperatureUpdate");
        kernelDensityBoundary = fluidCompute.FindKernel("CSDensityBoundary");
        kernelVelocityBoundary = fluidCompute.FindKernel("CSVelocityBoundary");
        kernelClearField = fluidCompute.FindKernel("CSClearField");
    }

    void CreateRenderTextures()
    {
        int res = config.gridResolution;

        densityRT = CreateFieldRT(res, RenderTextureFormat.RFloat);
        velocityRT = CreateFieldRT(res, RenderTextureFormat.RGFloat);
        pressureRT = CreateFieldRT(res, RenderTextureFormat.RFloat);
        temperatureRT = CreateFieldRT(res, RenderTextureFormat.RFloat);
        divergenceRT = CreateFieldRT(res, RenderTextureFormat.RFloat);

        densityRT2 = CreateFieldRT(res, RenderTextureFormat.RFloat);
        velocityRT2 = CreateFieldRT(res, RenderTextureFormat.RGFloat);
        pressureRT2 = CreateFieldRT(res, RenderTextureFormat.RFloat);
        temperatureRT2 = CreateFieldRT(res, RenderTextureFormat.RFloat);
        divergenceRT2 = CreateFieldRT(res, RenderTextureFormat.RFloat);

        chargeDensityRT = CreateFieldRT(res, RenderTextureFormat.RFloat);
        electricFieldRT = CreateFieldRT(res, RenderTextureFormat.RGFloat);
    }

    RenderTexture CreateFieldRT(int resolution, RenderTextureFormat format)
    {
        RenderTexture rt = new RenderTexture(resolution, resolution, 0, format)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        rt.Create();
        return rt;
    }

    public void ClearAllFields()
    {
        int tg = config.gridResolution / 16;
        fluidCompute.SetInt("_GridRes", config.gridResolution);
        fluidCompute.SetFloat("_AmbientDensity", config.ambientDensity);
        fluidCompute.SetFloat("_AmbientTemperature", config.ambientTemperature);

        SetRT(kernelClearField, "_DensityOut", densityRT);
        SetRT(kernelClearField, "_VelocityOut", velocityRT);
        SetRT(kernelClearField, "_PressureOut", pressureRT);
        SetRT(kernelClearField, "_TemperatureOut", temperatureRT);
        SetRT(kernelClearField, "_DivergenceOut", divergenceRT);
        fluidCompute.Dispatch(kernelClearField, tg, tg, 1);

        SetRT(kernelClearField, "_DensityOut", densityRT2);
        SetRT(kernelClearField, "_VelocityOut", velocityRT2);
        SetRT(kernelClearField, "_PressureOut", pressureRT2);
        SetRT(kernelClearField, "_TemperatureOut", temperatureRT2);
        SetRT(kernelClearField, "_DivergenceOut", divergenceRT2);
        fluidCompute.Dispatch(kernelClearField, tg, tg, 1);
    }

    public void InjectSource(Vector2 worldPos, float strength, float temperature, float density)
    {
        int tg = config.gridResolution / 16;
        fluidCompute.SetInt("_GridRes", config.gridResolution);
        fluidCompute.SetFloat("_Dx", config.Dx);
        fluidCompute.SetFloat("_Dt", config.dt);
        fluidCompute.SetFloat("_SourceRadius", config.laserSpotRadius);
        fluidCompute.SetFloat("_AblationEfficiency", config.ablationEfficiency);
        fluidCompute.SetVector("_SourcePos", worldPos);
        fluidCompute.SetFloat("_SourceStrength", strength);
        fluidCompute.SetFloat("_SourceTemperature", temperature);
        fluidCompute.SetFloat("_SourceDensity", density);

        SetRT(kernelInjectSource, "_Density", densityRT);
        SetRT(kernelInjectSource, "_Temperature", temperatureRT);
        SetRT(kernelInjectSource, "_Velocity", velocityRT);
        SetRT(kernelInjectSource, "_DensityOut", densityRT2);
        SetRT(kernelInjectSource, "_TemperatureOut", temperatureRT2);
        SetRT(kernelInjectSource, "_VelocityOut", velocityRT2);
        fluidCompute.Dispatch(kernelInjectSource, tg, tg, 1);
        SwapFields();
    }

    public void Step(RenderTexture externalChargeDensity, RenderTexture externalEField)
    {
        if (!initialized) return;

        int tg = config.gridResolution / 16;
        fluidCompute.SetInt("_GridRes", config.gridResolution);
        fluidCompute.SetFloat("_Dx", config.Dx);
        fluidCompute.SetFloat("_Dt", config.dt);
        fluidCompute.SetFloat("_Viscosity", config.viscosity);
        fluidCompute.SetFloat("_ThermalDiffusivity", config.thermalDiffusivity);
        fluidCompute.SetFloat("_DensityDiffusion", config.densityDiffusion);
        fluidCompute.SetFloat("_PressureScale", config.pressureScale);
        fluidCompute.SetFloat("_AmbientDensity", config.ambientDensity);
        fluidCompute.SetFloat("_AmbientTemperature", config.ambientTemperature);
        fluidCompute.SetFloat("_AblationEfficiency", config.ablationEfficiency);
        fluidCompute.SetFloat("_RadiationCooling", config.radiationCoolingCoeff);
        fluidCompute.SetFloat("_BremsstrahlungCoeff", config.bremsstrahlungCoeff);
        fluidCompute.SetFloat("_ChargeCoupling", config.chargeCouplingStrength);

        if (externalChargeDensity != null)
            fluidCompute.SetTexture(kernelPressureGradientSubtract, "_ChargeDensity", externalChargeDensity);
        if (externalEField != null)
            fluidCompute.SetTexture(kernelPressureGradientSubtract, "_ElectricField", externalEField);

        SetRT(kernelAdvection, "_Density", densityRT);
        SetRT(kernelAdvection, "_Velocity", velocityRT);
        SetRT(kernelAdvection, "_Temperature", temperatureRT);
        SetRT(kernelAdvection, "_DensityOut", densityRT2);
        SetRT(kernelAdvection, "_VelocityOut", velocityRT2);
        SetRT(kernelAdvection, "_TemperatureOut", temperatureRT2);
        fluidCompute.Dispatch(kernelAdvection, tg, tg, 1);
        SwapFields();

        SetRT(kernelDiffusion, "_Density", densityRT);
        SetRT(kernelDiffusion, "_Velocity", velocityRT);
        SetRT(kernelDiffusion, "_Temperature", temperatureRT);
        SetRT(kernelDiffusion, "_DensityOut", densityRT2);
        SetRT(kernelDiffusion, "_VelocityOut", velocityRT2);
        SetRT(kernelDiffusion, "_TemperatureOut", temperatureRT2);
        fluidCompute.Dispatch(kernelDiffusion, tg, tg, 1);
        SwapFields();

        ComputeDivergence(tg);

        fluidCompute.SetInt("_PressureIter", config.pressureIterations);
        SetRT(kernelClearField, "_PressureOut", pressureRT);
        fluidCompute.SetInt("_GridRes", config.gridResolution);
        fluidCompute.SetFloat("_AmbientDensity", 0f);
        fluidCompute.SetFloat("_AmbientTemperature", 0f);
        fluidCompute.Dispatch(kernelClearField, tg, tg, 1);
        fluidCompute.SetFloat("_AmbientDensity", config.ambientDensity);
        fluidCompute.SetFloat("_AmbientTemperature", config.ambientTemperature);

        for (int i = 0; i < config.pressureIterations; i++)
        {
            SetRT(kernelPressureJacobi, "_Pressure", pressureRT);
            SetRT(kernelPressureJacobi, "_Divergence", divergenceRT);
            SetRT(kernelPressureJacobi, "_PressureOut", pressureRT2);
            fluidCompute.Dispatch(kernelPressureJacobi, tg, tg, 1);
            SwapPressure();
        }

        SetRT(kernelPressureGradientSubtract, "_Pressure", pressureRT);
        SetRT(kernelPressureGradientSubtract, "_Velocity", velocityRT);
        SetRT(kernelPressureGradientSubtract, "_Divergence", divergenceRT);
        SetRT(kernelPressureGradientSubtract, "_VelocityOut", velocityRT2);
        SetRT(kernelPressureGradientSubtract, "_DivergenceOut", divergenceRT2);
        fluidCompute.Dispatch(kernelPressureGradientSubtract, tg, tg, 1);
        SwapVelocity();
        SwapDivergence();

        SetRT(kernelTemperatureUpdate, "_Density", densityRT);
        SetRT(kernelTemperatureUpdate, "_Temperature", temperatureRT);
        SetRT(kernelTemperatureUpdate, "_TemperatureOut", temperatureRT2);
        fluidCompute.Dispatch(kernelTemperatureUpdate, tg, tg, 1);
        SwapTemperature();

        SetRT(kernelDensityBoundary, "_Density", densityRT);
        SetRT(kernelDensityBoundary, "_DensityOut", densityRT2);
        fluidCompute.Dispatch(kernelDensityBoundary, tg, tg, 1);
        SwapDensity();

        SetRT(kernelVelocityBoundary, "_Velocity", velocityRT);
        SetRT(kernelVelocityBoundary, "_VelocityOut", velocityRT2);
        fluidCompute.Dispatch(kernelVelocityBoundary, tg, tg, 1);
        SwapVelocity();
    }

    void ComputeDivergence(int tg)
    {
        SetRT(kernelPressureGradientSubtract, "_Velocity", velocityRT);
        SetRT(kernelPressureGradientSubtract, "_Pressure", pressureRT);
        SetRT(kernelPressureGradientSubtract, "_VelocityOut", velocityRT2);
        SetRT(kernelPressureGradientSubtract, "_DivergenceOut", divergenceRT2);
        fluidCompute.Dispatch(kernelPressureGradientSubtract, tg, tg, 1);
        SwapDivergence();
    }

    void SetRT(int kernel, string name, RenderTexture rt)
    {
        fluidCompute.SetTexture(kernel, name, rt);
    }

    void SwapFields()
    {
        Swap(ref densityRT, ref densityRT2);
        Swap(ref velocityRT, ref velocityRT2);
        Swap(ref temperatureRT, ref temperatureRT2);
    }

    void SwapDensity() { Swap(ref densityRT, ref densityRT2); }
    void SwapVelocity() { Swap(ref velocityRT, ref velocityRT2); }
    void SwapPressure() { Swap(ref pressureRT, ref pressureRT2); }
    void SwapTemperature() { Swap(ref temperatureRT, ref temperatureRT2); }
    void SwapDivergence() { Swap(ref divergenceRT, ref divergenceRT2); }

    static void Swap(ref RenderTexture a, ref RenderTexture b)
    {
        RenderTexture tmp = a;
        a = b;
        b = tmp;
    }

    public void CopyFieldTo(RenderTexture source, RenderTexture dest)
    {
        Graphics.Blit(source, dest);
    }

    void OnDestroy()
    {
        ReleaseRT(densityRT, densityRT2, velocityRT, velocityRT2,
                  pressureRT, pressureRT2, temperatureRT, temperatureRT2,
                  divergenceRT, divergenceRT2, chargeDensityRT, electricFieldRT);
    }

    static void ReleaseRT(params RenderTexture[] rts)
    {
        foreach (var rt in rts)
        {
            if (rt != null)
            {
                rt.Release();
                Destroy(rt);
            }
        }
    }
}
