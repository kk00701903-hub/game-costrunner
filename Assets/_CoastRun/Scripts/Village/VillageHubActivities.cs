using System;
using System.Collections;
using UnityEngine;

namespace CoastRun.Village
{
    /// 196차(사용자: 「중문관광지 놀거리 액티비티」): 마리나(요트·제트스키) · 색달해변 서핑 · 승마·전동카트 체험장 · 테디베어 박물관·포토존
    public partial class VillageHub
    {
        void AddActivitySpots()
        {
            _spots.Add(new Spot { id = "marina", title = Loc.T("⛵ 중몬 마리나 · 요트 / 제트스키", "⛵ Jungmon Marina · Yacht / Jet ski"), pos = VillageZones.MarinaSpot, radius = 3f, on = MarinaMenu });
            _spots.Add(new Spot { id = "surf", title = Loc.T("서핑 숍 · 보드 빌리기", "Surf shop · Rent a board"), pos = VillageZones.SurfSpot, radius = 3f, on = SurfMenu });
            _spots.Add(new Spot { id = "track", title = Loc.T("승마·전동카트 체험장", "Riding & E-kart park"), pos = VillageZones.TrackSpot, radius = 3.2f, on = TrackMenu });
            _spots.Add(new Spot { id = "photo", title = Loc.T("테디곰 포토존 · 기념사진", "Teddy photo zone"), pos = VillageZones.PhotoSpot, radius = 2.6f, on = PhotoZone });
        }

        int ActStamp => Save.week * 4 + Save.phaseIndex;
        static readonly int[] ActPrice = { 150, 250, 100, 120, 120 };
        static readonly int[] ActStress = { 15, 25, 15, 12, 12 };
        static readonly int[] ActCoin = { 6, 0, 4, 5, 12 };   // 돈은 조금 모자라게(195차 밸런스) — 요트는 순수 힐링
        static readonly int[] ActCap = { 180, 0, 100, 140, 150 };

        (string, Color, Action) ActItem(JejuActivity.Kind k, Color c)
        {
            int i = (int)k; bool done = Save.actStamp != null && Save.actStamp.Length > i && Save.actStamp[i] == ActStamp;
            string lab = done ? Loc.T($"{JejuActivity.Title(k)} — 이번 턴엔 탔어", $"{JejuActivity.Title(k)} — done this turn")
                              : Loc.T($"{JejuActivity.Title(k)} · {ActPrice[i]}G (스트레스 −{ActStress[i]})", $"{JejuActivity.Title(k)} · {ActPrice[i]}G (stress −{ActStress[i]})");
            return (lab, done ? new Color(0.62f, 0.62f, 0.66f) : c, () => StartActivity(k));
        }
        void MarinaMenu()
        {
            if (Save == null) return;
            _hud.Choice(Loc.T("⛵ 중몬 마리나", "⛵ Jungmon Marina"), Loc.T("요트로 돌고래를 찾거나, 제트스키로 부표 사이를 달리자.", "Look for dolphins on a yacht, or race the buoys on a jet ski."),
                new (string, Color, Action)[] { ActItem(JejuActivity.Kind.Yacht, new Color(0.35f, 0.62f, 0.95f)), ActItem(JejuActivity.Kind.Jetski, new Color(1f, 0.60f, 0.25f)), (Loc.T("다음에", "Later"), new Color(0.6f, 0.6f, 0.66f), null) });
        }
        void SurfMenu()
        {
            if (Save == null) return;
            _hud.Choice(Loc.T("색동해변 서핑 숍", "Saekdong surf shop"), Loc.T("보드 빌려서 파도 한번 타 볼래?", "Rent a board and catch a wave?"),
                new (string, Color, Action)[] { ActItem(JejuActivity.Kind.Surf, new Color(0.30f, 0.75f, 0.90f)), (Loc.T("다음에", "Later"), new Color(0.6f, 0.6f, 0.66f), null) });
        }
        void TrackMenu()
        {
            if (Save == null) return;
            _hud.Choice(Loc.T("승마·전동카트 체험장", "Riding & E-kart park"), Loc.T("조랑말 타고 울타리 넘기, 또는 전동카트 한 바퀴!", "Jump fences on a pony, or a lap in an e-kart!"),
                new (string, Color, Action)[] { ActItem(JejuActivity.Kind.Horse, new Color(0.72f, 0.52f, 0.35f)), ActItem(JejuActivity.Kind.Kart, new Color(0.95f, 0.35f, 0.40f)), (Loc.T("다음에", "Later"), new Color(0.6f, 0.6f, 0.66f), null) });
        }

        void StartActivity(JejuActivity.Kind k)
        {
            if (Save == null || JejuActivity.Playing) return;
            int i = (int)k;
            if (Save.actStamp == null || Save.actStamp.Length < 5) Save.actStamp = new int[5];
            if (Save.actStamp[i] == ActStamp) { _hud.Bubble(Hero, Loc.T("이번 턴엔 이미 탔어. 다른 것도 해 보자!", "Already did this turn. Try something else!")); return; }
            if (Save.stats.money < ActPrice[i]) { _hud.Bubble(Hero, Loc.T($"{ActPrice[i]}G 가 필요해…", $"Need {ActPrice[i]}G…")); return; }
            if (k != JejuActivity.Kind.Yacht && Save.stats.stamina < 10) { _hud.Bubble(Hero, Loc.T("너무 지쳤어. 좀 쉬고 하자.", "Too tired. Rest first.")); return; }
            Save.stats.money -= ActPrice[i]; Save.actStamp[i] = ActStamp; _gm.Persist(); RefreshStatus();
            _busy = true;
            JejuActivity.Play(k, score =>
            {
                _busy = false;
                int coin = Mathf.Min(ActCap[i], score * ActCoin[i]);
                Save.stats.money += coin; Save.stats.stress = Mathf.Max(0, Save.stats.stress - ActStress[i]);
                if (k != JejuActivity.Kind.Yacht) Save.stats.stamina = Mathf.Max(1, Save.stats.stamina - 8);
                Save.stats.Clamp();
                if (k == JejuActivity.Kind.Yacht && score > 0) MissionTick(VillageMission.Kind.TourPhoto);
                _gm.Persist(); RefreshStatus();
                VillagePang.Burst(_player.position + Vector3.up * 1.4f, new Color(0.55f, 0.85f, 1f), Color.white, 1.2f);
                CoastToast.Pop(Loc.T($"스트레스 −{ActStress[i]}" + (coin > 0 ? $" · +{coin}G" : ""), $"Stress −{ActStress[i]}" + (coin > 0 ? $" · +{coin}G" : "")));
            });
        }

        void PhotoZone()
        {
            if (Save == null) return;
            if (Save.actStamp == null || Save.actStamp.Length < 6) { var a = new int[6]; if (Save.actStamp != null) Array.Copy(Save.actStamp, a, Mathf.Min(5, Save.actStamp.Length)); Save.actStamp = a; }
            if (Save.actStamp[5] == ActStamp) { _hud.Bubble(Hero, Loc.T("여기선 방금 찍었어!", "Just took one here!")); return; }
            void Shoot(string pose)
            {
                Save.actStamp[5] = ActStamp; Save.stats.stress = Mathf.Max(0, Save.stats.stress - 5); Save.stats.Clamp();
                MissionTick(VillageMission.Kind.TourPhoto); _gm.Persist(); RefreshStatus();
                StartCoroutine(PhotoFlash(pose));
            }
            _hud.Choice(Loc.T("테디곰 포토존", "Teddy photo zone"), Loc.T("커다란 곰 인형 옆에서 한 장! 포즈는?", "A shot with the giant teddy! Pose?"),
                new (string, Color, Action)[] {
                    (Loc.T("✌ 브이", "✌ Peace"), new Color(1f, 0.62f, 0.72f), () => Shoot("✌")),
                    (Loc.T("곰 안기", "Hug the bear"), new Color(0.85f, 0.62f, 0.45f), () => Shoot("♡")),
                    (Loc.T("♥ 하트", "♥ Heart"), new Color(0.95f, 0.45f, 0.55f), () => Shoot("♥")),
                });
        }
        IEnumerator PhotoFlash(string pose)
        {
            _busy = true;
            var cv = CoastUiCanvas.Create("PhotoFlash", 171); var root = CoastUiCanvas.Root(cv);
            var f = CoastHudLayout.MakeImage(root, "F", Vector2.zero, Vector2.one, new Vector2(-400f, -400f), new Vector2(400f, 400f), Color.white); f.raycastTarget = true;
            var t = CoastHudLayout.MakeText(root, "T", pose + "\n" + Loc.T("찰칵!", "Click!"), 60, TextAnchor.MiddleCenter, new Vector2(0f, 0.4f), new Vector2(1f, 0.6f), Vector2.zero, Vector2.zero); t.color = EventCardKit.BrownInk;
            for (float u = 0f; u < 0.8f; u += Time.deltaTime) { f.color = new Color(1f, 1f, 1f, 1f - u / 0.8f * 0.9f); yield return null; }
            yield return new WaitForSeconds(0.5f);
            Destroy(cv.gameObject); _busy = false;
            CoastToast.Pop(Loc.T("기념사진 저장 · 스트레스 −5", "Photo saved · stress −5"));
        }

        /// 테디베어 박물관 안 — 관람(입장료) · 포토
        void TeddyMenu()
        {
            if (Save == null) return;
            if (Save.actStamp == null || Save.actStamp.Length < 7) { var a = new int[7]; if (Save.actStamp != null) Array.Copy(Save.actStamp, a, Mathf.Min(a.Length, Save.actStamp.Length)); Save.actStamp = a; }
            bool seen = Save.actStamp[6] == ActStamp;
            _hud.Choice(Loc.T("테디곰 박물관", "Teddy Cub Museum"), Loc.T("세계 여러 나라 곰 인형과 제주 해녀 곰이 있어요.", "Bears from around the world — and a haenyeo bear."),
                new (string, Color, Action)[] {
                    (seen ? Loc.T("관람 — 이번 턴엔 봤어", "Tour — done this turn") : Loc.T("관람하기 · 80G (스트레스 −10)", "Take the tour · 80G (stress −10)"), seen ? new Color(0.62f, 0.62f, 0.66f) : new Color(0.85f, 0.62f, 0.45f), () =>
                    {
                        if (seen) return; if (Save.stats.money < 80) { _hud.Bubble(Hero, Loc.T("80G 가 필요해…", "Need 80G…")); return; }
                        Save.stats.money -= 80; Save.actStamp[6] = ActStamp; Save.stats.stress = Mathf.Max(0, Save.stats.stress - 10); Save.stats.Clamp(); _gm.Persist(); RefreshStatus();
                        CoastToast.Pop(Loc.T("스트레스 −10", "Stress −10"));
                        _hud.Bubble(Hero, Loc.T("해녀복 입은 곰, 우주복 입은 곰… 귤 모자 쓴 곰이 제일 귀엽다.", "A haenyeo bear, an astronaut bear… the tangerine-hat bear is the cutest."));
                    }),
                    (Loc.T("다음에", "Later"), new Color(0.6f, 0.6f, 0.66f), null),
                });
        }

        // ── 개발용 ────────────────────────────────────────────────────────
        public void DevActivity(int k) { JejuActivity.DevAuto = true; if (Save != null) { Save.stats.money += ActPrice[k]; if (Save.actStamp != null && Save.actStamp.Length > k) Save.actStamp[k] = -1; } StartActivity((JejuActivity.Kind)k); }
        public void DevGoSpot(string id) { var sp = _spots.Find(s => s.id == id); if (sp == null) { Debug.LogWarning("[Act] no spot " + id); return; } Teleport(sp.pos + new Vector3(0f, 0f, 2.0f), 180f); }
    }

    /// 물 위 둥실(요트·제트스키)
    public class SeaFloat : MonoBehaviour
    {
        Vector3 _p; float _ph; void Start() { _p = transform.position; _ph = transform.position.x * 0.7f; }
        void Update() { transform.position = _p + Vector3.up * Mathf.Sin(Time.time * 1.3f + _ph) * 0.12f; transform.rotation = Quaternion.Euler(Mathf.Sin(Time.time * 1.1f + _ph) * 3f, transform.eulerAngles.y, Mathf.Sin(Time.time * 0.9f + _ph) * 4f); }
    }
}
