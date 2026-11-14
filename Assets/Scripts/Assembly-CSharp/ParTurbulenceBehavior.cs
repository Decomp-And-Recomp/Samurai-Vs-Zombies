using UnityEngine;

public class ParTurbulenceBehavior : MonoBehaviour
{
	private int x;

	private float rtime;

	private float wobblex;

	private float wobbley;

	private float wobblez;

	public float amplitudeX;

	public float frequencyX;

	public float amplitudeY;

	public float frequencyY;

	public float amplitudeZ;

	public float frequencyZ;

	private void Start()
	{
	}

	private void Update()
	{
		rtime = Time.time;
		ParticleSystem.Particle[] particles = base.GetComponent<LegacyParticleEmitter>().particles;
		x = 0;
		ParticleSystem.Particle[] array = particles;
		for (int i = 0; i < array.Length; i++)
		{
			ParticleSystem.Particle particle = array[i];
			wobblex = amplitudeX * Mathf.Sin(rtime * particle.position[1] * particle.position[2] / 57.2958f * frequencyX * 10f) * 0.01f;
			wobbley = amplitudeY * Mathf.Sin(rtime * particle.position[0] * particle.position[2] / 57.2958f * frequencyY * 10f) * 0.01f;
			wobblez = amplitudeZ * Mathf.Sin(rtime * particle.position[0] * particle.position[1] / 57.2958f * frequencyZ * 10f) * 0.01f;
			particles[x].velocity += new Vector3(wobblex, wobbley, wobblez);
			x++;
		}
		base.GetComponent<LegacyParticleEmitter>().particles = particles;
	}
}
