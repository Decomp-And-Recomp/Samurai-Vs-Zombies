using System.Collections.Generic;
using UnityEngine;

public class Bell
{
	private GameObject mBellRinger;

	private CharacterModelController mBellRingerController;

	private int mBellLevel;

	private GameRange mRange;

	private float mDamage;

	private float mAttackFrequency = 5f;

	private float mTimerSinceBeginSwing;

	private bool mAgainstPlayer;

	public Bell(GameObject bellRingerObject)
	{
		switch (Singleton<PlayModesManager>.instance.selectedMode)
		{
		case "classic":
			mBellLevel = Singleton<Profile>.instance.bellLevel;
			break;
		case "zombies":
			mBellLevel = WeakGlobalInstance<WaveManager>.instance.bellLevel;
			mAgainstPlayer = true;
			break;
		}
		mBellRinger = bellRingerObject;
		if (mBellRinger == null)
		{
			Debug.Log("ERROR: missing Bell ringer reference.");
			mBellLevel = 0;
		}
		if (mBellLevel == 0)
		{
			if (mBellRinger != null)
			{
				LegacyActivation.SetRecursively(mBellRinger, false);
			}
			return;
		}
		mBellRingerController = mBellRinger.GetComponent<CharacterModelController>();
		if (mBellRingerController == null)
		{
			mBellRingerController = mBellRinger.AddComponent<CharacterModelController>();
		}
		GetBellStats();
	}

	public void Update()
	{
		if (mBellLevel == 0)
		{
			return;
		}
		if (WeakGlobalSceneBehavior<InGameImpl>.instance.playerWon && mBellRingerController.isEffectivelyIdle)
		{
			mBellRingerController.PlayVictoryAnim();
			return;
		}
		mTimerSinceBeginSwing += Time.deltaTime;
		if (!(mTimerSinceBeginSwing < mAttackFrequency) && mBellRingerController.isEffectivelyIdle && WeakGlobalInstance<CharactersManager>.instance.IsCharacterInRange(mRange, !mAgainstPlayer, false, true, false, true))
		{
			PlayRingerSwing();
		}
	}

	private void GetBellStats()
	{
		SDFTreeNode sDFTreeNode = SingletonMonoBehaviour<ResourcesManager>.instance.Open("Registry/Bell");
		SDFTreeNode sDFTreeNode2 = sDFTreeNode.to(string.Format("{0:000}", mBellLevel));
		if (sDFTreeNode2 == null)
		{
			Debug.Log("ERROR: Unable to find proper bell stats for level " + mBellLevel);
			return;
		}
		float num = float.Parse(sDFTreeNode2["radius"]);
		mDamage = float.Parse(sDFTreeNode2["damage"]);
		mAttackFrequency = float.Parse(sDFTreeNode["attackFrequency"]);
		float z = mBellRinger.transform.position.z;
		mRange = new GameRange(z - num, z + num);
	}

	private void PlayRingerSwing()
	{
		mBellRingerController.PerformSpecialAction("ring", OnBellDamage, mAttackFrequency);
		mTimerSinceBeginSwing = 0f;
	}

	private void OnBellDamage()
	{
		List<Character> charactersInRange = WeakGlobalInstance<CharactersManager>.instance.GetCharactersInRange(mRange, !mAgainstPlayer);
		foreach (Character item in charactersInRange)
		{
			item.RecievedAttack(EAttackType.Sonic, mDamage);
		}
	}
}
