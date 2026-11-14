using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AutoPaperdoll))]
public class AnimationEffects : MonoBehaviour
{
	private class AnimEndedFunc
	{
		internal AnimationState parentState;

		internal Action funcToCall;
	}

	public bool playRandomAnimAtStartup;

	public float cameraShakeIntensityMult = 1f;

	private List<Transform> mHiddenBodyParts;

	private List<AnimEndedFunc> mAnimEndedFunctions;

	private AutoPaperdoll mAutoPaperdoll;

	private void Start()
	{
		mAutoPaperdoll = GetComponent<AutoPaperdoll>();
		if ((mHiddenBodyParts == null || mHiddenBodyParts.Count == 0) && (mAnimEndedFunctions == null || mAnimEndedFunctions.Count == 0))
		{
			base.enabled = false;
		}
		if (!playRandomAnimAtStartup)
		{
			return;
		}
		Animation animation = base.GetComponent<Animation>();
		if (!(animation != null))
		{
			return;
		}
		animation.playAutomatically = false;
		int num = UnityEngine.Random.Range(0, animation.GetClipCount());
		foreach (AnimationState item in animation)
		{
			if (num == 0 && item != null && item.clip != null)
			{
				animation.Play(item.clip.name);
				break;
			}
			num--;
		}
	}

	public void HideBodyPart(string theJointLabel)
	{
		if (mHiddenBodyParts == null)
		{
			mHiddenBodyParts = new List<Transform>();
		}
		AutoPaperdoll.LabeledJoint jointData = mAutoPaperdoll.GetJointData(theJointLabel);
		Transform joint = jointData.joint;
		if (!mHiddenBodyParts.Contains(joint))
		{
			mHiddenBodyParts.Add(joint);
		}
		joint.localScale = Vector3.zero;
		base.enabled = true;
	}

	public void HideBodyPartThisAnimOnly(AnimationEvent animEvent)
	{
		if (animEvent != null && !(animEvent.animationState == null))
		{
			HideBodyPart(animEvent.stringParameter);
			AnimEndedFunc animEndedFunc = new AnimEndedFunc();
			animEndedFunc.parentState = animEvent.animationState;
			string bodyPartName = animEvent.stringParameter;
			animEndedFunc.funcToCall = delegate
			{
				RestoreHiddenBodyPart(bodyPartName);
			};
			if (mAnimEndedFunctions == null)
			{
				mAnimEndedFunctions = new List<AnimEndedFunc>();
			}
			mAnimEndedFunctions.Add(animEndedFunc);
			base.enabled = true;
		}
	}

	public void RestoreHiddenBodyPart(string theJointLabel)
	{
		if (mHiddenBodyParts != null)
		{
			AutoPaperdoll.LabeledJoint jointData = mAutoPaperdoll.GetJointData(theJointLabel);
			Transform joint = jointData.joint;
			mHiddenBodyParts.Remove(joint);
		}
	}

	public void SpawnEffectPrefab(AnimationEvent theAnimEvent)
	{
		GameObject gameObject = theAnimEvent.objectReferenceParameter as GameObject;
		if (!gameObject)
		{
			Debug.LogWarning("Must specify a Prefab to spawn for SpawnEffectPrefab animation event");
			return;
		}
		GameObject gameObject2 = mAutoPaperdoll.InstantiateObjectOnJoint(gameObject, theAnimEvent.stringParameter, false, false);
		if (gameObject2 != null)
		{
			gameObject2.SendMessage("SpawnedFrom", base.gameObject, SendMessageOptions.DontRequireReceiver);
		}
	}

	public void SpawnEffectPrefabIgnoreRotation(AnimationEvent theAnimEvent)
	{
		GameObject gameObject = theAnimEvent.objectReferenceParameter as GameObject;
		if (!gameObject)
		{
			Debug.LogWarning("Must specify a Prefab to spawn for SpawnEffectPrefab animation event");
			return;
		}
		GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, mAutoPaperdoll.GetJointPosition(theAnimEvent.stringParameter), Quaternion.identity) as GameObject;
		if (gameObject2 != null)
		{
			gameObject2.SendMessage("SpawnedFrom", base.gameObject, SendMessageOptions.DontRequireReceiver);
		}
	}

	public void AttachEffectPrefabToJoint(AnimationEvent theAnimEvent)
	{
		GameObject gameObject = theAnimEvent.objectReferenceParameter as GameObject;
		if (!gameObject)
		{
			Debug.LogWarning("Must specify a Prefab to spawn for AttachEffectPrefabToJoint animation event");
			return;
		}
		GameObject gameObject2 = mAutoPaperdoll.InstantiateObjectOnJoint(gameObject, theAnimEvent.stringParameter, true, false);
		if (gameObject2 != null)
		{
			gameObject2.SendMessage("SpawnedFrom", base.gameObject, SendMessageOptions.DontRequireReceiver);
		}
	}

	public void AttachEffectPrefabToJointThisAnimOnly(AnimationEvent theAnimEvent)
	{
		GameObject gameObject = theAnimEvent.objectReferenceParameter as GameObject;
		if (!gameObject)
		{
			Debug.LogWarning("Must specify a Prefab to spawn for AttachEffectPrefabToJoint animation event");
			return;
		}
		AutoPaperdoll.LabeledJoint jointData = mAutoPaperdoll.GetJointData(theAnimEvent.stringParameter);
		if (jointData != null && jointData.joint != null)
		{
			for (int i = 0; i < jointData.joint.childCount; i++)
			{
				if (jointData.joint.GetChild(i).name == gameObject.name + "(Clone)")
				{
					return;
				}
			}
		}
		GameObject newObject = mAutoPaperdoll.InstantiateObjectOnJoint(gameObject, theAnimEvent.stringParameter, true, false);
		if (newObject != null)
		{
			newObject.SendMessage("SpawnedFrom", base.gameObject, SendMessageOptions.DontRequireReceiver);
			AnimEndedFunc animEndedFunc = new AnimEndedFunc();
			animEndedFunc.parentState = theAnimEvent.animationState;
			animEndedFunc.funcToCall = delegate
			{
				internal_killEffect(newObject);
			};
			if (mAnimEndedFunctions == null)
			{
				mAnimEndedFunctions = new List<AnimEndedFunc>();
			}
			mAnimEndedFunctions.Add(animEndedFunc);
			base.enabled = true;
		}
	}

	public void ShakeCamera(float theShakeIntensity)
	{
		CameraShaker.RequestShake(base.transform.position, theShakeIntensity * cameraShakeIntensityMult);
	}

	public void MeltThisGameObject()
	{
		ModelMelter.MeltGameObject(base.gameObject, DeleteThisGameObject);
	}

	public void DeleteThisGameObject()
	{
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public void StopAllParticleEmitters()
	{
		LegacyParticleEmitter[] componentsInChildren = GetComponentsInChildren<LegacyParticleEmitter>();
		LegacyParticleEmitter[] array = componentsInChildren;
		foreach (LegacyParticleEmitter particleEmitter in array)
		{
			if (particleEmitter != null)
			{
				particleEmitter.emit = false;
			}
		}
	}

	private void LateUpdate()
	{
		bool flag = mHiddenBodyParts != null && mHiddenBodyParts.Count > 0;
		bool flag2 = mAnimEndedFunctions != null && mAnimEndedFunctions.Count > 0;
		if (!flag && !flag2)
		{
			mHiddenBodyParts = null;
			mAnimEndedFunctions = null;
			base.enabled = false;
			return;
		}
		if (flag)
		{
			foreach (Transform mHiddenBodyPart in mHiddenBodyParts)
			{
				mHiddenBodyPart.localScale = Vector3.zero;
			}
		}
		if (!flag2)
		{
			return;
		}
		for (int num = mAnimEndedFunctions.Count - 1; num >= 0; num--)
		{
			AnimEndedFunc animEndedFunc = mAnimEndedFunctions[num];
			if (animEndedFunc == null)
			{
				mAnimEndedFunctions.RemoveAt(num);
			}
			else if (animEndedFunc.parentState == null || !animEndedFunc.parentState.enabled)
			{
				if (animEndedFunc.funcToCall != null)
				{
					animEndedFunc.funcToCall();
				}
				mAnimEndedFunctions.RemoveAt(num);
			}
		}
	}

	private void internal_killEffect(GameObject effect)
	{
		if (!(effect == null))
		{
			EffectKiller effectKiller = effect.AddComponent<EffectKiller>();
			effectKiller.autoDeleteAfterTime = float.Epsilon;
			effectKiller.deleteWhenAllChildrenDead = true;
			effectKiller.letParticlesFinishForTimedDelete = true;
		}
	}
}
