using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    public PlasmaSimConfig config;

    public PlasmaFluidSolver fluidSolver;
    public ElectricFieldSolver efieldSolver;
    public ParticleCollisionModule particleModule;
    public TargetSurface targetSurface;
    public LaserPulseController laserController;
    public ElectrodeController electrodeController;
    public PlasmaGlowRenderer glowRenderer;
    public ThermalOverlayRenderer thermalRenderer;
    public SnapshotManager snapshotManager;
    public SimulationUIController uiController;

    public float simulationTimeScale = 1.0f;
    public bool pauseSimulation = false;
    public bool enableElectricField = false;
    public bool enableParticlePhysics = true;

    private Mesh displayQuad;
    private float simulationTime;

    public float SimulationTime => simulationTime;

    void Awake()
    {
        Initialize();
    }

    void Initialize()
    {
        if (config == null)
        {
            config = Resources.Load<PlasmaSimConfig>("PlasmaSimConfig");
            if (config == null)
            {
                Debug.LogError("PlasmaSimConfig not found! Please create one via Assets > Create > PlasmaSim > Configuration");
                return;
            }
        }

        if (fluidSolver == null)
            fluidSolver = GetComponent<PlasmaFluidSolver>();
        if (efieldSolver == null)
            efieldSolver = GetComponent<ElectricFieldSolver>();
        if (particleModule == null)
            particleModule = GetComponent<ParticleCollisionModule>();
        if (targetSurface == null)
            targetSurface = GetComponent<TargetSurface>();
        if (laserController == null)
            laserController = GetComponent<LaserPulseController>();
        if (electrodeController == null)
            electrodeController = GetComponent<ElectrodeController>();
        if (glowRenderer == null)
            glowRenderer = GetComponent<PlasmaGlowRenderer>();
        if (thermalRenderer == null)
            thermalRenderer = GetComponent<ThermalOverlayRenderer>();
        if (snapshotManager == null)
            snapshotManager = GetComponent<SnapshotManager>();

        fluidSolver?.Initialize(config);
        efieldSolver?.Initialize(config);
        particleModule?.Initialize(config);
        targetSurface?.Initialize(config);
        glowRenderer?.Initialize(config);
        thermalRenderer?.Initialize(config);
        snapshotManager?.Initialize(config);

        CreateDisplayQuad();

        if (laserController != null)
        {
            laserController.config = config;
            laserController.fluidSolver = fluidSolver;
            laserController.targetSurface = targetSurface;
        }

        if (electrodeController != null)
        {
            electrodeController.config = config;
            electrodeController.efieldSolver = efieldSolver;
        }
    }

    void CreateDisplayQuad()
    {
        GameObject quadObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadObj.transform.SetParent(transform);
        quadObj.transform.localPosition = Vector3.zero;
        quadObj.transform.localRotation = Quaternion.identity;
        quadObj.transform.localScale = new Vector3(config.domainSize * 10f, config.domainSize * 10f, 1f);
        quadObj.name = "PlasmaDisplayQuad";

        MeshCollider mc = quadObj.GetComponent<MeshCollider>();
        if (mc != null) Destroy(mc);

        displayQuad = quadObj.GetComponent<MeshFilter>().mesh;
    }

    void Update()
    {
        if (pauseSimulation || config == null) return;

        float dt = config.dt * simulationTimeScale;

        if (enableElectricField && efieldSolver != null && efieldSolver.IsInitialized)
        {
            efieldSolver.Step(fluidSolver.densityRT, fluidSolver.temperatureRT);
            fluidSolver.Step(efieldSolver.ChargeDensityTexture, efieldSolver.ElectricFieldTexture);
        }
        else
        {
            fluidSolver.Step(null, null);
        }

        simulationTime += dt;

        UpdateRendering();
    }

    void UpdateRendering()
    {
        if (glowRenderer != null && fluidSolver.IsInitialized)
        {
            glowRenderer.RenderToQuad(fluidSolver.densityRT, fluidSolver.temperatureRT, displayQuad);
        }

        if (thermalRenderer != null && config.showThermalOverlay && fluidSolver.IsInitialized)
        {
            thermalRenderer.RenderToQuad(fluidSolver.temperatureRT, fluidSolver.densityRT, displayQuad);
        }
    }

    public void ResetSimulation()
    {
        simulationTime = 0f;

        if (fluidSolver != null && fluidSolver.IsInitialized)
        {
            fluidSolver.ClearAllFields();
        }

        if (efieldSolver != null && efieldSolver.IsInitialized)
        {
            efieldSolver.ClearElectrodes();
        }

        if (particleModule != null)
        {
            particleModule.Reset();
        }

        if (targetSurface != null && targetSurface.IsInitialized)
        {
            targetSurface.ResetDamage();
        }
    }

    public void SetThermalOverlayVisible(bool visible)
    {
        if (thermalRenderer != null)
        {
            thermalRenderer.SetVisible(visible);
        }
    }

    public void ToggleElectricField(bool enabled)
    {
        enableElectricField = enabled;
    }

    public void TogglePause()
    {
        pauseSimulation = !pauseSimulation;
    }

    public void SetTimeScale(float scale)
    {
        simulationTimeScale = Mathf.Clamp(scale, 0.1f, 5f);
    }

    public void SaveSnapshot(string name = "")
    {
        if (snapshotManager != null)
        {
            if (string.IsNullOrEmpty(name))
                snapshotManager.QuickSave();
            else
                snapshotManager.SaveSnapshot(name);
        }
    }

    public void LoadSnapshot(string name = "")
    {
        if (snapshotManager != null)
        {
            if (string.IsNullOrEmpty(name))
                snapshotManager.QuickLoad();
            else
                snapshotManager.LoadSnapshot(name);
        }
    }
}
