using UnityEngine;

public class ParAttractorBehavior : MonoBehaviour
{
	private int x;

	private Vector3 gVector;

	private Vector3 pVector;

	public float attractionForce = 0.1f;

	public Vector3 AttractorLoc;

	public bool UseWorldSpace;

	public float minExclusion = 0.1f;

	private void Start()
	{
	}

	private void Update()
	{
		ParticleSystem.Particle[] particles = base.GetComponent<LegacyParticleEmitter>().particles;
		if (UseWorldSpace)
		{
			pVector = AttractorLoc;
		}
		else
		{
			pVector = base.transform.position - AttractorLoc;
		}
		x = 0;
		ParticleSystem.Particle[] array = particles;
		foreach (ParticleSystem.Particle particle in array)
		{
			gVector = particle.position - pVector;
			if (gVector.sqrMagnitude > minExclusion * minExclusion)
			{
				gVector *= attractionForce / gVector.sqrMagnitude;
				particles[x].velocity -= gVector;
			}
			x++;
		}
		base.GetComponent<LegacyParticleEmitter>().particles = particles;
	}
}
