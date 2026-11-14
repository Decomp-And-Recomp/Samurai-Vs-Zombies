using System.Collections.Generic;
using UnityEngine;

public class PrefabPreloader : WeakGlobalInstance<PrefabPreloader>
{
	private Dictionary<string, GameObject> mPreloadedModels = new Dictionary<string, GameObject>();

	public PrefabPreloader()
	{
		SetUniqueInstance(this);
	}

	public void Preload(string prefabPath)
	{
		if (!mPreloadedModels.ContainsKey(prefabPath))
		{
			Preload(prefabPath, Resources.Load(prefabPath) as GameObject);
		}
	}

	public void Preload(string prefabPath, GameObject prefab)
	{
		if (!mPreloadedModels.ContainsKey(prefabPath))
		{
			GameObject gameObject = (GameObject)Object.Instantiate(prefab);
			gameObject.transform.position = new Vector3(0f, 10000f, -10000f);
			gameObject.SetActive(false);
			mPreloadedModels.Add(prefabPath, gameObject);
		}
	}
}
