using UnityEngine;

namespace CoastRun
{
    /// Locked look-dev palette. Edit in Inspector — materials refresh immediately.
    [CreateAssetMenu(menuName = "Coast Run/Coast Palette", fileName = "CoastPalette")]
    public class CoastPaletteConfig : ScriptableObject
    {
        [Header("Core (StyleBible)")]
        public Color skyBlue = new Color(0.310f, 0.659f, 0.847f);      // #4FA8D8
        public Color seaTeal = new Color(0.160f, 0.720f, 0.730f);      // #29B8BA 14차: 목표 이미지의 청록 바다
        public Color roadGrey = new Color(0.541f, 0.522f, 0.471f);     // #8A8578
        public Color townCream = new Color(0.949f, 0.902f, 0.816f);    // #F2E6D0
        public Color accentOrange = new Color(0.910f, 0.392f, 0.184f); // #E8642F
        public Color coinYellow = new Color(1.000f, 0.788f, 0.235f);   // #FFC93C

        private void OnEnable() => CoastPalette.Bind(this);

        private void OnValidate()
        {
            CoastPalette.Bind(this);
            CoastMaterials.RefreshTracked();
        }
    }
}
