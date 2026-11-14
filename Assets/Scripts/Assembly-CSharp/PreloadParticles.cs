using UnityEngine;

[AddComponentMenu("Particles/PreloadParticles")]
[ExecuteInEditMode]
public class PreloadParticles : MonoBehaviour
{
	public float simulateTime;

	private void Awake()
	{
		LegacyParticleEmitter particleEmitter = base.GetComponent<LegacyParticleEmitter>();
		if (!(particleEmitter == null))
		{
			float num = Mathf.Max(Time.deltaTime, 0.01f);
			for (float num2 = simulateTime; num2 > 0f; num2 -= num)
			{
				particleEmitter.Simulate(Mathf.Min(num2, num));
			}
		}
	}
}
