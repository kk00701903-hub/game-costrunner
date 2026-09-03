using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

public enum BootMode
{
    Runner,

    /// The king cycle on its own. This is the scene that decides whether the
    /// rule reads without explanation, so it has to be one keystroke away.
    KingArena
}

public class GameBootstrap : MonoBehaviour
{
    private const string BootModeKey = "r347_boot_mode";

    [SerializeField] private float testDepotDistance = 400f;
    [SerializeField] private bool autoRun = true;
    [Tooltip("Collapse line that gains ground every time the deck takes a hit.")]
    [SerializeField] private bool spawnCollapseLine = true;
    [Tooltip("Retrieval drones that close in the more tags you are carrying.")]
    [SerializeField] private bool spawnRetrievalDrones = true;
    [SerializeField] private BootMode bootMode = BootMode.Runner;

    /// Survives the domain reload between the editor menu and play mode.
    public static BootMode PendingMode
    {
        get { return (BootMode)PlayerPrefs.GetInt(BootModeKey, 0); }
        set
        {
            PlayerPrefs.SetInt(BootModeKey, (int)value);
            PlayerPrefs.Save();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBoot()
    {
        // Coast Run is the active game — never spawn A-0347 on those scenes.
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path.IndexOf("_CoastRun", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return;
        if (Object.FindAnyObjectByType<CoastRun.CoastRunBootstrap>() != null)
            return;
        if (Object.FindAnyObjectByType<CoastRun.MainMenuBootstrap>() != null)
            return;
        if (Object.FindAnyObjectByType<CoastRun.GameSession>() != null)
            return;

        if (Object.FindObjectOfType<GameBootstrap>() != null)
            return;
        if (Object.FindObjectOfType<GameManager>() != null)
            return;

        GameObject go = new GameObject("GameBootstrap");
        go.AddComponent<GameBootstrap>();
    }

    private void Awake()
    {
        if (!autoRun)
            return;

        bootMode = PendingMode;
        BuildTestWorld();
    }

    private void Update()
    {
        // Iterating on the boss means restarting into the arena constantly.
        if (Input.GetKeyDown(KeyCode.F2))
            Reboot(BootMode.KingArena);
        else if (Input.GetKeyDown(KeyCode.F3))
            Reboot(BootMode.Runner);
    }

    private static void Reboot(BootMode mode)
    {
        PendingMode = mode;
        if (GameManager.Instance != null)
            GameManager.Instance.Restart();
    }

    private void BuildTestWorld()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }

        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 800f;

        CameraProfile profile = CameraProfile.Active;
        if (profile != null)
        {
            cam.nearClipPlane = profile.nearClip;
            cam.farClipPlane = profile.farClip;
        }

        VisualBootstrap.EnsureRunStack(cam);
        PortraitViewport.Ensure(cam);

        Light sun = RenderSettings.sun;
        if (sun == null)
        {
            Light[] lights = Object.FindObjectsOfType<Light>();
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type == LightType.Directional)
                {
                    sun = lights[i];
                    break;
                }
            }
        }

        if (sun == null)
        {
            GameObject lightGo = new GameObject("Directional Light");
            sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        sun.intensity = Mathf.Max(sun.intensity, 1.05f);
        sun.color = new Color(1f, 0.96f, 0.90f);
        sun.shadows = LightShadows.Soft;
        RenderSettings.sun = sun;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientIntensity = 1.15f;

        Transform player = CreatePlayer();
        CameraFollow follow = cam.GetComponent<CameraFollow>();
        if (follow == null)
            follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player);

        GameObject systems = GameObject.Find("GameSystems");
        if (systems == null)
            systems = new GameObject("GameSystems");

        GameManager gm = systems.GetComponent<GameManager>();
        if (gm == null)
            gm = systems.AddComponent<GameManager>();
        gm.Configure(player, testDepotDistance);

        UIManager ui = systems.GetComponent<UIManager>();
        if (ui == null)
            ui = systems.AddComponent<UIManager>();

        DayNightCycle dayNight = systems.GetComponent<DayNightCycle>();
        if (dayNight == null)
            dayNight = systems.AddComponent<DayNightCycle>();
        dayNight.RefreshEnvironment();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.38f, 0.42f, 0.48f);
            cam.fieldOfView = CameraProfile.Active != null ? CameraProfile.Active.baseFov : 54f;
        }

        RunBackdrop.Ensure(player);
        if (systems.GetComponent<ZoneDirector>() == null)
            systems.AddComponent<ZoneDirector>();
        if (systems.GetComponent<ZoneAccentLights>() == null)
            systems.AddComponent<ZoneAccentLights>();
        if (systems.GetComponent<GameAudio>() == null)
            systems.AddComponent<GameAudio>();
        if (systems.GetComponent<ItemSlot>() == null)
            systems.AddComponent<ItemSlot>();
        if (systems.GetComponent<StoryEngine>() == null)
            systems.AddComponent<StoryEngine>();
        if (CounterCueTone.Instance == null && GameObject.Find("CounterCueTone") == null)
            new GameObject("CounterCueTone").AddComponent<CounterCueTone>();

        // Portrait: vertical FOV stays readable; CameraProfile owns the rest.
        if (cam != null && profile != null)
            cam.fieldOfView = profile.baseFov;

        if (spawnCollapseLine && GameObject.Find("CollapseLine") == null)
            new GameObject("CollapseLine").AddComponent<CollapseLine>();

        if (spawnRetrievalDrones && GameObject.Find("RetrievalDrones") == null)
            new GameObject("RetrievalDrones").AddComponent<RetrievalDrones>();

        GameObject spawnerGo = GameObject.Find("RoadSpawner");
        if (spawnerGo == null)
            spawnerGo = new GameObject("RoadSpawner");
        RoadSpawner spawner = spawnerGo.GetComponent<RoadSpawner>();
        if (spawner == null)
            spawner = spawnerGo.AddComponent<RoadSpawner>();

        TryBindTestUi(ui);
        PortraitBarsOverlay.Ensure();

        if (bootMode == BootMode.KingArena)
        {
            spawner.ForceStraight = true;
            StartCoroutine(OpenKingArena());
            Debug.Log("King arena. Arrows / WASD. Step into the gold lane to counter. F3 for the runner.");
            return;
        }

        Debug.Log(
            "Test package ready (" + MobileDisplay.DeviceName + " " + MobileDisplay.Width + "x" + MobileDisplay.Height + "). " +
            "WASD / arrows + swipe. Depot at " + testDepotDistance + " m. F2 for the king arena.");
    }

    /// The spawner needs one frame to lay road before the king can be placed on it.
    private System.Collections.IEnumerator OpenKingArena()
    {
        yield return null;
        yield return null;

        GameObject go = GameObject.Find("KingFight");
        if (go == null)
            go = new GameObject("KingFight");

        KingFight fight = go.GetComponent<KingFight>();
        if (fight == null)
            fight = go.AddComponent<KingFight>();

        if (ItemSlot.Instance != null)
            ItemSlot.Instance.RecordEntryLoadout();

        fight.Begin();
    }

    private static Transform CreatePlayer()
    {
        GameObject existing = GameObject.Find("Player");
        if (existing != null)
            return existing.transform;

        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        try
        {
            player.tag = "Player";
        }
        catch (UnityException)
        {
            Debug.LogWarning("Add Tag 'Player' in Project Settings.");
        }

        player.transform.position = new Vector3(0f, 0.85f, 2f);
        Collider capsuleCol = player.GetComponent<Collider>();
        if (capsuleCol != null)
            Object.Destroy(capsuleCol);

        // Movement is transform + AABB. No CharacterController, no Rigidbody.
        player.AddComponent<PlayerVitals>();
        player.AddComponent<PlayerController>();

        GameConfig cfg = GameConfig.Active;
        if (cfg != null && cfg.use3DCharacter)
            player.AddComponent<PlayerCharacterView>();
        else
            player.AddComponent<PlayerSpriteView>();

        return player.transform;
    }

    private static void TryBindTestUi(UIManager ui)
    {
        if (ui == null)
            return;

        try
        {
            DestroyExistingUi();
            BindTestUi(ui);
            Debug.Log("347 UI: cinematic portrait HUD bound.");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("347: UI bind failed — world still runs. " + ex.Message);
        }
    }

    private static void DestroyExistingUi()
    {
        Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] == null || canvases[i].gameObject.name == "PortraitBars")
                continue;
            Object.Destroy(canvases[i].gameObject);
        }

        EventSystem[] systems = Object.FindObjectsOfType<EventSystem>();
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] != null)
                Object.Destroy(systems[i].gameObject);
        }
    }

    private static void BindTestUi(UIManager ui)
    {
        Font font = UiChrome.BuiltinFont();
        Sprite panelSprite = UiArt.Panel();
        Sprite restartSprite = UiArt.PrimaryButton();
        Sprite keyArt = UiArt.ConceptOpening();
        Sprite towerArt = UiArt.ConceptDepot();
        Sprite frameItem = UiArt.ItemFrame();
        Sprite iconTag = UiArt.IconTag();
        Sprite iconLetter = UiArt.IconLetter();
        Sprite iconCoin = UiArt.IconCoin();
        Sprite deckOk = UiArt.DeckPip(3);
        Sprite deckCrack = UiArt.DeckPip(2);
        Sprite deckBroken = UiArt.DeckPip(1);

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        Camera uiCamera = Camera.main;
        if (uiCamera == null)
        {
            Debug.LogWarning("347: Main Camera missing during UI bind.");
        }

        GameObject canvasGo = new GameObject("Canvas");
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = uiCamera;
        canvas.planeDistance = 10f;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UiTheme.Reference;
        scaler.matchWidthOrHeight = 0f;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        // --- Prologue: full-bleed art, copy in lower third, one pulsing CTA ---
        GameObject prologue = MakeDim(canvas.transform, "ProloguePanel", new Color(0f, 0f, 0f, 1f));
        AddBackdrop(prologue.transform, keyArt, new Color(0.72f, 0.74f, 0.76f));
        MakeDim(prologue.transform, "Veil", new Color(0.02f, 0.02f, 0.03f, 0.55f));

        GameObject prologueCard = MakeCard(prologue.transform, "Card", panelSprite, new Vector2(920f, 520f));
        RectTransform prologueCardRt = prologueCard.GetComponent<RectTransform>();
        prologueCardRt.anchorMin = prologueCardRt.anchorMax = prologueCardRt.pivot = new Vector2(0.5f, 0f);
        prologueCardRt.anchoredPosition = new Vector2(0f, UiTheme.SafeBottom + 132f);

        Text brand = UiChrome.Label(prologueCard.transform, "Brand", "347", 22, TextAnchor.UpperCenter, UiTheme.Gold, font);
        brand.rectTransform.anchorMin = new Vector2(0.1f, 0.82f);
        brand.rectTransform.anchorMax = new Vector2(0.9f, 0.96f);
        brand.rectTransform.offsetMin = brand.rectTransform.offsetMax = Vector2.zero;

        Text prologueText = UiChrome.Label(prologueCard.transform, "PrologueText", UIManager.PrologueCopy,
            30, TextAnchor.MiddleCenter, UiTheme.Ink, font);
        prologueText.rectTransform.anchorMin = new Vector2(0.08f, 0.22f);
        prologueText.rectTransform.anchorMax = new Vector2(0.92f, 0.82f);
        prologueText.rectTransform.offsetMin = prologueText.rectTransform.offsetMax = Vector2.zero;

        Text prologueCta = UiChrome.Label(prologueCard.transform, "TapCta", "탭하여 시작",
            28, TextAnchor.MiddleCenter, UiTheme.GoldHot, font);
        prologueCta.rectTransform.anchorMin = new Vector2(0.15f, 0.04f);
        prologueCta.rectTransform.anchorMax = new Vector2(0.85f, 0.18f);
        prologueCta.rectTransform.offsetMin = prologueCta.rectTransform.offsetMax = Vector2.zero;
        UiChrome.Drop(prologueCta, new Color(0f, 0f, 0f, 0.6f), new Vector2(0f, -2f));

        // --- In-run HUD: cinematic minimal — pause | distance pill | coin pill ---
        GameObject hud = new GameObject("HudRoot");
        hud.transform.SetParent(canvas.transform, false);
        Stretch(hud.AddComponent<RectTransform>());
        hud.AddComponent<SafeAreaFitter>();

        UiChrome.PauseButton(hud.transform, () =>
        {
            if (UIManager.Instance != null)
                UIManager.Instance.TogglePause();
        });

        RectTransform distPill = UiChrome.HudPill(hud.transform, "DistancePill",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(360f, UiTheme.PillHeight));
        UiChrome.FlagIcon(distPill, new Vector2(18f, 0f));
        Text distance = UiChrome.Label(distPill, "Distance", "DISTANCE: 0 m",
            UiTheme.DistanceSize, TextAnchor.MiddleLeft, UiTheme.Ink, font);
        distance.fontStyle = FontStyle.Bold;
        RectTransform distRt = distance.rectTransform;
        distRt.anchorMin = new Vector2(0f, 0f);
        distRt.anchorMax = new Vector2(1f, 1f);
        distRt.offsetMin = new Vector2(52f, 0f);
        distRt.offsetMax = new Vector2(-12f, 0f);

        RectTransform coinPill = UiChrome.HudPill(hud.transform, "CoinPill",
            new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(168f, UiTheme.PillHeight));
        coinPill.anchoredPosition = new Vector2(-(UiTheme.SafeSide + 84f), coinPill.anchoredPosition.y);
        if (iconCoin != null)
            UiChrome.HudIcon(coinPill, "CoinIcon", iconCoin, new Vector2(14f, 0f), 26f);
        Text score = UiChrome.Label(coinPill, "Score", "× 0",
            UiTheme.ScoreSize, TextAnchor.MiddleLeft, UiTheme.Ink, font);
        score.fontStyle = FontStyle.Bold;
        RectTransform scoreRt = score.rectTransform;
        scoreRt.anchorMin = new Vector2(0f, 0f);
        scoreRt.anchorMax = new Vector2(1f, 1f);
        scoreRt.offsetMin = new Vector2(46f, 0f);
        scoreRt.offsetMax = new Vector2(-10f, 0f);

        // Hidden meta — still bound for zone banners / accessibility toggles.
        Text zone = UiChrome.Label(hud.transform, "Zone", Zones.Label(Zone.Arcade), UiTheme.MetaSize, TextAnchor.MiddleLeft, UiTheme.InkMuted, font);
        zone.enabled = false;
        Text best = UiChrome.Label(hud.transform, "Best", "최고 0", UiTheme.MetaSize, TextAnchor.MiddleRight, UiTheme.InkMuted, font);
        best.enabled = false;

        Image[] deckPips = UiChrome.DeckPips(hud.transform, "DeckPips",
            new Vector2(UiTheme.SafeSide, UiTheme.SafeBottom + 118f),
            deckOk, deckCrack, deckBroken);
        deckPips[0].transform.parent.gameObject.SetActive(false);
        for (int i = 0; i < deckPips.Length; i++)
        {
            RectTransform prt = deckPips[i].rectTransform;
            prt.sizeDelta = new Vector2(34f, 38f);
            prt.anchoredPosition = new Vector2(i * 38f, 0f);
        }

        Text hp = UiChrome.Label(hud.transform, "DeckNumeric", "데크 3 / 3", 18, TextAnchor.MiddleLeft, UiTheme.InkMuted, font);
        hp.enabled = UIManager.NumericHp;
        RectTransform hpRt = hp.rectTransform;
        hpRt.anchorMin = hpRt.anchorMax = hpRt.pivot = new Vector2(0f, 0f);
        hpRt.anchoredPosition = new Vector2(UiTheme.SafeSide + 128f, UiTheme.SafeBottom + 124f);
        hpRt.sizeDelta = new Vector2(160f, 28f);

        Text supplies = MakeCollectRow(hud.transform, font, iconTag, iconLetter, iconCoin);
        supplies.gameObject.SetActive(false);

        Text king = UiChrome.Label(hud.transform, "King", "왕  9 / 9   ·   P1", 26, TextAnchor.MiddleCenter, UiTheme.Gold, font);
        king.enabled = false;
        RectTransform kingRt = king.rectTransform;
        kingRt.anchorMin = kingRt.anchorMax = kingRt.pivot = new Vector2(0.5f, 1f);
        kingRt.anchoredPosition = new Vector2(0f, -(UiTheme.SafeTop + 72f));
        kingRt.sizeDelta = new Vector2(480f, 36f);

        Image itemFill;
        Text item;
        RectTransform itemSlot = UiChrome.ItemSlotFrame(hud.transform, out itemFill, out item, frameItem);
        itemSlot.gameObject.SetActive(false);
        itemSlot.anchoredPosition = new Vector2(UiTheme.SafeSide, UiTheme.SafeBottom + 248f);
        itemSlot.sizeDelta = new Vector2(88f, 88f);
        Image itemPlate = itemSlot.GetComponent<Image>();

        Text combo = UiChrome.Label(hud.transform, "Combo", string.Empty, 32, TextAnchor.MiddleRight, UiTheme.GoldHot, font);
        RectTransform comboRt = combo.rectTransform;
        comboRt.anchorMin = comboRt.anchorMax = comboRt.pivot = new Vector2(1f, 1f);
        comboRt.anchoredPosition = new Vector2(-UiTheme.SafeSide, -(UiTheme.SafeTop + 72f));
        comboRt.sizeDelta = new Vector2(180f, 40f);

        GameObject subtitleRoot = UiChrome.SubtitlePlate(hud.transform, out Text subtitle);
        Image subtitlePlate = subtitleRoot.GetComponent<Image>();

        Text tutorialHint = UiChrome.Label(hud.transform, "TutorialHint", string.Empty, 22, TextAnchor.MiddleCenter, UiTheme.InkMuted, font);
        tutorialHint.enabled = false;
        RectTransform tutRt = tutorialHint.rectTransform;
        tutRt.anchorMin = new Vector2(0.12f, 0.16f);
        tutRt.anchorMax = new Vector2(0.88f, 0.20f);
        tutRt.offsetMin = tutRt.offsetMax = Vector2.zero;

        Text banner = UiChrome.Label(hud.transform, "ZoneBanner", string.Empty, UiTheme.BannerSize, TextAnchor.MiddleCenter, UiTheme.Ink, font);
        Color bannerC = banner.color;
        bannerC.a = 0f;
        banner.color = bannerC;
        RectTransform bannerRt = banner.rectTransform;
        bannerRt.anchorMin = bannerRt.anchorMax = bannerRt.pivot = new Vector2(0.5f, 1f);
        bannerRt.anchoredPosition = new Vector2(0f, -(UiTheme.SafeTop + 78f));
        bannerRt.sizeDelta = new Vector2(640f, 56f);

        Text turnHint = UiChrome.Label(hud.transform, "TurnHint", string.Empty, UiTheme.HintSize, TextAnchor.MiddleCenter, UiTheme.Gold, font);
        RectTransform hintRt = turnHint.rectTransform;
        hintRt.anchorMin = new Vector2(0.15f, 0.22f);
        hintRt.anchorMax = new Vector2(0.85f, 0.27f);
        hintRt.offsetMin = hintRt.offsetMax = Vector2.zero;

        Image collapseVeil = MakeOverlay(canvas.transform, "CollapseVeil", new Color(0.58f, 0.58f, 0.56f, 0f));
        CanvasGroup hurtVignette = MakeVignette(canvas.transform, "HurtVignette", new Color(0.62f, 0.06f, 0.06f));

        // --- Recovery log (never "GAME OVER") ---
        GameObject gameOver = MakeDim(canvas.transform, "GameOverPanel", UiTheme.Dim);
        GameObject gameOverCard = MakeCard(gameOver.transform, "Card", panelSprite, new Vector2(920f, 640f));
        Text stamp = UiChrome.Label(gameOverCard.transform, "Stamp", "회수 로그  ·  A-0347", 22, TextAnchor.MiddleCenter, UiTheme.Gold, font);
        stamp.rectTransform.anchorMin = new Vector2(0.1f, 0.88f);
        stamp.rectTransform.anchorMax = new Vector2(0.9f, 0.96f);
        stamp.rectTransform.offsetMin = stamp.rectTransform.offsetMax = Vector2.zero;

        Text gameOverText = UiChrome.Label(gameOverCard.transform, "GameOverText", string.Empty, 30, TextAnchor.UpperCenter, UiTheme.Ink, font);
        gameOverText.rectTransform.anchorMin = new Vector2(0.08f, 0.38f);
        gameOverText.rectTransform.anchorMax = new Vector2(0.92f, 0.86f);
        gameOverText.rectTransform.offsetMin = gameOverText.rectTransform.offsetMax = Vector2.zero;

        Text gameOverScore = UiChrome.Label(gameOverCard.transform, "GameOverScore", string.Empty, 24, TextAnchor.MiddleCenter, UiTheme.InkMuted, font);
        gameOverScore.rectTransform.anchorMin = new Vector2(0.08f, 0.26f);
        gameOverScore.rectTransform.anchorMax = new Vector2(0.92f, 0.38f);
        gameOverScore.rectTransform.offsetMin = gameOverScore.rectTransform.offsetMax = Vector2.zero;

        UiChrome.PrimaryCta(gameOverCard.transform, "RestartButton", "다시 달리기",
            new Vector2(0f, -210f), restartSprite, () =>
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.Restart();
            });

        // --- Ending ---
        GameObject ending = MakeDim(canvas.transform, "EndingPanel", new Color(0f, 0.02f, 0.06f, 1f));
        AddBackdrop(ending.transform, towerArt != null ? towerArt : keyArt, new Color(0.68f, 0.70f, 0.73f));
        MakeDim(ending.transform, "Veil", new Color(0f, 0.02f, 0.05f, 0.55f));
        GameObject endingCard = MakeCard(ending.transform, "Card", panelSprite, new Vector2(920f, 620f));

        Text endingText = UiChrome.Label(endingCard.transform, "EndingText", string.Empty, 32, TextAnchor.UpperCenter, UiTheme.Ink, font);
        endingText.rectTransform.anchorMin = new Vector2(0.08f, 0.42f);
        endingText.rectTransform.anchorMax = new Vector2(0.92f, 0.90f);
        endingText.rectTransform.offsetMin = endingText.rectTransform.offsetMax = Vector2.zero;

        Text endingScore = UiChrome.Label(endingCard.transform, "EndingScore", string.Empty, 24, TextAnchor.MiddleCenter, UiTheme.InkMuted, font);
        endingScore.rectTransform.anchorMin = new Vector2(0.08f, 0.30f);
        endingScore.rectTransform.anchorMax = new Vector2(0.92f, 0.42f);
        endingScore.rectTransform.offsetMin = endingScore.rectTransform.offsetMax = Vector2.zero;

        Button continueBtn = UiChrome.PrimaryCta(endingCard.transform, "ContinueButton", "목록에 다시 오르기",
            new Vector2(0f, -160f), restartSprite, () =>
            {
                if (UIManager.Instance != null)
                    UIManager.Instance.ContinueRun();
            });
        UiChrome.PrimaryCta(endingCard.transform, "EndingRestart", "다시 달리기",
            new Vector2(0f, -270f), null, () =>
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.Restart();
            });

        ui.Bind(prologue, prologueText, hud, distance, supplies, gameOver, ending, endingText, 3.2f);
        ui.BindExtras(score, turnHint, gameOverText, gameOverScore, endingScore, continueBtn.gameObject);
        ui.BindScreenEffects(collapseVeil, hurtVignette, hp);
        ui.BindCombat(king, item, combo);
        ui.BindStory(subtitle, banner, zone, tutorialHint);
        ui.BindPolish(best, prologueCta, deckPips, itemFill, itemPlate, subtitlePlate);
    }

    /// <summary>Play Mode only — wipes stale Canvas and rebuilds the cinematic HUD.</summary>
    public static void RebuildRunUi()
    {
        UIManager ui = UIManager.Instance;
        if (ui == null)
        {
            Debug.LogWarning("347: UIManager missing — cannot rebuild UI.");
            return;
        }

        DestroyExistingUi();
        BindTestUi(ui);
        PortraitBarsOverlay.Ensure();
        Debug.Log("347 UI: rebuilt cinematic portrait HUD.");
    }

    private static Text MakeCollectRow(
        Transform parent,
        Font font,
        Sprite tagIcon,
        Sprite letterIcon,
        Sprite coinIcon)
    {
        GameObject row = new GameObject("Collect");
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.AddComponent<RectTransform>();
        rowRt.anchorMin = rowRt.anchorMax = rowRt.pivot = new Vector2(1f, 1f);
        rowRt.anchoredPosition = new Vector2(-UiTheme.SafeSide, -(UiTheme.SafeTop + 142f));
        rowRt.sizeDelta = new Vector2(380f, 40f);

        Text text = UiChrome.Label(row.transform, "Label", "태그 0   편지 0   조각 0",
            20, TextAnchor.MiddleRight, UiTheme.InkMuted, font);
        RectTransform trt = text.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(0f, 0f);
        trt.offsetMax = Vector2.zero;
        text.alignment = TextAnchor.MiddleRight;

        float x = -8f;
        void Chip(Sprite s)
        {
            if (s == null)
                return;
            GameObject go = new GameObject("Chip");
            go.transform.SetParent(row.transform, false);
            Image img = go.AddComponent<Image>();
            img.sprite = s;
            img.preserveAspect = true;
            img.raycastTarget = false;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(x - 200f, 0f);
            rt.sizeDelta = new Vector2(32f, 32f);
            x -= 36f;
        }

        // Visual accents only; counts stay in the label so RefreshSupplies stays simple.
        Chip(coinIcon);
        Chip(letterIcon);
        Chip(tagIcon);
        return text;
    }

    private static Image MakeOverlay(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        Stretch(go.GetComponent<RectTransform>());
        return image;
    }

    /// Four edge strips instead of a texture, so the middle of the screen stays
    /// clean while the borders bleed.
    private static CanvasGroup MakeVignette(Transform parent, string name, Color color)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        RectTransform rootRt = root.AddComponent<RectTransform>();
        Stretch(rootRt);
        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        Vector2[] mins = { new Vector2(0f, 0f), new Vector2(0f, 0.86f), new Vector2(0f, 0f), new Vector2(0.9f, 0f) };
        Vector2[] maxs = { new Vector2(1f, 0.14f), new Vector2(1f, 1f), new Vector2(0.1f, 1f), new Vector2(1f, 1f) };

        for (int i = 0; i < mins.Length; i++)
        {
            GameObject strip = new GameObject("Edge" + i);
            strip.transform.SetParent(root.transform, false);
            Image edge = strip.AddComponent<Image>();
            edge.color = new Color(color.r, color.g, color.b, 0.75f);
            edge.raycastTarget = false;
            RectTransform rt = strip.GetComponent<RectTransform>();
            rt.anchorMin = mins[i];
            rt.anchorMax = maxs[i];
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        return group;
    }

    private static void AddBackdrop(Transform parent, Sprite sprite, Color tint)
    {
        if (sprite == null)
            return;

        GameObject go = new GameObject("Backdrop");
        go.transform.SetParent(parent, false);
        go.transform.SetAsFirstSibling();
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = false;
        image.color = tint;
        image.raycastTarget = false;
        Stretch(go.GetComponent<RectTransform>());
    }

    private static GameObject MakeDim(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.color = color;
        Stretch(go.GetComponent<RectTransform>());
        return go;
    }

    private static GameObject MakeCard(Transform parent, string name, Sprite panel, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        if (panel != null)
        {
            image.sprite = panel;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.pixelsPerUnitMultiplier = 1.1f;
        }
        else
        {
            image.color = new Color(0.08f, 0.07f, 0.06f, 0.92f);
        }

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        return go;
    }

    private static Text MakeHudRow(
        Transform parent,
        string name,
        Sprite icon,
        string copy,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 pos,
        float width,
        Font font)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.AddComponent<RectTransform>();
        rowRt.anchorMin = rowRt.anchorMax = anchor;
        rowRt.pivot = pivot;
        rowRt.anchoredPosition = pos;
        rowRt.sizeDelta = new Vector2(width, 52f);

        float textLeft = 0f;
        if (icon != null)
        {
            GameObject iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(row.transform, false);
            Image image = iconGo.AddComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            RectTransform irt = iconGo.GetComponent<RectTransform>();
            irt.anchorMin = irt.anchorMax = irt.pivot = new Vector2(0f, 0.5f);
            irt.anchoredPosition = Vector2.zero;
            irt.sizeDelta = new Vector2(48f, 48f);
            textLeft = 56f;
        }

        Text text = MakeText(row.transform, "Label", copy, 26, TextAnchor.MiddleLeft);
        text.alignment = TextAnchor.MiddleLeft;
        if (font != null)
            text.font = font;
        RectTransform trt = text.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(textLeft, 0f);
        trt.offsetMax = Vector2.zero;
        return text;
    }

    private static Text MakeText(Transform parent, string name, string copy, int size, TextAnchor anchor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.text = copy;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = new Color(0.95f, 0.93f, 0.88f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        RectTransform rt = text.rectTransform;
        rt.anchorMin = new Vector2(0.1f, 0.2f);
        rt.anchorMax = new Vector2(0.9f, 0.8f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return text;
    }

    private static void PadText(RectTransform rt, float x, float y)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(x, y);
        rt.offsetMax = new Vector2(-x, -y);
    }

    private static void MakeRestartButton(Transform parent, string name, string label, Vector2 anchored, Sprite sprite, Font font)
    {
        MakeActionButton(parent, name, label, anchored, sprite, font, () =>
        {
            if (GameManager.Instance != null)
                GameManager.Instance.Restart();
        });
    }

    private static GameObject MakeActionButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchored,
        Sprite sprite,
        Font font,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        Button button = go.AddComponent<Button>();
        if (onClick != null)
            button.onClick.AddListener(onClick);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchored;

        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            rt.sizeDelta = new Vector2(360f, 99f);

            Text caption = MakeText(go.transform, "Label", label, 22, TextAnchor.MiddleCenter);
            if (font != null)
                caption.font = font;
            RectTransform captionRt = caption.rectTransform;
            captionRt.anchorMin = captionRt.anchorMax = new Vector2(0.5f, 0f);
            captionRt.pivot = new Vector2(0.5f, 1f);
            captionRt.anchoredPosition = new Vector2(0f, -6f);
            captionRt.sizeDelta = new Vector2(300f, 32f);
        }
        else
        {
            image.color = new Color(0.18f, 0.18f, 0.18f, 0.95f);
            rt.sizeDelta = new Vector2(300f, 64f);
            Text text = MakeText(go.transform, "Label", label, 24, TextAnchor.MiddleCenter);
            if (font != null)
                text.font = font;
            Stretch(text.rectTransform);
        }

        return go;
    }

    private static Sprite LoadUiSprite(string fileName, bool keyBlack)
    {
        string path = "UI/" + fileName;
        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex == null)
            return null;

        if (keyBlack)
            tex = KeyBlackToAlpha(tex);

        Sprite imported = Resources.Load<Sprite>(path);
        Vector4 border = imported != null ? imported.border : Vector4.zero;
        if (fileName == "UI_Panel_Dark" && border.sqrMagnitude < 1f)
            border = new Vector4(64f, 64f, 64f, 64f);
        return Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            border);
    }

    private static Sprite LoadConceptSprite(string fileName)
    {
        string path = "Concept/" + fileName;
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null)
            return sprite;

        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex == null)
            return null;

        return Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    private static Texture2D KeyBlackToAlpha(Texture2D src)
    {
        Color32[] pixels;
        try
        {
            pixels = src.GetPixels32();
        }
        catch (UnityException)
        {
            return src;
        }

        var copy = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false)
        {
            name = src.name + "_UI",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int i = 0; i < pixels.Length; i++)
        {
            int m = pixels[i].r;
            if (pixels[i].g > m)
                m = pixels[i].g;
            if (pixels[i].b > m)
                m = pixels[i].b;
            int a = (m - 10) * 12;
            if (a < 0)
                a = 0;
            if (a > 255)
                a = 255;
            pixels[i].a = (byte)a;
        }

        copy.SetPixels32(pixels);
        copy.Apply(false, true);
        return copy;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
