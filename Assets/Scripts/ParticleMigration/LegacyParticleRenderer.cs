using UnityEngine;
using UnityEngine.Rendering;

public sealed class LegacyParticleRenderer : MonoBehaviour
{
    public Material[] materials;
    public int castShadows, receiveShadows, stretchParticles;
    public float cameraVelocityScale, lengthScale = 2f, velocityScale, maxParticleSize = 1f;
    public int tilesX = 1, tilesY = 1;
    public float cycles = 1f;
    private ParticleSystemRenderer particleRenderer;

    public Material material
    {
        get { EnsureRenderer(); return particleRenderer.material; }
        set { EnsureRenderer(); particleRenderer.material = value; }
    }

    private void EnsureRenderer()
    {
        if (particleRenderer == null) Apply(GetComponent<LegacyParticleEmitter>().RenderingSystem);
    }

    public void Apply(ParticleSystem system)
    {
        particleRenderer = system.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterials = materials;
        particleRenderer.enabled = enabled;
        particleRenderer.shadowCastingMode = castShadows == 0 ? ShadowCastingMode.Off : ShadowCastingMode.On;
        particleRenderer.receiveShadows = receiveShadows != 0;
        switch (stretchParticles)
        {
            case 1: particleRenderer.renderMode = ParticleSystemRenderMode.Stretch; break;
            case 3: particleRenderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard; break;
            case 4: particleRenderer.renderMode = ParticleSystemRenderMode.VerticalBillboard; break;
            default: particleRenderer.renderMode = ParticleSystemRenderMode.Billboard; break;
        }
        particleRenderer.sortMode = stretchParticles == 2 ? ParticleSystemSortMode.Distance : ParticleSystemSortMode.None;
        particleRenderer.cameraVelocityScale = cameraVelocityScale;
        particleRenderer.lengthScale = lengthScale;
        particleRenderer.velocityScale = velocityScale;
        particleRenderer.maxParticleSize = maxParticleSize;
        var sheet = system.textureSheetAnimation;
        sheet.enabled = tilesX > 1 || tilesY > 1;
        sheet.numTilesX = Mathf.Max(1, tilesX);
        sheet.numTilesY = Mathf.Max(1, tilesY);
        sheet.cycleCount = Mathf.Max(1, Mathf.CeilToInt(cycles));
        sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, cycles / sheet.cycleCount));
    }

    private void OnEnable() { if (particleRenderer != null) particleRenderer.enabled = true; }
    private void OnDisable() { if (particleRenderer != null) particleRenderer.enabled = false; }
}
