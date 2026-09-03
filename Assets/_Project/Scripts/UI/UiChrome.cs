using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// Runtime UI factory. Keeps BindTestUi readable and every plate on-theme.
public static class UiChrome
{
    public static Font BuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public static Image SoftPlate(Transform parent, string name, Color color, Vector2 size, Vector2 anchored)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.sprite = WhiteSprite();
        img.color = color;
        img.raycastTarget = false;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchored;
        return img;
    }

    public static Text Label(
        Transform parent,
        string name,
        string copy,
        int size,
        TextAnchor align,
        Color color,
        Font font = null)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.text = copy;
        text.fontSize = size;
        text.alignment = align;
        text.color = color;
        text.font = font != null ? font : BuiltinFont();
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.supportRichText = true;
        return text;
    }

    public static Outline Outline(Graphic g, Color color, Vector2 distance)
    {
        Outline o = g.gameObject.AddComponent<Outline>();
        o.effectColor = color;
        o.effectDistance = distance;
        return o;
    }

    public static Shadow Drop(Graphic g, Color color, Vector2 distance)
    {
        Shadow s = g.gameObject.AddComponent<Shadow>();
        s.effectColor = color;
        s.effectDistance = distance;
        return s;
    }

    /// Three deck pips — the real HP read. Numbers are accessibility only.
    public static Image[] DeckPips(
        Transform parent,
        string name,
        Vector2 anchored,
        Sprite ok = null,
        Sprite cracked = null,
        Sprite broken = null)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        RectTransform rt = root.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = anchored;
        rt.sizeDelta = new Vector2(128f, 44f);

        Image[] pips = new Image[3];
        Sprite[] stages = { ok, cracked, broken };
        for (int i = 0; i < 3; i++)
        {
            GameObject pip = new GameObject("Pip" + i);
            pip.transform.SetParent(root.transform, false);
            Image img = pip.AddComponent<Image>();
            Sprite stage = stages[Mathf.Min(i, stages.Length - 1)];
            // All three start healthy; UIManager recolors/swaps by HP.
            img.sprite = ok != null ? ok : (stage != null ? stage : WhiteSprite());
            img.preserveAspect = true;
            img.color = Color.white;
            img.raycastTarget = false;
            RectTransform prt = pip.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0f, 0.5f);
            prt.anchoredPosition = new Vector2(i * 48f, 0f);
            prt.sizeDelta = new Vector2(42f, 48f);
            pips[i] = img;
        }

        return pips;
    }

    public static Sprite WhiteSprite()
    {
        if (_white != null)
            return _white;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        Color32[] px = new Color32[16];
        for (int i = 0; i < px.Length; i++)
            px[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(px);
        tex.Apply(false, true);
        _white = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
        return _white;
    }

    private static Sprite _white;

    public static RectTransform ItemSlotFrame(
        Transform parent,
        out Image fill,
        out Text label,
        Sprite frame = null)
    {
        GameObject root = new GameObject("ItemSlot");
        root.transform.SetParent(parent, false);
        RectTransform rt = root.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(UiTheme.SafeSide, UiTheme.SafeBottom + 168f);
        rt.sizeDelta = new Vector2(112f, 112f);

        Image plate = root.AddComponent<Image>();
        plate.sprite = frame != null ? frame : WhiteSprite();
        plate.preserveAspect = true;
        plate.color = frame != null ? Color.white : UiTheme.SlotIdle;
        plate.raycastTarget = false;

        if (frame == null)
            Outline(plate, new Color(1f, 1f, 1f, 0.18f), new Vector2(2f, -2f));

        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(root.transform, false);
        fill = fillGo.AddComponent<Image>();
        fill.sprite = WhiteSprite();
        fill.color = new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.45f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = (int)Image.Origin360.Top;
        fill.fillClockwise = false;
        fill.raycastTarget = false;
        Stretch(fill.rectTransform);
        fill.rectTransform.offsetMin = new Vector2(8f, 8f);
        fill.rectTransform.offsetMax = new Vector2(-8f, -8f);

        label = Label(root.transform, "Label", string.Empty, 22, TextAnchor.MiddleCenter, UiTheme.Ink);
        RectTransform lrt = label.rectTransform;
        Stretch(lrt);
        lrt.offsetMin = new Vector2(10f, 10f);
        lrt.offsetMax = new Vector2(-10f, -10f);

        return rt;
    }

    public static GameObject SubtitlePlate(Transform parent, out Text text)
    {
        GameObject root = new GameObject("SubtitlePlate");
        root.transform.SetParent(parent, false);
        RectTransform rt = root.AddComponent<RectTransform>();
        // Lower third — keep the lane/read clear in portrait and wide Game View.
        rt.anchorMin = new Vector2(0.08f, 0.08f);
        rt.anchorMax = new Vector2(0.92f, 0.14f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image plate = root.AddComponent<Image>();
        plate.sprite = WhiteSprite();
        plate.color = UiTheme.PillBg;
        plate.raycastTarget = false;

        text = Label(root.transform, "Subtitle", string.Empty, UiTheme.SubtitleSize, TextAnchor.MiddleCenter, UiTheme.Ink);
        RectTransform trt = text.rectTransform;
        Stretch(trt);
        trt.offsetMin = new Vector2(24f, 8f);
        trt.offsetMax = new Vector2(-24f, -8f);
        text.fontStyle = FontStyle.Normal;

        return root;
    }

    public static Button PrimaryCta(
        Transform parent,
        string name,
        string copy,
        Vector2 anchored,
        Sprite sprite,
        UnityAction onClick)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(0.85f, 0.62f, 0.12f, 0.95f);
        }

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        if (onClick != null)
            btn.onClick.AddListener(onClick);

        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(1f, 0.92f, 0.55f, 1f);
        colors.pressedColor = new Color(0.75f, 0.55f, 0.12f, 1f);
        btn.colors = colors;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchored;
        rt.sizeDelta = new Vector2(420f, 96f);

        Text label = Label(go.transform, "Label", copy, UiTheme.CtaSize, TextAnchor.MiddleCenter, UiTheme.Ink);
        Stretch(label.rectTransform);
        Drop(label, new Color(0f, 0f, 0f, 0.55f), new Vector2(0f, -2f));

        return btn;
    }

    public static void PulseScale(Transform t, float amp, float speed)
    {
        if (t == null)
            return;
        float s = 1f + Mathf.Sin(Time.unscaledTime * speed) * amp;
        t.localScale = new Vector3(s, s, 1f);
    }

    /// Dark rounded pill — cinematic HUD chip (reference: distance / coin counters).
    public static RectTransform HudPill(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.sprite = WhiteSprite();
        img.color = UiTheme.PillBg;
        img.raycastTarget = false;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = new Vector2(0f, -(UiTheme.SafeTop + UiTheme.HudTopInset + UiTheme.PillHeight * 0.5f));
        return rt;
    }

    public static Button PauseButton(Transform parent, UnityAction onClick)
    {
        GameObject go = new GameObject("PauseButton");
        go.transform.SetParent(parent, false);
        Image bg = go.AddComponent<Image>();
        bg.sprite = CircleSprite();
        bg.color = UiTheme.PillBgSolid;
        bg.raycastTarget = true;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 0.85f);
        btn.colors = colors;
        if (onClick != null)
            btn.onClick.AddListener(onClick);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(UiTheme.SafeSide + UiTheme.PauseSize * 0.5f,
            -(UiTheme.SafeTop + UiTheme.HudTopInset + UiTheme.PillHeight * 0.5f));
        rt.sizeDelta = new Vector2(UiTheme.PauseSize, UiTheme.PauseSize);

        Image barL = SoftPlate(go.transform, "BarL", Color.white, new Vector2(4f, 18f), new Vector2(-5f, 0f));
        barL.raycastTarget = false;
        Image barR = SoftPlate(go.transform, "BarR", Color.white, new Vector2(4f, 18f), new Vector2(5f, 0f));
        barR.raycastTarget = false;
        return btn;
    }

    public static Image HudIcon(Transform parent, string name, Sprite sprite, Vector2 anchored, float size = 28f)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite != null ? sprite : WhiteSprite();
        img.preserveAspect = true;
        img.color = Color.white;
        img.raycastTarget = false;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = anchored;
        rt.sizeDelta = new Vector2(size, size);
        return img;
    }

    public static Image FlagIcon(Transform parent, Vector2 anchored)
    {
        GameObject root = new GameObject("FlagIcon");
        root.transform.SetParent(parent, false);
        RectTransform rt = root.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = anchored;
        rt.sizeDelta = new Vector2(26f, 26f);

        SoftPlate(root.transform, "Pole", new Color(0.85f, 0.85f, 0.82f, 1f), new Vector2(3f, 22f), new Vector2(-10f, 0f));
        SoftPlate(root.transform, "A", new Color(0.95f, 0.95f, 0.93f, 1f), new Vector2(10f, 10f), new Vector2(-2f, 4f));
        SoftPlate(root.transform, "B", new Color(0.18f, 0.18f, 0.18f, 1f), new Vector2(10f, 10f), new Vector2(-2f, -6f));
        Image img = root.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0f);
        img.raycastTarget = false;
        return img;
    }

    public static Sprite CircleSprite()
    {
        if (_circle != null)
            return _circle;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        float r = size * 0.5f - 1f;
        Vector2 c = new Vector2(r, r);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                tex.SetPixel(x, y, d <= r ? Color.white : Color.clear);
            }
        }

        tex.Apply(false, true);
        _circle = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _circle;
    }

    private static Sprite _circle;
}
