using System.Collections.Generic;
using UnityEngine;

public class Arrow : Projectile
{
	private const float kVelocity = 1000f;

	private const float kArrowStickTime = 2.5f;

	private const float kArrowHalfLength = 20f;

	private const float kBigExplosionRaidus = 160f;

	private const float kExplosionRadius = 80f;

	private WeakPtr<Character> mTargetRef;

	private float mDamage;

	private GameObject mArrow;

	private Transform mTransform;

	private Vector3 mCachedTargetPos;

	private bool mUsingArcShot;

	private float mOriginalDistance;

	private float mArcHeightLastFrame;

	public override bool isDone
	{
		get
		{
			return mArrow == null;
		}
	}

	public Vector3 targetPosition
	{
		get
		{
			if (mTargetRef.ptr != null)
			{
				mCachedTargetPos = mTargetRef.ptr.controller.autoPaperdoll.GetJointPosition("impact_target");
			}
			return mCachedTargetPos;
		}
	}

	public Arrow(EProjectileType type, Character shooter, Character target, float damage, Vector3 spawnPos)
	{
		base.type = type;
		base.shooter = shooter;
		mTargetRef = new WeakPtr<Character>(target);
		mDamage = damage;
		mArrow = (GameObject)Object.Instantiate(WeakGlobalInstance<ProjectileManager>.instance.PrefabForProjectile(type));
		mTransform = mArrow.transform;
		mTransform.position = spawnPos;
		SoundThemePlayer component = mArrow.GetComponent<SoundThemePlayer>();
		if (component != null && component.autoPlayEvent != "Spawn")
		{
			component.PlaySoundEvent("Spawn");
		}
		mOriginalDistance = Vector3.Distance(spawnPos, targetPosition) - 20f;
		mUsingArcShot = ProjectileManager.ProjectileArcs(type) && Mathf.Abs(spawnPos.z - targetPosition.z) > 300f;
		MoveArrow();
		mTransform.position = spawnPos;
		mArcHeightLastFrame = 0f;
	}

	public override void Update()
	{
		if (mArrow == null)
		{
			return;
		}
		base.Update();
		if (type == EProjectileType.Bullet)
		{
			mTargetRef.ptr.RecievedAttack(EAttackType.Bullet, mDamage);
			Object.Destroy(mArrow);
			mArrow = null;
		}
		else
		{
			if (!MoveArrow())
			{
				return;
			}
			if (mTargetRef.ptr != null)
			{
				if (mTargetRef.ptr.health <= 0f && type != EProjectileType.HealBolt && type != EProjectileType.EvilHealBolt)
				{
					Object.Destroy(mArrow);
					mArrow = null;
					return;
				}
				EAttackType attackType = EAttackType.Arrow;
				switch (type)
				{
				case EProjectileType.HealBolt:
					if (mTargetRef.ptr.isEnemy)
					{
						mTargetRef.ptr.RecievedHealing((0f - mDamage) * 0.25f);
					}
					else
					{
						mTargetRef.ptr.RecievedHealing(mDamage);
					}
					Object.Destroy(mArrow);
					break;
				case EProjectileType.EvilHealBolt:
					if (mTargetRef.ptr.isEnemy)
					{
						mTargetRef.ptr.RecievedHealing(mDamage);
					}
					else
					{
						mTargetRef.ptr.RecievedHealing((0f - mDamage) * 0.25f);
					}
					Object.Destroy(mArrow);
					break;
				case EProjectileType.ExplodingArrow:
				case EProjectileType.HeroExplodingArrow:
					ApplyExplosion(mTargetRef.ptr, false);
					Object.Destroy(mArrow);
					break;
				case EProjectileType.BigExplodingArrow:
				case EProjectileType.HeroBigExplodingArrow:
					ApplyExplosion(mTargetRef.ptr, true);
					Object.Destroy(mArrow);
					break;
				case EProjectileType.IceArrow:
					ApplyIceEffect(shooter, mTargetRef.ptr);
					goto default;
				case EProjectileType.Shuriken:
					attackType = EAttackType.Shuriken;
					mTargetRef.ptr.RecievedAttack(attackType, mDamage);
					Object.Destroy(mArrow);
					break;
				case EProjectileType.FireArrow:
				case EProjectileType.BlueFireArrow:
				case EProjectileType.HeroFireArrow:
				case EProjectileType.HeroBlueFireArrow:
					attackType = EAttackType.FireArrow;
					goto default;
				default:
				{
					mTargetRef.ptr.RecievedAttack(attackType, mDamage);
					mTargetRef.ptr.controller.autoPaperdoll.AttachObjectToJoint(mArrow, "impact_target", false);
					LegacyParticleRenderer[] componentsInChildren = mArrow.GetComponentsInChildren<LegacyParticleRenderer>();
					LegacyParticleRenderer[] array = componentsInChildren;
					foreach (LegacyParticleRenderer particleRenderer in array)
					{
						if (particleRenderer != null)
						{
							particleRenderer.enabled = false;
						}
					}
					Object.Destroy(mArrow, 2.5f);
					break;
				}
				}
			}
			else
			{
				Object.Destroy(mArrow);
			}
			mArrow = null;
			mTransform = null;
		}
	}

	public override void Destroy()
	{
		if (mArrow != null)
		{
			Object.Destroy(mArrow);
			mArrow = null;
		}
		base.Destroy();
	}

	private bool MoveArrow()
	{
		bool flag = false;
		Vector3 vector = targetPosition;
		Vector3 position = mTransform.position;
		if (mUsingArcShot)
		{
			position.y -= mArcHeightLastFrame;
		}
		float num = 1000f * Time.deltaTime;
		float num2 = Vector3.Distance(position, vector) - 20f;
		if (num > num2)
		{
			flag = true;
			num = num2;
		}
		position = Vector3.MoveTowards(position, vector, num);
		mArcHeightLastFrame = 0f;
		if (mUsingArcShot && !flag)
		{
			num2 -= num;
			if (num2 < mOriginalDistance)
			{
				float num3 = num2 / mOriginalDistance - 0.5f;
				float num4 = mOriginalDistance * 0.2f;
				mArcHeightLastFrame = num4 - 4f * num4 * (num3 * num3);
				position.y += mArcHeightLastFrame;
			}
		}
		if (!flag)
		{
			FaceTowardTarget(mTransform, position);
		}
		mTransform.position = position;
		return flag;
	}

	private void FaceTowardTarget(Transform arrowTransform, Vector3 targetPos)
	{
		arrowTransform.LookAt(targetPos);
	}

	private void ApplyExplosion(Character target, bool bigVersion)
	{
		if (bigVersion)
		{
			target.controller.SpawnEffectAtJoint(Resources.Load("FX/ExplosionBig") as GameObject, "impact_target", false);
		}
		else
		{
			target.controller.SpawnEffectAtJoint(Resources.Load("FX/Explosion") as GameObject, "impact_target", false);
		}
		List<Character> charactersInRange = WeakGlobalInstance<CharactersManager>.instance.GetCharactersInRange(target.position.z - ((!bigVersion) ? 80f : 160f), target.position.z + ((!bigVersion) ? 80f : 160f), target.isEnemy);
		foreach (Character item in charactersInRange)
		{
			if (item.isFlying == target.isFlying)
			{
				item.RecievedAttack(EAttackType.Explosion, mDamage);
			}
		}
	}

	private void ApplyIceEffect(Character shooter, Character target)
	{
		target.ApplyIceEffect(shooter.bowAttackFrequency);
	}
}
