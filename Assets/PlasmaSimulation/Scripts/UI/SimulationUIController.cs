using UnityEngine;
using UnityEngine.UI;

public class SimulationUIController : MonoBehaviour
{
    public PlasmaSimConfig config;
    public SimulationManager simManager;
    public LaserPulseController laserController;
    public ElectrodeController electrodeController;
    public SnapshotManager snapshotManager;

    [Header("UI References")]
    public Slider energySlider;
    public Text energyValueText;
    public Toggle thermalToggle;
    public Toggle efieldToggle;
    public Button saveSnapshotBtn;
    public Button loadSnapshotBtn;
    public Button resetBtn;
    public Button addPositiveElectrodeBtn;
    public Button addNegativeElectrodeBtn;
    public Button clearElectrodesBtn;
    public Text fpsText;
    public Text simTimeText;
    public InputField snapshotNameInput;

    private float fpsUpdateTimer;
    private int frameCount;
    private float currentFps;

    void Start()
    {
        SetupUI();
    }

    void SetupUI()
    {
        if (energySlider != null)
        {
            energySlider.minValue = 10f;
            energySlider.maxValue = 1000f;
            energySlider.value = laserController != null ? laserController.GetEnergy() : 100f;
            energySlider.onValueChanged.AddListener(OnEnergyChanged);
        }

        if (thermalToggle != null)
        {
            thermalToggle.isOn = config.showThermalOverlay;
            thermalToggle.onValueChanged.AddListener(OnThermalToggle);
        }

        if (efieldToggle != null)
        {
            efieldToggle.isOn = config.showElectricField;
            efieldToggle.onValueChanged.AddListener(OnEFieldToggle);
        }

        if (saveSnapshotBtn != null)
            saveSnapshotBtn.onClick.AddListener(OnSaveSnapshot);

        if (loadSnapshotBtn != null)
            loadSnapshotBtn.onClick.AddListener(OnLoadSnapshot);

        if (resetBtn != null)
            resetBtn.onClick.AddListener(OnReset);

        if (addPositiveElectrodeBtn != null)
            addPositiveElectrodeBtn.onClick.AddListener(() => OnAddElectrode(1));

        if (addNegativeElectrodeBtn != null)
            addNegativeElectrodeBtn.onClick.AddListener(() => OnAddElectrode(-1));

        if (clearElectrodesBtn != null)
            clearElectrodesBtn.onClick.AddListener(OnClearElectrodes);
    }

    void Update()
    {
        frameCount++;
        fpsUpdateTimer += Time.deltaTime;
        if (fpsUpdateTimer >= 0.5f)
        {
            currentFps = frameCount / fpsUpdateTimer;
            frameCount = 0;
            fpsUpdateTimer = 0f;
        }

        if (fpsText != null)
            fpsText.text = $"FPS: {currentFps:F0}";

        if (simTimeText != null && simManager != null)
            simTimeText.text = $"Time: {simManager.SimulationTime:F2}s";
    }

    void OnEnergyChanged(float value)
    {
        if (laserController != null)
            laserController.SetEnergy(value);
        if (energyValueText != null)
            energyValueText.text = $"{value:F0} J";
    }

    void OnThermalToggle(bool on)
    {
        config.showThermalOverlay = on;
        if (simManager != null)
            simManager.SetThermalOverlayVisible(on);
    }

    void OnEFieldToggle(bool on)
    {
        config.showElectricField = on;
    }

    void OnSaveSnapshot()
    {
        if (snapshotManager == null) return;
        string name = (snapshotNameInput != null && !string.IsNullOrEmpty(snapshotNameInput.text))
            ? snapshotNameInput.text
            : "snapshot_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        snapshotManager.SaveSnapshot(name);
    }

    void OnLoadSnapshot()
    {
        if (snapshotManager == null) return;
        string name = (snapshotNameInput != null && !string.IsNullOrEmpty(snapshotNameInput.text))
            ? snapshotNameInput.text
            : null;
        if (name != null)
        {
            snapshotManager.LoadSnapshot(name);
        }
        else
        {
            snapshotManager.QuickLoad();
        }
    }

    void OnReset()
    {
        if (simManager != null)
            simManager.ResetSimulation();
    }

    void OnAddElectrode(int sign)
    {
        if (electrodeController == null) return;
        float voltage = config.electrodeVoltage * sign;
        Vector2 pos = new Vector2(
            sign > 0 ? config.domainSize * 0.25f : config.domainSize * 0.75f,
            config.domainSize * 0.5f
        );
        electrodeController.PlaceElectrode(pos, voltage, sign);
    }

    void OnClearElectrodes()
    {
        if (electrodeController != null)
            electrodeController.ClearAllElectrodes();
    }
}
