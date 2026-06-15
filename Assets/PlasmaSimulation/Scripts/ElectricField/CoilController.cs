using UnityEngine;

public class CoilController : MonoBehaviour
{
    public MagneticFieldSolver magneticSolver;
    public PlasmaSimConfig config;

    public float coilRadius = 0.05f;
    public float coilCurrent = 100f;

    public void PlaceCoil(Vector2 worldPos, int sign)
    {
        if (magneticSolver != null && magneticSolver.IsInitialized)
        {
            magneticSolver.AddCoil(worldPos, coilRadius, coilCurrent, sign);
        }
    }

    public void PlaceCoil(Vector2 worldPos, float radius, float current, int sign)
    {
        if (magneticSolver != null && magneticSolver.IsInitialized)
        {
            magneticSolver.AddCoil(worldPos, radius, current, sign);
        }
    }

    public void RemoveLastCoil()
    {
        if (magneticSolver != null)
        {
            var coils = magneticSolver.GetCoils();
            if (coils.Count > 0)
            {
                magneticSolver.RemoveCoil(coils.Count - 1);
            }
        }
    }

    public void ClearAllCoils()
    {
        if (magneticSolver != null)
        {
            magneticSolver.ClearCoils();
            magneticSolver.ClearMagneticField();
        }
    }

    public void SetPulseFactor(float factor)
    {
        if (config != null)
        {
            config.magneticFieldPulse = Mathf.Clamp01(factor);
        }
    }

    public void ToggleMagneticField(bool enabled)
    {
        if (config != null)
        {
            config.enableMagneticField = enabled;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (magneticSolver == null) return;

        var coils = magneticSolver.GetCoils();
        for (int i = 0; i < coils.Count; i++)
        {
            var coil = coils[i];
            Gizmos.color = coil.sign > 0 ? Color.magenta : Color.cyan;
            Gizmos.DrawWireSphere(new Vector3(coil.position.x, coil.position.y, 0), coil.radius);

            int segments = 32;
            for (int s = 0; s < segments; s++)
            {
                float angle0 = (float)s / segments * Mathf.PI * 2f;
                float angle1 = (float)(s + 1) / segments * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(
                    coil.position.x + Mathf.Cos(angle0) * coil.radius,
                    coil.position.y + Mathf.Sin(angle0) * coil.radius,
                    0);
                Vector3 p1 = new Vector3(
                    coil.position.x + Mathf.Cos(angle1) * coil.radius,
                    coil.position.y + Mathf.Sin(angle1) * coil.radius,
                    0);
                Gizmos.DrawLine(p0, p1);
            }
        }
    }
}
