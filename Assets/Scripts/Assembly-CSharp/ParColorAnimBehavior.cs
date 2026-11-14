using UnityEngine;

public class ParColorAnimBehavior : MonoBehaviour
{
	private int x;

	private float lifePerc;

	public AnimationCurve RCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

	public AnimationCurve GCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

	public AnimationCurve BCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

	public AnimationCurve ACurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

	private void Start()
	{
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
			particles[x].startColor = new Color(RCurve.Evaluate(lifePerc), GCurve.Evaluate(lifePerc), BCurve.Evaluate(lifePerc), ACurve.Evaluate(lifePerc));
			x++;
		}
		base.GetComponent<LegacyParticleEmitter>().particles = particles;
	}
}
