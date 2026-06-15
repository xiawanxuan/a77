using UnityEngine;

[CreateAssetMenu(fileName = "PlasmaSimConfig", menuName = "PlasmaSim/Configuration")]
public class PlasmaSimConfig : ScriptableObject
{
    [Header("Grid")]
    public int gridResolution = 512;
    public float domainSize = 1.0f;

    [Header("Fluid Solver")]
    public float dt = 0.016f;
    public int pressureIterations = 20;
    public float viscosity = 0.0001f;
    public float thermalDiffusivity = 0.0002f;
    public float densityDiffusion = 0.00005f;
    public float ambientDensity = 0.001f;
    public float ambientTemperature = 300f;
    public float pressureScale = 1.0f;

    [Header("Laser")]
    public float laserPulseEnergy = 100f;
    public float laserSpotRadius = 0.02f;
    public float laserPulseDuration = 0.1f;
    [Range(0.1f, 100f)] public float laserEnergyMultiplier = 1f;
    public AnimationCurve laserTemporalProfile = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.1f, 1f), new Keyframe(0.5f, 0.8f), new Keyframe(1f, 0f));

    [Header("Plasma Generation")]
    public float ionizationThreshold = 5000f;
    public float ablationEfficiency = 0.3f;
    public float plasmaExpansionSpeed = 2.0f;
    public float initialPlasmaTemperature = 15000f;
    public float initialPlasmaDensity = 1.0f;

    [Header("Particle Physics")]
    public float recombinationRate = 0.01f;
    public float threeBodyRecombCoeff = 1e-27f;
    public float radiativeRecombCoeff = 1e-13f;
    public float ionizationEnergy = 13.6f;
    public float electronMass = 9.109e-31f;
    public float ionMass = 1.673e-27f;
    public float boltzmannConstant = 1.381e-23f;

    [Header("Electric Field")]
    public float electrodeVoltage = 1000f;
    public float permittivity = 8.854e-12f;
    public float chargeCouplingStrength = 0.5f;
    public int electricFieldIterations = 30;

    [Header("Magnetic Field")]
    public float vacuumPermeability = 1.25663706212e-6f;
    public float coilCurrent = 100f;
    public float coilRadius = 0.05f;
    public float lorentzForceStrength = 1.0f;
    public float magneticViscosity = 0.00001f;
    public int magneticFieldIterations = 20;
    public float maxMagneticField = 10f;
    public bool enableMagneticField = false;
    [Range(0f, 1f)] public float magneticFieldPulse = 1.0f;

    [Header("Radiation Cooling")]
    public float radiationCoolingCoeff = 1e-4f;
    public float bremsstrahlungCoeff = 1e-6f;

    [Header("Rendering")]
    public Gradient plasmaColorGradient;
    public float glowIntensity = 2.0f;
    public float glowRadius = 1.5f;
    public float thermalAlpha = 0.6f;
    public bool showThermalOverlay = true;
    public bool showElectricField = false;

    [Header("Snapshot")]
    public string snapshotFolder = "PlasmaSnapshots";

    void OnEnable()
    {
        if (plasmaColorGradient == null || plasmaColorGradient.colorKeys.Length == 0)
        {
            plasmaColorGradient = new Gradient();
            plasmaColorGradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.clear, 0f),
                    new GradientColorKey(new Color(0.2f, 0f, 0.4f), 0.15f),
                    new GradientColorKey(new Color(0.6f, 0f, 0.2f), 0.3f),
                    new GradientColorKey(new Color(1f, 0.3f, 0f), 0.5f),
                    new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0.7f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.5f, 0.15f),
                    new GradientAlphaKey(0.8f, 0.3f),
                    new GradientAlphaKey(0.9f, 0.5f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
        }
    }

    public float Dx => domainSize / gridResolution;
}
