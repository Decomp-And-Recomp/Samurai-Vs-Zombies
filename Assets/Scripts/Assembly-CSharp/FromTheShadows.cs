using System.Collections.Generic;
using UnityEngine;

public class FromTheShadows : MonoBehaviour
{
	public BoxCollider attackArea;

	public FromTheShadowsAttack attackEffectPrefab;

	private List<Character> mActiveAttackTargets = new List<Character>();

	private GameRange mAttackRange;

	private void Start()
	{
		if (!Singleton<Profile>.instance.fromTheShadows)
		{
			LegacyActivation.SetRecursively(base.gameObject, false);
		}
		else
		{
			mAttackRange = new GameRange(attackArea.bounds.min.z, attackArea.bounds.max.z);
		}
	}

	private void Update()
	{
		for (int num = mActiveAttackTargets.Count - 1; num >= 0; num--)
		{
			if (mActiveAttackTargets[num] == null || mActiveAttackTargets[num].health <= 0f)
			{
				mActiveAttackTargets.RemoveAt(num);
			}
		}
		List<Character> charactersInRange = WeakGlobalInstance<CharactersManager>.instance.GetCharactersInRange(mAttackRange, true);
		foreach (Character item in charactersInRange)
		{
			if (item.health > 0f && item.isInKnockback && !mActiveAttackTargets.Contains(item))
			{
				mActiveAttackTargets.Add(item);
				GameObject gameObject = Object.Instantiate(attackEffectPrefab.gameObject, item.position, Quaternion.identity) as GameObject;
				gameObject.GetComponent<FromTheShadowsAttack>().target = item;
			}
		}
	}
}
