using UnityEngine;
using UnityEngine.UI;

/// Loads 『347』 UI/concept art from Resources/ using PexelsImagePack.json filenames; legacy fallbacks second.
/// Fetch via Tools > 347 > Fetch Pexels Images (.env PEXELS_API_KEY required).
public static class UiArt
{
    public static Sprite ConceptOpening()
    {
        return LoadConcept("Concept_Opening")
            ?? LoadConcept("Concept_KeyArt");
    }

    public static Sprite ConceptDepot()
    {
        return LoadConcept("Concept_Depot")
            ?? LoadConcept("Concept_Tower")
            ?? ConceptOpening();
    }

    public static Sprite Panel()
    {
        return LoadUi("UI_Panel_City", false)
            ?? LoadUi("UI_Panel_Dark", false);
    }

    public static Sprite PrimaryButton()
    {
        return LoadUi("UI_Btn_Primary", false)
            ?? LoadUi("UI_Btn_Restart", false);
    }

    public static Sprite ItemFrame()
    {
        return LoadUi("UI_Frame_Item", false);
    }

    public static Sprite IconTag()
    {
        return LoadUi("UI_Icon_Tag", true)
            ?? LoadUi("UI_Icon_Supply", true);
    }

    public static Sprite IconLetter()
    {
        return LoadUi("UI_Icon_Letter", true);
    }

    public static Sprite IconCoin()
    {
        return LoadUi("UI_Icon_Coin", true);
    }

    public static Sprite IconDepot()
    {
        return LoadUi("UI_Icon_Depot", true)
            ?? LoadUi("UI_Icon_Tower", true);
    }

    public static Sprite DeckPip(int hpRemaining)
    {
        if (hpRemaining <= 1)
            return LoadUi("UI_Deck_Broken", true) ?? LoadUi("UI_Deck_Cracked", true);
        if (hpRemaining == 2)
            return LoadUi("UI_Deck_Cracked", true) ?? LoadUi("UI_Deck_Ok", true);
        return LoadUi("UI_Deck_Ok", true);
    }

    public static void ApplySprite(Image image, Sprite sprite, bool preserveAspect = true)
    {
        if (image == null || sprite == null)
            return;
        image.sprite = sprite;
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
    }

    public static Sprite LoadUi(string fileName, bool keyBlack)
    {
        string path = "UI/" + fileName;
        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex == null)
            return null;

        if (keyBlack)
            tex = KeyBlackToAlpha(tex);

        Sprite imported = Resources.Load<Sprite>(path);
        Vector4 border = imported != null ? imported.border : Vector4.zero;
        if ((fileName == "UI_Panel_Dark" || fileName == "UI_Panel_City") && border.sqrMagnitude < 1f)
            border = new Vector4(64f, 64f, 64f, 64f);
        if ((fileName == "UI_Btn_Restart" || fileName == "UI_Btn_Primary") && border.sqrMagnitude < 1f)
            border = new Vector4(48f, 32f, 48f, 32f);

        return Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            border);
    }

    public static Sprite LoadConcept(string fileName)
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
            if (!src.isReadable)
                return src;

            pixels = src.GetPixels32();
        }
        catch (System.Exception)
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
}
