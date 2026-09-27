using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 214차(사용자: 「남은것 진행해」 — 212차 평가 ⑨ 「이번 주 목표 → 결과 → 이야기에 반영」):
    /// 챕터마다 꼬마와의 약속 3개(벌레·낚시·텃밭… 마을 활동). 챕터가 넘어갈 때(잠 → 다음 장) 지킨 개수만큼 꼬마와의 인연(♥)이 오르고,
    /// 인연이 쌓여야 오프닝 「그 약속」 기억 조각(OPEN_F1~F3)이 장소에 나타난다. 컷씬 순서·자막은 그대로(바꾸지 않음).
    public partial class VillageHub
    {
        static readonly VillageMission.Kind[] CgPool = {
            VillageMission.Kind.Bug, VillageMission.Kind.Chop, VillageMission.Kind.Mine, VillageMission.Kind.Farm,
            VillageMission.Kind.Fish, VillageMission.Kind.Pick, VillageMission.Kind.Talk, VillageMission.Kind.Crab };
        static int CgGoal(VillageMission.Kind k) => k == VillageMission.Kind.Pick ? 5 : k == VillageMission.Kind.Bug || k == VillageMission.Kind.Talk || k == VillageMission.Kind.Farm ? 3 : 2;
        /// 기억 조각 index(1·2·3) 별 필요한 인연
        public static int FragBond(int index) => index <= 1 ? 1 : index == 2 ? 3 : 5;

        static string CgTitle(VillageMission.Kind k, int n)
        {
            switch (k)
            {
                case VillageMission.Kind.Bug: return Loc.T($"벌레 {n}마리 잡기", $"Catch {n} bugs");
                case VillageMission.Kind.Chop: return Loc.T($"나무 {n}번 패기", $"Chop wood {n}×");
                case VillageMission.Kind.Mine: return Loc.T($"돌 {n}번 캐기", $"Mine rocks {n}×");
                case VillageMission.Kind.Farm: return Loc.T($"텃밭 {n}번 돌보기", $"Tend the garden {n}×");
                case VillageMission.Kind.Fish: return Loc.T($"낚시 {n}번 하기", $"Go fishing {n}×");
                case VillageMission.Kind.Pick: return Loc.T($"바닥에서 {n}개 줍기", $"Pick up {n} things");
                case VillageMission.Kind.Talk: return Loc.T($"마을 사람 {n}명과 이야기", $"Talk to {n} villagers");
                default: return Loc.T($"꽃게 {n}마리 잡기", $"Catch {n} crabs");
            }
        }
        VillageMission.Kind CgKind(int i) => (VillageMission.Kind)Save.cgKind[i];
        int CgDone() { int d = 0; for (int i = 0; i < 3; i++) if (Save.cgProg[i] >= CgGoal(CgKind(i))) d++; return d; }
        bool _cgAnnounce;

        /// 마을에 들어올 때(아침마다): 챕터가 바뀌었으면 지난 약속을 매기고 새 약속 3개를 정한다.
        void InitBond214()
        {
            if (Save == null) return;
            if (Save.cgKind == null || Save.cgKind.Length != 3) Save.cgKind = new int[3];
            if (Save.cgProg == null || Save.cgProg.Length != 3) Save.cgProg = new int[3];
            if (Save.cgChapter == Save.chapter) return;
            if (Save.cgChapter <= 0)
                Save.bond214 = Mathf.Max(Save.bond214, Save.chapter - 1);   // 이 기능 전의 세이브: 지나온 장만큼 인연을 채워 둔다(기억 조각이 갑자기 막히지 않게)
            else if (Save.cgChapter < Save.chapter)
            {
                int done = CgDone(); int gain = done >= 3 ? 2 : done == 2 ? 1 : 0;
                Save.bond214 += gain; Save.cgLastDone = done; Save.cgLastChapter = Save.cgChapter;
            }
            var rng = new System.Random(Save.chapter * 131 + Save.seed * 7 + Save.playthrough * 3);
            var pool = new List<VillageMission.Kind>(CgPool);
            for (int i = 0; i < 3; i++) { int j = rng.Next(pool.Count); Save.cgKind[i] = (int)pool[j]; pool.RemoveAt(j); Save.cgProg[i] = 0; }
            Save.cgChapter = Save.chapter; _cgAnnounce = true;
            _gm.Persist();
        }

        /// 새 약속 알림 — 도입·튜토리얼·컷씬이 끝나고 손이 비었을 때 꼬마가 말한다
        IEnumerator CgAnnounceCo()
        {
            while (Save == null || !Save.prologueSeen || Save.storyTut < TutDone || _busy || _hud == null || _hud.Locked || _storyPlaying || CinematicPlayer.IsPlaying) yield return new WaitForSeconds(0.5f);
            yield return new WaitForSeconds(1.2f);
            while (_busy || _hud.Locked || _storyPlaying) yield return new WaitForSeconds(0.5f);
            _cgAnnounce = false;
            string head = "";
            if (Save.cgLastDone >= 0 && Save.cgLastChapter == Save.chapter - 1)
            {
                head = Save.cgLastDone >= 3 ? Loc.T("약속 세 개 다 했다! ", "All three done! ")
                     : Save.cgLastDone == 2 ? Loc.T("두 개 했어. 하나는 못 했지. ", "Two done. One left. ")
                     : Loc.T("누나… 약속 잊어버렸지. ", "Sis… you forgot, huh. ");
                if (Save.cgLastDone >= 2) CoastToast.Show(Loc.T($"꼬마와의 인연 ♥ +{(Save.cgLastDone >= 3 ? 2 : 1)}  (♥ {Save.bond214})", $"Bond ♥ +{(Save.cgLastDone >= 3 ? 2 : 1)} (♥ {Save.bond214})"));
            }
            string list = $"{CgTitle(CgKind(0), CgGoal(CgKind(0)))} · {CgTitle(CgKind(1), CgGoal(CgKind(1)))} · {CgTitle(CgKind(2), CgGoal(CgKind(2)))}";
            _hud.Bubble(Loc.T("꼬마", "Kid"), head + Loc.T($"이번엔 이거 하자. {list}", $"This time, let's do these: {list}"));
        }

        /// MissionTick 에서 같이 부른다 — 같은 종류 약속이 있으면 진행
        void CgTick(VillageMission.Kind k, int n)
        {
            if (Save == null || Save.cgKind == null || Save.cgChapter != Save.chapter) return;
            for (int i = 0; i < 3; i++)
            {
                if (CgKind(i) != k) continue;
                int g = CgGoal(k); if (Save.cgProg[i] >= g) continue;
                Save.cgProg[i] = Mathf.Min(g, Save.cgProg[i] + n);
                CoastToast.Show(Save.cgProg[i] >= g ? Loc.T($"꼬마와의 약속 하나 지켰다 ({CgDone()}/3)", $"Kept a promise ({CgDone()}/3)")
                                                    : Loc.T($"꼬마와의 약속 · {CgTitle(k, g)} {Save.cgProg[i]}/{g}", $"Promise · {CgTitle(k, g)} {Save.cgProg[i]}/{g}"));
            }
        }

        /// 마을 메뉴 · 하루 기록 한 줄
        string CgSummary() => Save == null || Save.cgChapter != Save.chapter ? null : Loc.T($"꼬마와의 약속 {CgDone()}/3 · 인연 ♥ {Save.bond214}", $"Promises {CgDone()}/3 · bond ♥ {Save.bond214}");

        void CgMenu()
        {
            if (Save == null) return; InitBond214();
            var rows = new List<(string, Color, Action)>();
            for (int i = 0; i < 3; i++)
            {
                var k = CgKind(i); int g = CgGoal(k); bool ok = Save.cgProg[i] >= g;
                rows.Add(((ok ? "✔ " : "") + CgTitle(k, g) + $"  {Save.cgProg[i]}/{g}", ok ? new Color(0.55f, 0.80f, 0.55f) : new Color(0.98f, 0.70f, 0.45f), (Action)null));
            }
            string next = "";
            foreach (var f in VillageStory.Frags)
            {
                if (VillageStory.Seen(f, Save)) continue;
                int need = FragBond(f.index) - Save.bond214;
                next = need > 0 ? Loc.T($"다음 기억 조각까지 인연 ♥ {need} 더", $"{need} more ♥ to the next memory") : Loc.T("기억 조각이 이야기 장소 가까이에서 기다린다", "A memory waits near a story place");
                break;
            }
            rows.Add((Loc.T("알았어", "OK"), new Color(0.60f, 0.52f, 0.92f), (Action)null));
            _hud.Choice(Loc.T($"꼬마와의 약속 · {Save.chapter}장", $"Promises · chapter {Save.chapter}"),
                Loc.T($"장이 넘어갈 때 3개 다 지키면 인연 ♥ +2, 2개면 +1 · 지금 인연 ♥ {Save.bond214}", $"All 3 by chapter end: ♥ +2 · 2: +1 · now ♥ {Save.bond214}") + (next != "" ? " · " + next : ""), rows.ToArray());
        }

        public void DevCgDone() { if (Save == null) return; InitBond214(); for (int i = 0; i < 3; i++) Save.cgProg[i] = CgGoal(CgKind(i)); _gm.Persist(); Debug.LogWarning($"[214] promises done ch={Save.chapter} bond={Save.bond214}"); }
        public void DevCgNextChapter() { if (Save == null) return; InitBond214(); Save.chapter = Mathf.Min(Save.chapter + 1, 20); InitBond214(); if (_cgAnnounce) StartCoroutine(CgAnnounceCo()); DevCgLog(); }   // 개발: 장 넘김(약속 매기기 시험)
        public void DevCgLog() { if (Save == null) return; Debug.LogWarning($"[214] ch={Save.chapter} cgCh={Save.cgChapter} kinds={Save.cgKind[0]},{Save.cgKind[1]},{Save.cgKind[2]} prog={Save.cgProg[0]},{Save.cgProg[1]},{Save.cgProg[2]} bond={Save.bond214} last={Save.cgLastDone}@{Save.cgLastChapter} frag={Save.storyFragMask}"); }
    }
}
