using System;
using System.Collections.Generic;
using UnityEngine;

public class Character : Weakable
{
	private const float kAutoRecoveryDelay = 2f;

	private const float kImpactPauseTime = 0.12f;

	private const int kMinKnockbackChance = 1;

	private const float kAttackRangeLeeway = 1.3f;

	private const float kMaxMoveDelay = 0.5f;

	private const float kLeniencyTimer = 1.2f;

	private const float kLeniencyTreshold = 0.1f;

	private const float kExplosionRange = 250f;

	private const float kMaxHealthPercentToPreferFrontLine = 0.4f;

	private const float kMaxHealthPercentToPreferWeaker = 0.2f;

	private const float kCloseDistRange = 35f;

	private CharacterModelController mCtrl;

	private CharacterStats mStats;

	private MiniHealthBar mHealthBar;

	private float mMeleeAttackDelay;

	private float mRangedAttackDelay;

	private float mMoveDelay;

	private bool mMeleeAttackDelivered;

	protected bool mRangedAttackDelivered;

	private ResourceDrops mResourceDrops;

	private float mAutoRecoveryDelay;

	private float? mLeniencyTimer;

	private bool mInMeleeAttackCycle;

	private bool mInRangedAttackCycle;

	private BuffIconClient mBuffIconClient;

	private List<GameObject> mPaperDollMeleeWeapon = new List<GameObject>();

	private GameObject mPaperDollRangedWeapon;

	protected bool mHasDOT;

	protected float mDOTdamage;

	protected float mDOTfrequency;

	protected float mDOTduration;

	protected float mDOTcurrentTime;

	protected bool mHasSpeedSnare;

	protected float mSpeedSnareDuration;

	protected float mDamageBuffDuration;

	protected float mDamageBuffPercent;

	private WeakPtr<Character> mSingleAttackTarget;

	private List<WeakPtr<Character>> mTargetedAttackers = new List<WeakPtr<Character>>();

	private string[] kMeleeJointIDs = new string[2] { "melee_weapon", "melee_weapon_2" };

	public BuffIconClient buffIcon
	{
		get
		{
			return mBuffIconClient;
		}
	}

	public GameObject meleeWeaponPrefab
	{
		get
		{
			if (mPaperDollMeleeWeapon.Count > 0)
			{
				return mPaperDollMeleeWeapon[0];
			}
			return null;
		}
		set
		{
			List<GameObject> list = new List<GameObject>(1);
			list.Add(value);
			meleeWeaponPrefabAsList = list;
		}
	}

	public List<GameObject> meleeWeaponPrefabAsList
	{
		get
		{
			return mPaperDollMeleeWeapon;
		}
		set
		{
			DestroyMeleeWeaponPrefabs();
			mPaperDollMeleeWeapon = value;
			if (mPaperDollMeleeWeapon == null)
			{
				mPaperDollMeleeWeapon = new List<GameObject>();
			}
			for (int i = 0; i < Mathf.Min(kMeleeJointIDs.Length, mPaperDollMeleeWeapon.Count); i++)
			{
				GameObject gameObject = mPaperDollMeleeWeapon[i];
				if (gameObject != null)
				{
					mPaperDollMeleeWeapon[i] = controller.InstantiateObjectOnJoint(gameObject, kMeleeJointIDs[i]);
				}
			}
		}
	}

	public GameObject rangedWeaponPrefab
	{
		get
		{
			return mPaperDollRangedWeapon;
		}
		set
		{
			if (mPaperDollRangedWeapon != null)
			{
				UnityEngine.Object.Destroy(mPaperDollRangedWeapon);
			}
			if (value != null)
			{
				mPaperDollRangedWeapon = controller.InstantiateObjectOnJoint(value, "ranged_weapon");
			}
		}
	}

	public CharacterStats stats
	{
		get
		{
			return mStats;
		}
		set
		{
			mStats = value;
			controller.speed = mStats.speed;
		}
	}

	public bool isInLeniencyMode
	{
		get
		{
			float? num = mLeniencyTimer;
			return num.HasValue;
		}
	}

	public float health
	{
		get
		{
			return mStats.health;
		}
		set
		{
			if (value < mStats.health)
			{
				if ((isPlayer || !isEnemy) && autoHealthRecovery >= 0f && WeakGlobalSceneBehavior<InGameImpl>.instance.allAlliesInvincibleTimer > 0f)
				{
					return;
				}
				if (isPlayer)
				{
					mAutoRecoveryDelay = 2f;
				}
			}
			mStats.health = Mathf.Clamp(value, 0f, mStats.maxHealth);
			if (isPlayer)
			{
				if (mStats.health <= 0.1f)
				{
					float? num = mLeniencyTimer;
					if (!num.HasValue)
					{
						mLeniencyTimer = 1.2f;
						mStats.health = 0.1f;
					}
					else
					{
						float? num2 = mLeniencyTimer;
						if (num2.HasValue && num2.Value > 0f)
						{
							mStats.health = 0.1f;
						}
					}
				}
				else
				{
					mLeniencyTimer = null;
				}
			}
			if (mStats.health <= 0f)
			{
				Die();
			}
		}
	}

	public float maxHealth
	{
		get
		{
			return mStats.maxHealth;
		}
		set
		{
			mStats.maxHealth = Mathf.Max(0f, value);
			mStats.health = Mathf.Min(mStats.health, mStats.maxHealth);
		}
	}

	public float autoHealthRecovery
	{
		get
		{
			return mStats.autoHealthRecovery;
		}
		set
		{
			mStats.autoHealthRecovery = value;
		}
	}

	public virtual bool isOver
	{
		get
		{
			return health <= 0f && controller.readyToVanish;
		}
	}

	public CharacterModelController.EPostDeathAction postDeathAction
	{
		get
		{
			if (Singleton<PlayModesManager>.instance.selectedMode == "zombies")
			{
				return isEnemy ? CharacterModelController.EPostDeathAction.ascend : ((!isPlayer) ? ((!isBase) ? CharacterModelController.EPostDeathAction.melt : CharacterModelController.EPostDeathAction.vanish) : CharacterModelController.EPostDeathAction.vanish);
			}
			return isEnemy ? CharacterModelController.EPostDeathAction.melt : ((!isPlayer) ? ((!isBase) ? CharacterModelController.EPostDeathAction.ascend : CharacterModelController.EPostDeathAction.vanish) : CharacterModelController.EPostDeathAction.vanish);
		}
	}

	public CharacterModelController controller
	{
		get
		{
			if (mCtrl == null)
			{
				Debug.Log("WARNING: cannot access 'controller' without setting the 'controllerObject' first.");
			}
			return mCtrl;
		}
	}

	public GameObject controlledObject
	{
		get
		{
			if (mCtrl == null)
			{
				return null;
			}
			return mCtrl.gameObject;
		}
		set
		{
			if (mCtrl != null)
			{
				UnityEngine.Object.Destroy(mCtrl.gameObject);
				mCtrl = null;
			}
			mCtrl = value.GetComponent<CharacterModelController>();
			if (!mCtrl)
			{
				mCtrl = value.AddComponent<CharacterModelController>();
			}
			mCtrl.onMeleeAttackDelivery = OnAttackDelivery;
			mCtrl.onRangedAttackDelivery = OnRangedAttackDelivery;
			mBuffIconClient = new BuffIconClient(mCtrl);
		}
	}

	public string uniqueID
	{
		get
		{
			return mStats.uniqueID;
		}
		set
		{
			mStats.uniqueID = value;
		}
	}

	public bool isUnique
	{
		get
		{
			return mStats.isUnique;
		}
		set
		{
			mStats.isUnique = value;
		}
	}

	public bool isEnemy
	{
		get
		{
			return mStats.isEnemy;
		}
		set
		{
			mStats.isEnemy = value;
		}
	}

	public bool isPlayer
	{
		get
		{
			return mStats.isPlayer;
		}
		set
		{
			mStats.isPlayer = value;
		}
	}

	public bool isBase
	{
		get
		{
			return mStats.isBase;
		}
		set
		{
			mStats.isBase = value;
		}
	}

	public bool isGateRusher
	{
		get
		{
			return mStats.isGateRusher;
		}
		set
		{
			mStats.isGateRusher = value;
		}
	}

	public bool isFlying
	{
		get
		{
			return mStats.isFlying;
		}
		set
		{
			mStats.isFlying = value;
		}
	}

	public string spawnFriendID
	{
		get
		{
			return mStats.spawnFriendID;
		}
		set
		{
			mStats.spawnFriendID = value;
		}
	}

	public bool exploseOnMelee
	{
		get
		{
			return mStats.exploseOnMelee;
		}
		set
		{
			mStats.exploseOnMelee = value;
		}
	}

	public float meleeFreeze
	{
		get
		{
			return mStats.meleeFreeze;
		}
		set
		{
			mStats.meleeFreeze = value;
		}
	}

	public bool isBoss
	{
		get
		{
			return mStats.isBoss;
		}
		set
		{
			mStats.isBoss = value;
		}
	}

	public bool canMeleeAttack
	{
		get
		{
			return isEffectivelyIdle && meleeAttackRange > 0f && (mInMeleeAttackCycle || meleePreAttackDelay <= 0f) && (mMeleeAttackDelay <= 0f || !mMeleeAttackDelivered);
		}
	}

	public bool canUseRangedAttack
	{
		get
		{
			return isEffectivelyIdle && bowAttackRange > 0f && bowProjectile != 0 && (mInRangedAttackCycle || rangedPreAttackDelay <= 0f) && (mRangedAttackDelay <= 0f || !mRangedAttackDelivered);
		}
	}

	public float meleePreAttackDelay
	{
		get
		{
			return isPlayer ? 0f : ((!isEnemy) ? WeakGlobalInstance<CharactersManager>.instance.allyMeleePreAttackDelay : WeakGlobalInstance<CharactersManager>.instance.enemyMeleePreAttackDelay);
		}
	}

	public float rangedPreAttackDelay
	{
		get
		{
			return isPlayer ? 0f : ((!isEnemy) ? WeakGlobalInstance<CharactersManager>.instance.allyRangedPreAttackDelay : WeakGlobalInstance<CharactersManager>.instance.enemyRangedPreAttackDelay);
		}
	}

	public bool isSpellCasterOnAllies
	{
		get
		{
			return (mStats.projectile == Projectile.EProjectileType.HealBolt || mStats.projectile == Projectile.EProjectileType.EvilHealBolt) && bowAttackRange > 0f;
		}
	}

	public bool hasRangedAttack
	{
		get
		{
			return bowAttackRange > 0f && mStats.projectile != Projectile.EProjectileType.HealBolt && mStats.projectile != Projectile.EProjectileType.None;
		}
	}

	public bool isEffectivelyIdle
	{
		get
		{
			return mCtrl.isEffectivelyIdle;
		}
	}

	public bool canMove
	{
		get
		{
			if (!isEffectivelyIdle)
			{
				return false;
			}
			UpdateMoveDelay();
			return mMoveDelay <= 0f;
		}
	}

	public bool isInKnockback
	{
		get
		{
			return mCtrl.isInKnockback;
		}
	}

	public EAttackType lastAttackTypeHitWith { get; protected set; }

	public Action onDeathEvent { get; set; }

	public float meleeAttackRange
	{
		get
		{
			return mStats.meleeAttackRange;
		}
		set
		{
			mStats.meleeAttackRange = Mathf.Abs(value);
		}
	}

	public GameRange meleeAttackGameRange
	{
		get
		{
			if (mCtrl.facing == FacingType.Right)
			{
				return new GameRange(position.z, position.z + mStats.meleeAttackRange);
			}
			return new GameRange(position.z - mStats.meleeAttackRange, position.z);
		}
	}

	public GameRange meleeAttackHitGameRange
	{
		get
		{
			if (mCtrl.facing == FacingType.Right)
			{
				return new GameRange(position.z, position.z + mStats.meleeAttackRange * 1.3f);
			}
			return new GameRange(position.z - mStats.meleeAttackRange * 1.3f, position.z);
		}
	}

	public float meleeAttackFrequency
	{
		get
		{
			return mStats.meleeAttackFrequency;
		}
		set
		{
			mStats.meleeAttackFrequency = Mathf.Abs(value);
		}
	}

	public bool meleeWeaponIsABlade
	{
		get
		{
			return mStats.meleeWeaponIsABlade;
		}
		set
		{
			mStats.meleeWeaponIsABlade = value;
		}
	}

	public int knockbackPower
	{
		get
		{
			return mStats.knockbackPower;
		}
		set
		{
			mStats.knockbackPower = value;
		}
	}

	public int knockbackResistance
	{
		get
		{
			return mStats.knockbackResistance;
		}
		set
		{
			mStats.knockbackResistance = value;
		}
	}

	public float bowAttackRange
	{
		get
		{
			return mStats.bowAttackRange;
		}
		set
		{
			mStats.bowAttackRange = Mathf.Abs(value);
		}
	}

	public GameRange bowAttackGameRange
	{
		get
		{
			if (mCtrl.facing == FacingType.Left)
			{
				return new GameRange(position.z - mStats.bowAttackRange, position.z - mStats.meleeAttackRange);
			}
			return new GameRange(position.z + mStats.meleeAttackRange, position.z + mStats.bowAttackRange);
		}
	}

	public GameRange bowAttackExtGameRange
	{
		get
		{
			if (mCtrl.facing == FacingType.Left)
			{
				return new GameRange(position.z - mStats.bowAttackRange * 1.3f, position.z - mStats.meleeAttackRange);
			}
			return new GameRange(position.z + mStats.meleeAttackRange, position.z + mStats.bowAttackRange * 1.3f);
		}
	}

	public GameRange bowAttackHitGameRange
	{
		get
		{
			if (mCtrl.facing == FacingType.Left)
			{
				return new GameRange(position.z - mStats.bowAttackRange * 1.3f, position.z);
			}
			return new GameRange(position.z, position.z + mStats.bowAttackRange * 1.3f);
		}
	}

	public GameRange buffEffectGameRange
	{
		get
		{
			return new GameRange(position.z - mStats.bowAttackRange, position.z + mStats.bowAttackRange);
		}
	}

	public float bowAttackFrequency
	{
		get
		{
			return mStats.bowAttackFrequency;
		}
		set
		{
			mStats.bowAttackFrequency = Mathf.Abs(value);
		}
	}

	public float bowDamage
	{
		get
		{
			return mStats.bowAttackDamage * (1f + mDamageBuffPercent);
		}
		set
		{
			mStats.bowAttackDamage = value;
		}
	}

	public Projectile.EProjectileType bowProjectile
	{
		get
		{
			return mStats.projectile;
		}
		set
		{
			mStats.projectile = value;
		}
	}

	public bool usesFocusedFire
	{
		get
		{
			return bowProjectile != Projectile.EProjectileType.IceArrow;
		}
	}

	public List<WeakPtr<Character>> targetedAttackers
	{
		get
		{
			return mTargetedAttackers;
		}
	}

	public float meleeDamage
	{
		get
		{
			return mStats.meleeAttackDamage * (1f + mDamageBuffPercent);
		}
		set
		{
			mStats.meleeAttackDamage = Mathf.Max(0f, value);
		}
	}

	public float damageBuffPercent
	{
		get
		{
			return mStats.damageBuffPercent;
		}
		set
		{
			mStats.damageBuffPercent = Mathf.Max(0f, value);
		}
	}

	public string upgradeAlliesFrom
	{
		get
		{
			return mStats.upgradeAlliesFrom;
		}
		set
		{
			mStats.upgradeAlliesFrom = value;
		}
	}

	public string upgradeAlliesTo
	{
		get
		{
			return mStats.upgradeAlliesTo;
		}
		set
		{
			mStats.upgradeAlliesTo = value;
		}
	}

	public float postAttackMoveDelay
	{
		get
		{
			float num = Mathf.Max(meleeAttackRange, bowAttackRange);
			num -= 150f;
			if (num <= 0f || isSpellCasterOnAllies)
			{
				return 0f;
			}
			return Mathf.Min(num / controller.speed, 0.5f);
		}
	}

	public Vector3 position
	{
		get
		{
			return mCtrl.position;
		}
		set
		{
			mCtrl.position = value;
		}
	}

	public float speedSnareTimeLeft
	{
		get
		{
			return (!mHasSpeedSnare) ? 0f : mSpeedSnareDuration;
		}
	}

	public ResourceDrops resourceDrops
	{
		get
		{
			return mResourceDrops;
		}
	}

	public bool soundEnabled
	{
		get
		{
			if (controlledObject != null)
			{
				SoundThemePlayer component = controlledObject.GetComponent<SoundThemePlayer>();
				if (component != null)
				{
					return component.enabled;
				}
			}
			return false;
		}
		set
		{
			if (!(controlledObject != null))
			{
				return;
			}
			SoundThemePlayer component = controlledObject.GetComponent<SoundThemePlayer>();
			if (component != null)
			{
				component.enabled = value;
				if (!value && component.GetComponent<AudioSource>() != null && component.GetComponent<AudioSource>().isPlaying)
				{
					component.GetComponent<AudioSource>().enabled = false;
					component.GetComponent<AudioSource>().Stop();
				}
			}
		}
	}

	protected Character singleAttackTarget
	{
		get
		{
			return (mSingleAttackTarget == null) ? null : mSingleAttackTarget.ptr;
		}
		set
		{
			if (singleAttackTarget == value)
			{
				return;
			}
			if (singleAttackTarget != null)
			{
				singleAttackTarget.targetedAttackers.RemoveAll((WeakPtr<Character> item) => item == null || item.ptr == null || item.ptr == this || item.ptr.health <= 0f);
			}
			if (value == null)
			{
				mSingleAttackTarget = null;
				return;
			}
			mSingleAttackTarget = new WeakPtr<Character>(value);
			value.targetedAttackers.Add(new WeakPtr<Character>(this));
		}
	}

	private float randomCriticalDamage
	{
		get
		{
			if (mStats.criticalMultiplier > 1f && UnityEngine.Random.value < mStats.criticalChance)
			{
				return mStats.meleeAttackDamage * mStats.criticalMultiplier - mStats.meleeAttackDamage;
			}
			return 0f;
		}
	}

	public Character()
	{
		mResourceDrops = new ResourceDrops();
	}

	public virtual void Update()
	{
		mBuffIconClient.Update();
		float? num = mLeniencyTimer;
		if (num.HasValue)
		{
			float? num2 = mLeniencyTimer;
			mLeniencyTimer = ((!num2.HasValue) ? null : new float?(num2.Value - Time.deltaTime));
		}
		if (health > 0f)
		{
			UpdateBuffsAndAfflictions();
			UpdateAutoRecovery();
			UpdateAttack();
		}
		else
		{
			Die();
		}
		if (mHealthBar != null)
		{
			mHealthBar.Update(controller.position, health / maxHealth);
		}
		UpdateAiming();
	}

	public virtual void Destroy()
	{
		if (isUnique && WeakGlobalInstance<Smithy>.instance != null)
		{
			WeakGlobalInstance<Smithy>.instance.RegisterUnique(uniqueID, false);
		}
		if (mCtrl != null)
		{
			mCtrl.Destroy();
			mCtrl = null;
		}
		if (mHealthBar != null)
		{
			mHealthBar.Destroy();
			mHealthBar = null;
		}
		BreakWeakLinks();
	}

	private void UpdateBuffsAndAfflictions()
	{
		float deltaTime = Time.deltaTime;
		if (mHasDOT)
		{
			if (mDOTduration > 0f)
			{
				mDOTduration -= deltaTime;
				mDOTcurrentTime -= deltaTime;
				if (mDOTcurrentTime <= 0f)
				{
					lastAttackTypeHitWith = EAttackType.DOT;
					health -= mDOTdamage;
					mDOTcurrentTime = mDOTfrequency;
				}
			}
			else
			{
				mDOTduration = 0f;
				mDOTdamage = 0f;
				mDOTcurrentTime = 0f;
				mDOTfrequency = 0f;
				mHasDOT = false;
			}
		}
		if (mHasSpeedSnare)
		{
			if (mSpeedSnareDuration > 0f)
			{
				mSpeedSnareDuration -= deltaTime;
			}
			else
			{
				mSpeedSnareDuration = 0f;
				controller.speedModifier = 1f;
				mHasSpeedSnare = false;
			}
		}
		if (mDamageBuffDuration > 0f)
		{
			mDamageBuffDuration = Mathf.Max(0f, mDamageBuffDuration - deltaTime);
			if (mDamageBuffDuration <= 0f || health <= 0f)
			{
				mDamageBuffPercent = 0f;
				buffIcon.Hide("FX/BuffIcon");
			}
		}
		if (damageBuffPercent > 0f && mDamageBuffDuration < bowAttackFrequency * 0.5f)
		{
			List<Character> charactersInRange = WeakGlobalInstance<CharactersManager>.instance.GetCharactersInRange(buffEffectGameRange, isEnemy);
			foreach (Character item in charactersInRange)
			{
				item.mDamageBuffDuration = bowAttackFrequency;
				if (damageBuffPercent > item.mDamageBuffPercent)
				{
					item.mDamageBuffPercent = damageBuffPercent;
				}
				item.buffIcon.Show("FX/BuffIcon");
			}
		}
		if (!isEnemy && !isBase && WeakGlobalSceneBehavior<InGameImpl>.instance != null && WeakGlobalSceneBehavior<InGameImpl>.instance.allAlliesInvincibleTimer > 0f)
		{
			buffIcon.Show("FX/BuffNightOfDeadIcon", WeakGlobalSceneBehavior<InGameImpl>.instance.allAlliesInvincibleTimer, 10);
		}
		if (string.IsNullOrEmpty(upgradeAlliesFrom) || !(mRangedAttackDelay <= 0f) || string.IsNullOrEmpty(upgradeAlliesTo))
		{
			return;
		}
		mRangedAttackDelay = bowAttackFrequency;
		List<Character> charactersInRange2 = WeakGlobalInstance<CharactersManager>.instance.GetCharactersInRange(buffEffectGameRange, isEnemy);
		foreach (Character item2 in charactersInRange2)
		{
			if (CheckIfTargetValid(item2) && item2.uniqueID == upgradeAlliesFrom)
			{
				Character cToChange = item2;
				CharactersManager instance = WeakGlobalInstance<CharactersManager>.instance;
				instance.postUpdateFunc = (Action)Delegate.Combine(instance.postUpdateFunc, (Action)delegate
				{
					WeakGlobalInstance<Smithy>.instance.ReplaceHelperWith(cToChange, upgradeAlliesTo);
				});
				if (upgradeAlliesFrom.Equals("Farmer", StringComparison.OrdinalIgnoreCase))
				{
					Singleton<Profile>.instance.upgradedFarmers++;
				}
				else if (upgradeAlliesFrom.Equals("SamuraiArcher", StringComparison.OrdinalIgnoreCase))
				{
					Singleton<Profile>.instance.upgradedArchers++;
				}
			}
		}
	}

	public virtual void RecievedAttack(EAttackType attackType, float damage)
	{
		if (health <= 0f)
		{
			return;
		}
		lastAttackTypeHitWith = attackType;
		float num = ((!isPlayer) ? 0.12f : 0f);
		bool flag = true;
		GameObject gameObject = null;
		switch (attackType)
		{
		case EAttackType.Arrow:
		case EAttackType.FireArrow:
			controller.PlaySoundEvent("ArrowImpact");
			gameObject = controller.arrowImpactEffect;
			break;
		case EAttackType.Bullet:
			controller.PlaySoundEvent("BulletImpact");
			gameObject = controller.arrowImpactEffect;
			break;
		case EAttackType.Shuriken:
			num = 0f;
			goto case EAttackType.Arrow;
		case EAttackType.Blade:
			controller.PlaySoundEvent("SwordImpact");
			gameObject = controller.bladeImpactEffect;
			break;
		case EAttackType.Blunt:
			controller.PlaySoundEvent("BluntWepImpact");
			gameObject = controller.bluntImpactEffect;
			break;
		case EAttackType.BladeCritical:
			controller.PlaySoundEvent("SwordImpactBig");
			gameObject = controller.bladeCriticalImpactEffect;
			break;
		case EAttackType.Stealth:
			controller.PlaySoundEvent("SwordImpactBig");
			gameObject = controller.bladeImpactEffect;
			break;
		case EAttackType.BluntCritical:
			controller.PlaySoundEvent("BluntWepImpactBig");
			gameObject = controller.bluntCriticalImpactEffect;
			break;
		case EAttackType.Trample:
			if ((double)UnityEngine.Random.value < 0.5)
			{
				controller.PlaySoundEvent("SwordImpact");
				gameObject = controller.bladeImpactEffect;
			}
			else
			{
				controller.PlaySoundEvent("BluntWepImpact");
				gameObject = controller.bluntImpactEffect;
			}
			break;
		case EAttackType.Sonic:
			num = 0f;
			break;
		case EAttackType.Slice:
			if (health > damage)
			{
				gameObject = controller.bladeImpactEffect;
				controller.SpawnEffectAtJoint(Resources.Load("FX/KatanaBlood") as GameObject, "impact_target", false);
			}
			break;
		case EAttackType.Lightning:
			flag = false;
			controller.PlayHurtAnim("shock");
			break;
		case EAttackType.Holy:
		case EAttackType.Stomp:
			flag = false;
			break;
		case EAttackType.DOT:
			flag = false;
			num = 0f;
			gameObject = null;
			break;
		case EAttackType.Explosion:
			if (health <= damage)
			{
				num = 0f;
			}
			break;
		}
		if (num > 0f)
		{
			controller.impactPauseTime = num;
		}
		if (gameObject != null)
		{
			controller.SpawnEffectAtJoint(gameObject, "impact_target", false);
		}
		if (flag)
		{
			ProceduralShaderManager.postShaderEvent(new DiffuseFadeInOutShaderEvent(mCtrl.gameObject, Color.red, 0f, 0f, 0.25f));
		}
		health -= damage;
		if (controller.name == "Base")
		{
			Singleton<PlayStatistics>.instance.stats.gateWasDamaged = true;
		}
	}

	public void RecievedHealing(float healAmount)
	{
		if (!(health <= 0f) && (!isPlayer || !(health <= 0.1f) || WeakGlobalSceneBehavior<InGameImpl>.instance.gate.health != 0f))
		{
			health += healAmount;
			bool flag = healAmount >= 0f == isEnemy;
			if (Singleton<PlayModesManager>.instance.selectedMode == "zombies")
			{
				flag = !flag;
			}
			controller.SpawnEffectAtJoint(Resources.Load((!flag) ? "FX/HealAura" : "FX/EvilHealAura") as GameObject, "impact_target", true);
		}
	}

	public bool TryKnockback(int attemptPower)
	{
		if (knockbackResistance >= 100 || attemptPower <= 0 || health <= 0f || isBase || isInKnockback)
		{
			return false;
		}
		if (attemptPower < 100)
		{
			attemptPower -= knockbackResistance;
		}
		if (attemptPower <= 1)
		{
			attemptPower = 1;
		}
		if (UnityEngine.Random.Range(0, 100) < attemptPower)
		{
			ForceKnockback();
			return true;
		}
		return false;
	}

	public void ForceKnockback()
	{
		if (!(health <= 0f) && !isBase)
		{
			controller.Knockback();
		}
	}

	public void Die()
	{
		string dieAnim = "die";
		switch (lastAttackTypeHitWith)
		{
		case EAttackType.BladeCritical:
		case EAttackType.Slice:
			if (!controller.isInKnockback)
			{
				dieAnim = "diehead";
			}
			break;
		case EAttackType.Explosion:
		case EAttackType.Sonic:
			dieAnim = "dieexplode";
			break;
		case EAttackType.Stealth:
			dieAnim = "diestealth";
			break;
		case EAttackType.Trample:
			if ((double)UnityEngine.Random.value < 0.2 && !controller.isInKnockback)
			{
				dieAnim = "diehead";
			}
			break;
		case EAttackType.Stomp:
			dieAnim = "diesmashed";
			break;
		case EAttackType.Lightning:
			dieAnim = "dieashes";
			break;
		}
		if (isEnemy && !controller.startedDieAnim && WeakGlobalInstance<WaveManager>.instance.isDone && WeakGlobalInstance<CharactersManager>.instance.enemiesCount == 1)
		{
			dieAnim = "dieexplode";
			controller.impactPauseTime = 0f;
		}
		Die(dieAnim);
	}

	public void Die(string dieAnim)
	{
		buffIcon.Clear();
		if (dieAnim.Length < 3 || controller.currentAnimation.Length < 3 || !(dieAnim.Substring(0, 3) == "die") || !(controller.currentAnimation.Substring(0, 3) == "die"))
		{
			singleAttackTarget = null;
			if (onDeathEvent != null)
			{
				CharactersManager instance = WeakGlobalInstance<CharactersManager>.instance;
				instance.postUpdateFunc = (Action)Delegate.Combine(instance.postUpdateFunc, onDeathEvent);
				onDeathEvent = null;
			}
			if (meleeWeaponPrefab != null)
			{
				SetRangeAttackMode(false);
			}
			controller.Die(dieAnim, postDeathAction);
		}
	}

	public void StartMeleeAttackDelayTimer()
	{
		if (!isPlayer && !mInMeleeAttackCycle)
		{
			WeakGlobalInstance<CharactersManager>.instance.RegisterAttackStarted(isEnemy, false);
		}
		mMeleeAttackDelay = meleeAttackFrequency;
		mMoveDelay = postAttackMoveDelay;
		mMeleeAttackDelivered = false;
	}

	public void StartRangedAttackDelayTimer()
	{
		if (!isPlayer && !mInRangedAttackCycle && !isSpellCasterOnAllies)
		{
			WeakGlobalInstance<CharactersManager>.instance.RegisterAttackStarted(isEnemy, true);
		}
		mRangedAttackDelay = bowAttackFrequency;
		mMoveDelay = postAttackMoveDelay;
		mRangedAttackDelivered = false;
	}

	public int DispersedAttackersCount(Character checkingAttacker)
	{
		targetedAttackers.RemoveAll((WeakPtr<Character> item) => item == null || item.ptr == null || item.ptr.health <= 0f || item.ptr.singleAttackTarget != this);
		int num = 0;
		foreach (WeakPtr<Character> targetedAttacker in targetedAttackers)
		{
			if (targetedAttacker.ptr != checkingAttacker && !targetedAttacker.ptr.usesFocusedFire)
			{
				num++;
			}
		}
		return num;
	}

	public void DestroyMeleeWeaponPrefabs()
	{
		if (mPaperDollMeleeWeapon != null)
		{
			foreach (GameObject item in mPaperDollMeleeWeapon)
			{
				if (item != null)
				{
					UnityEngine.Object.Destroy(item);
				}
			}
			mPaperDollMeleeWeapon.Clear();
		}
		else
		{
			mPaperDollMeleeWeapon = new List<GameObject>();
		}
	}

	public void SetDamageOverTime(float damagePerTick, float tickFrequency, float durationOfEffect)
	{
		mDOTcurrentTime = 0f;
		mDOTdamage = damagePerTick;
		mDOTfrequency = tickFrequency;
		mDOTduration = durationOfEffect;
		mHasDOT = true;
	}

	public void SetSpeedModifier(float speedMod, float durationOfEffect)
	{
		controller.speedModifier = speedMod;
		mSpeedSnareDuration = durationOfEffect;
		mHasSpeedSnare = true;
	}

	public void MaterialColorFadeInOut(Color color, float fadeInTime, float holdTime, float fadeOutTime)
	{
		ProceduralShaderManager.postShaderEvent(new DiffuseFadeInOutShaderEvent(controlledObject, color, fadeInTime, holdTime, fadeOutTime));
	}

	public void MaterialColorSetPermanentColor(Color color)
	{
		ProceduralShaderManager.postShaderEvent(new DiffusePermShaderEvent(controlledObject, color));
	}

	public void MaterialColorFlicker(Color color, float fadeInTime, float holdTime, float fadeOutTime, float durationOfEffect)
	{
		ProceduralShaderManager.postShaderEvent(new DiffuseFlickerShaderEvent(controlledObject, color, fadeInTime, holdTime, fadeOutTime, durationOfEffect));
	}

	public bool CheckIfTargetValid(Character target)
	{
		if (target == null || target.health <= 0f)
		{
			return false;
		}
		return true;
	}

	public bool CheckIfTargetValidForRangedAttack(Character target)
	{
		if (!CheckIfTargetValid(target))
		{
			return false;
		}
		if (!bowAttackHitGameRange.Contains(target.position.z))
		{
			return false;
		}
		return true;
	}

	public void ApplyIceEffect(float duration)
	{
		float num = 0.5f;
		if (!(controller.speedModifier < num))
		{
			string abilityID = "Lethargy";
			if (Singleton<PlayModesManager>.instance.selectedMode == "zombies")
			{
				abilityID = "LethargyZM";
			}
			float fadeInTime = float.Parse(Singleton<AbilitiesDatabase>.instance.GetAttribute(abilityID, "ColorEffectFadeIn"));
			float fadeOutTime = float.Parse(Singleton<AbilitiesDatabase>.instance.GetAttribute(abilityID, "ColorEffectFadeOut"));
			GameObject prefab = Resources.Load("FX/Lethargy") as GameObject;
			UnityEngine.Object.Destroy(controller.InstantiateObjectOnJoint(prefab, "head_effect"), duration);
			SetSpeedModifier(num, duration);
			Color color = new Color((float)int.Parse(Singleton<AbilitiesDatabase>.instance.GetAttribute(abilityID, "Red")) / 255f, (float)int.Parse(Singleton<AbilitiesDatabase>.instance.GetAttribute(abilityID, "Green")) / 255f, (float)int.Parse(Singleton<AbilitiesDatabase>.instance.GetAttribute(abilityID, "Blue")) / 255f);
			MaterialColorFadeInOut(color, fadeInTime, duration, fadeOutTime);
		}
	}

	public void SpawnOffspring(string friendID, int count)
	{
		for (int i = 0; i < count; i++)
		{
			Vector3 spawnPos = position;
			spawnPos.z += UnityEngine.Random.Range(-50f, 100f);
			spawnPos.x = WeakGlobalInstance<CharactersManager>.instance.GetBestSpawnXPos(spawnPos, WeakGlobalInstance<Smithy>.instance.helperSpawnArea.size.x, CharactersManager.ELanePreference.any, isEnemy, false, false);
			if (isEnemy)
			{
				Enemy enemy = WeakGlobalInstance<WaveManager>.instance.ConstructEnemy(friendID, WeakGlobalInstance<WaveManager>.instance.enemiesSpawnArea.size.x / 2f, spawnPos, true);
				if (enemy != null)
				{
					enemy.TryKnockback(100);
					WeakGlobalInstance<CharactersManager>.instance.AddCharacter(enemy);
					WeakGlobalInstance<WaveManager>.instance.totalEnemies++;
				}
			}
			else
			{
				Helper helper = WeakGlobalInstance<Smithy>.instance.ForceSpawn(friendID);
				if (helper != null)
				{
					helper.position = spawnPos;
					helper.TryKnockback(100);
				}
			}
		}
	}

	private void UpdateAutoRecovery()
	{
		if (health != 0f && autoHealthRecovery != 0f)
		{
			if (mAutoRecoveryDelay > 0f)
			{
				mAutoRecoveryDelay -= Time.deltaTime;
			}
			if (mAutoRecoveryDelay <= 0f || !isPlayer)
			{
				health += Time.deltaTime * autoHealthRecovery;
			}
		}
	}

	protected bool IsInMeleeRangeOfOpponent(bool thatCanAttack)
	{
		return meleeAttackRange > 0f && WeakGlobalInstance<CharactersManager>.instance.IsCharacterInRange((!thatCanAttack || controller.isMoving) ? meleeAttackGameRange : meleeAttackHitGameRange, !isEnemy, isGateRusher, !thatCanAttack, false, true);
	}

	protected bool IsInBowRangeOfOpponent()
	{
		return bowAttackRange > 0f && WeakGlobalInstance<CharactersManager>.instance.IsCharacterInRange(bowAttackGameRange, !isEnemy, isGateRusher, true, false, true);
	}

	protected bool IsInRangeOfHurtAlly()
	{
		return IsInRangeOfHurtAlly(true);
	}

	protected bool IsInRangeOfHurtAlly(bool includeDecayingCharacters)
	{
		return bowAttackRange > 0f && WeakGlobalInstance<CharactersManager>.instance.IsCharacterInRange(buffEffectGameRange, isEnemy, false, true, true, includeDecayingCharacters);
	}

	protected void ActivateHealthBar()
	{
		if (mHealthBar == null)
		{
			mHealthBar = new MiniHealthBar((!isEnemy) ? new Color(0f, 1f, 0f) : new Color(1f, 0f, 0f), isBase);
		}
	}

	protected void AttackByExploding()
	{
		mStats.health = 0f;
		Die("dieexplode");
		mStats.meleeAttackRange = 250f;
		OnAttackDelivery();
	}

	private void UpdateAttack()
	{
		if (mMeleeAttackDelay > 0f)
		{
			mInMeleeAttackCycle = true;
			mMeleeAttackDelay = Mathf.Max(0f, mMeleeAttackDelay - Time.deltaTime * controller.speedModifier);
		}
		else
		{
			mInMeleeAttackCycle = false;
		}
		if (mRangedAttackDelay > 0f)
		{
			mInRangedAttackCycle = true;
			mRangedAttackDelay = Mathf.Max(0f, mRangedAttackDelay - Time.deltaTime * controller.speedModifier);
		}
		else
		{
			mInRangedAttackCycle = false;
		}
	}

	private void UpdateMoveDelay()
	{
		if (mMoveDelay > 0f && isEffectivelyIdle && mMeleeAttackDelay <= 0f && mRangedAttackDelay <= 0f)
		{
			mMoveDelay = Mathf.Max(0f, mMoveDelay - Time.deltaTime * controller.speedModifier);
		}
	}

	private void OnAttackDelivery()
	{
		mMeleeAttackDelivered = true;
		List<Character> charactersInRange = WeakGlobalInstance<CharactersManager>.instance.GetCharactersInRange(meleeAttackHitGameRange, !isEnemy);
		int num = knockbackPower;
		float num2 = meleeFreeze;
		foreach (Character item in charactersInRange)
		{
			if (!CheckIfTargetValid(item) || item.isFlying)
			{
				continue;
			}
			float num3 = randomCriticalDamage;
			EAttackType eAttackType = EAttackType.Blunt;
			if (meleeWeaponIsABlade)
			{
				eAttackType = ((!(num3 > 0f)) ? EAttackType.Blade : EAttackType.BladeCritical);
			}
			else if (num3 > 0f)
			{
				eAttackType = EAttackType.BluntCritical;
			}
			else if (exploseOnMelee)
			{
				eAttackType = EAttackType.Explosion;
			}
			item.RecievedAttack(eAttackType, meleeDamage + randomCriticalDamage);
			item.TryKnockback(num);
			num -= 100;
			if (num2 > 0f)
			{
				item.ApplyIceEffect(num2);
				num2 *= 0.6f;
			}
			if (isPlayer)
			{
				switch (eAttackType)
				{
				case EAttackType.Blade:
				case EAttackType.BladeCritical:
					Singleton<PlayStatistics>.instance.stats.heroUsedHisMeleeAttack = true;
					break;
				case EAttackType.Arrow:
					Singleton<PlayStatistics>.instance.stats.heroUsedHisRangedAttack = true;
					break;
				}
			}
		}
	}

	private void OnRangedAttackDelivery()
	{
		if (CheckIfTargetValidForRangedAttack(singleAttackTarget))
		{
			mRangedAttackDelivered = true;
			controller.SetArrowVisible(false);
			WeakGlobalInstance<ProjectileManager>.instance.SpawnProjectile(bowProjectile, bowDamage, this, singleAttackTarget, controller.autoPaperdoll.GetJointPosition("projectile_spawn"));
		}
		else
		{
			controller.Idle();
		}
	}

	protected void OnCastHeal()
	{
		mRangedAttackDelivered = true;
		List<Character> list = new List<Character>();
		List<Character> charactersInRange = WeakGlobalInstance<CharactersManager>.instance.GetCharactersInRange(bowAttackHitGameRange, isEnemy);
		float num = health / maxHealth;
		foreach (Character item in charactersInRange)
		{
			if (item.health >= item.maxHealth)
			{
				list.Add(item);
				continue;
			}
			if (item.autoHealthRecovery < 0f)
			{
				list.Add(item);
				continue;
			}
			float num2 = item.health / item.maxHealth;
			if (num2 < num)
			{
				num = num2;
			}
		}
		foreach (Character item2 in list)
		{
			charactersInRange.Remove(item2);
		}
		list.Clear();
		foreach (Character item3 in charactersInRange)
		{
			float num3 = item3.health / item3.maxHealth;
			if (num3 > num + 0.4f && num3 > num + 0.2f)
			{
				list.Add(item3);
			}
		}
		foreach (Character item4 in list)
		{
			charactersInRange.Remove(item4);
		}
		list.Clear();
		float num4 = ((!isEnemy) ? float.MinValue : float.MaxValue);
		num = 1f;
		foreach (Character item5 in charactersInRange)
		{
			float num5 = item5.health / item5.maxHealth;
			if (!item5.isPlayer && !item5.isGateRusher && ((isEnemy && item5.position.z < num4) || (!isEnemy && item5.position.z > num4)) && num5 <= num + 0.4f)
			{
				num4 = item5.position.z;
				if (num5 < num)
				{
					num = num5;
				}
			}
		}
		foreach (Character item6 in charactersInRange)
		{
			if (Mathf.Abs(item6.position.z - num4) > 35f)
			{
				list.Add(item6);
			}
			else if (item6.health / item6.maxHealth > num + 0.2f)
			{
				list.Add(item6);
			}
		}
		foreach (Character item7 in list)
		{
			charactersInRange.Remove(item7);
		}
		list.Clear();
		Character character = null;
		foreach (Character item8 in charactersInRange)
		{
			if (character == null || item8.health < character.health)
			{
				character = item8;
			}
		}
		if (character == null)
		{
			character = ((isEnemy || !(Mathf.Abs(position.z - WeakGlobalSceneBehavior<InGameImpl>.instance.hero.position.z) <= bowAttackRange)) ? this : WeakGlobalSceneBehavior<InGameImpl>.instance.hero);
		}
		controller.SetArrowVisible(false);
		WeakGlobalInstance<ProjectileManager>.instance.SpawnProjectile(bowProjectile, bowDamage, this, character, controller.autoPaperdoll.GetJointPosition("projectile_spawn"));
	}

	protected void OnCastSpawnAllies()
	{
		if (spawnFriendID == null || spawnFriendID == string.Empty)
		{
			Debug.Log("WARNING: The 'spawnFriendID' was not specified.");
			return;
		}
		mRangedAttackDelivered = true;
		SpawnOffspring(spawnFriendID, 1);
	}

	protected void SetRangeAttackMode(bool inRangeAttack)
	{
		if (rangedWeaponPrefab != null)
		{
			LegacyActivation.SetRecursively(rangedWeaponPrefab, inRangeAttack);
		}
		if (ProjectileManager.ProjectileNeedsBothHands(bowProjectile))
		{
			foreach (GameObject item in mPaperDollMeleeWeapon)
			{
				if (item != null)
				{
					LegacyActivation.SetRecursively(item, !inRangeAttack);
				}
			}
		}
		if (ProjectileManager.ProjectileShownWhileAiming(bowProjectile))
		{
			controller.SetArrowVisible(inRangeAttack, bowProjectile);
		}
		controller.SetUseRangedAttackIdle(inRangeAttack);
	}

	private void UpdateAiming()
	{
		bool flag = controller.currentAnimation == "rangedattack" && !mRangedAttackDelivered;
		if (flag || controller.currentAnimation == "rangedattackidle")
		{
			if (!CheckIfTargetValidForRangedAttack(singleAttackTarget))
			{
				if (flag)
				{
					controller.Idle();
					return;
				}
				singleAttackTarget = WeakGlobalInstance<CharactersManager>.instance.GetBestRangedAttackTarget(this, bowAttackExtGameRange);
				if (singleAttackTarget == null)
				{
					controller.ResetAimAngle();
					controller.ResetFacingAngle();
					return;
				}
			}
			Vector3 jointPosition = singleAttackTarget.controller.autoPaperdoll.GetJointPosition("impact_target");
			controller.FaceTowards(jointPosition);
			if (controller.autoPaperdoll.HasJoint("aim_angle"))
			{
				controller.AimTowards(bowProjectile, jointPosition);
			}
		}
		else if (hasRangedAttack && controller.currentAnimation != "rangedattack")
		{
			controller.ResetAimAngle();
			if (controller.currentAnimation != "idle")
			{
				controller.ResetFacingAngle();
			}
		}
	}
}
