using UnityEngine;

public class ParSizeAnimBehavior : MonoBehaviour
{
	private int x;

	private float lifePerc;

	public int maxParticles = 200;

	public float minSize = 0.5f;

	public float maxSize = 1.5f;

	public AnimationCurve SizeCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

	private float[] sizeArray;

	private void Start()
	{
		sizeArray = new float[maxParticles];
		for (x = 0; x < maxParticles; x++)
		{
			sizeArray[x] = Random.Range(minSize, maxSize);
		}
		base.GetComponent<LegacyParticleEmitter>().minSize = 0f;
		base.GetComponent<LegacyParticleEmitter>().maxSize = 0f;
	}

	private void Update()
	{
		ParticleSystem.Particle[] particles = base.GetComponent<LegacyParticleEmitter>().particles;
		x = 0;
		ParticleSystem.Particle[] array = particles;
		for (int i = 0; i < array.Length; i++)
		{
			ParticleSystem.Particle particle = array[i];
			lifePerc = 1f - particle.remainingLifetime / particle.startLifetime;
			particles[x].startSize = sizeArray[x] * SizeCurve.Evaluate(lifePerc);
			x++;
		}
		base.GetComponent<LegacyParticleEmitter>().particles = particles;
	}
}
