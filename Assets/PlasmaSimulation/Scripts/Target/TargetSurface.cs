using UnityEngine;

public class TargetSurface : MonoBehaviour
{
    public PlasmaSimConfig config;
    public MeshRenderer targetRenderer;
    public Material targetMaterial;

    private Texture2D damageMap;
    private Color[] damagePixels;
    private int resolution = 512;

    public bool IsInitialized { get; private set; }

    public void Initialize(PlasmaSimConfig cfg)
    {
        config = cfg;
        resolution = cfg.gridResolution;
        damageMap = new Texture2D(resolution, resolution, TextureFormat.RFloat, false);
        damagePixels = new Color[resolution * resolution];
        for (int i = 0; i < damagePixels.Length; i++)
            damagePixels[i] = new Color(0, 0, 0, 1);

        damageMap.SetPixels(damagePixels);
        damageMap.Apply();

        if (targetMaterial != null)
        {
            targetMaterial.SetTexture("_DamageMap", damageMap);
        }

        IsInitialized = true;
    }

    public Vector2 WorldToTargetUV(Vector3 worldPos)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        float u = (localPos.x / transform.lossyScale.x) + 0.5f;
        float v = (localPos.y / transform.lossyScale.y) + 0.5f;
        return new Vector2(u, v);
    }

    public Vector2 WorldToDomainPos(Vector3 worldPos)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        float x = localPos.x / transform.lossyScale.x + 0.5f;
        float y = localPos.y / transform.lossyScale.y + 0.5f;
        return new Vector2(x * config.domainSize, y * config.domainSize);
    }

    public void ApplyDamage(Vector2 domainPos, float energy, float radius)
    {
        float dx = config.Dx;
        int cx = (int)(domainPos.x / dx);
        int cy = (int)(domainPos.y / dx);
        int r = Mathf.CeilToInt(radius / dx);

        for (int dy = -r; dy <= r; dy++)
        {
            for (int ddx = -r; ddx <= r; ddx++)
            {
                int px = cx + ddx;
                int py = cy + dy;
                if (px < 0 || px >= resolution || py < 0 || py >= resolution) continue;

                float dist = Mathf.Sqrt(ddx * ddx + dy * dy) * dx;
                float falloff = Mathf.Exp(-dist * dist / (2f * radius * radius));
                float damage = falloff * energy * 0.01f;

                int idx = py * resolution + px;
                float prevDamage = damagePixels[idx].r;
                damagePixels[idx] = new Color(Mathf.Min01(prevDamage + damage), 0, 0, 1);
            }
        }

        damageMap.SetPixels(damagePixels);
        damageMap.Apply();
    }

    public float GetDamageAt(Vector2 domainPos)
    {
        int px = (int)(domainPos.x / config.Dx);
        int py = (int)(domainPos.y / config.Dx);
        if (px < 0 || px >= resolution || py < 0 || py >= resolution) return 0;
        return damagePixels[py * resolution + px].r;
    }

    public void ResetDamage()
    {
        for (int i = 0; i < damagePixels.Length; i++)
            damagePixels[i] = new Color(0, 0, 0, 1);
        damageMap.SetPixels(damagePixels);
        damageMap.Apply();
    }

    public Texture2D GetDamageMap() => damageMap;
}
