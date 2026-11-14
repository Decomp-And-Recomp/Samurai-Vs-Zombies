using System;
using System.Collections;
using UnityEngine;

public class InGameImpl : WeakGlobalSceneBehavior<InGameImpl>
{
	private const int kMaxHelpers = 5;

	private const float kDialogPriority = 500f;

	private const float kWinPriority = 1000f;

	private const float kSlowMoEffectStartScale = 0.1f;

	private const float kSlowMoEffectIncrement = 0.8f;

	private const float kCameraZoomInOffset = 200f;

	private const float kCameraZoomOutSpeed = 80f;

	private const float kCameraMoveUpSpeed = 25f;

	private const int kWaveToShowEndGameNarrative = 70;

	public Transform heroSpawnPointLeft;

	public Transform heroSpawnPointRight;

	public GameObject enemiesTargetLeft;

	public GameObject vortexLeft;

	public GameObject gateSparklesLeft;

	public GameObject enemiesTargetRight;

	public Camera gameCamera;

	public Transform heroWalkLeftEdge;

	public Transform heroWalkRightEdge;

	public BoxCollider enemiesSpawnAreaLeft;

	public BoxCollider enemiesSpawnAreaRight;

	public BoxCollider helpersSpawnAreaLeft;

	public BoxCollider helpersSpawnAreaRight;

	public GameObject bellRingerObject;

	public Transform[] villageArcher = new Transform[3];

	private PrefabPreloader mPrefabPreloader = new PrefabPreloader();

	private BuffIconManager mBuffIconManager = new BuffIconManager();

	private GameHUD mHUD;

	private Hero mHero;

	private Base mBase;

	private Bell mBell;

	private CharactersManager mCharactersManager;

	private CollectableManager mCollectableManager;

	private ProjectileManager mProjectileManager;

	private WaveManager mWaveManager;

	private ProceduralShaderManager mShaderManager;

	private Smithy mSmithy;

	private TutorialManager mTutorial;

	private LegionOnTheLooseManager mLegionManager;

	private RailManager mRailManager;

	private DialogHandler mDialogHandler;

	private NarrativeManager mNarrativeManager;

	private TimedEventManager mTimedEventManager = new TimedEventManager();

	private VillageArchers mVillageArchers;

	private GraveHandsEffect mGraveHandsEffect;

	private FPSCounter mFPSCounter;

	private bool mGameOver;

	private bool mPauseGameForBlockingDialog = true;

	private bool mHaveShowReviveDialog;

	private float mAllAlliesInvincibleTimer;

	private WaveBanner mWaveBanner;

	private GenericGameBanner mGenericGameBanner;

	private MiniHealthBarRegistrar mMiniHealthBarRegistrar = new MiniHealthBarRegistrar();

	private float mCameraYOffset = 100f;

	private float mBaseTimeScale = 1f;

	private bool mStartedSlowMoFinisher;

	private PlayModesManager.GameDirection mGameDirection;

	public Transform heroSpawnPoint
	{
		get
		{
			if (mGameDirection == PlayModesManager.GameDirection.RightToLeft)
			{
				return heroSpawnPointRight;
			}
			return heroSpawnPointLeft;
		}
	}

	public GameObject enemiesTarget
	{
		get
		{
			if (mGameDirection == PlayModesManager.GameDirection.RightToLeft)
			{
				return enemiesTargetRight;
			}
			return enemiesTargetLeft;
		}
	}

	public BoxCollider enemiesSpawnArea
	{
		get
		{
			if (mGameDirection == PlayModesManager.GameDirection.RightToLeft)
			{
				return enemiesSpawnAreaLeft;
			}
			return enemiesSpawnAreaRight;
		}
	}

	public BoxCollider helpersSpawnArea
	{
		get
		{
			if (mGameDirection == PlayModesManager.GameDirection.RightToLeft)
			{
				return helpersSpawnAreaRight;
			}
			return helpersSpawnAreaLeft;
		}
	}

	public bool gameOver
	{
		get
		{
			return mGameOver;
		}
	}

	public bool playerWon
	{
		get
		{
			return mGameOver && hero.health > 0f;
		}
	}

	public bool enemiesWon
	{
		get
		{
			return mGameOver && hero.health <= 0f;
		}
	}

	public bool useSlowMoFinisher
	{
		get
		{
			if (mStartedSlowMoFinisher)
			{
				return true;
			}
			if (hero != null && (hero.controller.currentAnimation == "attack" || hero.controller.currentAnimation == "katanaslash"))
			{
				return true;
			}
			return false;
		}
	}

	public Hero hero
	{
		get
		{
			return mHero;
		}
	}

	public Base gate
	{
		get
		{
			return mBase;
		}
	}

	public string activeCharm { get; private set; }

	public GameHUD HUD
	{
		get
		{
			return mHUD;
		}
	}

	public float allAlliesInvincibleTimer
	{
		get
		{
			return mAllAlliesInvincibleTimer;
		}
		set
		{
			mAllAlliesInvincibleTimer = value;
		}
	}

	private bool gamePaused
	{
		get
		{
			return Time.timeScale == 0f;
		}
		set
		{
			if (value != (Time.timeScale == 0f))
			{
				SoundThemeManager.SetAllLoopingSoundsPaused(value);
				if (value)
				{
					Time.timeScale = 0f;
				}
				else
				{
					Time.timeScale = mBaseTimeScale;
				}
			}
		}
	}

	private void Start()
	{
		mGameDirection = Singleton<PlayModesManager>.instance.gameDirection;
		SetUniqueInstance(this);
		ApplicationUtilities.instance.onApplicationPause += OnApplicationPause;
		HideUnusedGate();
		Singleton<PlayStatistics>.instance.ResetStats(Singleton<Profile>.instance.waveToBeat);
		activeCharm = Singleton<Profile>.instance.selectedCharm;
		WeakGlobalInstance<SUIScreen>.instance.fader.FadeFromBlack();
		mRailManager = new RailManager();
		mProjectileManager = new ProjectileManager();
		mWaveManager = new WaveManager(Singleton<Profile>.instance.waveToBeat, enemiesSpawnArea, enemiesTarget.transform.position.z);
		mWaveManager.PreloadEnemies();
		mSmithy = new Smithy();
		mHUD = new GameHUD();
		mHUD.onPauseGameRequest = onPauseRequest;
		mHUD.onAbilityTriggered = Singleton<AbilitiesDatabase>.instance.Execute;
		mHero = new Hero(heroSpawnPoint);
		mHero.controller.constraintLeft = heroWalkLeftEdge.position.z;
		mHero.controller.constraintRight = heroWalkRightEdge.position.z;
		mBase = new Base(enemiesTarget);
		mBell = new Bell(bellRingerObject);
		mShaderManager = new ProceduralShaderManager();
		mCollectableManager = new CollectableManager(heroWalkLeftEdge.position.z + 16f, heroWalkRightEdge.position.z - 16f, heroSpawnPoint.position.x);
		mVillageArchers = new VillageArchers();
		mCharactersManager = new CharactersManager();
		mCharactersManager.AddCharacter(mHero);
		mCharactersManager.AddCharacter(mBase);
		mSmithy.characterManagerRef = mCharactersManager;
		mSmithy.helperSpawnArea = helpersSpawnArea;
		if (mGameDirection == PlayModesManager.GameDirection.LeftToRight)
		{
			mSmithy.helpersZTarget = enemiesSpawnArea.transform.position.z;
		}
		else
		{
			mSmithy.helpersZTarget = enemiesSpawnArea.transform.position.z;
		}
		mHUD.observedCharacter = mHero;
		mWaveBanner = new WaveBanner();
		mLegionManager = new LegionOnTheLooseManager();
		mTutorial = new TutorialManager(mWaveManager.tutorial, "InGameReactives");
		if (!Singleton<Profile>.instance.inBonusWave)
		{
			mNarrativeManager = new NarrativeManager();
		}
		if (Singleton<PlayModesManager>.instance.selectedMode == "zombies")
		{
			mGraveHandsEffect = new GraveHandsEffect();
		}
		ApplyInitialCharmEffects();
		SpendCharm();
		CreateFPSCounter();
		if (gameCamera.nearClipPlane < 10f)
		{
			gameCamera.transform.position = new Vector3(gameCamera.transform.position.x - (10f - gameCamera.nearClipPlane), gameCamera.transform.position.y, gameCamera.transform.position.z);
			gameCamera.nearClipPlane = 10f;
		}
		if (gameCamera.farClipPlane > 10000f)
		{
			gameCamera.farClipPlane /= 10f;
		}
		UnityEngine.Debug.Log(string.Concat("Camera at: ", gameCamera.transform.position, ", near: ", gameCamera.nearClipPlane, ", far: ", gameCamera.farClipPlane));
		mCameraYOffset = gameCamera.transform.position.y - heroSpawnPointLeft.transform.position.y;
		RenderSettings.fog = false;
		if (Singleton<Profile>.instance.waveToBeat == 1 && Singleton<Profile>.instance.GetWaveLevel(1) == 1)
		{
			Singleton<PlayHavenTowerControl>.instance.InvokeContent("tutorial_start");
		}
	}

	private void OnDestroy()
	{
		Time.timeScale = 1f;
		ApplicationUtilities.instance.onApplicationPause -= OnApplicationPause;
	}

	private void Update()
	{
		if (SceneBehaviourUpdate() || WeakGlobalInstance<SUIScreen>.instance.fader.onFadingDone != null)
		{
			return;
		}
		if (mNarrativeManager != null)
		{
			mNarrativeManager.Update();
			if (mNarrativeManager.isBlocking)
			{
				gamePaused = true;
				return;
			}
		}
		if (mTutorial != null)
		{
			if (WeakGlobalInstance<WaveManager>.instance.waveLevel == 1 && mTutorial.nextCommandIsHighPriority)
			{
				mTutorial.RunNextCommand();
			}
			UpdateTutorial();
			if (mTutorial != null && mTutorial.isBlocking)
			{
				gamePaused = true;
				return;
			}
		}
		if (mDialogHandler != null)
		{
			mPauseGameForBlockingDialog = true;
			mDialogHandler.Update();
			if (mDialogHandler.isDone)
			{
				mDialogHandler.Destroy();
				mDialogHandler = null;
			}
			else if (mDialogHandler.isBlocking)
			{
				if (mPauseGameForBlockingDialog)
				{
					gamePaused = true;
				}
				mHUD.UpdateCurrenciesOnly();
				return;
			}
		}
		gamePaused = false;
		if (mWaveBanner != null)
		{
			mWaveBanner.Update();
			if (mWaveBanner.isDone)
			{
				mWaveBanner.Destroy();
				mWaveBanner = null;
			}
		}
		if (mGenericGameBanner != null)
		{
			mGenericGameBanner.Update();
			if (mGenericGameBanner.isDone)
			{
				mGenericGameBanner.Destroy();
				mGenericGameBanner = null;
			}
		}
		if (mGraveHandsEffect != null)
		{
			mGraveHandsEffect.Update();
		}
		mTimedEventManager.Update();
		mSmithy.Update();
		mWaveManager.Update();
		mCharactersManager.Update();
		mCollectableManager.Update();
		mProjectileManager.Update();
		mShaderManager.update();
		mBell.Update();
		mVillageArchers.Update();
		mLegionManager.Update();
		CheckWinLoseConditions();
		mMiniHealthBarRegistrar.UpdateCulling();
		if (mAllAlliesInvincibleTimer > 0f)
		{
			mAllAlliesInvincibleTimer = Mathf.Max(0f, mAllAlliesInvincibleTimer - Time.deltaTime);
		}
		mHUD.Update();
		UpdateFPSCounter();
		if (Input.GetKeyUp(KeyCode.Escape))
		{
			onPauseRequest();
		}
	}

	private void LateUpdate()
	{
		UpdateCamera();
	}

	public void RunSpecialWaveCommand(string cmd)
	{
		switch (cmd)
		{
		case "@bossalert":
			if (mGenericGameBanner != null)
			{
				mGenericGameBanner.Destroy();
			}
			mGenericGameBanner = new GenericGameBanner(Singleton<PlayModesManager>.instance.selectedModeData["bannerfile_boss"], "bossalert");
			break;
		case "@legionalert":
		{
			if (mGenericGameBanner != null)
			{
				mGenericGameBanner.Destroy();
			}
			string bannerSprite = Singleton<PlayModesManager>.instance.selectedModeData["bannerfile_legion"];
			mGenericGameBanner = new GenericGameBanner(bannerSprite, "legionalert");
			break;
		}
		default:
			UnityEngine.Debug.Log("WARNING: Unknown wave command: " + cmd);
			break;
		}
	}

	public void ShowInGameStore(string itemID)
	{
		OnSUIGenericCallback extraUpdateCode = delegate
		{
			mHUD.consumables.DirtyCellCounters();
			mHUD.UpdateConsumableVisualsOnly();
			mHUD.UpdateCurrenciesOnly();
		};
		if (mDialogHandler != null)
		{
			mDialogHandler.PushCreator(delegate
			{
				mDialogHandler.extraUpdateCode = extraUpdateCode;
				return InGameStoreDialog.Create(Singleton<PlayModesManager>.instance.revivePotionID, true);
			});
		}
		else
		{
			SetDialogHandler(new DialogHandler(500f, InGameStoreDialog.Create(itemID, true)));
			mDialogHandler.extraUpdateCode = extraUpdateCode;
		}
	}

	public void RunAfterDelay(Action fn, float delay)
	{
		if (fn != null)
		{
			StartCoroutine(RunAfterDelayInternal(fn, delay));
		}
	}

	public void RunNextUpdate(Action fn)
	{
		if (fn != null)
		{
			StartCoroutine(RunAfterYieldInternal(fn));
		}
	}

	private void onPauseRequest()
	{
		if (!mGameOver)
		{
			PauseMenu pauseMenu = new PauseMenu();
			pauseMenu.onQuitGameRequest = OnQuitGameRequest;
			pauseMenu.onRestartGameRequest = OnRestartGameRequest;
			SetDialogHandler(new DialogHandler(500f, pauseMenu));
		}
	}

	private void OnQuitGameRequest()
	{
		WeakGlobalInstance<SUIScreen>.instance.fader.onFadingDone = delegate
		{
			if (Singleton<Profile>.instance.inBonusWave || (Singleton<Profile>.instance.waveToBeat == 1 && Singleton<Profile>.instance.GetWaveLevel(2) == 0))
			{
				Singleton<MenusFlow>.instance.LoadScene("MainMenu");
			}
			else
			{
				Singleton<MenusFlow>.instance.LoadScene("Store");
			}
		};
		WeakGlobalInstance<SUIScreen>.instance.fader.FadeToBlack();
		ApplicationUtilities.instance.HideAds();
	}

	private void OnRestartGameRequest()
	{
		WeakGlobalInstance<SUIScreen>.instance.fader.onFadingDone = delegate
		{
			WaveManager.LoadNextWaveLevel();
		};
		WeakGlobalInstance<SUIScreen>.instance.fader.FadeToBlack();
		ApplicationUtilities.instance.HideAds();
	}

	private void onFinishGameRequest()
	{
		WeakGlobalInstance<SUIScreen>.instance.fader.onFadingDone = delegate
		{
			Singleton<MenusFlow>.instance.LoadScene("Debriefing");
		};
		WeakGlobalInstance<SUIScreen>.instance.fader.FadeToBlack();
	}

	private void SetDialogHandler(DialogHandler d)
	{
		if (mDialogHandler != null)
		{
			mDialogHandler.Destroy();
		}
		mDialogHandler = d;
	}

	private void UpdateTutorial()
	{
		mTutorial.Update();
		if (mTutorial.isDone)
		{
			mTutorial.Destroy();
			mTutorial = null;
		}
	}

	private void UpdateCamera()
	{
		gameCamera.transform.position = new Vector3(gameCamera.transform.position.x, mHero.position.y + mCameraYOffset, mHero.position.z);
	}

	private void CheckWinLoseConditions()
	{
		bool flag = false;
		if (mHero.isOver)
		{
			WeakGlobalInstance<CollectableManager>.instance.OpenPresents(true);
			if (!mHaveShowReviveDialog)
			{
				mHaveShowReviveDialog = true;
				ShowReviveDialog();
				return;
			}
			flag = true;
		}
		else
		{
			if (mBase.health == 0f)
			{
				mHero.health = 0f;
			}
			mHaveShowReviveDialog = false;
		}
		if (flag)
		{
			mGameOver = true;
			if (Singleton<Profile>.instance.inBonusWave)
			{
				Singleton<Profile>.instance.bonusWaveToBeat++;
				Singleton<Profile>.instance.wavesSinceLastBonusWave = 0;
			}
			else
			{
				Singleton<Analytics>.instance.LogEvent("Deaths", Singleton<Profile>.instance.waveToBeat.ToString());
			}
			CreateLoseDialog();
			Singleton<Profile>.instance.Save();
		}
		else if ((mWaveManager.isDone && mCharactersManager.enemiesCount == 0) || mHUD.cheatClicked())
		{
			CreateWinDialog();
			mGameOver = true;
			StartCoroutine(PostWinDelay());
		}
	}

	private void CreateWinDialog()
	{
		Camera.main.GetComponent<AudioSource>().volume = 0f;
		if (useSlowMoFinisher)
		{
			mBaseTimeScale = 0.1f;
			Time.timeScale = mBaseTimeScale;
			gameCamera.transform.position = new Vector3(gameCamera.transform.position.x - 200f, gameCamera.transform.position.y, gameCamera.transform.position.z);
			mCameraYOffset /= 2f;
			mStartedSlowMoFinisher = true;
		}
		string attribName = "postWinTime";
		WinLoseDisplay initialDialog;
		if (!Singleton<Profile>.instance.inBonusWave && Singleton<Profile>.instance.waveToBeat == 70 && Singleton<Profile>.instance.GetWaveLevel(70) == 1 && Singleton<PlayModesManager>.instance.selectedMode == "classic")
		{
			attribName = "endGameTime";
			initialDialog = new WinLoseDisplay("Layouts/EndGameMessage");
		}
		else
		{
			initialDialog = new WinLoseDisplay(Singleton<PlayModesManager>.instance.selectedModeData["layout_VictoryMessage"]);
		}
		SetDialogHandler(new DialogHandler(1000f, initialDialog));
		mDialogHandler.fadeLevel = 1f;
		mDialogHandler.fadeSpeed = float.Parse(Singleton<Config>.instance.root.to("Game")[attribName]);
		mDialogHandler.extraUpdateCode = delegate
		{
			if (useSlowMoFinisher)
			{
				mBaseTimeScale = Mathf.Min(1f, mBaseTimeScale + 0.8f * Time.deltaTime);
				Time.timeScale = mBaseTimeScale;
				mCameraYOffset += 25f * Time.deltaTime;
				gameCamera.transform.position = new Vector3(gameCamera.transform.position.x + 80f * Time.deltaTime, gameCamera.transform.position.y, gameCamera.transform.position.z);
			}
			mPauseGameForBlockingDialog = false;
			mCharactersManager.Update();
			mCollectableManager.Update();
			mProjectileManager.Update();
			mShaderManager.update();
			mBell.Update();
			mVillageArchers.Update();
			mHUD.Update();
		};
	}

	private void CreateLoseDialog()
	{
		Camera.main.GetComponent<AudioSource>().volume = 0f;
		WinLoseDisplay winLoseDisplay = new WinLoseDisplay(Singleton<PlayModesManager>.instance.selectedModeData["layout_LoseMessage"]);
		winLoseDisplay.onPlayerPressed = onFinishGameRequest;
		SetDialogHandler(new DialogHandler(500f, winLoseDisplay));
		mDialogHandler.fadeLevel = 0.3f;
		mDialogHandler.extraUpdateCode = delegate
		{
			mPauseGameForBlockingDialog = false;
			mShaderManager.update();
			mHUD.Update();
		};
	}

	private IEnumerator PostWinDelay()
	{
		float waitTime = float.Parse(Singleton<Config>.instance.root.to("Game")["postWinTime"]);
		yield return new WaitForSeconds(waitTime);
		RegisterWin();
		onFinishGameRequest();
	}

	private void RegisterWin()
	{
		mGameOver = true;
		WeakGlobalInstance<WaveManager>.instance.AddSpecialRewardsToCollectables();
		WeakGlobalInstance<CollectableManager>.instance.BankAllResources();
		if (Singleton<Profile>.instance.inBonusWave)
		{
			Singleton<Profile>.instance.bonusWaveToBeat++;
			Singleton<Profile>.instance.wavesSinceLastBonusWave = 0;
		}
		else
		{
			Singleton<Analytics>.instance.LogEvent("WaveWins", Singleton<Profile>.instance.waveToBeat.ToString());
			Singleton<Profile>.instance.SetWaveLevel(Singleton<Profile>.instance.waveToBeat, Singleton<Profile>.instance.GetWaveLevel(Singleton<Profile>.instance.waveToBeat) + 1);
			Singleton<Profile>.instance.waveToBeat++;
			if (Singleton<Profile>.instance.GetWaveLevel(Singleton<Profile>.instance.waveToBeat) == 0)
			{
				Singleton<Profile>.instance.SetWaveLevel(Singleton<Profile>.instance.waveToBeat, 1);
			}
			if (Singleton<PlayModesManager>.instance.selectedMode == "classic")
			{
				Singleton<Profile>.instance.wavesSinceLastBonusWave++;
			}
		}
		Singleton<PlayStatistics>.instance.stats.lastWaveWon = true;
		Singleton<Profile>.instance.Save();
	}

	private IEnumerator RunAfterDelayInternal(Action fn, float delay)
	{
		yield return new WaitForSeconds(delay);
		if (fn != null)
		{
			fn();
		}
	}

	private IEnumerator RunAfterYieldInternal(Action fn)
	{
		yield return null;
		if (fn != null)
		{
			fn();
		}
	}

	private void SpendCharm()
	{
		if (!Singleton<Profile>.instance.inBonusWave)
		{
			string selectedCharm = Singleton<Profile>.instance.selectedCharm;
			if (selectedCharm != string.Empty)
			{
				Singleton<Profile>.instance.SetNumCharms(selectedCharm, Mathf.Max(0, Singleton<Profile>.instance.GetNumCharms(selectedCharm) - 1));
				Singleton<Profile>.instance.selectedCharm = string.Empty;
			}
		}
	}

	private void ApplyInitialCharmEffects()
	{
		string attribute = Singleton<CharmsDatabase>.instance.GetAttribute(activeCharm, "helper");
		if (attribute != string.Empty)
		{
			Helper helper = WeakGlobalInstance<Smithy>.instance.ForceSpawn(attribute);
			if (helper != null)
			{
				helper.maxHealth *= mWaveManager.multipliers.enemiesHealth;
				helper.health = helper.maxHealth;
				helper.meleeDamage *= mWaveManager.multipliers.enemiesDamages;
			}
		}
	}

	private void ShowReviveDialog()
	{
		ReviveDialog reviveDialog = new ReviveDialog();
		reviveDialog.priority = 500.5f;
		SetDialogHandler(new DialogHandler(500f, reviveDialog));
	}

	private void OnApplicationPause(bool paused)
	{
		if (!gamePaused)
		{
			ApplicationUtilities.instance.HideAds();
			onPauseRequest();
		}
	}

	private void HideUnusedGate()
	{
		switch (mGameDirection)
		{
		case PlayModesManager.GameDirection.LeftToRight:
			if (enemiesTargetRight != null)
			{
				UnityEngine.Object.DestroyImmediate(enemiesTargetRight, true);
				enemiesTargetRight = null;
			}
			enemiesTargetLeft = CheckHalloweenGateReplacement(enemiesTargetLeft);
			break;
		case PlayModesManager.GameDirection.RightToLeft:
			if (enemiesTargetLeft != null)
			{
				UnityEngine.Object.DestroyImmediate(enemiesTargetLeft, true);
				enemiesTargetLeft = null;
				if (vortexLeft != null)
				{
					UnityEngine.Object.DestroyImmediate(vortexLeft, true);
					vortexLeft = null;
				}
				if (gateSparklesLeft != null)
				{
					UnityEngine.Object.DestroyImmediate(gateSparklesLeft, true);
					gateSparklesLeft = null;
				}
				enemiesTargetRight = CheckHalloweenGateReplacement(enemiesTargetRight);
			}
			break;
		}
	}

	private GameObject CheckHalloweenGateReplacement(GameObject orig)
	{
		if (Singleton<EventsManager>.instance.IsEventActive("halloween"))
		{
			Vector3 position = orig.transform.position;
			UnityEngine.Object.DestroyImmediate(orig);
			GameObject gameObject = UnityEngine.Object.Instantiate(Resources.Load("Props/PFGateHalloween") as GameObject) as GameObject;
			gameObject.transform.position = position;
			return gameObject;
		}
		return orig;
	}

	private void CreateFPSCounter()
	{
		if (Debug.isDebugBuild)
		{
			mFPSCounter = new FPSCounter("default32", new Vector2(8f, 400f), 5000f);
		}
	}

	private void UpdateFPSCounter()
	{
		if (Debug.isDebugBuild)
		{
			mFPSCounter.Update();
		}
	}
}
