using UnityEngine;

public class ParticleCollisionModule : MonoBehaviour
{
    public PlasmaSimConfig config;

    private int gridRes;
    private float[] electronDensity;
    private float[] ionDensity;
    private float[] neutralDensity;

    public float[] ElectronDensity => electronDensity;
    public float[] IonDensity => ionDensity;
    public float[] NeutralDensity => neutralDensity;

    public void Initialize(PlasmaSimConfig cfg)
    {
        config = cfg;
        gridRes = cfg.gridResolution;
        int totalCells = gridRes * gridRes;
        electronDensity = new float[totalCells];
        ionDensity = new float[totalCells];
        neutralDensity = new float[totalCells];
    }

    public void Step(float dt, float[] fluidDensity, float[] fluidTemperature)
    {
        if (electronDensity == null) return;

        float kT_eV = 0;
        float ionizationRate = 0;
        float threeBodyRate = 0;
        float radiativeRate = 0;
        float ne_new = 0;
        float ni_new = 0;
        float nn_new = 0;

        for (int i = 0; i < fluidDensity.Length; i++)
        {
            float rho = fluidDensity[i];
            float T = fluidTemperature[i];

            if (rho < config.ambientDensity * 1.5f)
            {
                electronDensity[i] *= 0.99f;
                ionDensity[i] *= 0.99f;
                neutralDensity[i] = rho;
                continue;
            }

            float normalizedDensity = rho / config.initialPlasmaDensity;
            float ne = electronDensity[i];
            float ni = ionDensity[i];
            float nn = neutralDensity[i];

            float totalN = ne + ni + nn;
            if (totalN < 1e-10f) totalN = 1e-10f;

            kT_eV = k_B_eV(T);

            if (T > config.ionizationThreshold && ne < normalizedDensity)
            {
                float sahaFactor = SahaIonization(T, ne, normalizedDensity);
                ionizationRate = sahaFactor * nn * dt;
                ionizationRate = Mathf.Min(ionizationRate, nn * 0.5f);
            }
            else
            {
                ionizationRate = 0;
            }

            threeBodyRate = config.threeBodyRecombCoeff * ne * ne * ni * dt;
            radiativeRate = config.radiativeRecombCoeff * ne * ni * dt;

            float totalRecomb = threeBodyRate + radiativeRate;
            totalRecomb = Mathf.Min(totalRecomb, ne * 0.5f);

            ne_new = ne + ionizationRate - totalRecomb;
            ni_new = ni + ionizationRate - totalRecomb;
            nn_new = nn - ionizationRate + totalRecomb;

            ne_new = Mathf.Max(ne_new, 0);
            ni_new = Mathf.Max(ni_new, 0);
            nn_new = Mathf.Max(nn_new, 0);

            electronDensity[i] = ne_new;
            ionDensity[i] = ni_new;
            neutralDensity[i] = nn_new;
        }
    }

    float SahaIonization(float T, float ne, float totalDensity)
    {
        if (T < 1000f) return 0f;

        float kT = config.boltzmannConstant * T;
        float lambda_dB = Mathf.Sqrt(config.boltzmannConstant * config.ionMass * T) / (2f * config.ionizationEnergy * 1.6e-19f);
        float sahaConstant = 2f / (lambda_dB * lambda_dB * lambda_dB) *
            Mathf.Exp(-config.ionizationEnergy * 1.6e-19f / kT);

        float neFactor = ne > 1e-10f ? ne : 1e-10f;
        float ratio = sahaConstant / (2f * neFactor);

        return Mathf.Min(ratio, 1e6f);
    }

    float k_B_eV(float T)
    {
        return config.boltzmannConstant * T / 1.6e-19f;
    }

    public float GetNetChargeDensity(int index)
    {
        if (index < 0 || index >= electronDensity.Length) return 0;
        return (ionDensity[index] - electronDensity[index]) * 1.6e-19f;
    }

    public float GetIonizationFraction(int index)
    {
        if (index < 0 || index >= electronDensity.Length) return 0;
        float total = electronDensity[index] + ionDensity[index] + neutralDensity[index];
        if (total < 1e-20f) return 0;
        return electronDensity[index] / total;
    }

    public void Reset()
    {
        if (electronDensity != null)
        {
            System.Array.Clear(electronDensity, 0, electronDensity.Length);
            System.Array.Clear(ionDensity, 0, ionDensity.Length);
            System.Array.Clear(neutralDensity, 0, neutralDensity.Length);
        }
    }

    public void GetSnapshotData(out float[] ne, out float[] ni, out float[] nn)
    {
        ne = (float[])electronDensity.Clone();
        ni = (float[])ionDensity.Clone();
        nn = (float[])neutralDensity.Clone();
    }

    public void LoadSnapshotData(float[] ne, float[] ni, float[] nn)
    {
        System.Array.Copy(ne, electronDensity, ne.Length);
        System.Array.Copy(ni, ionDensity, ni.Length);
        System.Array.Copy(nn, neutralDensity, nn.Length);
    }
}
