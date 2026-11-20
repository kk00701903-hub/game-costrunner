using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun.Village
{
    /// 196차(사용자): 새 게임 = 송전탑 위에서 깨어남(아무것도 모름) → 탑 아래 담요 자리(빛) → CS1 → 꼬마와 마을 튜토리얼(가게 · 텃밭 · 언덕 위 빈집)
    ///   → 이후 챕터마다 이야기 장소가 빛나고 꼬마가 앞장선다. 그 자리에 들어가면 컷씬(+단서·선택). 안 본 이야기가 있으면 잠을 못 잔다(꼬마가 데려다준다).
    public partial class VillageHub
    {
        StoryBeacon _storyBeacon; string _storyBeaconKey; Vector3 _storyPos; bool _storyPlaying; float _storyCheckT;
        bool _storyCam; Vector3 _storyCamPos, _storyCamLook, _storyCamVel;
        /// 개발용: 말풍선을 1.6초 뒤 저절로 닫는다(원격 테스트)
        public static bool DevAutoBubble;
        Transform _blanket;

        const int TutDone = 9;
        static readonly string[] TutSpot = { null, "shop", "garden", "hero" };
        static readonly string[] TutKo = { null, "마을 가게", "텃밭", "언덕 위 빈집" };
        static readonly string[] TutEn = { null, "Village shop", "Garden", "Empty house on the hill" };

        // ── 시작 ─────────────────────────────────────────────────────────
        void InitStory()
        {
            if (Save == null) return;
            VillageStory.Migrate(Save);
            PushToolTiers();   // 198차
            VillageStoryMan.Create(transform, _player, () => _interior != null || VillageZones.At(_player.position) != VillageZones.Zone.None || CinematicPlayer.IsPlaying || _storyPlaying);   // 198차: 남자 주인공
            BuildBlanket();
            if (!Save.prologueSeen && Save.chapter == 1 && VillageStory.PlaceMode) { StartCoroutine(IntroTowerWake()); return; }
            if (_creatures != null) _creatures.KidHidden = Save.prologueSeen && Save.storyTut == 0 && !VillageStory.Seen(VillageStory.Order[0], Save);
            RefreshStoryBeacon(true);
        }

        /// 탑 아래 담요 자리 — 낡은 담요 + 돌 세 개(CS1·CS8 의 자리)
        void BuildBlanket()
        {
            if (_blanket != null) return;
            var b = VillageStory.Blanket; var g = VillageWorld.Ground(b.x, b.y);
            _blanket = new GameObject("StoryBlanket").transform; _blanket.SetParent(_world != null ? _world : transform, false); _blanket.position = g; _blanket.rotation = Quaternion.Euler(0f, 18f, 0f);
            var m1 = CoastMaterials.CreateLit(new Color(0.62f, 0.30f, 0.26f)); var m2 = CoastMaterials.CreateLit(new Color(0.80f, 0.70f, 0.52f));
            for (int i = 0; i < 5; i++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(s.GetComponent<Collider>()); s.name = "BlanketStripe"; s.transform.SetParent(_blanket, false);
                s.transform.localPosition = new Vector3(-0.64f + i * 0.32f, 0.03f, 0f); s.transform.localScale = new Vector3(0.32f, 0.04f, 1.1f);
                s.GetComponent<MeshRenderer>().sharedMaterial = i % 2 == 0 ? m1 : m2;
            }
            var stone = CoastMaterials.CreateLit(new Color(0.42f, 0.42f, 0.44f));
            for (int i = 0; i < 3; i++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(s.GetComponent<Collider>()); s.name = "Stone3"; s.transform.SetParent(_blanket, false);
                s.transform.localPosition = new Vector3(1.15f + (i % 2) * 0.25f, 0.1f, -0.3f + i * 0.28f); s.transform.localScale = new Vector3(0.28f, 0.2f, 0.24f);
                s.GetComponent<MeshRenderer>().sharedMaterial = stone;
            }
        }

        /// 196차(통합 테스트: 새 게임 첫 화면부터 큰 벌레에게 물려 HP 25 → 1): 도입·CS1 전·꼬마 튜토리얼 동안은 벌레·산적이 멈추고 물지 않는다
        bool StoryCalm => Save != null && VillageStory.PlaceMode && (!Save.prologueSeen || (Save.storyTut >= 1 && Save.storyTut <= 3) || (Save.storyTut == 0 && !VillageStory.Seen(VillageStory.Order[0], Save)));

        // ── 현재 목표 ─────────────────────────────────────────────────────
        enum TKind { None, Tut, Scene, Frag }
        TKind StoryTarget(out VillageStory.Scene sc, out Vector3 pos, out string label)
        {
            sc = null; pos = Vector3.zero; label = null;
            if (Save == null || !Save.prologueSeen) return TKind.None;
            if (Save.storyTut >= 1 && Save.storyTut <= 3)
            {
                var sp = _spots.Find(x => x.id == TutSpot[Save.storyTut]);
                pos = sp != null ? sp.pos : VillageWorld.Ground(0f, 30f);
                label = Loc.T(TutKo[Save.storyTut], TutEn[Save.storyTut]);
                return TKind.Tut;
            }
            sc = VillageStory.Pending(Save);
            if (sc != null) { pos = ScenePos(sc); label = Loc.T(sc.placeKo, sc.placeEn); return TKind.Scene; }
            sc = VillageStory.PendingFrag(Save);
            if (sc != null) { pos = ScenePos(sc); label = Loc.T(sc.placeKo, sc.placeEn); return TKind.Frag; }
            return TKind.None;
        }

        Vector3 ScenePos(VillageStory.Scene s)
        {
            if (!string.IsNullOrEmpty(s.spot)) { var sp = _spots.Find(x => x.id == s.spot); if (sp != null) return sp.pos; }
            var p = s.pos;
            if (s.spot == "bus" || p == Vector2.zero) p = new Vector2(VillageEast.StopX, VillageEast.StopZ);
            return VillageWorld.Ground(p.x, p.y);
        }

        void RefreshStoryBeacon(bool force = false)
        {
            var k = StoryTarget(out var sc, out var pos, out var label);
            string key = k == TKind.None ? "" : k + ":" + (sc != null ? sc.id : Save.storyTut.ToString());
            if (!force && key == _storyBeaconKey) return;
            _storyBeaconKey = key;
            if (_storyBeacon != null) Destroy(_storyBeacon.gameObject); _storyBeacon = null;
            VillageMap.StoryTarget = null;
            if (_creatures != null) _creatures.LeadTo = null;
            if (k == TKind.None) { RefreshStatus(); return; }
            _storyPos = pos;
            var col = k == TKind.Frag ? new Color(0.45f, 0.75f, 1f) : k == TKind.Tut ? new Color(0.55f, 0.95f, 0.55f) : new Color(1f, 0.82f, 0.30f);
            _storyBeacon = StoryBeacon.Make(transform, pos, col, 3.0f);   // 196차: 큰 영역(반경 3 m)
            VillageMap.StoryTarget = new Vector2(pos.x, pos.z); VillageMap.StoryLabel = label; VillageMap.StoryCol = col;
            RefreshStatus();
        }

        /// HUD 미션 띠에 이야기 줄(있으면 미션 대신)
        string StoryLine()
        {
            if (Save == null) return null;
            if (!Save.prologueSeen) return Loc.T("……", "……");
            var k = StoryTarget(out var sc, out var _, out var label);
            if (k == TKind.Tut) return Loc.T($"▶ 꼬마를 따라가기 ({Save.storyTut}/3) · {label}", $"▶ Follow the kid ({Save.storyTut}/3) · {label}");
            if (k == TKind.Scene) return sc.id == "CS1" ? Loc.T("✨ 빛나는 담요 자리로 걸어가 보자", "✨ Walk to the glowing blanket") : Loc.T($"✨ 이야기 「{VillageStory.Title(sc)}」 · {label}", $"✨ Story '{VillageStory.Title(sc)}' · {label}");
            return null;   // 기억 조각은 미니맵 별로만(미션 띠는 그대로)
        }

        // ── 매 프레임 ─────────────────────────────────────────────────────
        void TickStory()
        {
            if (Save == null || _storyPlaying || !Save.prologueSeen || _player == null) return;
            _storyCheckT -= Time.deltaTime;
            if (_storyCheckT <= 0f) { _storyCheckT = 0.5f; RefreshStoryBeacon(); }
            if (_storyBeacon == null) return;
            bool outside = _interior == null && VillageZones.At(_player.position) == VillageZones.Zone.None;
            _storyBeacon.gameObject.SetActive(outside);
            var d = _storyPos - _player.position; d.y = 0f; float dist = d.magnitude;
            if (_creatures != null) _creatures.LeadTo = outside && dist > 5f ? _storyPos : (Vector3?)null;
            if (!outside || _busy || _hud.Locked || CinematicPlayer.IsPlaying) return;
            if (dist < 3.2f) StartCoroutine(ArriveStory());
        }

        IEnumerator ArriveStory()
        {
            var k = StoryTarget(out var sc, out var _, out var _);
            if (k == TKind.None) yield break;
            _storyPlaying = true; _busy = true; _walkTo = null; CancelAuto(false);
            if (k == TKind.Tut) { yield return TutArrive(); _busy = false; _storyPlaying = false; RefreshStoryBeacon(true); yield break; }
            yield return PlayPlaceScene(sc);
            _busy = false; _storyPlaying = false;
            RefreshStoryBeacon(true);
        }

        IEnumerator PlayPlaceScene(VillageStory.Scene s)
        {
            yield return FadeScreen(true, 0.4f);
            string id = s.kind == VillageStory.Kind.Cut ? "CS" + s.index : s.kind == VillageStory.Kind.Ev ? "EV" + s.index : s.id;
            if (VillageStory.DevForce == s) VillageStory.DevForce = null;
            bool done = false;
            CinematicPlayer.Play(id, () => done = true);
            while (!done) yield return null;
            if (s.kind == VillageStory.Kind.Cut) { StoryProgress.MarkCutsceneSeen(s.index); LevelSystem.Add(LevelSystem.ExpChapterRead); }
            else if (s.kind == VillageStory.Kind.Ev) { StoryProgress.MarkEventSeen(s.index); LevelSystem.Add(LevelSystem.ExpChapterRead / 2); }
            VillageStory.MarkSeen(s, Save);
            _gm.Persist();
            if (s.kind == VillageStory.Kind.Ev)
            {
                bool b = false; StoryEventBeat.ShowAfterEvent(Save, s.index, () => b = true); while (!b) yield return null;
            }
            if (s.kind != VillageStory.Kind.Frag)
            {
                bool c = false; ClueSystem.ShowAfterScene(Save, id, () => c = true); while (!c) yield return null;
            }
            _gm.Persist();
            TitleAudio.PlayRaising();
            SnapCamera();
            yield return FadeScreen(false, 0.5f);
            RefreshStatus();
            if (s.id == "CS1") { yield return TutStart(); yield break; }
            // 다음 이야기 예고(한 줄) — 다음 장면이 이미 열려 있으면 그쪽을 알려 준다
            var next = VillageStory.Pending(Save);
            if (next != null) CoastToast.Show(Loc.T($"✨ 이야기가 이어진다 — {next.placeKo}", $"✨ The story continues — {next.placeEn}"));
            else
            {
                int nc = 0; foreach (var o in VillageStory.Order) if (!VillageStory.Seen(o, Save)) { nc = o.chapter; break; }
                if (nc > 0) CoastToast.Show(Loc.T($"다음 이야기는 {nc}장에서 — 그때 장소가 빛난다", $"Next story in chapter {nc} — its place will glow"));
            }
        }

        // ── 튜토리얼(꼬마와 함께) ─────────────────────────────────────────
        IEnumerator TutStart()
        {
            if (_creatures != null) { _creatures.KidHidden = false; _creatures.SnapKid(); }
            Save.storyTut = 1; _gm.Persist();
            yield return Say(Kid, Loc.T("누나, 따라와. 마을 구경시켜 줄게. 빛나는 데로 가면 돼.", "Follow me, sis. I'll show you the village. Just walk to the glow."));
            CoastToast.Show(Loc.T("조이스틱을 밀어 걷는다 — 끝까지 밀면 달린다", "Push the stick to walk — all the way to run"));
        }

        IEnumerator TutArrive()
        {
            int st = Save.storyTut;
            if (st == 1)
            {
                yield return Say(Kid, Loc.T("여기가 가게. 먹을 거랑 씨앗을 팔아.", "This is the shop. Food and seeds."));
                yield return Say(Kid, Loc.T("잡은 벌레, 캔 돌, 딴 귤도 여기서 사 줘. 돈이 모이면 뭐든 살 수 있어.", "They buy bugs, ores and tangerines too. Save up and you can buy anything."));
                Save.storyTut = 2;
            }
            else if (st == 2)
            {
                yield return Say(Kid, Loc.T("여기 텃밭. 밭 칸에 들어가서 씨를 심으면 자라.", "The garden. Step on a plot and plant seeds."));
                yield return Say(Kid, Loc.T("다음은 언덕 위 빈집. 오른쪽 「이동」 버튼을 누르면 알아서 걸어가.", "Next, the empty house up the hill. Tap 'Move' and you'll walk there."));
                Save.storyTut = 3;
            }
            else
            {
                yield return Say(Kid, Loc.T("이 집 비어 있어. 누나가 써도 돼.", "Nobody lives here. You can use it, sis."));
                yield return Say(Kid, Loc.T("밤이 되면 안에 들어가서 침대에서 자. 자고 나면 한 주가 지나.", "At night, sleep in the bed inside. A week passes when you sleep."));
                yield return Say(Kid, Loc.T("마을에 빛나는 데가 생기면 내가 데려다줄게. 거기 가면… 뭔가 생각날지도 몰라.", "When a place starts glowing, I'll take you there. Maybe… you'll remember something."));
                Save.storyTut = TutDone;
                CoastToast.Show(Loc.T("★ 튜토리얼 끝 — 빛나는 장소에 가면 이야기가 이어진다", "★ Tutorial done — glowing places continue the story"));
            }
            _gm.Persist(); RefreshStatus();
        }

        string Kid => Loc.T("꼬마", "Kid");
        IEnumerator Say(string who, string line)
        {
            _hud.Bubble(who, line);
            float t = 0f;
            while (_hud.PopupOpen) { t += Time.deltaTime; if (DevAutoBubble && t > 1.6f) _hud.ClosePopup(); yield return null; }
        }

        /// 꼬마에게 말 걸기 — 갈 곳이 있으면 그곳을 알려 주고 데려다준다
        bool StoryKidTalk()
        {
            var k = StoryTarget(out var sc, out var _, out var label);
            if (k == TKind.None || k == TKind.Tut && Save.storyTut == 0) return false;
            string line = k == TKind.Tut ? Loc.T($"{label}(으)로 가자! 빛나는 데야.", $"To the {label}! Where it glows.")
                        : sc != null && !string.IsNullOrEmpty(sc.kidKo) ? Loc.T(sc.kidKo, sc.kidEn) : Loc.T("저기 빛나는 데 가 보자.", "Let's go to the glow.");
            _hud.Choice(Kid, line + "\n" + Loc.T($"◎ {label}", $"◎ {label}"), new (string, Color, Action)[] {
                (Loc.T("▶ 데려다줘", "▶ Take me there"), new Color(1f, 0.72f, 0.30f), StoryGuide),
                (Loc.T("응, 이따가", "Later"), new Color(0.6f, 0.6f, 0.66f), null),
            });
            return true;
        }

        /// 잠 막기 — 아직 안 본 이야기(또는 튜토리얼)가 있으면 꼬마가 붙잡는다
        bool StorySleepGate()
        {
            if (Save == null || !VillageStory.PlaceMode) return false;
            var k = StoryTarget(out var sc, out var _, out var label);
            if (k != TKind.Tut && k != TKind.Scene) return false;
            string why = k == TKind.Tut ? Loc.T("누나, 아직 마을 구경 안 끝났어!", "Sis, the tour isn't over yet!")
                       : Loc.T("자기 전에 가 볼 데가 있어.", "There's somewhere to go before bed.");
            _hud.Choice(Kid, why + "\n" + Loc.T($"◎ {label}", $"◎ {label}"), new (string, Color, Action)[] {
                (Loc.T("▶ 데려다줘", "▶ Take me there"), new Color(1f, 0.72f, 0.30f), () => { if (_interior != null) ExitHouse(); StoryGuide(); }),
                (Loc.T("알았어", "OK"), new Color(0.6f, 0.6f, 0.66f), null),
            });
            return true;
        }

        /// 자동 이동으로 이야기 장소까지(도로가 없으면 옛 큰길 경유)
        void StoryGuide()
        {
            var k = StoryTarget(out var _, out var pos, out var label);
            if (k == TKind.None) return;
            if (_interior != null) { CoastToast.Show(Loc.T("밖으로 나가서 가자!", "Let's head outside first!")); return; }
            var flat = pos - _player.position; flat.y = 0f;
            List<Vector3> route = flat.magnitude < 8f ? new List<Vector3> { pos } : (VillageRoad.Route(Save, _player.position, pos, out var _) ?? Route(_player.position, pos));
            if (_autoCo != null) StopCoroutine(_autoCo);
            if (_autoHunt) SetAutoHunt(false);
            var sp = new Spot { id = "story", title = label, pos = pos, radius = 2.6f, on = null };
            _autoDest = "story"; _autoCo = StartCoroutine(AutoMoveCo(sp, route)); _hud.SetButtonOn("AutoMove", true);
        }

        // ── 도입: 송전탑 위에서 깨어난다 ───────────────────────────────────
        Canvas _introCv; Image _introFade; Text _introText;
        IEnumerator IntroTowerWake()
        {
            _storyPlaying = true; _busy = true;
            if (_creatures != null) _creatures.KidHidden = true;
            if (_hud != null && _hud.Root != null) _hud.Root.gameObject.SetActive(false);
            if (_map != null) _map.SetHidden(true);
            _introCv = CoastUiCanvas.Create("StoryIntro", 145);
            var root = CoastUiCanvas.Root(_introCv);
            _introFade = CoastHudLayout.MakeImage(root, "Fade", Vector2.zero, Vector2.one, new Vector2(-400f, -400f), new Vector2(400f, 400f), Color.black); _introFade.raycastTarget = false;
            _introText = CoastHudLayout.MakeText(root, "T", "", 30, TextAnchor.MiddleCenter, new Vector2(0f, 0.30f), new Vector2(1f, 0.40f), Vector2.zero, Vector2.zero);
            _introText.color = new Color(1f, 1f, 1f, 0f); CoastUiArt.OutlineText(_introText, new Color(0f, 0f, 0f, 0.7f), 2f);

            // 탑 꼭대기 자리
            Vector3 top = VillageWorld.Ground(15f, 41f) + Vector3.up * 14f; Vector3 center = top;
            if (VillageWorld.Tower != null)
            {
                var rs = VillageWorld.Tower.GetComponentsInChildren<Renderer>(); bool any = false; Bounds b = default;
                foreach (var r in rs) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
                if (any)
                {
                    center = b.center;
                    // 196차: 탑 꼭대기 「판」 — 넓은(1.2 m 넘는) 부품 가운데 가장 높은 윗면(가는 피뢰침은 뺀다)
                    float plate = b.min.y + b.size.y * 0.8f;
                    foreach (var r in rs) { var rb = r.bounds; if (rb.size.x > 1.2f && rb.size.z > 1.2f && rb.max.y > plate && rb.max.y < b.max.y - 0.2f) plate = rb.max.y; }
                    top = new Vector3(b.center.x, plate + 0.02f, b.center.z);
                }
            }
            _cc.enabled = false; _walkTo = null; _vel = Vector3.zero;
            _player.position = top; _player.rotation = Quaternion.Euler(0f, 200f, 0f);
            Debug.LogWarning($"[Story] tower top {top} center {center}");
            // 1컷: 암전 → 아래에서 올려다본 탑 꼭대기(하늘 앞에 선 작은 사람)
            _storyCam = true;
            var fwd = _player.forward; var side = Vector3.Cross(Vector3.up, fwd).normalized;
            _player.position = top + fwd * 0.55f; var me = _player.position;
            _storyCamPos = me + fwd * 7.5f - Vector3.up * 3.2f + side * 1.5f; _storyCamLook = me + Vector3.up * 1.1f;
            _cam.transform.position = _storyCamPos; _cam.transform.LookAt(_storyCamLook); _storyCamVel = Vector3.zero;
            yield return new WaitForSeconds(0.8f);
            yield return IntroFade(0f, 2.4f);
            _storyCamPos = me + fwd * 5.2f - Vector3.up * 1.2f + side * 1.0f;
            yield return new WaitForSeconds(1.8f);
            yield return IntroText("……?", 1.8f);
            // 2컷: 등 뒤 위 — 탑 아래로 마을·바다(어디인지 모른다)
            _storyCamPos = me - fwd * 4.2f + Vector3.up * 2.6f + side * 0.8f; _storyCamLook = me + fwd * 28f - Vector3.up * 13f;
            yield return new WaitForSeconds(3.4f);
            // 3컷: 옆 높은 곳에서 발밑(아득한 높이)
            _storyCamPos = me + side * 6f + Vector3.up * 1.8f; _storyCamLook = me - Vector3.up * 9f;
            yield return new WaitForSeconds(2.6f);
            // 내려가기 버튼
            bool go = false;
            var btn = CoastUiArt.GlossyPill(root, "Down", new Color(1f, 0.62f, 0.70f), 22, 8); btn.raycastTarget = true;
            var brt = btn.rectTransform; brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.16f); brt.sizeDelta = new Vector2(360f, 84f);
            var bt = CoastHudLayout.MakeText(brt, "T", Loc.T("⬇ 탑에서 내려가기", "⬇ Climb down"), 28, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 3f), Vector2.zero); bt.color = Color.white; bt.fontStyle = FontStyle.Bold;
            var bb = btn.gameObject.AddComponent<Button>(); bb.transition = Selectable.Transition.None; bb.onClick.AddListener(() => { CoastPrefs.Vibrate(); go = true; });
            float wait = 0f;
            while (!go) { wait += Time.deltaTime; if (DevAutoBubble && wait > 2f) go = true; brt.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(Time.time * 5f)); yield return null; }
            Destroy(btn.gameObject);
            // 내려간다: 카메라가 따라 내려가며 암전
            var land = VillageWorld.Ground(7.6f, 37.4f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 2.4f)
            {
                var p = Vector3.Lerp(me, new Vector3(me.x, land.y + 0.5f, me.z), Mathf.SmoothStep(0f, 1f, t));
                _player.position = p; _storyCamPos = p + side * 5f + Vector3.up * 1.5f; _storyCamLook = p;
                if (t > 0.55f) _introFade.color = new Color(0f, 0f, 0f, Mathf.InverseLerp(0.55f, 0.95f, t));
                yield return null;
            }
            _introFade.color = Color.black;
            // 탑 아래 — 담요 자리가 보이는 곳에 선다
            var toBlanket = VillageWorld.Ground(VillageStory.Blanket.x, VillageStory.Blanket.y) - land; toBlanket.y = 0f;
            _player.position = land; _player.rotation = Quaternion.LookRotation(toBlanket.normalized, Vector3.up);
            _cc.enabled = true; _storyCam = false; SnapCamera();
            Save.prologueSeen = true; Save.villageX = land.x; Save.villageZ = land.z; _gm.Persist();
            if (_hud != null && _hud.Root != null) _hud.Root.gameObject.SetActive(true);
            if (_map != null) _map.SetHidden(false);
            RefreshStoryBeacon(true);
            yield return IntroFade(0f, 1.2f);
            Destroy(_introCv.gameObject); _introCv = null;
            _busy = false; _storyPlaying = false;
            CoastToast.Show(Loc.T("조이스틱을 밀어 빛나는 곳으로 걸어가 보자", "Push the stick and walk to the glow"));
            RefreshStatus();
        }
        IEnumerator IntroFade(float to, float dur)
        {
            float a0 = _introFade.color.a;
            for (float t = 0f; t < dur; t += Time.deltaTime) { _introFade.color = new Color(0f, 0f, 0f, Mathf.Lerp(a0, to, t / dur)); yield return null; }
            _introFade.color = new Color(0f, 0f, 0f, to);
        }
        IEnumerator IntroText(string s, float hold)
        {
            _introText.text = s;
            for (float t = 0f; t < 0.5f; t += Time.deltaTime) { _introText.color = new Color(1f, 1f, 1f, t / 0.5f); yield return null; }
            yield return new WaitForSeconds(hold);
            for (float t = 0f; t < 0.5f; t += Time.deltaTime) { _introText.color = new Color(1f, 1f, 1f, 1f - t / 0.5f); yield return null; }
            _introText.color = new Color(1f, 1f, 1f, 0f);
        }
        /// LateUpdate 에서: 연출 카메라(부드럽게 따라감)
        void StoryCamTick()
        {
            _cam.transform.position = Vector3.SmoothDamp(_cam.transform.position, _storyCamPos, ref _storyCamVel, 0.9f);
            var want = Quaternion.LookRotation((_storyCamLook - _cam.transform.position).normalized, Vector3.up);
            _cam.transform.rotation = Quaternion.Slerp(_cam.transform.rotation, want, 1f - Mathf.Exp(-Time.deltaTime * 2.5f));
        }

        // ── 개발용 ────────────────────────────────────────────────────────
        /// 새 게임 도입을 다시(세이브는 prologueSeen·튜토리얼만 되돌림 — 테스트 뒤 183 세이브 복원)
        public void DevStoryIntro() { if (Save == null) return; Save.prologueSeen = false; Save.storyTut = 0; VillageStory.DevForce = VillageStory.Find("CS1"); if (_interior != null) ExitHouse(); StopAllStory(); StartCoroutine(IntroTowerWake()); }
        public void DevStoryGoTarget()
        {
            var k = StoryTarget(out var _, out var pos, out var _); if (k == TKind.None) { Debug.LogWarning("[Story] no target"); return; }
            if (_interior != null) ExitHouse();
            var dir = (_player.position - pos); dir.y = 0f; if (dir.sqrMagnitude < 0.01f) dir = Vector3.back;
            Teleport(pos + dir.normalized * 3.0f);
            Debug.LogWarning($"[Story] target {k} {_storyBeaconKey} at {pos}");
        }
        public void DevStoryLog()
        {
            var k = StoryTarget(out var sc, out var pos, out var label);
            Debug.LogWarning($"[Story] ch={Save.chapter} wk={Save.week} pro={Save.prologueSeen} tut={Save.storyTut} frag={Save.storyFragMask} target={k} {(sc != null ? sc.id : "-")} '{label}' at {pos} line='{StoryLine()}' seen={Save.storySeenMask} init={Save.storyMaskInit}");
            var fd = _player != null ? _player.parent.GetComponentInChildren<FaceDecal>() : null;
            if (fd != null && fd.Head != null) { var mr = fd.GetComponent<MeshRenderer>(); Debug.LogWarning($"[Story] face decal pos={fd.transform.position} head={fd.Head.position} enabled={(mr != null && mr.enabled)} active={fd.gameObject.activeInHierarchy} cam={_cam.transform.position}"); }
            else Debug.LogWarning("[Story] face decal none");
        }
        public void DevTrySleep() { SleepBed(); Debug.LogWarning("[Story] try sleep → popup=" + _hud.PopupOpen + " busy=" + _busy); }
        public void DevKidTalk() { bool r = StoryKidTalk(); Debug.LogWarning("[Story] kid talk story=" + r); }
        public void DevStoryGuide() { StoryGuide(); Debug.LogWarning("[Story] guide auto=" + (_autoCo != null)); }
        public void DevStoryForce(string id) { VillageStory.DevForce = VillageStory.Find(id); RefreshStoryBeacon(true); DevStoryLog(); }
        public void DevStorySkipTut() { if (Save == null) return; Save.prologueSeen = true; Save.storyTut = TutDone; VillageStory.MarkSeen(VillageStory.Order[0], Save); if (_creatures != null) _creatures.KidHidden = false; _gm.Persist(); RefreshStoryBeacon(true); }
        void StopAllStory() { _storyPlaying = false; _busy = false; _storyCam = false; if (_introCv != null) Destroy(_introCv.gameObject); _introCv = null; }
    }
}
