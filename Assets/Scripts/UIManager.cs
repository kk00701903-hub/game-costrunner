using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    /// The convenience store, three seconds before the floor goes. Nothing here
    /// explains the city; it just states what has already been decided.
    public const string PrologueCopy =
        "편의점 냉장고 문이 열려 있었다. 도하는 배달 두 건을 남겨두고 있었다.\n\n" +
        "지반이 동쪽부터 내려앉기 시작한다.\n\n" +
        "「자산 A-0347. 회수 절차를 시작합니다. 협조에 감사드립니다.」";

    [Header("Prologue")]
    [SerializeField] private GameObject prologuePanel;
    [SerializeField] private Text prologueText;
    [SerializeField] private Text prologueCta;
    [SerializeField] private float prologueSeconds = 3f;

    [Header("HUD")]
    [SerializeField] private GameObject hudRoot;
    [SerializeField] private Text distanceText;
    [SerializeField] private Text suppliesText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text bestText;
    [SerializeField] private Text turnHintText;
    [SerializeField] private Text hpText;
    [SerializeField] private Image[] deckPips;
    [SerializeField] private Text kingText;
    [SerializeField] private Text itemText;
    [SerializeField] private Image itemFill;
    [SerializeField] private Image itemPlate;
    [SerializeField] private Text comboText;
    [SerializeField] private Image subtitlePlate;

    [Header("Story")]
    [SerializeField] private Text subtitleText;
    [SerializeField] private Text bannerText;
    [SerializeField] private Text zoneText;

    [Header("Screen Effects")]
    [Tooltip("Grey wash that drains colour as the collapse line closes in.")]
    [SerializeField] private Image collapseVeil;
    [Tooltip("Red edge vignette shown on the last crack in the deck.")]
    [SerializeField] private CanvasGroup hurtVignette;

    [Header("Popups")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Text gameOverText;
    [SerializeField] private Text gameOverScoreText;
    [SerializeField] private GameObject endingPanel;
    [SerializeField] private Text endingText;
    [SerializeField] private Text endingScoreText;
    [SerializeField] private GameObject continueButton;

    [SerializeField] private Text tutorialHintText;

    private const string NumericHpKey = "r347_numeric_hp";
    private const string SubtitleScaleKey = "r347_subtitle_scale";

    private float _hintFlash;
    private float _veilAlpha;
    private float _comboFlash;
    private float _comboPunch;
    private float _bannerPunch;
    private float _subtitleTimer;
    private float _bannerTimer;
    private float _tutorialHintTimer;
    private float _displayedScore;
    private PlayerController _player;
    private PlayerVitals _vitals;
    private bool _prologueWaiting;
    private bool _paused;
    private Coroutine _logRoutine;

    /// Accessibility option: the deck cracks are the real display, this is the
    /// fallback for players who need a number.
    public static bool NumericHp
    {
        get { return PlayerPrefs.GetInt(NumericHpKey, 0) != 0; }
        set
        {
            PlayerPrefs.SetInt(NumericHpKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public void Bind(
        GameObject prologue,
        Text prologueLabel,
        GameObject hud,
        Text distance,
        Text supplies,
        GameObject gameOver,
        GameObject ending,
        Text endingLabel,
        float seconds = 3f)
    {
        prologuePanel = prologue;
        prologueText = prologueLabel;
        hudRoot = hud;
        distanceText = distance;
        suppliesText = supplies;
        gameOverPanel = gameOver;
        endingPanel = ending;
        endingText = endingLabel;
        prologueSeconds = seconds;
    }

    public void BindExtras(
        Text score,
        Text turnHint,
        Text gameOverHeadline,
        Text gameOverScore,
        Text endingScore,
        GameObject continueBtn)
    {
        scoreText = score;
        turnHintText = turnHint;
        gameOverText = gameOverHeadline;
        gameOverScoreText = gameOverScore;
        endingScoreText = endingScore;
        continueButton = continueBtn;
    }

    public void BindScreenEffects(Image veil, CanvasGroup vignette, Text hp)
    {
        collapseVeil = veil;
        hurtVignette = vignette;
        hpText = hp;
    }

    public void BindCombat(Text king, Text item, Text combo)
    {
        kingText = king;
        itemText = item;
        comboText = combo;
    }

    public void BindStory(Text subtitle, Text banner, Text zone, Text tutorialHint = null)
    {
        subtitleText = subtitle;
        bannerText = banner;
        zoneText = zone;
        tutorialHintText = tutorialHint;
        ApplySubtitleScale();
    }

    public void BindPolish(
        Text best,
        Text prologueTap,
        Image[] pips,
        Image itemSlotFill,
        Image itemSlotPlate,
        Image subtitleBg)
    {
        bestText = best;
        prologueCta = prologueTap;
        deckPips = pips;
        itemFill = itemSlotFill;
        itemPlate = itemSlotPlate;
        subtitlePlate = subtitleBg;
    }

    public void ShowTutorialHint(string text, float seconds)
    {
        if (tutorialHintText == null)
            return;
        tutorialHintText.text = text ?? "";
        tutorialHintText.enabled = !string.IsNullOrEmpty(text);
        _tutorialHintTimer = seconds;
    }

    public void HideTutorialHint()
    {
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "";
            tutorialHintText.enabled = false;
        }
        _tutorialHintTimer = 0f;
    }

    /// 1 small, 2 default, 3 large. Radio is most of the writing in this game,
    /// so the size of it is a real setting rather than a nicety.
    public static int SubtitleScale
    {
        get { return Mathf.Clamp(PlayerPrefs.GetInt(SubtitleScaleKey, 2), 1, 3); }
        set
        {
            PlayerPrefs.SetInt(SubtitleScaleKey, Mathf.Clamp(value, 1, 3));
            PlayerPrefs.Save();
        }
    }

    private void ApplySubtitleScale()
    {
        if (subtitleText == null)
            return;

        subtitleText.fontSize = SubtitleScale == 1 ? 24 : SubtitleScale == 3 ? 34 : UiTheme.SubtitleSize;
    }

    public void ShowSubtitle(Speaker who, string text, float seconds, bool garbled)
    {
        if (subtitleText == null)
            return;

        string name = SpeakerName(who);
        string body = garbled ? "<i>" + text + "</i>" : text;

        subtitleText.text = string.IsNullOrEmpty(name)
            ? body
            : "<color=#" + SpeakerColor(who) + ">" + name + "</color>  " + body;

        _subtitleTimer = Mathf.Max(0.4f, seconds);
        SetSubtitlePlate(true);
    }

    public void ShowBanner(string text, float seconds = 2.2f)
    {
        if (bannerText == null)
            return;

        bannerText.text = text;
        _bannerTimer = seconds;
        _bannerPunch = 1f;
        Color c = bannerText.color;
        c.a = 1f;
        bannerText.color = c;
    }

    private static string SpeakerName(Speaker who)
    {
        switch (who)
        {
            case Speaker.Sweeper:
                return "청소부";
            case Speaker.Bungeo:
                return "붕어";
            case Speaker.Retrieval:
                return "회수반";
            case Speaker.Ihan:
                return "이한";
            case Speaker.Doha:
                return "도하";
            default:
                return string.Empty;
        }
    }

    private static string SpeakerColor(Speaker who)
    {
        switch (who)
        {
            case Speaker.Sweeper:
                return "9AA0A6";
            case Speaker.Bungeo:
                return "E8C74D";
            case Speaker.Retrieval:
                return "D8564B";
            case Speaker.Ihan:
                return "F2F2F2";
            default:
                return "BFD8D0";
        }
    }

    private void UpdateStoryText()
    {
        TutorialHints.Tick();

        if (subtitleText != null && _subtitleTimer > 0f)
        {
            _subtitleTimer -= Time.deltaTime;
            if (_subtitleTimer <= 0f)
            {
                subtitleText.text = string.Empty;
                SetSubtitlePlate(false);
            }
        }

        if (_tutorialHintTimer > 0f)
        {
            _tutorialHintTimer -= Time.deltaTime;
            if (tutorialHintText != null)
            {
                Color c = tutorialHintText.color;
                c.a = Mathf.Lerp(0.55f, 1f, (Mathf.Sin(Time.unscaledTime * 5f) + 1f) * 0.5f);
                tutorialHintText.color = c;
            }

            if (_tutorialHintTimer <= 0f)
                HideTutorialHint();
        }

        if (bannerText == null)
            return;

        if (_bannerTimer > 0f)
        {
            _bannerTimer -= Time.deltaTime;
            float life = Mathf.Clamp01(_bannerTimer);
            Color c = bannerText.color;
            // Hold full, then fade the last half-second.
            c.a = life > 0.45f ? 1f : life / 0.45f;
            bannerText.color = c;

            if (_bannerPunch > 0f)
            {
                _bannerPunch = Mathf.MoveTowards(_bannerPunch, 0f, Time.unscaledDeltaTime * 2.4f);
                float s = 1f + _bannerPunch * 0.12f;
                bannerText.rectTransform.localScale = new Vector3(s, s, 1f);
            }

            if (_bannerTimer <= 0f)
            {
                bannerText.text = string.Empty;
                bannerText.rectTransform.localScale = Vector3.one;
            }
        }
    }

    private void SetSubtitlePlate(bool on)
    {
        if (subtitlePlate == null)
            return;
        Color c = subtitlePlate.color;
        c.a = on ? 0.62f : 0f;
        subtitlePlate.color = c;
    }

    /// Driven by the collapse line. Colour draining out is the only warning the
    /// screen gives before contact.
    public void SetCollapseWarning(float t)
    {
        _veilAlpha = Mathf.Clamp01(t);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        SetActive(hudRoot, false);
        SetActive(gameOverPanel, false);
        SetActive(endingPanel, false);

        if (prologueText != null)
            prologueText.text = PrologueCopy;

        if (turnHintText != null)
            turnHintText.text = string.Empty;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += ShowGameOver;
            GameManager.Instance.OnEnding += ShowEnding;
            GameManager.Instance.OnSuppliesChanged += RefreshSupplies;
            GameManager.Instance.OnPickup += FlashCombo;
        }

        StartCoroutine(RunPrologue());
    }

    private void Update()
    {
        UpdateStoryText();
        UpdatePrologueCta();

        GameManager gm = GameManager.Instance;
        if (gm == null)
            return;

        // Cinematic top pills — distance center, coin/score right.
        if (scoreText != null)
        {
            _displayedScore = Mathf.MoveTowards(_displayedScore, gm.Score, Time.deltaTime * Mathf.Max(420f, gm.Score * 0.8f));
            scoreText.text = "× " + Mathf.FloorToInt(_displayedScore).ToString("N0");
        }

        if (bestText != null)
            bestText.text = "최고 " + Mathf.Max(gm.BestScore, gm.Score).ToString("N0");

        if (distanceText != null)
        {
            distanceText.text = !gm.IsEndless && gm.CurrentZone == Zone.Depot
                ? "DEPOT: " + Mathf.CeilToInt(gm.RemainingDistance).ToString("N0") + " m"
                : "DISTANCE: " + Mathf.FloorToInt(gm.TraveledDistance).ToString("N0") + " m";
        }

        if (zoneText != null)
            zoneText.text = Zones.Label(gm.CurrentZone);

        UpdateTurnHint();
        UpdateScreenEffects();
        UpdateCombatHud(gm);
        UpdateComboMotion();
    }

    private void UpdatePrologueCta()
    {
        if (!_prologueWaiting || prologueCta == null)
            return;

        Color c = prologueCta.color;
        c.a = Mathf.Lerp(0.35f, 1f, (Mathf.Sin(Time.unscaledTime * 3.2f) + 1f) * 0.5f);
        prologueCta.color = c;
        UiChrome.PulseScale(prologueCta.transform, 0.035f, 3.2f);
    }

    private void UpdateComboMotion()
    {
        if (comboText == null)
            return;

        if (_comboPunch > 0f)
        {
            _comboPunch = Mathf.MoveTowards(_comboPunch, 0f, Time.unscaledDeltaTime * 3.5f);
            float s = 1f + _comboPunch * 0.45f;
            comboText.rectTransform.localScale = new Vector3(s, s, 1f);
        }
        else
        {
            comboText.rectTransform.localScale = Vector3.one;
        }
    }

    private void UpdateCombatHud(GameManager gm)
    {
        if (kingText != null)
        {
            KingFight fight = KingFight.Instance;
            bool active = fight != null && fight.Active;
            if (kingText.enabled != active)
                kingText.enabled = active;
            if (active)
                kingText.text = "왕  " + fight.Hp + " / " + fight.MaxHp + "   ·   P" + fight.Phase;
        }

        ItemSlot slot = ItemSlot.Instance;
        bool has = slot != null && (slot.HasItem || slot.HasActive);

        if (itemPlate != null)
        {
            bool framed = itemPlate.sprite != null && itemPlate.sprite.texture != null
                && itemPlate.sprite.texture.width > 8;
            if (framed)
            {
                itemPlate.color = has
                    ? Color.white
                    : new Color(1f, 1f, 1f, 0.55f);
            }
            else
            {
                itemPlate.color = has
                    ? (slot.HasActive ? UiTheme.SlotReady : new Color(0.16f, 0.28f, 0.34f, 0.92f))
                    : UiTheme.SlotIdle;
            }

            itemPlate.transform.localScale = has && slot != null && !slot.HasActive
                ? Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.04f)
                : Vector3.one;
        }

        if (itemFill != null)
        {
            if (slot != null && slot.HasActive)
            {
                itemFill.fillAmount = Mathf.Clamp01(slot.ActiveFraction);
                itemFill.enabled = true;
            }
            else
            {
                itemFill.fillAmount = 0f;
                itemFill.enabled = false;
            }
        }

        if (itemText != null)
        {
            if (slot == null || (!slot.HasItem && !slot.HasActive))
                itemText.text = string.Empty;
            else if (slot.HasActive)
                itemText.text = ItemShort(slot.Active);
            else
                itemText.text = ItemShort(slot.Held) + "\n탭";
        }

        if (comboText != null)
        {
            if (_comboFlash > 0f)
            {
                _comboFlash -= Time.deltaTime;
                comboText.text = "×" + gm.Combo.ToString("0.0");
                Color c = comboText.color;
                c.a = Mathf.Clamp01(_comboFlash / 0.25f);
                if (_comboFlash > 0.55f)
                    c.a = 1f;
                comboText.color = c;
            }
            else if (comboText.text.Length > 0)
            {
                comboText.text = string.Empty;
            }
        }
    }

    private static string ItemShort(PickupKind kind)
    {
        switch (kind)
        {
            case PickupKind.BoosterCell:
                return "부스터";
            case PickupKind.Shield:
                return "차폐";
            case PickupKind.ReverseScan:
                return "역스캔";
            default:
                return string.Empty;
        }
    }

    private void UpdateScreenEffects()
    {
        if (collapseVeil != null)
        {
            Color c = collapseVeil.color;
            c.a = Mathf.MoveTowards(c.a, _veilAlpha * 0.42f, Time.deltaTime * 0.9f);
            collapseVeil.color = c;
        }

        PlayerVitals vitals = VitalsRef();
        int hp = vitals != null ? vitals.Hp : 3;
        int max = vitals != null ? vitals.MaxHp : 3;

        if (hurtVignette != null)
        {
            float want = hp <= 1 ? 0.72f : 0f;
            hurtVignette.alpha = Mathf.MoveTowards(hurtVignette.alpha, want, Time.deltaTime * 1.4f);
        }

        UpdateDeckPips(hp, max);

        if (hpText != null)
        {
            bool show = NumericHp;
            if (hpText.enabled != show)
                hpText.enabled = show;
            if (show)
                hpText.text = "데크 " + hp + " / " + max;
        }
    }

    private void UpdateDeckPips(int hp, int max)
    {
        if (deckPips == null || deckPips.Length == 0)
            return;

        Sprite filled = UiArt.DeckPip(Mathf.Max(1, hp));
        Sprite empty = UiArt.DeckPip(1);
        for (int i = 0; i < deckPips.Length; i++)
        {
            if (deckPips[i] == null)
                continue;
            bool on = i < hp && i < max;
            if (on && filled != null)
                deckPips[i].sprite = filled;
            else if (!on && empty != null)
                deckPips[i].sprite = empty;

            Color c = on ? Color.white : new Color(1f, 1f, 1f, 0.28f);
            if (on && filled == null)
                c = hp <= 1 ? UiTheme.DeckCrit : hp == 2 ? UiTheme.DeckWarn : UiTheme.DeckOk;
            deckPips[i].color = c;
            deckPips[i].rectTransform.localScale = on && hp <= 1
                ? Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 8f) * 0.08f)
                : Vector3.one;
        }
    }

    private PlayerVitals VitalsRef()
    {
        if (_vitals == null)
        {
            PlayerController player = PlayerRef();
            if (player != null)
                _vitals = player.GetComponent<PlayerVitals>();
        }

        return _vitals;
    }

    private void UpdateTurnHint()
    {
        if (turnHintText == null)
            return;

        PlayerController player = PlayerRef();
        int dir = player != null ? player.TurnHintDirection : 0;

        if (dir == 0)
        {
            turnHintText.text = string.Empty;
            turnHintText.rectTransform.localScale = Vector3.one;
            return;
        }

        _hintFlash += Time.deltaTime * 6f;
        turnHintText.text = dir < 0 ? "◀  왼쪽" : "오른쪽  ▶";
        Color c = turnHintText.color;
        c.a = Mathf.Lerp(0.55f, 1f, (Mathf.Sin(_hintFlash) + 1f) * 0.5f);
        turnHintText.color = c;
        float s = 1f + Mathf.Sin(_hintFlash) * 0.06f;
        turnHintText.rectTransform.localScale = new Vector3(s, s, 1f);
    }

    private PlayerController PlayerRef()
    {
        if (_player == null)
            _player = FindObjectOfType<PlayerController>();

        return _player;
    }

    private IEnumerator RunPrologue()
    {
        ResumeIfPaused();
        OnboardingMetrics.PrologueShown();
        SetActive(prologuePanel, true);
        _prologueWaiting = true;

        // Fail-proof first input: any key/tap dismisses after a short beat.
        float minShow = 0.55f;
        float shown = 0f;
        while (_prologueWaiting)
        {
            shown += Time.unscaledDeltaTime;
            if (shown >= minShow && AnyDismissInput())
            {
                OnboardingMetrics.FirstInput();
                _prologueWaiting = false;
            }

            if (shown >= Mathf.Max(minShow, prologueSeconds))
                _prologueWaiting = false;

            yield return null;
        }

        SetActive(prologuePanel, false);
        SetActive(hudRoot, true);
        _displayedScore = 0f;
        RefreshSupplies(GameManager.Instance != null ? GameManager.Instance.Supplies : 0);
        TutorialHints.ResetRun();

        if (GameManager.Instance != null)
            GameManager.Instance.BeginRun();

        EconomyBootstrap.GrantTutorialEnd();
    }

    private static bool AnyDismissInput()
    {
        if (Input.anyKeyDown)
            return true;
        if (Input.GetMouseButtonDown(0))
            return true;
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            return true;
        return false;
    }

    /// No "GAME OVER". Losing a run is a filing action taken by someone who is
    /// not present, and the screen is that filing entry.
    private void ShowGameOver()
    {
        ResumeIfPaused();
        GameManager gm = GameManager.Instance;
        SetActive(hudRoot, false);
        SetActive(gameOverPanel, true);

        if (gameOverScoreText != null && gm != null)
        {
            gameOverScoreText.text =
                "회차 " + gm.RunCount +
                "   ·   " + Mathf.FloorToInt(gm.DeathDistance).ToString("N0") + "m" +
                "   ·   점수 " + gm.Score.ToString("N0") +
                "\n최고 " + gm.BestScore.ToString("N0") + "   ·   태그 " + gm.Tags;
        }

        if (_logRoutine != null)
            StopCoroutine(_logRoutine);

        if (gm != null && gm.IsFirstDeathScreen)
            _logRoutine = StartCoroutine(TypeRecoveryLog(gm));
        else if (gameOverText != null && gm != null)
            gameOverText.text = gm.RecoveryLog() + "\n\n" + gm.CauseCopy();
    }

    private IEnumerator TypeRecoveryLog(GameManager gm)
    {
        // First death: unskippable typewriter. The remark line arrives late.
        string body =
            "회수 완료 — A-0347\n" +
            Zones.Label(gm.DeathZone) + " / 주행 " +
            Mathf.FloorToInt(gm.DeathDistance).ToString("N0") + "m / 회차 " + gm.RunCount +
            "\n\n" +
            "구역 상태 : 지반 침하 진행 중 (서 → 동)\n" +
            "회수 근거 : 재난대응 임시령 4조 — 구역 내 자산 회수";
        string remark = "비고      : " + gm.RecoveryRemark();

        if (gameOverText == null)
            yield break;

        gameOverText.text = "";
        float perChar = 4.5f / Mathf.Max(1, body.Length);
        for (int i = 0; i < body.Length; i++)
        {
            gameOverText.text += body[i];
            yield return new WaitForSecondsRealtime(perChar);
        }

        yield return new WaitForSecondsRealtime(0.8f);
        gameOverText.text += "\n";
        for (int i = 0; i < remark.Length; i++)
        {
            gameOverText.text += remark[i];
            yield return new WaitForSecondsRealtime(0.04f);
        }
    }

    private void ShowEnding()
    {
        GameManager gm = GameManager.Instance;
        SetActive(hudRoot, false);

        if (endingText != null && gm != null)
            endingText.text = gm.EndingCopy();

        if (endingScoreText != null && gm != null)
        {
            endingScoreText.text =
                "회차 " + gm.RunCount +
                "   ·   점수 " + gm.Score.ToString("N0") +
                "   ·   최고 " + gm.BestScore.ToString("N0") +
                "\n편지 " + gm.Letters + " / 5";
        }

        // Nothing to continue into once the 347th attempt has been filed.
        bool canContinue = gm != null && !gm.IsEndless && gm.Ending != EndingKind.Hidden347;
        SetActive(continueButton, canContinue);
        SetActive(endingPanel, true);
    }

    /// Wired to the ending panel button: hides the popup and hands control back.
    public void ContinueRun()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.ContinueRun();
        SetActive(endingPanel, false);
        SetActive(hudRoot, true);
    }

    public void TogglePause()
    {
        _paused = !_paused;
        Time.timeScale = _paused ? 0f : 1f;
    }

    private void ResumeIfPaused()
    {
        if (!_paused)
            return;
        _paused = false;
        Time.timeScale = 1f;
    }

    private void RefreshSupplies(int count)
    {
        if (suppliesText == null)
            return;

        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            suppliesText.text = "수집 " + count;
            return;
        }

        suppliesText.text = "태그 " + gm.Tags + "   편지 " + gm.Letters + "   조각 " + gm.DeckPieces;
    }

    private void FlashCombo(PickupKind kind)
    {
        if (kind == PickupKind.Tag)
        {
            _comboFlash = 0.95f;
            _comboPunch = 1f;
        }
    }

    private static void SetActive(GameObject go, bool on)
    {
        if (go != null)
            go.SetActive(on);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= ShowGameOver;
            GameManager.Instance.OnEnding -= ShowEnding;
            GameManager.Instance.OnSuppliesChanged -= RefreshSupplies;
            GameManager.Instance.OnPickup -= FlashCombo;
        }

        if (Instance == this)
            Instance = null;
    }
}
