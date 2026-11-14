using System;
using System.Collections.Generic;
using UnityEngine;

public class ModelMelter : MonoBehaviour
{
	public bool ascendToTheHeavens;

	private Action mDoneEvent;

	private float mTimeLeft;

	private float mShaderTimeLeft;

	private float mTotalMeltTime;

	private List<Material> mMaterials;

	public static void MeltGameObject(GameObject theObject, Action onDone)
	{
		ModelMelter modelMelter = Initialize(theObject);
		modelMelter.mDoneEvent = onDone;
		modelMelter.ascendToTheHeavens = false;
		UnityEngine.Object.Instantiate(Resources.Load("FX/EnemyDie"), theObject.transform.root.position, Quaternion.identity);
	}

	public static void AscendGameObject(GameObject theObject, Action onDone)
	{
		ModelMelter modelMelter = Initialize(theObject);
		modelMelter.mDoneEvent = onDone;
		modelMelter.ascendToTheHeavens = true;
		UnityEngine.Object.Instantiate(Resources.Load("FX/AllyDie"), theObject.transform.root.position, Quaternion.identity);
	}

	private static ModelMelter Initialize(GameObject theObject)
	{
		Vector3 position = theObject.transform.position;
		GameObject gameObject = new GameObject("ModelMelterParent");
		GameObject gameObject2 = new GameObject("ModelMelter");
		ModelMelter result = gameObject2.AddComponent<ModelMelter>();
		Renderer componentInChildren = theObject.GetComponentInChildren<SkinnedMeshRenderer>();
		if (componentInChildren == null)
		{
			componentInChildren = theObject.GetComponentInChildren<MeshRenderer>();
		}
		if (componentInChildren != null)
		{
			position = componentInChildren.bounds.center;
			position.y = theObject.transform.position.y;
		}
		gameObject.transform.position = position;
		gameObject2.transform.position = position;
		gameObject2.transform.parent = gameObject.transform;
		theObject.transform.parent = gameObject2.transform;
		return result;
	}

	private void Start()
	{
		mMaterials = new List<Material>();
		Renderer[] componentsInChildren = GetComponentsInChildren<SkinnedMeshRenderer>();
		Renderer[] array = componentsInChildren;
		foreach (Renderer renderer in array)
		{
			mMaterials.AddRange(renderer.materials);
		}
		componentsInChildren = GetComponentsInChildren<MeshRenderer>();
		Renderer[] array2 = componentsInChildren;
		foreach (Renderer renderer2 in array2)
		{
			mMaterials.AddRange(renderer2.materials);
		}
		foreach (Material mMaterial in mMaterials)
		{
			mMaterial.shader = Resources.Load("Shaders/DiffuseWithColorIntensity") as Shader;
		}
		LegacyParticleEmitter[] componentsInChildren2 = GetComponentsInChildren<LegacyParticleEmitter>();
		LegacyParticleEmitter[] array3 = componentsInChildren2;
		foreach (LegacyParticleEmitter particleEmitter in array3)
		{
			if (particleEmitter != null)
			{
				particleEmitter.emit = false;
			}
		}
		Animation[] componentsInChildren3 = base.gameObject.GetComponentsInChildren<Animation>();
		Animation[] array4 = componentsInChildren3;
		foreach (Animation animation in array4)
		{
			if (animation != null)
			{
				animation.enabled = false;
			}
		}
		Animation animation2 = base.gameObject.AddComponent<Animation>();
		AnimationClip animationClip = null;
		animationClip = ((!ascendToTheHeavens) ? (Resources.Load("FX/ModelMeltDown") as AnimationClip) : (Resources.Load("FX/ModelMeltUp") as AnimationClip));
		if (animationClip != null)
		{
			animation2.AddClip(animationClip, "melt");
			animation2.Play("melt");
			mTotalMeltTime = animationClip.length;
			mTimeLeft = animationClip.length;
			mShaderTimeLeft = mTimeLeft;
			if (ascendToTheHeavens)
			{
				mShaderTimeLeft *= 0.66f;
			}
		}
		else
		{
			mTimeLeft = 0f;
		}
	}

	private void Update()
	{
		mTimeLeft -= Time.deltaTime;
		if (mTimeLeft <= 0f)
		{
			base.enabled = false;
			while (base.transform.childCount > 0)
			{
				base.transform.GetChild(0).parent = null;
			}
			if (mDoneEvent != null)
			{
				mDoneEvent();
			}
			UnityEngine.Object.Destroy(base.transform.root.gameObject);
			return;
		}
		mShaderTimeLeft = Mathf.Max(0f, mShaderTimeLeft - Time.deltaTime);
		foreach (Material mMaterial in mMaterials)
		{
			mMaterial.SetColor("_MainColor", (!ascendToTheHeavens) ? Color.black : Color.white);
			mMaterial.SetFloat("_ColorIntensity", Mathf.Clamp(1f - mShaderTimeLeft / mTotalMeltTime, 0f, 1f));
		}
	}
}
