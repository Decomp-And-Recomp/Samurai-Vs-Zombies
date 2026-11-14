using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PachinkoMachineImpl : WeakGlobalSceneBehavior<PachinkoMachineImpl>
{
	private const float kDialogPriorities = 500f;

	public TaggedAnimPlayer Tumbler1;

	public TaggedAnimPlayer Tumbler2;

	public TaggedAnimPlayer Tumbler3;

	public TaggedAnimPlayer BumperLeft;

	public TaggedAnimPlayer BumperRight;

	public float MiddleBumperSpinSpeed = 1f;

	public Material PrizeMaterial;

	public TaggedAnimPlayer TumblerSideDoors;

	public GameObject WinnerLightsOff;

	public GameObject WinnerLightsRed;

	public GameObject WinnerLightsGreen;

	public SoundThemePlayer PachinkoSFX;

	public int WinnerMaxFlashes = 20;

	public float WinnerLightFlashFrequency = 0.125f;

	public Material MultiplierMaterial;

	public int MaxMultiplier = 5;

	public LegacyParticleEmitter multiplierMaxedEffect;

	public float BoardLightsChangeSpeed = 1f;

	public Renderer[] MiddleArrowsLight;

	public Renderer[] SidesArrowsLight;

	private Vector3 mPreviousGravity;

	private float mPreviousFixedTimeStep;

	private Queue mSlotQueue = new Queue();

	private SDFTreeNode mPachinkoData;

	private int mFlowerOpenReward;

	private int mFlowerClosedReward;

	private int mSlotReward;

	private TutorialManager mTutorial;

	private List<SpoilsDisplay.Entry> mTotalSpoils = new List<SpoilsDisplay.Entry>();

	private bool mShouldQuitAfterDialog;

	private SUILayout mLayout;

	private SUILabel mCoinsCounter;

	private SUILabel mGemsCounter;

	private SUILabel mBallsCounter;

	private SUILabel mPrizeLabel;

	private SUISprite mWinner;

	private int mMultiplierValue;

	private Texture2D[] mMultiplierTextures = new Texture2D[10];

	private float mBoardLightsTimer;

	private int mCurrentBoardLightMiddle;

	private int mCurrentBoardLightSide;

	private DialogHandler mDialogHandler;

	private SUILayout mStoreCurrencies;

	private float mTimeTillStartTutorial = 0.25f;

	private bool mHasAttemptedToStartTutorial;

	private bool mRequestQuitOnNextUpdate;

	public bool isSlotMachineAnimating
	{
		get
		{
			return !Tumbler1.IsDone() || !Tumbler2.IsDone() || !Tumbler3.IsDone();
		}
	}

	public int flowerRewardOpen
	{
		get
		{
			return mFlowerOpenReward * mMultiplierValue;
		}
	}

	public int flowerRewardClosed
	{
		get
		{
			return mFlowerClosedReward * mMultiplierValue;
		}
	}

	public int slotReward
	{
		get
		{
			return mSlotReward * mMultiplierValue;
		}
	}

	public int multiplier
	{
		get
		{
			return mMultiplierValue;
		}
	}

	private void Start()
	{
		SetUniqueInstance(this);
		WeakGlobalInstance<SUIScreen>.instance.fader.FadeFromBlack();
		mLayout = new SUILayout("Layouts/PachinkoLayout");
		foreach (KeyValuePair<string, SUILayout.ObjectData> @object in mLayout.objects)
		{
			AdjustUIElementForAspectRatio(@object.Value.obj as IHasVisualAttributes);
		}
		((SUIButton)mLayout["back"]).onButtonPressed = RequestQuit;
		((SUIButton)mLayout["purchaseBtn"]).onButtonPressed = TriggerStore;
		((SUIButton)mLayout["helpBtn"]).onButtonPressed = TriggerHelp;
		for (int i = 0; i < mMultiplierTextures.Length; i++)
		{
			mMultiplierTextures[i] = Resources.Load("Sprites/Menus/pachinko_" + i) as Texture2D;
		}
		mCoinsCounter = (SUILabel)mLayout["coinsCounter"];
		mGemsCounter = (SUILabel)mLayout["gemsCounter"];
		mBallsCounter = (SUILabel)mLayout["ballsCounter"];
		mPrizeLabel = (SUILabel)mLayout["prizeName"];
		mPrizeLabel.maxLines = 1;
		mPrizeLabel.text = string.Empty;
		mWinner = (SUISprite)mLayout["winner"];
		mWinner.visible = false;
		mPachinkoData = SingletonMonoBehaviour<ResourcesManager>.instance.Open("Registry/Pachinko");
		mFlowerOpenReward = int.Parse(mPachinkoData["FlowerOpenReward"]);
		mFlowerClosedReward = int.Parse(mPachinkoData["FlowerClosedReward"]);
		mSlotReward = int.Parse(mPachinkoData["CoinCatchReward"]);
		mPreviousGravity = Physics.gravity;
		Physics.gravity = new Vector3(0f, float.Parse(mPachinkoData["gravity"]), 0f);
		mPreviousFixedTimeStep = Time.fixedDeltaTime;
		Time.fixedDeltaTime = 0.01f;
		mSlotQueue.Clear();
		BumperLeft.PlayAnim("spin", 1f, WrapMode.Loop, MiddleBumperSpinSpeed);
		BumperRight.PlayAnim("spin", 1f, WrapMode.Loop, MiddleBumperSpinSpeed);
		BumperLeft.paused = true;
		BumperRight.paused = true;
	}

	private void Update()
	{
		if (SceneBehaviourUpdate())
		{
			return;
		}
		mTimeTillStartTutorial -= Time.deltaTime;
		if (!mHasAttemptedToStartTutorial && mTimeTillStartTutorial <= 0f)
		{
			mHasAttemptedToStartTutorial = true;
			mTutorial = new TutorialManager("PachinkoTutorial", string.Empty);
		}
		if (mRequestQuitOnNextUpdate)
		{
			mRequestQuitOnNextUpdate = false;
			RequestQuit();
			return;
		}
		if (mTutorial != null)
		{
			Time.timeScale = 0f;
			mTutorial.Update();
			if (mTutorial.isDone)
			{
				mTutorial.Destroy();
				mTutorial = null;
			}
			return;
		}
		if (mDialogHandler != null)
		{
			Time.timeScale = 0f;
			if (mStoreCurrencies == null)
			{
				mStoreCurrencies = new SUILayout("Layouts/TopCornerCurrencies");
				mStoreCurrencies.AnimateIn();
			}
			mStoreCurrencies.Update();
			UpdateStoreCurrencies();
			mDialogHandler.Update();
			if (mDialogHandler.isDone)
			{
				mDialogHandler.Destroy();
				mDialogHandler = null;
				mStoreCurrencies.Destroy();
				mStoreCurrencies = null;
				if (mShouldQuitAfterDialog)
				{
					Quit();
					return;
				}
			}
			else if (mDialogHandler.isBlocking)
			{
				return;
			}
		}
		Time.timeScale = 1f;
		mCoinsCounter.text = Singleton<Profile>.instance.coins.ToString();
		mGemsCounter.text = Singleton<Profile>.instance.gems.ToString();
		mBallsCounter.text = Singleton<Profile>.instance.pachinkoBalls.ToString();
		mLayout.Update();
		UpdateSlotMachine();
		if (Input.GetKeyUp(KeyCode.Escape))
		{
			RequestQuit();
		}
	}

	private void FixedUpdate()
	{
		UpdateFlashingArrows(Time.deltaTime);
		GameObject[] array = GameObject.FindGameObjectsWithTag("PachinkoBall");
		int num = array.Length;
		if (num > MaxMultiplier)
		{
			num = MaxMultiplier;
		}
		if (num == 0)
		{
			num = 1;
		}
		if (mMultiplierValue != num)
		{
			if (mMultiplierValue < num)
			{
				Singleton<SUISoundManager>.instance.Play("multiplierx" + num);
			}
			mMultiplierValue = num;
			if (mMultiplierValue == MaxMultiplier)
			{
				multiplierMaxedEffect.emit = true;
			}
			else
			{
				multiplierMaxedEffect.emit = false;
			}
			MultiplierMaterial.SetTexture("_MainTex", mMultiplierTextures[mMultiplierValue]);
		}
	}

	public void RequestQuitOnNextUpdate()
	{
		mRequestQuitOnNextUpdate = true;
	}

	public void EnqueSlotSpin(string result)
	{
		mSlotQueue.Enqueue(result);
	}

	public void TriggerStoreFromLever()
	{
		if (mDialogHandler != null)
		{
			mDialogHandler.Destroy();
		}
		mDialogHandler = new DialogHandler(500f, InPachinkoStoreDialog.Create("special_pachinkoBallsPack", false));
	}

	public void TriggerStore()
	{
		if (mDialogHandler != null)
		{
			mDialogHandler.Destroy();
		}
		mDialogHandler = new DialogHandler(500f, InGameStoreDialog.Create("special_pachinkoBallsPack", false));
	}

	public void TriggerHelp()
	{
		if (mDialogHandler != null)
		{
			mDialogHandler.Destroy();
		}
		mDialogHandler = new DialogHandler(500f, new PachinkoHelpDialog());
	}

	public void TriggerSummary()
	{
		if (mDialogHandler != null)
		{
			mDialogHandler.Destroy();
		}
		mDialogHandler = new DialogHandler(500f, new PachinkoSummaryDialog(mTotalSpoils));
	}

	public IEnumerator WinCondition(string result)
	{
		TumblerSideDoors.PlayAnim("open", 1f, WrapMode.Once, 1f);
		StartCoroutine(FlashWinnerLights());
		mWinner.visible = true;
		PachinkoSFX.PlaySoundEvent("Win");
		if (Singleton<CharmsDatabase>.instance.Contains(result))
		{
			mPrizeLabel.text = Singleton<Localizer>.instance.Parse(Singleton<CharmsDatabase>.instance.GetAttribute(result, "displayName"));
			PrizeMaterial.mainTexture = Resources.Load(Singleton<CharmsDatabase>.instance.GetAttribute(result, "icon")) as Texture2D;
			CashIn.From(result, 1, "Random Loot", "CREDIT_IN_GAME_AWARD");
			AddToSpoil(result, 1);
		}
		else if (Singleton<PotionsDatabase>.instance.Contains(result))
		{
			mPrizeLabel.text = Singleton<Localizer>.instance.Parse(Singleton<PotionsDatabase>.instance.GetAttribute(result, "displayName"));
			PrizeMaterial.mainTexture = Resources.Load(Singleton<PotionsDatabase>.instance.GetAttribute(result, "icon")) as Texture2D;
			CashIn.From(result, 1, "Random Loot", "CREDIT_IN_GAME_AWARD");
			AddToSpoil(result, 1);
		}
		else if (result.Substring(0, 4) == "coin")
		{
			int numCoins = int.Parse(result.Substring(4));
			mPrizeLabel.text = string.Format(Singleton<Localizer>.instance.Get("coinsNum"), numCoins.ToString());
			PrizeMaterial.mainTexture = Resources.Load("Sprites/Icons/consume_coin") as Texture2D;
			CashIn.From("coins", numCoins, "Random Loot", "CREDIT_IN_GAME_AWARD");
			AddToSpoil("coins", numCoins);
		}
		else if (result.Substring(0, 3) == "gem")
		{
			int numGems = int.Parse(result.Substring(3));
			mPrizeLabel.text = string.Format(Singleton<Localizer>.instance.Get("gemsNum"), numGems.ToString());
			PrizeMaterial.mainTexture = Resources.Load("Sprites/Icons/consume_jem") as Texture2D;
			CashIn.From("gems", numGems, "Random Loot", "CREDIT_IN_GAME_AWARD");
			AddToSpoil("gems", numGems);
		}
		else
		{
			mPrizeLabel.text = "???????";
		}
		Singleton<Profile>.instance.Save();
		yield return new WaitForSeconds(2f);
		mWinner.visible = false;
		mPrizeLabel.text = string.Empty;
		TumblerSideDoors.PlayAnim("open", 1f, WrapMode.Once, -1f);
	}

	public void AddToSpoil(string id, int count)
	{
		foreach (SpoilsDisplay.Entry mTotalSpoil in mTotalSpoils)
		{
			if (mTotalSpoil.id == id)
			{
				mTotalSpoil.count += count;
				return;
			}
		}
		SpoilsDisplay.Entry item = SpoilsDisplay.BuildEntry(id, count);
		mTotalSpoils.Add(item);
	}

	private void UpdateStoreCurrencies()
	{
		((SUILabel)mStoreCurrencies["currencyCounter"]).text = Singleton<Profile>.instance.coins + "\n" + Singleton<Profile>.instance.gems;
	}

	private void AdjustUIElementForAspectRatio(IHasVisualAttributes obj)
	{
		Vector2 position = obj.position;
		position.x -= SUIScreen.width / 2f;
		position.x /= WeakGlobalInstance<SUIScreen>.instance.autoScaler.aspectRatioModifier;
		position.x += SUIScreen.width / 2f;
		obj.position = position;
	}

	private void RequestQuit()
	{
		((SUIButton)mLayout["back"]).onButtonPressed = null;
		if (mTotalSpoils.Count == 0)
		{
			Quit();
			return;
		}
		TriggerSummary();
		mShouldQuitAfterDialog = true;
	}

	private void Quit()
	{
		((SUIButton)mLayout["back"]).onButtonPressed = null;
		WeakGlobalInstance<SUIScreen>.instance.fader.onFadingDone = delegate
		{
			Physics.gravity = mPreviousGravity;
			Time.fixedDeltaTime = mPreviousFixedTimeStep;
			if (Singleton<Profile>.instance.readyToStartBonusWave)
			{
				WaveManager.LoadNextWaveLevel();
			}
			else
			{
				Singleton<MenusFlow>.instance.LoadScene("Store");
			}
		};
		WeakGlobalInstance<SUIScreen>.instance.fader.FadeToBlack();
	}

	private void UpdateSlotMachine()
	{
		if (mSlotQueue.Count > 0 && !isSlotMachineAnimating)
		{
			PlaySlotAnimation(mSlotQueue.Dequeue().ToString());
		}
	}

	private void PlaySlotAnimation(string result)
	{
		PachinkoSFX.PlaySoundEvent("Spin");
		PachinkoSFX.PlaySoundEvent("SpinLoop");
		StartCoroutine(StopTumblers(result));
	}

	private IEnumerator StopTumblers(string result)
	{
		if (result == "loss")
		{
			List<string> animationNames = Tumbler1.GetAnimationNames();
			string anim1 = string.Empty;
			string anim2 = string.Empty;
			string anim3 = string.Empty;
			while (string.Compare(anim1, anim2) == 0 && string.Compare(anim1, anim3) == 0)
			{
				anim1 = animationNames[Random.Range(0, animationNames.Count)];
				anim2 = animationNames[Random.Range(0, animationNames.Count)];
				anim3 = animationNames[Random.Range(0, animationNames.Count)];
			}
			Tumbler1.PlayAnim(anim1);
			Tumbler2.PlayAnim(anim2);
			Tumbler3.PlayAnim(anim3);
			yield return new WaitForSeconds(0.33f);
			yield break;
		}
		string animToPlay = string.Empty;
		if (result.Contains("revive"))
		{
			animToPlay = "revive";
		}
		else if (Singleton<CharmsDatabase>.instance.Contains(result))
		{
			animToPlay = "charm";
		}
		else if (Singleton<PotionsDatabase>.instance.Contains(result))
		{
			animToPlay = "sushi";
		}
		else if (result.Substring(0, 4) == "coin")
		{
			animToPlay = "coin";
		}
		else if (result.Substring(0, 3) == "gem")
		{
			animToPlay = "gem";
		}
		yield return new WaitForSeconds(0.25f);
		Tumbler1.PlayAnim(animToPlay);
		yield return new WaitForSeconds(0.25f);
		Tumbler2.PlayAnim(animToPlay);
		Tumbler3.PlayAnim(animToPlay);
		yield return new WaitForSeconds(0.33f);
		StartCoroutine("WinCondition", result);
	}

	private IEnumerator FlashWinnerLights()
	{
		WinnerLightsOff.GetComponent<Renderer>().enabled = false;
		BumperLeft.paused = false;
		BumperRight.paused = false;
		for (int i = 0; i < WinnerMaxFlashes; i++)
		{
			if (i % 2 > 0)
			{
				WinnerLightsGreen.GetComponent<Renderer>().enabled = true;
				WinnerLightsRed.GetComponent<Renderer>().enabled = false;
			}
			else
			{
				WinnerLightsGreen.GetComponent<Renderer>().enabled = false;
				WinnerLightsRed.GetComponent<Renderer>().enabled = true;
			}
			yield return new WaitForSeconds(WinnerLightFlashFrequency);
		}
		WinnerLightsGreen.GetComponent<Renderer>().enabled = false;
		WinnerLightsRed.GetComponent<Renderer>().enabled = false;
		WinnerLightsOff.GetComponent<Renderer>().enabled = true;
		BumperLeft.paused = true;
		BumperRight.paused = true;
	}

	private void UpdateFlashingArrows(float deltaTime)
	{
		mBoardLightsTimer += deltaTime;
		if (mBoardLightsTimer >= BoardLightsChangeSpeed)
		{
			Renderer[] middleArrowsLight = MiddleArrowsLight;
			foreach (Renderer renderer in middleArrowsLight)
			{
				renderer.enabled = false;
			}
			MiddleArrowsLight[mCurrentBoardLightMiddle].enabled = true;
			mCurrentBoardLightMiddle++;
			if (mCurrentBoardLightMiddle > MiddleArrowsLight.Length - 1)
			{
				mCurrentBoardLightMiddle = 0;
			}
			Renderer[] sidesArrowsLight = SidesArrowsLight;
			foreach (Renderer renderer2 in sidesArrowsLight)
			{
				renderer2.enabled = false;
			}
			SidesArrowsLight[mCurrentBoardLightSide].enabled = true;
			mCurrentBoardLightSide++;
			if (mCurrentBoardLightSide > SidesArrowsLight.Length - 1)
			{
				mCurrentBoardLightSide = 0;
			}
			mBoardLightsTimer = 0f;
		}
	}
}
