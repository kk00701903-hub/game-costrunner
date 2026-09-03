using UnityEngine;

public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }

    [Header("Clips (auto-loads Resources/Audio if empty)")]
    [SerializeField] private AudioClip windLoop;
    [SerializeField] private AudioClip boardRumble;
    [SerializeField] private AudioClip bgmDrone;
    [SerializeField] private AudioClip supplyPickup;
    [SerializeField] private AudioClip collisionThud;

    private AudioSource _wind;
    private AudioSource _board;
    private AudioSource _bgm;
    private AudioSource _sfx;
    private AudioSource _creak;
    private PlayerController _player;
    private PlayerVitals _vitals;
    private float _windTarget = 0.14f;
    private float _bgmTarget;
    private float _windSpike;
    private float _collapsePressure;
    private float _duckTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        LoadClips();
        _wind = MakeSource("Wind", windLoop, true, 0.14f);
        _board = MakeSource("Board", boardRumble, true, 0f);
        _bgm = MakeSource("Bgm", bgmDrone, true, 0f);
        _sfx = MakeSource("Sfx", null, false, 1f);
        _creak = MakeSource("DeckCreak", boardRumble, true, 0f);
        _creak.pitch = 0.55f;
    }

    private void Start()
    {
        _player = FindObjectOfType<PlayerController>();
        _vitals = _player != null ? _player.GetComponent<PlayerVitals>() : null;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRunStarted += HandleRunStarted;
            GameManager.Instance.OnGameOver += HandleGameOver;
            GameManager.Instance.OnEnding += HandleEnding;
            GameManager.Instance.OnSuppliesChanged += HandleSupply;
        }

        if (_wind.clip != null)
            _wind.Play();
    }

    private void Update()
    {
        bool playing = GameManager.Instance != null && GameManager.Instance.IsPlaying;
        float boardTarget = 0f;
        float pitch = 1f;

        if (playing && _player != null && !_player.IsDead)
        {
            float t = Mathf.InverseLerp(9f, 20f, _player.CurrentSpeed);
            boardTarget = _player.IsGrounded ? Mathf.Lerp(0.08f, 0.3f, t) : 0.04f;
            if (_player.IsSliding)
                boardTarget *= 0.65f;
            pitch = Mathf.Lerp(0.88f, 1.16f, t);
        }

        if (_duckTimer > 0f)
            _duckTimer -= Time.deltaTime;

        // The board keeps running through a SILENCE line; everything else drops
        // out so the only thing left is the sound of still moving.
        float duck = _duckTimer > 0f ? 0f : 1f;

        if (_board.clip != null)
        {
            _board.volume = Mathf.MoveTowards(_board.volume, boardTarget, Time.deltaTime * 1.6f);
            _board.pitch = Mathf.MoveTowards(_board.pitch, pitch, Time.deltaTime * 1.2f);
            if (!_board.isPlaying && _board.volume > 0.02f)
                _board.Play();
        }

        UpdateDeckCreak(playing, duck);

        if (_wind.clip != null)
        {
            _windSpike = Mathf.MoveTowards(_windSpike, 0f, Time.deltaTime * 0.55f);
            float target = (_windTarget + _windSpike + _collapsePressure * 0.35f) * duck;
            _wind.volume = Mathf.MoveTowards(_wind.volume, target, Time.deltaTime * 1.4f);

            // The collapse line reads as a low rumble rather than more wind.
            float targetPitch = Mathf.Lerp(1f, 0.62f, _collapsePressure);
            _wind.pitch = Mathf.MoveTowards(_wind.pitch, targetPitch, Time.deltaTime * 0.7f);
        }

        if (_bgm.clip != null)
        {
            _bgm.volume = Mathf.MoveTowards(_bgm.volume, _bgmTarget * duck, Time.deltaTime * 0.5f);
            if (!_bgm.isPlaying && _bgm.volume > 0.02f)
                _bgm.Play();
        }
    }

    /// One crack left in the deck adds a creak to the ride, which is the only
    /// warning the HUD-less health display gives.
    private void UpdateDeckCreak(bool playing, float duck)
    {
        if (_creak == null || _creak.clip == null)
            return;

        if (_vitals == null && _player != null)
            _vitals = _player.GetComponent<PlayerVitals>();

        bool creaking = playing && _vitals != null && _vitals.Hp == 1 && _player != null && !_player.IsDead;
        float target = creaking ? 0.22f * duck : 0f;
        _creak.volume = Mathf.MoveTowards(_creak.volume, target, Time.deltaTime * 1.2f);

        if (!_creak.isPlaying && _creak.volume > 0.02f)
            _creak.Play();
    }

    private void LoadClips()
    {
        if (windLoop == null)
            windLoop = Resources.Load<AudioClip>("Audio/Audio_Wind_Loop");
        if (boardRumble == null)
            boardRumble = Resources.Load<AudioClip>("Audio/Audio_Board_Rumble");
        if (bgmDrone == null)
            bgmDrone = Resources.Load<AudioClip>("Audio/BGM_Drone_Low");
        if (supplyPickup == null)
            supplyPickup = Resources.Load<AudioClip>("Audio/Audio_Supply_Pickup");
        if (collisionThud == null)
            collisionThud = Resources.Load<AudioClip>("Audio/Audio_Collision_Thud");
    }

    private AudioSource MakeSource(string name, AudioClip clip, bool loop, float volume)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = loop;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.dopplerLevel = 0f;
        src.volume = volume;
        return src;
    }

    /// A short wind swell instead of a new clip, since the world has no engines.
    public void PlayTurn()
    {
        if (_wind == null || _wind.clip == null)
            return;

        _windSpike = 0.34f;
        _wind.volume = Mathf.Min(1f, _wind.volume + _windSpike);
        _wind.pitch = 1.28f;
    }

    /// 0 = collapse line at its normal distance, 1 = about to be swallowed.
    public void SetCollapsePressure(float pressure)
    {
        _collapsePressure = Mathf.Clamp01(pressure);
    }

    public void PlayHit()
    {
        if (_sfx == null || collisionThud == null)
            return;

        _sfx.pitch = Random.Range(0.82f, 0.95f);
        _sfx.PlayOneShot(collisionThud, 0.55f);
        _sfx.pitch = 1f;
    }

    public void PlayShieldBreak()
    {
        if (_sfx == null || collisionThud == null)
            return;

        _sfx.pitch = 1.85f;
        _sfx.PlayOneShot(collisionThud, 0.4f);
        _sfx.pitch = 1f;
    }

    /// The counter window cue has to own a band nothing else uses, so it is a
    /// short metallic chirp well above the wind and the board.
    public void PlayCounterCue()
    {
        if (CounterCueTone.Instance != null)
        {
            CounterCueTone.Instance.PlayOpen();
            return;
        }

        if (_sfx == null)
            return;

        AudioClip clip = supplyPickup != null ? supplyPickup : collisionThud;
        if (clip == null)
            return;

        _sfx.pitch = 1.55f;
        _sfx.PlayOneShot(clip, 0.7f);
        _sfx.pitch = 1f;
    }

    public void PlayCounterHit()
    {
        if (_sfx == null || collisionThud == null)
            return;

        _sfx.pitch = 0.68f;
        _sfx.PlayOneShot(collisionThud, 0.85f);
        _sfx.pitch = 1f;
    }

    /// Master ducking for a SILENCE story line. Everything but the ride drops.
    public void DuckAll(float seconds)
    {
        _duckTimer = Mathf.Max(_duckTimer, seconds);
    }

    private void HandleRunStarted()
    {
        _windTarget = 0.22f;
        _bgmTarget = 0.2f;
        if (_bgm.clip != null && !_bgm.isPlaying)
            _bgm.Play();
        if (_board.clip != null && !_board.isPlaying)
            _board.Play();
    }

    private void HandleGameOver()
    {
        _windTarget = 0.08f;
        _bgmTarget = 0.06f;
        if (_board != null)
            _board.volume = 0f;
        if (collisionThud != null && _sfx != null)
            _sfx.PlayOneShot(collisionThud, 0.9f);
    }

    private void HandleEnding()
    {
        _windTarget = 0.16f;
        _bgmTarget = 0.24f;
        if (_board != null)
            _board.volume = 0f;
    }

    private void HandleSupply(int count)
    {
        if (supplyPickup != null && count > 0)
            _sfx.PlayOneShot(supplyPickup, 0.85f);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRunStarted -= HandleRunStarted;
            GameManager.Instance.OnGameOver -= HandleGameOver;
            GameManager.Instance.OnEnding -= HandleEnding;
            GameManager.Instance.OnSuppliesChanged -= HandleSupply;
        }

        if (Instance == this)
            Instance = null;
    }
}
