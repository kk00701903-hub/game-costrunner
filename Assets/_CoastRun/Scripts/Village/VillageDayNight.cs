using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CoastRun.Village
{
    /// 155차(사용자: 「밤낮 효과, 밤이 되면 집으로」): 마을 시계(SaveData.villageHour, 실시간 40 s = 1시간) + 조명·하늘·안개·후처리 보간 + 가로등.
    /// 08:00 시작 → 17:30 노을 → 20:00 밤(귀신, 상점 닫힘) → 집에서 자면 다음 날 08:00.
    public class VillageDayNight : MonoBehaviour
    {
        public const float SecondsPerHour = 40f;
        public static float SkyTintR = 1f, SkyTintG = 1f, SkyTintB = 1f;
        public static Color SkyTint => new Color(SkyTintR, SkyTintG, SkyTintB, 1f);
        public float Hour = 8f;
        public bool IsNight => Hour >= 20f || Hour < 5.5f;
        public bool IsDusk => Hour >= 17.5f && Hour < 20f;
        public bool ShopOpen => Hour >= 8f && Hour < 19f;
        public bool Paused;
        Light _sun, _fill; Volume _vol; ColorAdjustments _ca; Vignette _vig;
        readonly List<Light> _lamps = new List<Light>(); readonly List<Renderer> _lampHeads = new List<Renderer>();
        Material _lampOn, _lampOff; SaveData _save; float _lastMinute = -1f;
        public System.Action OnMinute;

        public static VillageDayNight Create(Transform worldRoot, SaveData save)
        {
            var dn = worldRoot.gameObject.AddComponent<VillageDayNight>();
            dn._save = save; dn.Hour = save != null && save.villageHour > 0f ? save.villageHour : 8f;
            var sunT = worldRoot.Find("Sun"); dn._sun = sunT != null ? sunT.GetComponent<Light>() : null;
            var fillT = worldRoot.Find("Fill"); dn._fill = fillT != null ? fillT.GetComponent<Light>() : null;
            var volT = worldRoot.Find("VillageVolume"); dn._vol = volT != null ? volT.GetComponent<Volume>() : null;
            if (dn._vol != null && dn._vol.profile != null) { dn._vol.profile.TryGet(out dn._ca); dn._vol.profile.TryGet(out dn._vig); }
            // 가로등: 머리(구)에 밤엔 포인트 라이트
            dn._lampOn = CoastMaterials.CreateUnlit(new Color(1f, 0.93f, 0.62f)); dn._lampOff = CoastMaterials.CreateUnlit(new Color(0.78f, 0.76f, 0.70f));
            foreach (Transform t in worldRoot)
            {
                if (t.name != "Lamp") continue;
                var head = t.childCount > 1 ? t.GetChild(1).GetComponent<Renderer>() : null; if (head == null) continue;
                dn._lampHeads.Add(head);
                var lg = new GameObject("LampLight").AddComponent<Light>(); lg.transform.SetParent(t, false); lg.transform.localPosition = new Vector3(0f, 2.9f, 0f);
                lg.type = LightType.Point; lg.range = 7f; lg.intensity = 0f; lg.color = new Color(1f, 0.85f, 0.55f); lg.shadows = LightShadows.None;
                dn._lamps.Add(lg);
            }
            dn.Apply();
            return dn;
        }

        void Update()
        {
            if (!Paused) Hour += Time.deltaTime / SecondsPerHour;
            if (Hour >= 24f) Hour -= 24f;
            if (_save != null) _save.villageHour = Hour;
            Apply();
            float minute = Mathf.Floor(Hour * 60f);
            if (minute != _lastMinute) { _lastMinute = minute; OnMinute?.Invoke(); }
        }

        /// 시각 → 조명 키프레임 보간
        void Apply()
        {
            // 0 낮, 1 노을, 2 밤, 3 새벽 — 가중치
            float day = Mathf.Clamp01(Mathf.Min((Hour - 6.5f) / 1.5f, (18.5f - Hour) / 1.0f));   // 8~17.5 = 1
            float dusk = Mathf.Clamp01(1f - Mathf.Abs(Hour - 18.6f) / 1.6f);
            float dawn = Mathf.Clamp01(1f - Mathf.Abs(Hour - 5.8f) / 1.0f);
            float night = Mathf.Clamp01(Mathf.Max((Hour - 19.2f) / 1.2f, (5.2f - Hour) / 1.0f)); if (Hour < 5.2f || Hour >= 20.4f) night = 1f;
            float sum = day + dusk + dawn + night + 1e-4f; day /= sum; dusk /= sum; dawn /= sum; night /= sum;
            Color sunC = new Color(1f, 0.96f, 0.90f) * day + new Color(1f, 0.62f, 0.40f) * dusk + new Color(1f, 0.78f, 0.62f) * dawn + new Color(0.42f, 0.50f, 0.85f) * night;
            float sunI = 1.05f * day + 0.80f * dusk + 0.75f * dawn + 0.28f * night;
            float sunX = 50f * day + 18f * dusk + 22f * dawn + 42f * night;
            Color fog = VillagePalette.Fog * day + new Color(0.98f, 0.72f, 0.62f) * dusk + new Color(0.95f, 0.80f, 0.78f) * dawn + new Color(0.10f, 0.12f, 0.24f) * night;
            Color sky = Color.white * day + new Color(1f, 0.74f, 0.62f) * dusk + new Color(1f, 0.86f, 0.84f) * dawn + new Color(0.14f, 0.16f, 0.34f) * night;
            Color ambSky = VillagePalette.SkyMid * day + new Color(0.85f, 0.55f, 0.50f) * dusk + new Color(0.80f, 0.70f, 0.75f) * dawn + new Color(0.16f, 0.18f, 0.34f) * night;
            Color ambEq = new Color(0.90f, 0.88f, 0.86f) * day + new Color(0.80f, 0.60f, 0.52f) * dusk + new Color(0.80f, 0.72f, 0.72f) * dawn + new Color(0.20f, 0.22f, 0.36f) * night;
            Color ambGr = new Color(0.66f, 0.68f, 0.56f) * day + new Color(0.50f, 0.42f, 0.36f) * dusk + new Color(0.55f, 0.52f, 0.45f) * dawn + new Color(0.10f, 0.11f, 0.18f) * night;
            if (_sun != null) { _sun.color = sunC; _sun.intensity = sunI; _sun.transform.rotation = Quaternion.Euler(sunX, 160f, 0f); }
            if (_fill != null) _fill.intensity = 0.32f * (day + dusk * 0.6f + dawn * 0.6f) + 0.10f * night;
            RenderSettings.fogColor = fog; RenderSettings.ambientSkyColor = ambSky; RenderSettings.ambientEquatorColor = ambEq; RenderSettings.ambientGroundColor = ambGr;
            SkyTintR = sky.r; SkyTintG = sky.g; SkyTintB = sky.b;
            if (_ca != null)
            {
                _ca.postExposure.Override(-0.55f * night + 0.05f * dusk);
                _ca.colorFilter.Override(new Color(1f, 0.99f, 0.965f) * (day + dawn) + new Color(1f, 0.90f, 0.82f) * dusk + new Color(0.72f, 0.78f, 1f) * night);
                _ca.saturation.Override(4f * day + 8f * dusk - 6f * night);
            }
            if (_vig != null) _vig.intensity.Override(0.13f + 0.22f * night);
            float lamp = Mathf.Clamp01(night * 1.2f + dusk * 0.6f);
            for (int i = 0; i < _lamps.Count; i++) _lamps[i].intensity = 2.6f * lamp;
            var lm = lamp > 0.3f ? _lampOn : _lampOff; foreach (var h in _lampHeads) if (h != null && h.sharedMaterial != lm) h.sharedMaterial = lm;
            Shader.SetGlobalFloat("_CoastNight", night);
        }

        public string ClockText()
        {
            int h = Mathf.FloorToInt(Hour), m = Mathf.FloorToInt((Hour - h) * 60f);
            string ampm = h < 12 ? Loc.T("오전", "AM") : h < 18 ? Loc.T("오후", "PM") : h < 20 ? Loc.T("저녁", "Eve") : Loc.T("밤", "Night");
            int h12 = h % 12; if (h12 == 0) h12 = 12;
            return $"{ampm} {h12:00}:{m:00}";
        }
        public void SetMorning() { Hour = 8f; if (_save != null) _save.villageHour = 8f; Apply(); }
    }
}
