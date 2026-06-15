using UnityEngine;

public class ElectrodeController : MonoBehaviour
{
    public ElectricFieldSolver efieldSolver;
    public PlasmaSimConfig config;
    public float electrodeRadius = 0.03f;

    public void PlaceElectrode(Vector2 worldPos, float voltage, int sign)
    {
        if (efieldSolver != null && efieldSolver.IsInitialized)
        {
            efieldSolver.AddElectrode(worldPos, electrodeRadius, voltage, sign);
        }
    }

    public void RemoveLastElectrode()
    {
        if (efieldSolver != null)
        {
            var electrodes = efieldSolver.GetElectrodes();
            if (electrodes.Count > 0)
            {
                efieldSolver.RemoveElectrode(electrodes.Count - 1);
            }
        }
    }

    public void ClearAllElectrodes()
    {
        if (efieldSolver != null)
        {
            efieldSolver.ClearElectrodes();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (efieldSolver == null) return;
        var electrodes = efieldSolver.GetElectrodes();
        foreach (var elec in electrodes)
        {
            Gizmos.color = elec.sign > 0 ? Color.red : Color.blue;
            Gizmos.DrawWireSphere(new Vector3(elec.position.x, elec.position.y, 0), elec.radius);
        }
    }
}
