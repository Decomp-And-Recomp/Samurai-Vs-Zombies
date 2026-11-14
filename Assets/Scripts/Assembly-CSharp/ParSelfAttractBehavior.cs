using UnityEngine;

public class ParSelfAttractBehavior : MonoBehaviour
{
	private int x;

	private Vector3 gVector;

	public float attractionForce = 0.001f;

	public float minExclusion = 0.001f;

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
			ParticleSystem.Particle[] array2 = particles;
			foreach (ParticleSystem.Particle particle2 in array2)
			{
				gVector = particle.position - particle2.position;
				if (gVector.sqrMagnitude > minExclusion * minExclusion)
				{
					gVector *= attractionForce / gVector.sqrMagnitude;
					particles[x].velocity -= gVector;
				}
			}
			x++;
		}
		base.GetComponent<LegacyParticleEmitter>().particles = particles;
	}
}
