using UnityEngine;

public class LaserPulseController : MonoBehaviour
{
    public PlasmaSimConfig config;
    public PlasmaFluidSolver fluidSolver;
    public TargetSurface targetSurface;
    public Camera mainCamera;

    public bool isActive = true;
    public float currentEnergy = 100f;

    private float pulseTimer;
    private bool isFiring;
    private Vector2 currentFirePos;

    public System.Action<Vector2, float> OnLaserFire;

    void Update()
    {
        if (!isActive || fluidSolver == null || !fluidSolver.IsInitialized) return;

        if (Input.GetMouseButtonDown(0))
        {
            StartPulse();
        }

        if (Input.GetMouseButton(0))
        {
            ContinuePulse();
        }

        if (Input.GetMouseButtonUp(0))
        {
            EndPulse();
        }

        float energyDelta = Input.mouseScrollDelta.y * 10f;
        currentEnergy = Mathf.Clamp(currentEnergy + energyDelta, 10f, 1000f);

        if (isFiring)
        {
            pulseTimer += Time.deltaTime;
            if (pulseTimer > config.laserPulseDuration)
            {
                EndPulse();
            }
        }
    }

    void StartPulse()
    {
        isFiring = true;
        pulseTimer = 0f;
        currentFirePos = GetMouseDomainPosition();
        FireLaser(currentFirePos);
    }

    void ContinuePulse()
    {
        if (!isFiring) return;
        currentFirePos = GetMouseDomainPosition();
        FireLaser(currentFirePos);
    }

    void EndPulse()
    {
        isFiring = false;
        pulseTimer = 0f;
    }

    void FireLaser(Vector2 domainPos)
    {
        float energy = currentEnergy * config.laserEnergyMultiplier;
        float temporalFactor = config.laserTemporalProfile.Evaluate(pulseTimer / config.laserPulseDuration);
        float effectiveEnergy = energy * temporalFactor;

        float sourceDensity = config.initialPlasmaDensity * effectiveEnergy * 0.01f;
        float sourceTemp = config.initialPlasmaTemperature * effectiveEnergy * 0.005f;
        float sourceStrength = effectiveEnergy * 0.01f;

        fluidSolver.InjectSource(domainPos, sourceStrength, sourceTemp, sourceDensity);

        if (targetSurface != null && targetSurface.IsInitialized)
        {
            targetSurface.ApplyDamage(domainPos, effectiveEnergy, config.laserSpotRadius);
        }

        OnLaserFire?.Invoke(domainPos, effectiveEnergy);
    }

    Vector2 GetMouseDomainPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        float t = -ray.origin.z / ray.direction.z;
        Vector3 hitPoint = ray.GetPoint(t);

        if (targetSurface != null)
        {
            return targetSurface.WorldToDomainPos(hitPoint);
        }

        return new Vector2(
            (hitPoint.x / 10f + 0.5f) * config.domainSize,
            (hitPoint.y / 10f + 0.5f) * config.domainSize
        );
    }

    public void SetEnergy(float energy)
    {
        currentEnergy = Mathf.Clamp(energy, 10f, 1000f);
    }

    public float GetEnergy() => currentEnergy;
}
