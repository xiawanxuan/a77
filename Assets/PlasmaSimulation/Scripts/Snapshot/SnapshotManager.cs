using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class SnapshotManager : MonoBehaviour
{
    public PlasmaSimConfig config;
    public PlasmaFluidSolver fluidSolver;
    public ElectricFieldSolver efieldSolver;
    public ParticleCollisionModule particleModule;
    public TargetSurface targetSurface;

    private string snapshotDirectory;

    public void Initialize(PlasmaSimConfig cfg)
    {
        config = cfg;
        snapshotDirectory = Path.Combine(Application.persistentDataPath, config.snapshotFolder);
        if (!Directory.Exists(snapshotDirectory))
        {
            Directory.CreateDirectory(snapshotDirectory);
        }
    }

    public void SaveSnapshot(string filename)
    {
        if (fluidSolver == null || !fluidSolver.IsInitialized) return;

        PlasmaSnapshot snapshot = new PlasmaSnapshot
        {
            gridResolution = config.gridResolution,
            domainSize = config.domainSize,
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff"),
            densityData = ReadRT(fluidSolver.densityRT),
            velocityData = ReadRT2(fluidSolver.velocityRT),
            pressureData = ReadRT(fluidSolver.pressureRT),
            temperatureData = ReadRT(fluidSolver.temperatureRT),
            simTime = Time.time
        };

        if (efieldSolver != null && efieldSolver.IsInitialized)
        {
            snapshot.potentialData = ReadRT(efieldSolver.potentialRT);
            snapshot.eFieldData = ReadRT2(efieldSolver.eFieldRT);
            snapshot.chargeDensityData = ReadRT(efieldSolver.chargeDensityRT);
        }

        if (particleModule != null)
        {
            particleModule.GetSnapshotData(out snapshot.electronDensity, out snapshot.ionDensity, out snapshot.neutralDensity);
        }

        string path = Path.Combine(snapshotDirectory, filename + ".psnap");
        using (FileStream fs = new FileStream(path, FileMode.Create))
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(fs, snapshot);
        }

        Debug.Log($"Snapshot saved: {path}");
    }

    public void LoadSnapshot(string filename)
    {
        string path = Path.Combine(snapshotDirectory, filename + ".psnap");
        if (!File.Exists(path))
        {
            Debug.LogError($"Snapshot not found: {path}");
            return;
        }

        PlasmaSnapshot snapshot;
        using (FileStream fs = new FileStream(path, FileMode.Open))
        {
            BinaryFormatter bf = new BinaryFormatter();
            snapshot = (PlasmaSnapshot)bf.Deserialize(fs);
        }

        if (snapshot.gridResolution != config.gridResolution)
        {
            Debug.LogError("Snapshot grid resolution mismatch!");
            return;
        }

        if (fluidSolver != null && fluidSolver.IsInitialized)
        {
            WriteRT(fluidSolver.densityRT, snapshot.densityData);
            WriteRT2(fluidSolver.velocityRT, snapshot.velocityData);
            WriteRT(fluidSolver.pressureRT, snapshot.pressureData);
            WriteRT(fluidSolver.temperatureRT, snapshot.temperatureData);
        }

        if (efieldSolver != null && efieldSolver.IsInitialized)
        {
            if (snapshot.potentialData != null) WriteRT(efieldSolver.potentialRT, snapshot.potentialData);
            if (snapshot.eFieldData != null) WriteRT2(efieldSolver.eFieldRT, snapshot.eFieldData);
            if (snapshot.chargeDensityData != null) WriteRT(efieldSolver.chargeDensityRT, snapshot.chargeDensityData);
        }

        if (particleModule != null && snapshot.electronDensity != null)
        {
            particleModule.LoadSnapshotData(snapshot.electronDensity, snapshot.ionDensity, snapshot.neutralDensity);
        }

        Debug.Log($"Snapshot loaded: {path}");
    }

    public string[] ListSnapshots()
    {
        if (!Directory.Exists(snapshotDirectory)) return new string[0];
        string[] files = Directory.GetFiles(snapshotDirectory, "*.psnap");
        for (int i = 0; i < files.Length; i++)
        {
            files[i] = Path.GetFileNameWithoutExtension(files[i]);
        }
        return files;
    }

    public void DeleteSnapshot(string filename)
    {
        string path = Path.Combine(snapshotDirectory, filename + ".psnap");
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    float[] ReadRT(RenderTexture rt)
    {
        if (rt == null) return null;
        int res = rt.width;
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(res, res, TextureFormat.RFloat, false);
        tex.ReadPixels(new Rect(0, 0, res, res), 0, 0);
        tex.Apply();

        RenderTexture.active = prev;

        Color[] pixels = tex.GetPixels();
        float[] data = new float[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
            data[i] = pixels[i].r;

        Destroy(tex);
        return data;
    }

    float[] ReadRT2(RenderTexture rt)
    {
        if (rt == null) return null;
        int res = rt.width;
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(res, res, TextureFormat.RGFloat, false);
        tex.ReadPixels(new Rect(0, 0, res, res), 0, 0);
        tex.Apply();

        RenderTexture.active = prev;

        Color[] pixels = tex.GetPixels();
        float[] data = new float[pixels.Length * 2];
        for (int i = 0; i < pixels.Length; i++)
        {
            data[i * 2] = pixels[i].r;
            data[i * 2 + 1] = pixels[i].g;
        }

        Destroy(tex);
        return data;
    }

    void WriteRT(RenderTexture rt, float[] data)
    {
        if (rt == null || data == null) return;
        int res = rt.width;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RFloat, false);
        Color[] pixels = new Color[data.Length];
        for (int i = 0; i < data.Length; i++)
            pixels[i] = new Color(data[i], 0, 0, 1);
        tex.SetPixels(pixels);
        tex.Apply();

        Graphics.Blit(tex, rt);
        Destroy(tex);
    }

    void WriteRT2(RenderTexture rt, float[] data)
    {
        if (rt == null || data == null) return;
        int res = rt.width;
        int pixelCount = res * res;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGFloat, false);
        Color[] pixels = new Color[pixelCount];
        for (int i = 0; i < pixelCount; i++)
        {
            pixels[i] = new Color(data[i * 2], data[i * 2 + 1], 0, 1);
        }
        tex.SetPixels(pixels);
        tex.Apply();

        Graphics.Blit(tex, rt);
        Destroy(tex);
    }

    public void QuickSave()
    {
        string filename = "quicksave_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        SaveSnapshot(filename);
    }

    public void QuickLoad()
    {
        string[] snapshots = ListSnapshots();
        if (snapshots.Length > 0)
        {
            System.Array.Sort(snapshots);
            LoadSnapshot(snapshots[snapshots.Length - 1]);
        }
    }
}

[System.Serializable]
public class PlasmaSnapshot
{
    public int gridResolution;
    public float domainSize;
    public string timestamp;
    public float simTime;

    public float[] densityData;
    public float[] velocityData;
    public float[] pressureData;
    public float[] temperatureData;

    public float[] potentialData;
    public float[] eFieldData;
    public float[] chargeDensityData;

    public float[] electronDensity;
    public float[] ionDensity;
    public float[] neutralDensity;
}
