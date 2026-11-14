using System.Collections.Generic;
using UnityEngine;

public class ParLineAgeBehavior : MonoBehaviour
{
	private int x;

	private void Start()
	{
	}

	private void Update()
	{
		ParticleSystem.Particle[] particles = base.GetComponent<LegacyParticleEmitter>().particles;
		List<ParticleSystem.Particle> list = new List<ParticleSystem.Particle>();
		list.AddRange(particles);
		list.Sort((ParticleSystem.Particle thing1, ParticleSystem.Particle thing2) => thing1.remainingLifetime.CompareTo(thing2.remainingLifetime));
		LineRenderer component = GetComponent<LineRenderer>();
		component.SetVertexCount(particles.Length);
		x = 0;
		foreach (ParticleSystem.Particle item in list)
		{
			component.SetPosition(x, item.position);
			x++;
		}
	}
}
