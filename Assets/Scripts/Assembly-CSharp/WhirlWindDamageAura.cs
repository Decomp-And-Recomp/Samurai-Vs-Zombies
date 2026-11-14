using System.Collections.Generic;
using UnityEngine;

public class WhirlWindDamageAura : MonoBehaviour
{
	private const float kDamageFrequency = 0.2f;

	private const float kDissipateTime = 2f;

	private float mDamagePerHit;

	private float mFlierDamageMultiplier = 1f;

	private float mTimeUntilNextDamage;

	private float mRemainingDuration;

	private GameRange mDamageRange = new GameRange(0f, 0f);

	private bool mStoppedEmitting;

	private LegacyParticleEmitter[] mEmitters;

	private void Start()
	{
		mEmitters = GetComponentsInChildren<LegacyParticleEmitter>();
		mRemainingDuration = float.Parse(Singleton<AbilitiesDatabase>.instance.GetAttribute("SummonTornadoes", "duration"));
		mDamagePerHit = Singleton<AbilitiesDatabase>.instance.Extrapolate<float>("SummonTornadoes", "infiniteUpgradeDamage", "damage") / (mRemainingDuration / 0.2f);
		mFlierDamageMultiplier = float.Parse(Singleton<AbilitiesDatabase>.instance.GetAttribute("SummonTornadoes", "flyerDamageMultiplier"));
		float num = float.Parse(Singleton<AbilitiesDatabase>.instance.GetAttribute("SummonTornadoes", "damageRadius"));
		mDamageRange.left = base.transform.position.z - num;
		mDamageRange.right = mDamageRange.left + num * 2f;
	}

	private void Update()
	{
		if (mStoppedEmitting)
		{
			bool flag = false;
			LegacyParticleEmitter[] array = mEmitters;
			foreach (LegacyParticleEmitter particleEmitter in array)
			{
				particleEmitter.emit = false;
				if (particleEmitter.particleCount > 0)
				{
					flag = true;
				}
			}
			if (!flag)
			{
				Object.Destroy(base.gameObject);
			}
			return;
		}
		mRemainingDuration -= Time.deltaTime;
		mTimeUntilNextDamage -= Time.deltaTime;
		while (mTimeUntilNextDamage <= 0f)
		{
			mTimeUntilNextDamage += 0.2f;
			List<Character> charactersInRange = WeakGlobalInstance<CharactersManager>.instance.GetCharactersInRange(mDamageRange, true);
			foreach (Character item in charactersInRange)
			{
				if (item != null)
				{
					if (item.isFlying)
					{
						item.RecievedAttack(EAttackType.DOT, mDamagePerHit * mFlierDamageMultiplier);
					}
					else
					{
						item.RecievedAttack(EAttackType.DOT, mDamagePerHit);
					}
				}
			}
		}
		if (mRemainingDuration <= 0f)
		{
			mStoppedEmitting = true;
		}
	}
}
