using UnityEngine;

public class EffectKiller : MonoBehaviour
{
	public bool deleteWhenAllChildrenDead = true;

	public float autoDeleteAfterTime;

	public bool letParticlesFinishForTimedDelete;

	private bool mHadChildren;

	private bool mStoppedParticleEmits;

	private LegacyParticleEmitter[] mEmitters;

	private void Start()
	{
		mHadChildren = base.transform.childCount > 0;
		if (autoDeleteAfterTime > 0f)
		{
			if (!letParticlesFinishForTimedDelete)
			{
				Object.Destroy(base.gameObject, autoDeleteAfterTime);
			}
			else
			{
				mEmitters = GetComponentsInChildren<LegacyParticleEmitter>();
				if (mEmitters.Length == 0)
				{
					letParticlesFinishForTimedDelete = false;
					Object.Destroy(base.gameObject, autoDeleteAfterTime);
				}
			}
		}
		else
		{
			letParticlesFinishForTimedDelete = false;
		}
		if (!mHadChildren && !letParticlesFinishForTimedDelete)
		{
			base.enabled = false;
		}
	}

	private void Update()
	{
		if (mHadChildren && base.transform.childCount <= 0)
		{
			Object.Destroy(base.gameObject);
		}
		if (!letParticlesFinishForTimedDelete)
		{
			return;
		}
		if (autoDeleteAfterTime > 0f)
		{
			autoDeleteAfterTime -= Time.deltaTime;
		}
		if (!(autoDeleteAfterTime <= 0f))
		{
			return;
		}
		if (mStoppedParticleEmits)
		{
			bool flag = false;
			if (mEmitters != null)
			{
				LegacyParticleEmitter[] array = mEmitters;
				foreach (LegacyParticleEmitter particleEmitter in array)
				{
					if (!(particleEmitter == null))
					{
						particleEmitter.emit = false;
						if (particleEmitter.particleCount > 0)
						{
							flag = true;
						}
					}
				}
			}
			if (!flag)
			{
				Object.Destroy(base.gameObject);
			}
			return;
		}
		mStoppedParticleEmits = true;
		if (mEmitters == null)
		{
			return;
		}
		LegacyParticleEmitter[] array2 = mEmitters;
		foreach (LegacyParticleEmitter particleEmitter2 in array2)
		{
			if (particleEmitter2 != null)
			{
				particleEmitter2.emit = false;
			}
		}
	}
}
