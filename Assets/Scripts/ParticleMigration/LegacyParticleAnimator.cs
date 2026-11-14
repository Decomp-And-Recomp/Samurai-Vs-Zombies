using UnityEngine;

public sealed class LegacyParticleAnimator : MonoBehaviour
{
    public bool doesAnimateColor = true;
    public Color[] colorAnimation = { Color.white, Color.white, Color.white, Color.white, Color.clear };
    public Vector3 worldRotationAxis, localRotationAxis;
    public float sizeGrow;
    public Vector3 rndForce, force;
    public float damping = 1f;
    public bool stopSimulation, autodestruct;

    public void Animate(ref ParticleSystem.Particle p, float seconds, bool worldSpace, Transform emitter)
    {
        Vector3 acceleration = force + LegacyParticleEmitter.RandomVector(rndForce);
        if (!worldSpace) acceleration = emitter.InverseTransformDirection(acceleration);
        p.velocity = (p.velocity + acceleration * seconds) * Mathf.Pow(Mathf.Max(0f, damping), seconds);
        Vector3 rotation = worldRotationAxis + emitter.TransformDirection(localRotationAxis);
        if (!worldSpace) rotation = emitter.InverseTransformDirection(rotation);
        Vector3 center = worldSpace ? emitter.position : Vector3.zero;
        p.position = center + Quaternion.Euler(rotation * seconds) * (p.position - center);
        p.startSize = Mathf.Max(0f, p.startSize + sizeGrow * seconds);
        if (doesAnimateColor && colorAnimation != null && colorAnimation.Length > 0)
        {
            float age = p.startLifetime > 0f ? 1f - p.remainingLifetime / p.startLifetime : 1f;
            float sample = Mathf.Clamp01(age) * (colorAnimation.Length - 1);
            int index = Mathf.Min(Mathf.FloorToInt(sample), colorAnimation.Length - 1);
            p.startColor = Color.Lerp(colorAnimation[index], colorAnimation[Mathf.Min(index + 1, colorAnimation.Length - 1)], sample - index);
        }
    }
}
