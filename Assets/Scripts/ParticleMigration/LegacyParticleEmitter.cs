using System.Collections.Generic;
using UnityEngine;

// Retains the serialized legacy effect settings while ParticleSystem supplies rendering.
// Simulation stays here because the old per-particle scripts edit velocity, color and size.
[DefaultExecutionOrder(-1000)]
public sealed class LegacyParticleEmitter : MonoBehaviour
{
    public bool emitEnabled = true;
    public float minSize = 1, maxSize = 1;
    public float minEnergy = 1, maxEnergy = 1;
    public float minEmission = 1, maxEmission = 1;
    public Vector3 worldVelocity, localVelocity, rndVelocity, tangentVelocity;
    public float emitterVelocityScale, angularVelocity, rndAngularVelocity;
    public bool rndRotation, worldSpace, oneShot;
    public Vector3 ellipsoid = Vector3.one;
    public float minEmitterRange;

    private ParticleSystem system;
    private LegacyParticleAnimator animatorSettings;
    private readonly List<ParticleSystem.Particle> live = new List<ParticleSystem.Particle>();
    private ParticleSystem.Particle[] renderBuffer = new ParticleSystem.Particle[32];
    private Vector3 previousPosition;
    private float emissionRemainder;
    private bool hasEmitted, wasEmitting;
    private uint nextSeed = 1;

    public bool emit { get { return emitEnabled; } set { emitEnabled = value; } }
    public int particleCount { get { return live.Count; } }
    public ParticleSystem.Particle[] particles
    {
        get { return live.ToArray(); }
        set { live.Clear(); live.AddRange(value); Publish(); }
    }

    public ParticleSystem RenderingSystem
    {
        get { EnsureSystem(); return system; }
    }

    private void Awake() { EnsureSystem(); }

    private void EnsureSystem()
    {
        if (system != null) return;
        system = GetComponent<ParticleSystem>();
        if (system == null) system = gameObject.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = system.main;
        main.playOnAwake = false;
        main.simulationSpace = worldSpace ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = 32;
        var emission = system.emission;
        emission.enabled = false;
        var shape = system.shape;
        shape.enabled = false;
        main.simulationSpeed = 0f;
        system.Play(false);
        animatorSettings = GetComponent<LegacyParticleAnimator>();
        previousPosition = transform.position;
        var rendererSettings = GetComponent<LegacyParticleRenderer>();
        if (rendererSettings != null) rendererSettings.Apply(system);
    }

    private void Update() { Simulate(Time.deltaTime); }

    public void Simulate(float seconds)
    {
        EnsureSystem();
        if (seconds <= 0f || (animatorSettings != null && animatorSettings.stopSimulation)) return;
        Vector3 emitterVelocity = (transform.position - previousPosition) / seconds;
        previousPosition = transform.position;
        if (emitEnabled)
        {
            if (oneShot)
            {
                if (!wasEmitting) Spawn(Mathf.RoundToInt(Random.Range(minEmission, maxEmission)), emitterVelocity);
            }
            else
            {
                emissionRemainder += Mathf.Max(0f, Random.Range(minEmission, maxEmission)) * seconds;
                int count = Mathf.FloorToInt(emissionRemainder);
                emissionRemainder -= count;
                Spawn(count, emitterVelocity);
            }
        }
        wasEmitting = emitEnabled;
        for (int i = live.Count - 1; i >= 0; --i)
        {
            var p = live[i];
            p.remainingLifetime -= seconds;
            if (p.remainingLifetime <= 0f) { live.RemoveAt(i); continue; }
            if (animatorSettings != null) animatorSettings.Animate(ref p, seconds, worldSpace, transform);
            p.position += p.velocity * seconds;
            p.rotation += p.angularVelocity * seconds;
            live[i] = p;
        }
        Publish();
        if (hasEmitted && live.Count == 0 && (!emitEnabled || oneShot) && animatorSettings != null && animatorSettings.autodestruct)
            Destroy(gameObject);
    }

    private void Spawn(int count, Vector3 emitterVelocity)
    {
        for (int i = 0; i < count; ++i)
        {
            Vector3 direction = Random.onUnitSphere;
            // Legacy range is a normalized inner radius. Preserve raw out-of-range data
            // in the asset, while constraining the actual spawn volume to its documented domain.
            float inner = Mathf.Clamp01(minEmitterRange);
            float radius = Mathf.Pow(Random.Range(inner * inner * inner, 1f), 1f / 3f);
            Vector3 localPosition = Vector3.Scale(direction * radius, ellipsoid);
            Vector3 velocity = localVelocity + RandomVector(rndVelocity) + Vector3.Cross(tangentVelocity, direction);
            velocity = transform.TransformDirection(velocity) + worldVelocity + emitterVelocity * emitterVelocityScale;
            float lifetime = Random.Range(minEnergy, maxEnergy);
            var p = new ParticleSystem.Particle
            {
                position = worldSpace ? transform.TransformPoint(localPosition) : localPosition,
                velocity = worldSpace ? velocity : transform.InverseTransformDirection(velocity),
                startLifetime = lifetime,
                remainingLifetime = lifetime,
                startSize = Random.Range(minSize, maxSize),
                startColor = Color.white,
                rotation = rndRotation ? Random.Range(0f, 360f) : 0f,
                angularVelocity = angularVelocity + Random.Range(-rndAngularVelocity, rndAngularVelocity),
                randomSeed = nextSeed++
            };
            live.Add(p);
            hasEmitted = true;
        }
    }

    internal static Vector3 RandomVector(Vector3 range)
    {
        return new Vector3(Random.Range(-range.x, range.x), Random.Range(-range.y, range.y), Random.Range(-range.z, range.z));
    }

    private void Publish()
    {
        EnsureSystem();
        var main = system.main;
        if (live.Count > main.maxParticles) main.maxParticles = Mathf.NextPowerOfTwo(live.Count);
        if (renderBuffer.Length < live.Count) renderBuffer = new ParticleSystem.Particle[Mathf.NextPowerOfTwo(live.Count)];
        live.CopyTo(renderBuffer);
        system.SetParticles(renderBuffer, live.Count);
    }
}
