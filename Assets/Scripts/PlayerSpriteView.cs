using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerSpriteView : MonoBehaviour
{
    private const string ResourceFolder = "Character/";

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Camera viewCamera;
    [SerializeField] private bool billboard = true;
    [SerializeField] private float displayHeight = 1.28f;
    [SerializeField] private Vector3 visualLocalPosition = new Vector3(0f, -0.92f, 0.08f);

    [Header("Back-view poses (auto-loads from Resources/Character if empty)")]
    [SerializeField] private Sprite idle;
    [SerializeField] private Sprite pushing;
    [SerializeField] private Sprite jump;
    [SerializeField] private Sprite slide;
    [SerializeField] private Sprite leanLeft;
    [SerializeField] private Sprite leanRight;

    [Header("Deck condition (auto-loads Deck_Ok / Deck_Crack / Deck_Broken)")]
    [SerializeField] private Sprite deckOk;
    [SerializeField] private Sprite deckCracked;
    [SerializeField] private Sprite deckBroken;

    [SerializeField] private float leanThreshold = 0.35f;

    private PlayerController _player;
    private PlayerVitals _vitals;
    private Transform _visual;
    private SpriteRenderer _deck;
    private float _uniformScale = 1f;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
        _vitals = GetComponent<PlayerVitals>();
        LoadDefaultSprites();
        EnsureVisual();
        EnsureDeck();
        CacheScale();
        HidePlaceholderMesh();
    }

    private void LateUpdate()
    {
        if (_player == null || spriteRenderer == null)
            return;

        if (billboard)
            Billboard();

        Sprite next = PickSprite();
        if (spriteRenderer.sprite != next)
            spriteRenderer.sprite = next;

        ApplyDeckCondition();

        if (_visual != null)
            _visual.localScale = Vector3.one * _uniformScale;
    }

    private void LoadDefaultSprites()
    {
        if (idle == null)
            idle = LoadPose("Doha_Idle", "Girl_Idle");
        if (pushing == null)
            pushing = LoadPose("Doha_Push", "Girl_Push");
        if (jump == null)
            jump = LoadPose("Doha_Jump", "Girl_Jump");
        if (slide == null)
            slide = LoadPose("Doha_Slide", "Girl_Slide");
        if (leanLeft == null)
            leanLeft = LoadPose("Doha_LeanL", "Girl_LeanL");
        if (leanRight == null)
            leanRight = LoadPose("Doha_LeanR", "Girl_LeanR");

        if (deckOk == null)
            deckOk = LoadPose("Deck_Ok", null);
        if (deckCracked == null)
            deckCracked = LoadPose("Deck_Crack", null);
        if (deckBroken == null)
            deckBroken = LoadPose("Deck_Broken", null);
    }

    /// New city art drops in as Doha_*; the old sprites stay as a fallback so
    /// nothing has to change in code when it lands.
    private static Sprite LoadPose(string fileName, string fallbackName)
    {
        Sprite sprite = LoadSingle(fileName);
        if (sprite == null && !string.IsNullOrEmpty(fallbackName))
            sprite = LoadSingle(fallbackName);

        return sprite;
    }

    private static Sprite LoadSingle(string fileName)
    {
        string path = ResourceFolder + fileName;
        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex != null)
        {
            Texture2D keyed = KeyNearBlack(tex);
            return Sprite.Create(
                keyed,
                new Rect(0f, 0f, keyed.width, keyed.height),
                new Vector2(0.5f, 0f),
                200f);
        }

        return Resources.Load<Sprite>(path);
    }

    private static Texture2D KeyNearBlack(Texture2D src)
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
            name = src.name,
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
            int a = pixels[i].a;
            if (m < 14)
                a = 0;
            else if (m < 36)
                a = a * (m - 14) / 22;
            pixels[i].a = (byte)Mathf.Clamp(a, 0, 255);
        }

        copy.SetPixels32(pixels);
        copy.Apply(false, true);
        return copy;
    }

    private void EnsureVisual()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(transform, false);
            spriteRenderer = visual.AddComponent<SpriteRenderer>();
            spriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            spriteRenderer.receiveShadows = false;
        }

        _visual = spriteRenderer.transform;
        _visual.localPosition = visualLocalPosition;
        spriteRenderer.sprite = idle != null ? idle : pushing;
        spriteRenderer.sortingOrder = 12;
    }

    private void EnsureDeck()
    {
        if (deckOk == null && deckCracked == null && deckBroken == null)
            return;

        GameObject go = new GameObject("Deck");
        go.transform.SetParent(_visual != null ? _visual : transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        _deck = go.AddComponent<SpriteRenderer>();
        _deck.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _deck.receiveShadows = false;
        _deck.sortingOrder = 1;
    }

    /// The health display lives on the board, not in a corner of the screen.
    /// Deck art is optional, so the tint carries the reading until it arrives.
    private void ApplyDeckCondition()
    {
        int hp = _vitals != null ? _vitals.Hp : 3;
        bool flash = _vitals != null && _vitals.IsInvulnerable && Mathf.Repeat(Time.time, 0.16f) < 0.08f;

        if (_deck != null)
        {
            Sprite want = hp >= 3 ? deckOk : hp == 2 ? deckCracked : deckBroken;
            if (want == null)
                want = deckOk != null ? deckOk : deckCracked;
            if (_deck.sprite != want)
                _deck.sprite = want;

            _deck.color = flash ? Color.white : Color.white * 0.95f;
        }

        Color tint = hp >= 3 ? Color.white
            : hp == 2 ? new Color(1f, 0.92f, 0.88f)
            : new Color(1f, 0.78f, 0.74f);

        spriteRenderer.color = flash ? Color.white : tint;
    }

    private void HidePlaceholderMesh()
    {
        MeshRenderer mesh = GetComponent<MeshRenderer>();
        if (mesh != null)
            mesh.enabled = false;
    }

    private void CacheScale()
    {
        Sprite source = idle != null ? idle : spriteRenderer != null ? spriteRenderer.sprite : null;
        if (source == null || source.bounds.size.y < 0.01f)
            return;

        _uniformScale = displayHeight / source.bounds.size.y;
    }

    private void Billboard()
    {
        if (_visual == null)
            return;

        Camera cam = viewCamera != null ? viewCamera : Camera.main;
        if (cam == null)
            return;

        _visual.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
    }

    private Sprite PickSprite()
    {
        if (_player.IsDead)
            return First(idle, pushing);

        if (_player.IsSliding)
            return First(slide, idle);

        if (_player.IsJumping || !_player.IsGrounded)
            return First(jump, idle);

        int turnLean = _player.TurnLeanDirection;
        if (turnLean != 0)
            return First(turnLean > 0 ? leanRight : leanLeft, idle);

        // Lean is driven by the gap to the lane target inside the player's own
        // frame, so it still reads correctly after the run changes direction.
        float dx = _player.LaneTargetOffset - _player.LateralOffset;
        if (dx > leanThreshold)
            return First(leanRight, idle);
        if (dx < -leanThreshold)
            return First(leanLeft, idle);

        if (_player.CurrentSpeed > 0.1f)
            return First(pushing, idle);

        return First(idle, pushing);
    }

    private static Sprite First(Sprite a, Sprite b)
    {
        return a != null ? a : b;
    }
}
