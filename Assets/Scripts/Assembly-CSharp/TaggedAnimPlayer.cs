using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animation))]
[AddComponentMenu("Framework/TaggedAnimPlayer")]
public class TaggedAnimPlayer : MonoBehaviour
{
	[Serializable]
	public class AnimOverrideSettings
	{
		public AnimationClip clip;

		public bool overrideBlendSpeed;

		public float blendSpeed = 0.3f;

		public Transform jointMaskRoot;

		public bool onlyUseSingleJoint;
	}

	private class MaskedAnimData
	{
		public Transform firstJoint;

		public bool onlyUseFirstJoint;

		public bool enabled = true;
	}

	public delegate void TaggedAnimCallback(string theAnimName);

	private const int kBaseAnimLayer = -10;

	private const int kActionAnimLayer = 0;

	private const int kFirstEffectAnimLayer = 10;

	public Animation jointAnimation;

	public float defaultBlendSpeed = 0.3f;

	public AnimOverrideSettings[] specificAnimSettings = new AnimOverrideSettings[0];

	public bool alsoPlayOnAllChildren;

	[NonSerialized]
	public bool autoReturnToBaseAnim = true;

	[NonSerialized]
	public TaggedAnimCallback animStartedCallback;

	[NonSerialized]
	public TaggedAnimCallback animDoneCallback;

	[NonSerialized]
	public TaggedAnimCallback currAnimDoneCallback;

	[NonSerialized]
	public Action currAnimChangedCallback;

	[HideInInspector]
	public float animFrame;

	[NonSerialized]
	public float actionAnimFadeOutSpeed;

	[HideInInspector]
	public List<Animation> extraAnimPlayers = new List<Animation>();

	[HideInInspector]
	public List<TaggedAnimPlayer> extraTaggedAnimPlayers = new List<TaggedAnimPlayer>();

	private Animation mTaggedAnimation;

	private AnimationState mBaseAnim;

	private AnimationState mBaseJointAnim;

	private AnimationState mActionAnim;

	private AnimationState mActionJointAnim;

	private List<AnimationState> mActiveBaseAnims = new List<AnimationState>();

	private List<AnimationState> mActiveActionAnims = new List<AnimationState>();

	private int mNextEffectLayerToUse = 10;

	private Dictionary<string, MaskedAnimData> mMaskedAnims = new Dictionary<string, MaskedAnimData>();

	private Dictionary<string, float> mDefaultBlendSpeeds = new Dictionary<string, float>();

	public AnimationState this[string name]
	{
		get
		{
			return mTaggedAnimation[name];
		}
	}

	public AnimationState actionAnimState
	{
		get
		{
			return mActionAnim;
		}
	}

	public AnimationState baseAnimState
	{
		get
		{
			return mBaseAnim;
		}
	}

	public AnimationState currAnimState
	{
		get
		{
			return (!(mActionAnim == null)) ? actionAnimState : baseAnimState;
		}
	}

	public AnimationState actionJointAnimState
	{
		get
		{
			return mActionJointAnim;
		}
	}

	public AnimationState baseJointAnimState
	{
		get
		{
			return mBaseJointAnim;
		}
	}

	public AnimationState currJointAnimState
	{
		get
		{
			return (!(mActionJointAnim == null)) ? actionJointAnimState : baseJointAnimState;
		}
	}

	public string actionAnim
	{
		get
		{
			return (!(mActionAnim == null)) ? mActionAnim.clip.name : string.Empty;
		}
	}

	public string baseAnim
	{
		get
		{
			return (!(mBaseAnim == null)) ? mBaseAnim.clip.name : string.Empty;
		}
	}

	public string currAnim
	{
		get
		{
			return (!(mActionAnim == null)) ? actionAnim : baseAnim;
		}
	}

	public float currAnimSpeed
	{
		get
		{
			return currAnimState.speed;
		}
		set
		{
			if (mActionAnim == null)
			{
				foreach (AnimationState mActiveBaseAnim in mActiveBaseAnims)
				{
					if (mActiveBaseAnim != null)
					{
						mActiveBaseAnim.speed = value;
					}
				}
			}
			else
			{
				foreach (AnimationState mActiveActionAnim in mActiveActionAnims)
				{
					if (mActiveActionAnim != null)
					{
						mActiveActionAnim.speed = value;
					}
				}
			}
			if (!alsoPlayOnAllChildren)
			{
				return;
			}
			foreach (TaggedAnimPlayer extraTaggedAnimPlayer in extraTaggedAnimPlayers)
			{
				if (extraTaggedAnimPlayer == null)
				{
					FlagNullChildPlayer();
				}
				else
				{
					extraTaggedAnimPlayer.currAnimSpeed = value;
				}
			}
		}
	}

	public float baseAnimSpeed
	{
		get
		{
			return baseAnimState.speed;
		}
		set
		{
			foreach (AnimationState mActiveBaseAnim in mActiveBaseAnims)
			{
				if (mActiveBaseAnim != null)
				{
					mActiveBaseAnim.speed = value;
				}
			}
			if (!alsoPlayOnAllChildren)
			{
				return;
			}
			foreach (TaggedAnimPlayer extraTaggedAnimPlayer in extraTaggedAnimPlayers)
			{
				if (extraTaggedAnimPlayer == null)
				{
					FlagNullChildPlayer();
				}
				else
				{
					extraTaggedAnimPlayer.baseAnimSpeed = value;
				}
			}
		}
	}

	public float currAnimTime
	{
		get
		{
			return currAnimState.time;
		}
		set
		{
			if (mActionAnim == null)
			{
				foreach (AnimationState mActiveBaseAnim in mActiveBaseAnims)
				{
					if (mActiveBaseAnim != null)
					{
						mActiveBaseAnim.time = value;
					}
				}
			}
			else
			{
				foreach (AnimationState mActiveActionAnim in mActiveActionAnims)
				{
					if (mActiveActionAnim != null)
					{
						mActiveActionAnim.time = value;
					}
				}
			}
			if (!alsoPlayOnAllChildren)
			{
				return;
			}
			foreach (TaggedAnimPlayer extraTaggedAnimPlayer in extraTaggedAnimPlayers)
			{
				if (extraTaggedAnimPlayer == null)
				{
					FlagNullChildPlayer();
				}
				else
				{
					extraTaggedAnimPlayer.currAnimTime = value;
				}
			}
		}
	}

	public float baseAnimTime
	{
		get
		{
			return baseAnimState.time;
		}
		set
		{
			foreach (AnimationState mActiveBaseAnim in mActiveBaseAnims)
			{
				if (mActiveBaseAnim != null)
				{
					mActiveBaseAnim.time = value;
				}
			}
			if (!alsoPlayOnAllChildren)
			{
				return;
			}
			foreach (TaggedAnimPlayer extraTaggedAnimPlayer in extraTaggedAnimPlayers)
			{
				if (extraTaggedAnimPlayer == null)
				{
					FlagNullChildPlayer();
				}
				else
				{
					extraTaggedAnimPlayer.baseAnimTime = value;
				}
			}
		}
	}

	public float currAnimNormalizedTime
	{
		get
		{
			return currAnimState.normalizedTime;
		}
		set
		{
			if (mActionAnim == null)
			{
				foreach (AnimationState mActiveBaseAnim in mActiveBaseAnims)
				{
					if (mActiveBaseAnim != null)
					{
						mActiveBaseAnim.normalizedTime = value;
					}
				}
			}
			else
			{
				foreach (AnimationState mActiveActionAnim in mActiveActionAnims)
				{
					if (mActiveActionAnim != null)
					{
						mActiveActionAnim.normalizedTime = value;
					}
				}
			}
			if (!alsoPlayOnAllChildren)
			{
				return;
			}
			foreach (TaggedAnimPlayer extraTaggedAnimPlayer in extraTaggedAnimPlayers)
			{
				if (extraTaggedAnimPlayer == null)
				{
					FlagNullChildPlayer();
				}
				else
				{
					extraTaggedAnimPlayer.currAnimNormalizedTime = value;
				}
			}
		}
	}

	public float baseAnimNormalizedTime
	{
		get
		{
			return baseAnimState.normalizedTime;
		}
		set
		{
			foreach (AnimationState mActiveBaseAnim in mActiveBaseAnims)
			{
				if (mActiveBaseAnim != null)
				{
					mActiveBaseAnim.normalizedTime = value;
				}
			}
			if (!alsoPlayOnAllChildren)
			{
				return;
			}
			foreach (TaggedAnimPlayer extraTaggedAnimPlayer in extraTaggedAnimPlayers)
			{
				if (extraTaggedAnimPlayer == null)
				{
					FlagNullChildPlayer();
				}
				else
				{
					extraTaggedAnimPlayer.baseAnimNormalizedTime = value;
				}
			}
		}
	}

	public bool paused
	{
		get
		{
			return !mTaggedAnimation.enabled;
		}
		set
		{
			if (mTaggedAnimation.enabled != value)
			{
				return;
			}
			SetAnimPlayerPaused(jointAnimation, value);
			SetAnimPlayerPaused(mTaggedAnimation, value);
			if (!alsoPlayOnAllChildren)
			{
				return;
			}
			foreach (TaggedAnimPlayer extraTaggedAnimPlayer in extraTaggedAnimPlayers)
			{
				if (extraTaggedAnimPlayer == null)
				{
					FlagNullChildPlayer();
				}
				else
				{
					extraTaggedAnimPlayer.paused = value;
				}
			}
			foreach (Animation extraAnimPlayer in extraAnimPlayers)
			{
				if (extraAnimPlayer == null)
				{
					FlagNullChildPlayer();
				}
				else
				{
					SetAnimPlayerPaused(extraAnimPlayer, value);
				}
			}
		}
	}

	private void Awake()
	{
		mTaggedAnimation = base.GetComponent<Animation>();
		TaggedAnimPlayer[] componentsInChildren = GetComponentsInChildren<TaggedAnimPlayer>(true);
		TaggedAnimPlayer[] array = componentsInChildren;
		foreach (TaggedAnimPlayer taggedAnimPlayer in array)
		{
			if (taggedAnimPlayer != this && taggedAnimPlayer != null)
			{
				extraTaggedAnimPlayers.Add(taggedAnimPlayer);
			}
		}
		Animation[] componentsInChildren2 = GetComponentsInChildren<Animation>(true);
		Animation[] array2 = componentsInChildren2;
		foreach (Animation theAnimationComponent in array2)
		{
			RegisterNewChildAnimationPlayer(theAnimationComponent);
		}
		if (specificAnimSettings != null && specificAnimSettings.Length > 0)
		{
			for (int k = 0; k < specificAnimSettings.Length; k++)
			{
				if (!(specificAnimSettings[k].clip != null))
				{
					continue;
				}
				if (specificAnimSettings[k].jointMaskRoot != null)
				{
					if (jointAnimation != null)
					{
						jointAnimation[specificAnimSettings[k].clip.name].AddMixingTransform(specificAnimSettings[k].jointMaskRoot, !specificAnimSettings[k].onlyUseSingleJoint);
					}
					MaskedAnimData maskedAnimData = new MaskedAnimData();
					maskedAnimData.enabled = true;
					maskedAnimData.firstJoint = specificAnimSettings[k].jointMaskRoot;
					maskedAnimData.onlyUseFirstJoint = specificAnimSettings[k].onlyUseSingleJoint;
					mMaskedAnims.Add(specificAnimSettings[k].clip.name, maskedAnimData);
				}
				if (specificAnimSettings[k].overrideBlendSpeed)
				{
					mDefaultBlendSpeeds.Add(specificAnimSettings[k].clip.name, specificAnimSettings[k].blendSpeed);
				}
			}
			specificAnimSettings = null;
		}
		actionAnimFadeOutSpeed = defaultBlendSpeed;
		if (jointAnimation != null)
		{
			mTaggedAnimation.wrapMode = jointAnimation.wrapMode;
			if (jointAnimation.playAutomatically && jointAnimation.clip != null)
			{
				SetBaseAnim(jointAnimation.clip.name);
			}
		}
		if (mTaggedAnimation.playAutomatically && mTaggedAnimation.clip != null)
		{
			SetBaseAnim(mTaggedAnimation.clip.name);
		}
	}

	private void OnDestroy()
	{
		OnAnimChanged();
	}

	public void RegisterNewChildAnimationPlayer(Animation theAnimationComponent)
	{
		if (theAnimationComponent == null || theAnimationComponent == mTaggedAnimation || theAnimationComponent == jointAnimation)
		{
			return;
		}
		TaggedAnimPlayer component = theAnimationComponent.GetComponent<TaggedAnimPlayer>();
		if (component != null)
		{
			if (!extraTaggedAnimPlayers.Contains(component))
			{
				extraTaggedAnimPlayers.Add(component);
			}
			return;
		}
		bool flag = false;
		foreach (TaggedAnimPlayer extraTaggedAnimPlayer in extraTaggedAnimPlayers)
		{
			if (extraTaggedAnimPlayer == null)
			{
				FlagNullChildPlayer();
			}
			else if (theAnimationComponent == extraTaggedAnimPlayer.mTaggedAnimation || theAnimationComponent == extraTaggedAnimPlayer.jointAnimation)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			extraAnimPlayers.Add(theAnimationComponent);
			if (theAnimationComponent.clip != null)
			{
				theAnimationComponent[theAnimationComponent.clip.name].layer = -10;
			}
		}
	}

	public List<string> GetAnimationNames()
	{
		List<string> list = new List<string>();
		foreach (AnimationState item in mTaggedAnimation)
		{
			if (!list.Contains(item.clip.name))
			{
				list.Add(item.clip.name);
			}
		}
		return list;
	}

	public void OnAutoPaperdollAdded(GameObject paperdoll)
	{
		if (!paperdoll.transform.IsChildOf(base.transform))
		{
			return;
		}
		Animation[] componentsInChildren = paperdoll.GetComponentsInChildren<Animation>(true);
		Animation[] array = componentsInChildren;
		foreach (Animation animation in array)
		{
			if (animation != null)
			{
				RegisterNewChildAnimationPlayer(animation);
			}
		}
	}

	public void PlayAnim(string theAnimName)
	{
		PlayAnim(theAnimName, GetDefaultBlendSpeed(theAnimName));
	}

	public void PlayAnim(string theAnimName, WrapMode theWrapMode)
	{
		PlayAnim(theAnimName, GetDefaultBlendSpeed(theAnimName), theWrapMode);
	}

	public void PlayAnim(string theAnimName, float theBlendSpeed)
	{
		PlayAnim(theAnimName, theBlendSpeed, 1f);
	}

	public void PlayAnim(string theAnimName, float theBlendSpeed, WrapMode theWrapMode)
	{
		PlayAnim(theAnimName, theBlendSpeed, theWrapMode, 1f);
	}

	public void PlayAnim(string theAnimName, float theBlendSpeed, float thePlaybackSpeed)
	{
		PlayAnim(theAnimName, theBlendSpeed, WrapMode.Default, thePlaybackSpeed);
	}

	public void PlayAnim(string theAnimName, float theBlendSpeed, WrapMode theWrapMode, float thePlaybackSpeed)
	{
		if (!(mTaggedAnimation[theAnimName] == null))
		{
			if (theAnimName == baseAnim)
			{
				baseAnimSpeed = thePlaybackSpeed;
				RevertToBaseAnim(theBlendSpeed);
				return;
			}
			UpdateBaseAnimPlayback(theAnimName);
			mActiveActionAnims.Clear();
			mActionAnim = PlayAnimOnPlayer(mTaggedAnimation, theAnimName, 0f, theWrapMode, thePlaybackSpeed, 0);
			mActiveActionAnims.Add(mActionAnim);
			PlayActionAnimOnChildren(theAnimName, theBlendSpeed, theWrapMode, thePlaybackSpeed);
			OnAnimChanged();
		}
	}

	public void SetBaseAnim(string theAnimName)
	{
		SetBaseAnim(theAnimName, GetDefaultBlendSpeed(theAnimName));
	}

	public void SetBaseAnim(string theAnimName, float theBlendSpeed)
	{
		AnimationState animationState = mTaggedAnimation[theAnimName];
		if (animationState == null || theAnimName == baseAnim)
		{
			return;
		}
		if (!IsLoopingAnim(animationState))
		{
			animationState.wrapMode = WrapMode.Loop;
		}
		float thePlaybackSpeed = ((!(mBaseAnim != null)) ? 1f : mBaseAnim.speed);
		mActiveBaseAnims.Clear();
		mBaseAnim = PlayAnimOnPlayer(mTaggedAnimation, theAnimName, 0f, animationState.wrapMode, thePlaybackSpeed, -10);
		mActiveBaseAnims.Add(mBaseAnim);
		AnimationState animationState2 = null;
		if (jointAnimation != null)
		{
			animationState2 = jointAnimation[theAnimName];
		}
		if (animationState2 != null)
		{
			bool flag = mActionJointAnim != null && mActionJointAnim.enabled && !IsUsingJointMask(mActionJointAnim.clip.name);
			if (!IsLoopingAnim(animationState2))
			{
				animationState2.wrapMode = WrapMode.Loop;
			}
			mBaseJointAnim = PlayAnimOnPlayer(jointAnimation, theAnimName, (!flag) ? theBlendSpeed : 0f, animationState.wrapMode, thePlaybackSpeed, -10);
			mActiveBaseAnims.Add(mBaseJointAnim);
			if (flag)
			{
				mBaseAnim.enabled = false;
				if (mBaseJointAnim != null)
				{
					mBaseJointAnim.weight = 0f;
				}
			}
		}
		if (alsoPlayOnAllChildren)
		{
			foreach (TaggedAnimPlayer extraTaggedAnimPlayer in extraTaggedAnimPlayers)
			{
				if (extraTaggedAnimPlayer == null)
				{
					FlagNullChildPlayer();
				}
				else
				{
					extraTaggedAnimPlayer.SetBaseAnim(theAnimName, theBlendSpeed);
				}
			}
			foreach (Animation extraAnimPlayer in extraAnimPlayers)
			{
				if (extraAnimPlayer == null)
				{
					FlagNullChildPlayer();
					continue;
				}
				AnimationState animationState3 = extraAnimPlayer[theAnimName];
				if (animationState3 != null && animationState3.clip != extraAnimPlayer.clip)
				{
					extraAnimPlayer.clip = animationState3.clip;
					if (!IsLoopingAnim(animationState3))
					{
						animationState3.clip.wrapMode = WrapMode.Loop;
					}
					PlayAnimOnPlayer(extraAnimPlayer, animationState3.clip.name, theBlendSpeed, animationState3.clip.wrapMode, thePlaybackSpeed, -10);
				}
			}
		}
		if (currAnimState == baseAnimState)
		{
			OnAnimChanged();
		}
	}

	public void ClearBaseAnim()
	{
		foreach (AnimationState mActiveBaseAnim in mActiveBaseAnims)
		{
			mActiveBaseAnim.enabled = false;
		}
		mActiveBaseAnims.Clear();
		mBaseAnim = null;
		mBaseJointAnim = null;
	}

	public void RevertToBaseAnim()
	{
		float value;
		if (mDefaultBlendSpeeds.TryGetValue(baseAnim, out value))
		{
			RevertToBaseAnim(value);
		}
		else
		{
			RevertToBaseAnim(actionAnimFadeOutSpeed);
		}
	}

	public void RevertToBaseAnim(float theFadeOutTime)
	{
		if (mActionAnim == null)
		{
			return;
		}
		if (mBaseJointAnim == null)
		{
			Debug.LogWarning("RevertToBaseAnim called with no base anim active");
			return;
		}
		string text = actionAnim;
		bool flag = false;
		if (mActionAnim != null)
		{
			mActionAnim.time = 0f;
			mActionAnim.weight = 0f;
			mActionAnim.enabled = false;
			flag = true;
			mActionAnim = null;
		}
		mActiveActionAnims.Clear();
		if (mActionJointAnim != null)
		{
			BlendOutActionAnim(jointAnimation, mActionJointAnim, theFadeOutTime);
			if (!IsUsingJointMask(mActionJointAnim.clip.name))
			{
				mBaseJointAnim.weight = 1f;
			}
			mActionJointAnim = null;
		}
		if (alsoPlayOnAllChildren)
		{
			foreach (TaggedAnimPlayer extraTaggedAnimPlayer in extraTaggedAnimPlayers)
			{
				if (extraTaggedAnimPlayer == null)
				{
					FlagNullChildPlayer();
				}
				else if (extraTaggedAnimPlayer.mBaseJointAnim != null && extraTaggedAnimPlayer.mBaseJointAnim.enabled)
				{
					extraTaggedAnimPlayer.RevertToBaseAnim();
				}
			}
			foreach (Animation extraAnimPlayer in extraAnimPlayers)
			{
				if (extraAnimPlayer == null)
				{
					FlagNullChildPlayer();
				}
				else if (extraAnimPlayer.IsPlaying(text))
				{
					BlendOutActionAnim(extraAnimPlayer, extraAnimPlayer[text], theFadeOutTime);
				}
			}
		}
		if (mBaseAnim == null)
		{
			mBaseAnim = PlayAnimOnPlayer(mTaggedAnimation, mBaseJointAnim.clip.name, 0f, WrapMode.Default, mBaseJointAnim.speed, -10);
			mActiveBaseAnims[0] = mBaseAnim;
		}
		mBaseAnim.enabled = true;
		mBaseAnim.time = mBaseJointAnim.time;
		if (flag)
		{
			OnAnimChanged();
		}
	}

	public AnimationState PlayEffectAnim(string theAnimName)
	{
		if (mTaggedAnimation[theAnimName] == null)
		{
			return null;
		}
		mTaggedAnimation[theAnimName].layer = mNextEffectLayerToUse;
		mNextEffectLayerToUse++;
		return mTaggedAnimation.PlayQueued(theAnimName, QueueMode.PlayNow, PlayMode.StopSameLayer);
	}

	public void SetAnimSpeed(float theSpeed)
	{
		currAnimSpeed = theSpeed;
	}

	public void SetBaseAnimSpeed(float theSpeed)
	{
		baseAnimSpeed = theSpeed;
	}

	public void SetJointMaskEnabled(bool enabled)
	{
		SetJointMaskEnabled(currAnim, enabled);
	}

	public void SetJointMaskEnabled(string theAnimName, bool enabled)
	{
		if (!mMaskedAnims.ContainsKey(theAnimName))
		{
			return;
		}
		AnimationState animationState = null;
		if (jointAnimation != null)
		{
			animationState = jointAnimation[theAnimName];
		}
		if (!(animationState != null))
		{
			return;
		}
		MaskedAnimData maskedAnimData = mMaskedAnims[theAnimName];
		if (maskedAnimData.enabled == enabled)
		{
			return;
		}
		maskedAnimData.enabled = enabled;
		if (enabled)
		{
			animationState.AddMixingTransform(maskedAnimData.firstJoint, !maskedAnimData.onlyUseFirstJoint);
		}
		else
		{
			animationState.RemoveMixingTransform(maskedAnimData.firstJoint);
		}
		if (currAnim == theAnimName && currAnimState != animationState)
		{
			if (enabled)
			{
				currAnimState.AddMixingTransform(maskedAnimData.firstJoint, !maskedAnimData.onlyUseFirstJoint);
			}
			else
			{
				currAnimState.RemoveMixingTransform(maskedAnimData.firstJoint);
			}
		}
	}

	public void SetJointMask(string theAnimName, Transform theJoint, bool onlyUseFirstJoint)
	{
		AnimationState animationState = null;
		if (jointAnimation != null)
		{
			animationState = jointAnimation[theAnimName];
		}
		if (!(animationState == null))
		{
			if (mMaskedAnims.ContainsKey(theAnimName))
			{
				SetJointMaskEnabled(theAnimName, false);
				mMaskedAnims.Remove(theAnimName);
			}
			MaskedAnimData maskedAnimData = new MaskedAnimData();
			maskedAnimData.enabled = false;
			maskedAnimData.firstJoint = theJoint;
			maskedAnimData.onlyUseFirstJoint = onlyUseFirstJoint;
			mMaskedAnims.Add(animationState.clip.name, maskedAnimData);
			SetJointMaskEnabled(theAnimName, true);
		}
	}

	public void SetDefaultBlendSpeed(string theAnimName, float theBlendSpeed)
	{
		if (mDefaultBlendSpeeds.ContainsKey(theAnimName))
		{
			mDefaultBlendSpeeds[theAnimName] = theBlendSpeed;
		}
		else
		{
			mDefaultBlendSpeeds.Add(theAnimName, theBlendSpeed);
		}
	}

	public bool IsUsingJointMask(string theAnimName)
	{
		if (mMaskedAnims.ContainsKey(theAnimName))
		{
			return mMaskedAnims[theAnimName].enabled;
		}
		return false;
	}

	public bool HasJointMaskSet(string theAnimName)
	{
		return mMaskedAnims.ContainsKey(theAnimName);
	}

	public bool IsPlaying(string theAnimName)
	{
		return mTaggedAnimation.IsPlaying(theAnimName);
	}

	public bool IsLoopingAnim()
	{
		return IsLoopingAnim(currAnimState);
	}

	public float GetDefaultBlendSpeed(string theAnimName)
	{
		float value;
		if (mDefaultBlendSpeeds.TryGetValue(theAnimName, out value))
		{
			return value;
		}
		return defaultBlendSpeed;
	}

	public bool IsLoopingAnim(AnimationState theAnim)
	{
		if (theAnim == null)
		{
			return false;
		}
		return theAnim.wrapMode == WrapMode.Loop || theAnim.wrapMode == WrapMode.PingPong || (theAnim.wrapMode == WrapMode.Default && (theAnim.clip.wrapMode == WrapMode.Loop || theAnim.clip.wrapMode == WrapMode.PingPong));
	}

	public bool IsDone()
	{
		if (mActionAnim == null || !mActionAnim.enabled)
		{
			return true;
		}
		return !IsLoopingAnim() && mActionAnim.normalizedTime >= 1f;
	}

	public void OnAnimStart(AnimationEvent animEvent)
	{
		if (!mActiveActionAnims.Contains(animEvent.animationState) && !mActiveBaseAnims.Contains(animEvent.animationState))
		{
			mActiveActionAnims.Clear();
			mActionAnim = animEvent.animationState;
			mActiveActionAnims.Add(mActionAnim);
			mActionAnim.time = 0f;
			PlayActionAnimOnChildren(mActionAnim.clip.name, GetDefaultBlendSpeed(mActionAnim.clip.name), WrapMode.Default, mActionAnim.speed);
		}
	}

	public void OnAnimDone(AnimationEvent animEvent)
	{
		if (!IsLoopingAnim(animEvent.animationState) && !mActiveBaseAnims.Contains(animEvent.animationState))
		{
			AnimationState animationState = mActionAnim;
			if (animDoneCallback != null)
			{
				animDoneCallback(animEvent.animationState.clip.name);
			}
			if (currAnimDoneCallback != null)
			{
				TaggedAnimCallback taggedAnimCallback = currAnimDoneCallback;
				currAnimDoneCallback = null;
				taggedAnimCallback(animEvent.animationState.clip.name);
			}
			if (autoReturnToBaseAnim && mBaseJointAnim != null && mBaseJointAnim.enabled && animationState == mActionAnim)
			{
				RevertToBaseAnim();
			}
		}
	}

	public void PlayAnimation(string theNewAnim)
	{
		PlayAnim(theNewAnim);
	}

	private static void SetAnimPlayerPaused(Animation theAnimPlayer, bool paused)
	{
		if (theAnimPlayer == null)
		{
			return;
		}
		theAnimPlayer.enabled = !paused;
		if (paused)
		{
			return;
		}
		foreach (AnimationState item in theAnimPlayer)
		{
			if (item != null && item.enabled)
			{
				item.enabled = false;
				item.enabled = true;
			}
		}
	}

	private void PlayActionAnimOnChildren(string theAnimName, float theBlendSpeed, WrapMode theWrapMode, float thePlaybackSpeed)
	{
		AnimationState animationState = PlayAnimOnPlayer(jointAnimation, theAnimName, theBlendSpeed, theWrapMode, thePlaybackSpeed, 0);
		if (animationState != null)
		{
			mActiveActionAnims.Add(animationState);
			mActionJointAnim = animationState;
		}
		if (alsoPlayOnAllChildren)
		{
			foreach (TaggedAnimPlayer extraTaggedAnimPlayer in extraTaggedAnimPlayers)
			{
				if (extraTaggedAnimPlayer == null)
				{
					FlagNullChildPlayer();
				}
				else
				{
					extraTaggedAnimPlayer.PlayAnim(theAnimName, theBlendSpeed);
				}
			}
			foreach (Animation extraAnimPlayer in extraAnimPlayers)
			{
				if (extraAnimPlayer == null)
				{
					FlagNullChildPlayer();
				}
				else
				{
					mActiveActionAnims.Add(PlayAnimOnPlayer(extraAnimPlayer, theAnimName, theBlendSpeed, theWrapMode, thePlaybackSpeed, 0));
				}
			}
		}
		if (animStartedCallback != null)
		{
			animStartedCallback(theAnimName);
		}
	}

	private AnimationState PlayAnimOnPlayer(Animation theAnimPlayer, string theAnimName, float theBlendSpeed, WrapMode theWrapMode, float thePlaybackSpeed, int theLayer)
	{
		AnimationState animationState = null;
		if (theAnimPlayer != null && theAnimPlayer[theAnimName] != null)
		{
			animationState = theAnimPlayer[theAnimName];
			animationState.layer = theLayer;
			if (theWrapMode != 0)
			{
				animationState.wrapMode = theWrapMode;
			}
			else if (animationState.wrapMode == WrapMode.Default)
			{
				animationState.wrapMode = animationState.clip.wrapMode;
			}
			if (!IsLoopingAnim(animationState))
			{
				animationState.wrapMode = WrapMode.ClampForever;
			}
			animationState.speed = thePlaybackSpeed;
			bool flag = !theAnimPlayer.enabled;
			if (flag)
			{
				theAnimPlayer.enabled = true;
			}
			if (theBlendSpeed <= 0f || flag)
			{
				if (theAnimPlayer.IsPlaying(theAnimName))
				{
					animationState = theAnimPlayer.PlayQueued(theAnimName, QueueMode.PlayNow, PlayMode.StopSameLayer);
				}
				else
				{
					theAnimPlayer.Play(theAnimName, PlayMode.StopSameLayer);
				}
			}
			else if (theAnimPlayer.IsPlaying(theAnimName))
			{
				animationState = theAnimPlayer.CrossFadeQueued(theAnimName, theBlendSpeed, QueueMode.PlayNow, PlayMode.StopSameLayer);
			}
			else
			{
				theAnimPlayer.CrossFade(theAnimName, theBlendSpeed, PlayMode.StopSameLayer);
			}
			animationState.speed = thePlaybackSpeed;
			if (flag)
			{
				theAnimPlayer.Sample();
				theAnimPlayer.enabled = false;
			}
		}
		return animationState;
	}

	private void UpdateBaseAnimPlayback(string theActionAnimName)
	{
		if (mBaseJointAnim == null)
		{
			return;
		}
		if (IsUsingJointMask(theActionAnimName))
		{
			if (mBaseAnim == null)
			{
				mBaseAnim = PlayAnimOnPlayer(mTaggedAnimation, mBaseJointAnim.clip.name, 0f, WrapMode.Default, mBaseJointAnim.speed, -10);
				mActiveBaseAnims[0] = mBaseAnim;
			}
			mBaseAnim.enabled = true;
			mBaseAnim.time = mBaseJointAnim.time;
			if (mBaseJointAnim != null && mBaseJointAnim.weight == 0f)
			{
				mBaseJointAnim.weight = 1f;
			}
		}
		else if (mBaseAnim != null)
		{
			mBaseAnim.enabled = false;
			if (mBaseJointAnim != null && mBaseJointAnim.weight >= 1f && mActionJointAnim != null && mActionJointAnim.enabled && mActionJointAnim.weight >= 1f)
			{
				mBaseJointAnim.weight = 0f;
			}
		}
	}

	private void BlendOutActionAnim(Animation animPlayer, AnimationState actionAnim, float theFadeOutTime)
	{
		if (!(animPlayer == null))
		{
			animPlayer.Blend(actionAnim.name, 0f, theFadeOutTime);
			StartCoroutine(ConfirmActionAnimBlendingOut(actionAnim));
		}
	}

	private IEnumerator ConfirmActionAnimBlendingOut(AnimationState theAnim)
	{
		float startingWeight = theAnim.weight;
		yield return null;
		yield return null;
		if (theAnim != null && !mActiveActionAnims.Contains(theAnim) && theAnim.weight >= startingWeight)
		{
			theAnim.time = 0f;
			theAnim.weight = 0f;
			theAnim.enabled = false;
		}
	}

	private void OnAnimChanged()
	{
		if (currAnimChangedCallback != null)
		{
			Action action = currAnimChangedCallback;
			currAnimChangedCallback = null;
			action();
		}
	}

	private void FlagNullChildPlayer()
	{
		if (base.gameObject.activeSelf)
		{
			StartCoroutine(ClearNullsFromChildPlayers());
		}
	}

	private IEnumerator ClearNullsFromChildPlayers()
	{
		yield return null;
		List<Animation> newExtraAnimPlayers = new List<Animation>();
		foreach (Animation baseAnimPlayer in extraAnimPlayers)
		{
			if (baseAnimPlayer != null)
			{
				newExtraAnimPlayers.Add(baseAnimPlayer);
			}
		}
		List<TaggedAnimPlayer> newExtraTaggedAnimPlayers = new List<TaggedAnimPlayer>();
		foreach (TaggedAnimPlayer taggedAnimPlayer in extraTaggedAnimPlayers)
		{
			if (taggedAnimPlayer != null)
			{
				newExtraTaggedAnimPlayers.Add(taggedAnimPlayer);
			}
		}
		extraAnimPlayers = newExtraAnimPlayers;
		extraTaggedAnimPlayers = newExtraTaggedAnimPlayers;
	}
}
