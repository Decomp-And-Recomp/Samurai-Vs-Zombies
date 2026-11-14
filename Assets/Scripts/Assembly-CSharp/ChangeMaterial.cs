using UnityEngine;

public class ChangeMaterial : MonoBehaviour
{
	public Material[] materials;

	private void Start()
	{
		if (!MultiLanguages.isMultiLanguages)
		{
			return;
		}
		LegacyParticleRenderer component = GetComponent<LegacyParticleRenderer>();
		if ((bool)component)
		{
			MultiLanguages.ESystemLanguage eSystemLanguage = MultiLanguages.ESystemLanguage.ESL_English;
			switch (MultiLanguages.GetCurrentLanguageToEnum())
			{
			case MultiLanguages.ESystemLanguage.ESL_Chinese:
				component.material = materials[1];
				break;
			case MultiLanguages.ESystemLanguage.ESL_Japanese:
				component.material = materials[3];
				break;
			case MultiLanguages.ESystemLanguage.ESL_Korean:
				component.material = materials[2];
				break;
			default:
				component.material = materials[0];
				break;
			}
		}
	}

	private void Update()
	{
	}
}
