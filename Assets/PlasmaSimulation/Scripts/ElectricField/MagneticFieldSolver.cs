using UnityEngine;
using System.Collections.Generic;

public class MagneticFieldSolver : MonoBehaviour
{
    public PlasmaSimConfig config;

    public RenderTexture magneticFieldRT;
    public RenderTexture magneticFieldRT2;
    private RenderTexture defaultChargeDensityRT;

    public ComputeShader magneticCompute;

    private int kernelCoilInject;
    private int kernelMagneticAdvection;
    private int kernelMagneticDiffusion;
    private int kernelLorentzForce;
    private int kernelMagneticClear;
    private int kernelMagneticBoundary;

    private bool initialized;
    private List<CoilData> coils = new List<CoilData>();

    public bool IsInitialized => initialized;
    public RenderTexture MagneticFieldTexture => magneticFieldRT;
    public List<CoilData> GetCoils() => coils;

    public struct CoilData
    {
        public Vector2 position;
        public float radius;
        public float current;
        public int sign;
    }

    public void Initialize(PlasmaSimConfig cfg)
    {
        config = cfg;

        if (magneticCompute == null)
        {
            magneticCompute = Resources.Load<ComputeShader>("MagneticFieldSolver");
        }
        if (magneticCompute == null)
        {
            Debug.LogError("MagneticFieldSolver compute shader not assigned!");
            return;
        }

        FindKernels();
        CreateRenderTextures();
        ClearMagneticField();
        initialized = true;
    }

    void FindKernels()
    {
        kernelCoilInject = magneticCompute.FindKernel("CSCoilInject");
        kernelMagneticAdvection = magneticCompute.FindKernel("CSMagneticAdvection");
        kernelMagneticDiffusion = magneticCompute.FindKernel("CSMagneticDiffusion");
        kernelLorentzForce = magneticCompute.FindKernel("CSLorentzForce");
        kernelMagneticClear = magneticCompute.FindKernel("CSMagneticClear");
        kernelMagneticBoundary = magneticCompute.FindKernel("CSMagneticBoundary");
    }

    void CreateRenderTextures()
    {
        int res = config.gridResolution;
        magneticFieldRT = CreateFieldRT(res, RenderTextureFormat.RFloat);
        magneticFieldRT2 = CreateFieldRT(res, RenderTextureFormat.RFloat);
        defaultChargeDensityRT = CreateFieldRT(res, RenderTextureFormat.RFloat);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = defaultChargeDensityRT;
        GL.Clear(true, true, new Color(0, 0, 0, 0));
        RenderTexture.active = prev;
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

    public void AddCoil(Vector2 position, float radius, float current, int sign)
    {
        coils.Add(new CoilData
        {
            position = position,
            radius = radius,
            current = current,
            sign = sign
        });
    }

    public void RemoveCoil(int index)
    {
        if (index >= 0 && index < coils.Count)
        {
            coils.RemoveAt(index);
        }
    }

    public void ClearCoils()
    {
        coils.Clear();
    }

    public void ClearMagneticField()
    {
        int tg = config.gridResolution / 16;
        SetRT(kernelMagneticClear, "_MagneticFieldOut", magneticFieldRT);
        magneticCompute.Dispatch(kernelMagneticClear, tg, tg, 1);
    }

    public void Step(RenderTexture velocityRT, RenderTexture densityRT,
                     RenderTexture chargeDensityRT, RenderTexture velocityOutRT)
    {
        if (!initialized) return;

        int tg = config.gridResolution / 16;
        magneticCompute.SetInt("_GridRes", config.gridResolution);
        magneticCompute.SetFloat("_Dx", config.Dx);
        magneticCompute.SetFloat("_Dt", config.dt);
        magneticCompute.SetFloat("_MagneticViscosity", config.magneticViscosity);
        magneticCompute.SetFloat("_VacuumPermeability", config.vacuumPermeability);
        magneticCompute.SetFloat("_LorentzStrength", config.lorentzForceStrength);
        magneticCompute.SetFloat("_MaxB", config.maxMagneticField);
        magneticCompute.SetFloat("_PulseFactor", config.magneticFieldPulse);

        foreach (var coil in coils)
        {
            magneticCompute.SetVector("_CoilPos", coil.position);
            magneticCompute.SetFloat("_CoilRadius", coil.radius);
            magneticCompute.SetFloat("_CoilCurrent", coil.current);
            magneticCompute.SetFloat("_CoilSign", coil.sign);

            SetRT(kernelCoilInject, "_MagneticField", magneticFieldRT);
            SetRT(kernelCoilInject, "_MagneticFieldOut", magneticFieldRT2);
            magneticCompute.Dispatch(kernelCoilInject, tg, tg, 1);
            SwapMagnetic();
        }

        SetRT(kernelMagneticAdvection, "_MagneticField", magneticFieldRT);
        SetRT(kernelMagneticAdvection, "_Velocity", velocityRT);
        SetRT(kernelMagneticAdvection, "_MagneticFieldOut", magneticFieldRT2);
        magneticCompute.Dispatch(kernelMagneticAdvection, tg, tg, 1);
        SwapMagnetic();

        for (int i = 0; i < config.magneticFieldIterations; i++)
        {
            SetRT(kernelMagneticDiffusion, "_MagneticField", magneticFieldRT);
            SetRT(kernelMagneticDiffusion, "_MagneticFieldOut", magneticFieldRT2);
            magneticCompute.Dispatch(kernelMagneticDiffusion, tg, tg, 1);
            SwapMagnetic();
        }

        SetRT(kernelMagneticBoundary, "_MagneticField", magneticFieldRT);
        SetRT(kernelMagneticBoundary, "_MagneticFieldOut", magneticFieldRT2);
        magneticCompute.Dispatch(kernelMagneticBoundary, tg, tg, 1);
        SwapMagnetic();

        SetRT(kernelLorentzForce, "_MagneticField", magneticFieldRT);
        SetRT(kernelLorentzForce, "_Velocity", velocityRT);
        SetRT(kernelLorentzForce, "_Density", densityRT);
        SetRT(kernelLorentzForce, "_ChargeDensity", chargeDensityRT != null ? chargeDensityRT : defaultChargeDensityRT);
        SetRT(kernelLorentzForce, "_VelocityOut", velocityOutRT);
        magneticCompute.Dispatch(kernelLorentzForce, tg, tg, 1);
    }

    void SetRT(int kernel, string name, RenderTexture rt)
    {
        magneticCompute.SetTexture(kernel, name, rt);
    }

    void SwapMagnetic()
    {
        RenderTexture tmp = magneticFieldRT;
        magneticFieldRT = magneticFieldRT2;
        magneticFieldRT2 = tmp;
    }

    void OnDestroy()
    {
        if (magneticFieldRT != null) { magneticFieldRT.Release(); Destroy(magneticFieldRT); }
        if (magneticFieldRT2 != null) { magneticFieldRT2.Release(); Destroy(magneticFieldRT2); }
        if (defaultChargeDensityRT != null) { defaultChargeDensityRT.Release(); Destroy(defaultChargeDensityRT); }
    }
}
