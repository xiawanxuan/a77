using UnityEngine;
using System.Collections.Generic;

public class ElectricFieldSolver : MonoBehaviour
{
    public PlasmaSimConfig config;

    public RenderTexture potentialRT;
    public RenderTexture potentialRT2;
    public RenderTexture eFieldRT;
    public RenderTexture eFieldRT2;
    public RenderTexture chargeDensityRT;
    public RenderTexture chargeDensityRT2;
    public RenderTexture electrodeMaskRT;

    public ComputeShader efieldCompute;
    private int kernelPoissonInit;
    private int kernelPoissonJacobi;
    private int kernelElectricField;
    private int kernelChargeDensity;
    private int kernelElectrodeInject;

    private bool initialized;
    private List<ElectrodeData> electrodes = new List<ElectrodeData>();

    public bool IsInitialized => initialized;
    public RenderTexture ElectricFieldTexture => eFieldRT;
    public RenderTexture ChargeDensityTexture => chargeDensityRT;

    public struct ElectrodeData
    {
        public Vector2 position;
        public float radius;
        public float voltage;
        public int sign;
    }

    public void Initialize(PlasmaSimConfig cfg)
    {
        config = cfg;
        if (efieldCompute == null)
        {
            efieldCompute = Resources.Load<ComputeShader>("ElectricFieldSolver");
        }
        if (efieldCompute == null)
        {
            Debug.LogError("ElectricFieldSolver compute shader not assigned!");
            return;
        }

        FindKernels();
        CreateRenderTextures();
        initialized = true;
    }

    void FindKernels()
    {
        kernelPoissonInit = efieldCompute.FindKernel("CSPoissonInit");
        kernelPoissonJacobi = efieldCompute.FindKernel("CSPoissonJacobi");
        kernelElectricField = efieldCompute.FindKernel("CSElectricField");
        kernelChargeDensity = efieldCompute.FindKernel("CSChargeDensity");
        kernelElectrodeInject = efieldCompute.FindKernel("CSElectrodeInject");
    }

    void CreateRenderTextures()
    {
        int res = config.gridResolution;

        potentialRT = CreateFieldRT(res, RenderTextureFormat.RFloat);
        potentialRT2 = CreateFieldRT(res, RenderTextureFormat.RFloat);
        eFieldRT = CreateFieldRT(res, RenderTextureFormat.RGFloat);
        eFieldRT2 = CreateFieldRT(res, RenderTextureFormat.RGFloat);
        chargeDensityRT = CreateFieldRT(res, RenderTextureFormat.RFloat);
        chargeDensityRT2 = CreateFieldRT(res, RenderTextureFormat.RFloat);
        electrodeMaskRT = CreateFieldRT(res, RenderTextureFormat.RFloat);
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

    public void AddElectrode(Vector2 position, float radius, float voltage, int sign)
    {
        electrodes.Add(new ElectrodeData
        {
            position = position,
            radius = radius,
            voltage = voltage,
            sign = sign
        });
        UpdateElectrodeMask();
    }

    public void RemoveElectrode(int index)
    {
        if (index >= 0 && index < electrodes.Count)
        {
            electrodes.RemoveAt(index);
            UpdateElectrodeMask();
        }
    }

    public void ClearElectrodes()
    {
        electrodes.Clear();
        UpdateElectrodeMask();
    }

    public List<ElectrodeData> GetElectrodes() => electrodes;

    void UpdateElectrodeMask()
    {
        int tg = config.gridResolution / 16;
        efieldCompute.SetInt("_GridRes", config.gridResolution);
        efieldCompute.SetFloat("_Dx", config.Dx);
        efieldCompute.SetFloat("_ElectrodeVoltage", 0f);

        SetRT(kernelPoissonInit, "_PotentialOut", electrodeMaskRT);
        SetRT(kernelPoissonInit, "_Potential", potentialRT);
        efieldCompute.Dispatch(kernelPoissonInit, tg, tg, 1);

        foreach (var elec in electrodes)
        {
            efieldCompute.SetVector("_ElectrodePos", elec.position);
            efieldCompute.SetFloat("_ElectrodeRadius", elec.radius);
            efieldCompute.SetFloat("_ElectrodeVoltage", elec.voltage);
            efieldCompute.SetFloat("_ElectrodeSign", elec.sign);

            SetRT(kernelElectrodeInject, "_PotentialOut", electrodeMaskRT);
            efieldCompute.Dispatch(kernelElectrodeInject, tg, tg, 1);
        }
    }

    public void Step(RenderTexture fluidDensity, RenderTexture fluidTemperature)
    {
        if (!initialized) return;

        int tg = config.gridResolution / 16;
        efieldCompute.SetInt("_GridRes", config.gridResolution);
        efieldCompute.SetFloat("_Dx", config.Dx);
        efieldCompute.SetFloat("_Permittivity", config.permittivity);
        efieldCompute.SetFloat("_ElectrodeVoltage", config.electrodeVoltage);
        efieldCompute.SetFloat("_ChargeScale", config.chargeCouplingStrength);

        SetRT(kernelChargeDensity, "_DensityField", fluidDensity);
        SetRT(kernelChargeDensity, "_TemperatureField", fluidTemperature);
        SetRT(kernelChargeDensity, "_ChargeDensOut", chargeDensityRT2);
        efieldCompute.Dispatch(kernelChargeDensity, tg, tg, 1);
        Swap(ref chargeDensityRT, ref chargeDensityRT2);

        SetRT(kernelPoissonInit, "_ElectrodeMask", electrodeMaskRT);
        SetRT(kernelPoissonInit, "_PotentialOut", potentialRT);
        efieldCompute.Dispatch(kernelPoissonInit, tg, tg, 1);

        foreach (var elec in electrodes)
        {
            efieldCompute.SetVector("_ElectrodePos", elec.position);
            efieldCompute.SetFloat("_ElectrodeRadius", elec.radius);
            efieldCompute.SetFloat("_ElectrodeVoltage", elec.voltage);
            efieldCompute.SetFloat("_ElectrodeSign", elec.sign);
            SetRT(kernelElectrodeInject, "_PotentialOut", potentialRT);
            efieldCompute.Dispatch(kernelElectrodeInject, tg, tg, 1);
        }

        for (int i = 0; i < config.electricFieldIterations; i++)
        {
            SetRT(kernelPoissonJacobi, "_Potential", potentialRT);
            SetRT(kernelPoissonJacobi, "_ChargeDens", chargeDensityRT);
            SetRT(kernelPoissonJacobi, "_ElectrodeMask", electrodeMaskRT);
            SetRT(kernelPoissonJacobi, "_PotentialOut", potentialRT2);
            efieldCompute.Dispatch(kernelPoissonJacobi, tg, tg, 1);
            Swap(ref potentialRT, ref potentialRT2);
        }

        SetRT(kernelElectricField, "_Potential", potentialRT);
        SetRT(kernelElectricField, "_EFieldOut", eFieldRT2);
        efieldCompute.Dispatch(kernelElectricField, tg, tg, 1);
        Swap(ref eFieldRT, ref eFieldRT2);
    }

    void SetRT(int kernel, string name, RenderTexture rt)
    {
        efieldCompute.SetTexture(kernel, name, rt);
    }

    static void Swap(ref RenderTexture a, ref RenderTexture b)
    {
        RenderTexture tmp = a;
        a = b;
        b = tmp;
    }

    void OnDestroy()
    {
        ReleaseRT(potentialRT, potentialRT2, eFieldRT, eFieldRT2,
                  chargeDensityRT, chargeDensityRT2, electrodeMaskRT);
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
