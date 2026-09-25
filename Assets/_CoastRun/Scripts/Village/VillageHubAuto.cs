using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun.Village
{
    /// 183차(사용자): ① 모바일 게임식 자동이동(장소 고르면 알아서 걸어가 들어간다)·자동사냥 ② 방망이로 정령 치기(VillageCreatures)
    /// ③ 말 사냥 → 고기 / 계속 잡으면 강한 경찰 ④ 동물의 숲식 팔기 → 땅 사기 → 4주마다 월세.
    public partial class VillageHub
    {
        Coroutine _autoCo, _huntCo; bool _autoHunt; Text _huntBtnT; string _autoDest;

        void InitAutoFeatures()
        {
            if (_hud != null)
            {
                _hud.AddSideButton("AutoMove", "➜", Loc.T("이동", "Go"), new Color(0.40f, 0.70f, 0.95f), -516f, AutoMoveMenu);
                _huntBtnT = _hud.AddSideButton("AutoHunt", "⚔", Loc.T("자동", "Auto"), new Color(0.92f, 0.45f, 0.45f), -638f, ToggleAutoHunt);
            }
            if (_creatures != null)
            {
                _creatures.OnHorseKilled = HorseKilled;
                _creatures.OnPoliceHit = PoliceHit;
                _creatures.OnPoliceDown = PoliceDown;
            }
            if (Save != null)
            {
                // 수배도: 주가 바뀔 때마다 1 씩 식는다
                if (Save.horseHeatWeek >= 0 && Save.week > Save.horseHeatWeek) { Save.horseHeat = Mathf.Max(0, Save.horseHeat - (Save.week - Save.horseHeatWeek)); Save.horseHeatWeek = Save.week; }
                int pay = VillageLand.CollectRent(Save, out int months);
                if (pay > 0) { _gm.Persist(); StartCoroutine(LaterToast(1.6f, Loc.T($"🏡 월세가 들어왔다! {months}달치 +{pay:N0}G", $"🏡 Rent received! {months} month(s) +{pay:N0}G"))); }
            }
            _spots.Add(new Spot { id = "ranch", title = Loc.T("🐴 방목장", "🐴 Ranch"), pos = VillageRanch.Gate, radius = 2.2f, on = () => _hud.Bubble(Loc.T("목장 주인", "Rancher"), Loc.T("우리 말들 예쁘지? …설마 방망이로 치려는 건 아니지? 그랬다간 경찰 부른다!", "Pretty horses, right? …You're not going to hit them, are you? I'll call the police!")) });
            if (_world != null && Save != null) _roadRoot = VillageRoad.BuildAll(_world, Save);   // 187차: 깐 도로
            for (int i = 0; i < VillageLand.Lots.Length; i++) _spots.Add(new Spot { id = "lot_" + i, title = LotTitle(i), pos = VillageLand.Front(i), radius = 2.6f, on = LotAct(i) });
        }
        IEnumerator LaterToast(float t, string msg) { yield return new WaitForSeconds(t); CoastToast.Show(msg); RefreshStatus(); }

        /// 조이스틱을 직접 밀었거나 다른 일로 멈출 때
        void CancelAuto(bool byStick)
        {
            if (_autoCo != null) { StopCoroutine(_autoCo); _autoCo = null; _walkTo = null; _hud.SetButtonOn("AutoMove", false); if (byStick) CoastToast.Show(Loc.T("자동 이동을 멈췄다", "Auto-move stopped")); }
            if (_autoHunt && byStick) { SetAutoHunt(false); CoastToast.Show(Loc.T("⚔ 자동사냥 해제 (직접 움직임)", "⚔ Auto-hunt off (manual move)")); }
        }

        // ── 185차(사용자: 「우리집 앞에서 들어갈 때 들어가겠냐고 물어봐 줘」) ─────────────
        /// 문 앞에 닿거나 행동 버튼을 누르면 확인 팝업. 「아직」이면 5초 동안 다시 묻지 않는다(문 앞에 서 있어도).
        void AskEnterHome(Action enter)
        {
            if (_busy || _hud.Locked) return;
            _doorCooldown = Time.time + 5f; _walkTo = null;
            _hud.Choice(Loc.T("🏠 우리집", "🏠 Our home"), Loc.T($"집에 들어갈까? (밥 · 잠 · 책상) — 오늘 활동 {ActCount}/{ActsPerDay}", $"Go inside? (eat · sleep · desk) — {ActCount}/{ActsPerDay}"),
                new (string, Color, Action)[] {
                    (Loc.T("🏠 들어가기", "🏠 Go in"), new Color(0.35f, 0.62f, 0.95f), () => { _doorCooldown = Time.time + 3f; enter(); }),
                    (Loc.T("아직", "Not yet"), new Color(0.6f, 0.6f, 0.66f), () => { _doorCooldown = Time.time + 5f; }),
                });
        }

        // ── 187차(사용자: 「대화 버튼은 없애고, 근처 가면 자동 대화, 근처에서 상대를 누르면 대화 버튼 누른 것처럼」) ──
        int _autoTalkNpc = -1; float _autoTalkT; readonly Dictionary<int, float> _talkedAt = new Dictionary<int, float>();
        void TickAutoTalk()
        {
            if (_creatures == null || _interior != null || Time.time < _autoTalkT) return;
            _autoTalkT = Time.time + 0.3f;
            int ni = _creatures.NearestNpcIndex(2.4f);
            if (ni < 0) { _autoTalkNpc = -1; return; }
            if (ni == _autoTalkNpc) return;   // 이미 이 사람 곁에서 말함 — 떠났다 다시 오면 또
            _autoTalkNpc = ni;
            if (_talkedAt.TryGetValue(ni, out var t0) && Time.time - t0 < 20f) return;
            _talkedAt[ni] = Time.time;
            // 부탁이 있는 사람은 부탁 팝업(자동 이동·사냥 중엔 말풍선만 — 가던 길을 막지 않게)
            if (_autoCo == null && !_autoHunt && VillageRequest.OnTalk(this, ni)) { MissionTick(VillageMission.Kind.Talk); return; }
            _creatures.TalkNpc(ni); MissionTick(VillageMission.Kind.Talk);
        }
        void OnPadTap()
        {
            if (_busy || _hud.Locked) return;
            if (TryTalkAt(_hud.CamPad.LastTap)) return;
            OnAct();
        }
        /// 화면 좌표에 가까운(120 px·7 m 안) 마을 사람이 있으면 그쪽을 보고 대화 — 왼쪽(조이스틱) 톡에서도 쓴다(188차)
        bool TryTalkAt(Vector2 screen)
        {
            if (_busy || _hud.Locked || _creatures == null || _cam == null) return false;
            int ni = _creatures.NpcAtScreen(_cam, screen, 120f * Mathf.Max(1f, Screen.width / 720f));
            if (ni < 0 || Vector3.Distance(_creatures.NpcPos(ni), _player.position) >= 7f) return false;
            var d = _creatures.NpcPos(ni) - _player.position; d.y = 0f; if (d.sqrMagnitude > 0.01f) _player.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
            _talkedAt[ni] = Time.time; _autoTalkNpc = ni;
            Debug.Log("[NpcTap] talk " + _creatures.NpcName(ni));
            if (VillageRequest.OnTalk(this, ni)) { MissionTick(VillageMission.Kind.Talk); return true; }
            _creatures.TalkNpc(ni); MissionTick(VillageMission.Kind.Talk); return true;
        }

        // ── ① 자동이동 ─────────────────────────────────────────────────
        struct Dest { public string label, spot; public Color col; }
        void AutoMoveMenu()
        {
            if (_busy || _hud.Locked) return;
            if (_interior != null) { CoastToast.Show(Loc.T("집 밖에서 쓸 수 있어. 문으로 나가자!", "Use it outside — head out the door!")); return; }
            if (InDungeon) { CoastToast.Show(Loc.T("갱도 안에서는 직접 걸어서 — 사다리·구멍을 찾아가자", "Walk on your own in the shaft")); return; }   // 199차
            if (ZoneAutoMove()) return;   // 195차: 시내·관광지·광산 안에서는 곧장 걷기
            // 199차(사용자: 「자동이동은 고르려면 너무 길다 — 카테고리별로 나눈 뒤 고르게」): 먼저 분류를 고르고, 그 안에서 장소를 고른다
            var cats = new List<(string, Color, Action)>();
            void Cat(string ko, string en, Color c, (string ko, string en, string spot, Color c)[] items)
            {
                int open = 0; foreach (var it in items) { var sp = _spots.Find(s => s.id == it.spot); if (sp != null && AutoMoveOpen(sp)) open++; }
                cats.Add((Loc.T($"{ko}  ({open}/{items.Length})", $"{en}  ({open}/{items.Length})"), c, () => AutoMoveList(Loc.T(ko, en), items)));
            }
            Cat("🏠 집 · 생활", "🏠 Home · daily", new Color(0.35f, 0.62f, 0.95f), new[] {
                ("🏠 마이룸 (우리집)", "🏠 My room (home)", "hero", new Color(0.35f, 0.62f, 0.95f)),
                ("👩 엄마 집 (펫·낮잠)", "👩 Mom's house (pets · nap)", "mom", new Color(0.98f, 0.78f, 0.55f)),
                ("🏥 병원 (치료)", "🏥 Hospital (treatment)", "hospital", new Color(0.95f, 0.55f, 0.58f)),
                ("🍊 귤빛 브런치 (HP 회복)", "🍊 Tangerine Brunch (HP)", "cafe", new Color(1f, 0.66f, 0.28f)) });
            Cat("🛍 가게 · 일 · 교통", "🛍 Shops · work · travel", new Color(0.98f, 0.62f, 0.72f), new[] {
                ("🛍 마을상점 (장보기·팔기)", "🛍 Village shop (buy · sell)", "shop", new Color(0.98f, 0.62f, 0.72f)),
                ("💼 알바나라", "💼 Job center", "job", new Color(0.62f, 0.55f, 0.90f)),
                ("🚌 버스 정류장 (시내·중문)", "🚌 Bus stop (city · Jungmun)", "bus", new Color(0.40f, 0.62f, 0.92f)) });
            Cat("🌾 농사 · 목장", "🌾 Farming · ranch", new Color(0.45f, 0.78f, 0.45f), new[] {
                ("🌱 텃밭", "🌱 Garden", "garden", new Color(0.45f, 0.78f, 0.45f)),
                ("🐔 농장 (닭·토끼)", "🐔 Farm (hens · rabbits)", "farm", new Color(0.95f, 0.80f, 0.45f)),
                ("🐴 방목장 (말)", "🐴 Ranch (horses)", "ranch", new Color(0.72f, 0.55f, 0.40f)),
                ("🐄 축사 (소·흑돼지)", "🐄 Barn", "barn", new Color(0.85f, 0.55f, 0.45f)),
                ("🍊 귤 과수원", "🍊 Orchard", "orchard", new Color(1f, 0.62f, 0.25f)),
                ("🐝 유채꽃 벌통", "🐝 Beehives", "hive", new Color(1f, 0.82f, 0.30f)) });
            Cat("🎣 채집 · 놀이", "🎣 Gathering · play", new Color(0.40f, 0.78f, 0.90f), new[] {
                ("⛏ 오름 광산", "⛏ Mine", "mine", new Color(0.55f, 0.50f, 0.45f)),
                ("🎣 바닷가 낚시", "🎣 Beach fishing", "beach", new Color(0.40f, 0.78f, 0.90f)),
                ("🎮 정자 (놀기)", "🎮 Pavilion (play)", "play", new Color(0.55f, 0.80f, 0.60f)) });
            cats.Add((Loc.T("🏡 땅 (매물 · 내 임대 주택) …", "🏡 Land (for sale · my rentals) …"), new Color(0.45f, 0.75f, 0.50f), LotWalkMenu));
            _hud.Choice(Loc.T("🧭 자동 이동", "🧭 Auto-move"), Loc.T("어디로 갈까? 분류를 먼저 고르자. 조이스틱을 밀면 멈춘다.", "Where to? Pick a category first. Push the stick to stop."), cats.ToArray());
        }

        void AutoMoveList(string title, (string ko, string en, string spot, Color c)[] items)
        {
            var list = new List<(string, Color, Action)>();
            // 190차(사용자: 「처음엔 우리집·알바나라만. 나머지는 비활성, 누르면 끊어진 길을 이어야 한다고」): 도로망으로 못 가는 곳은 회색 🔒
            foreach (var it in items)
            {
                var sp = _spots.Find(s => s.id == it.spot); if (sp == null) continue; string spot = it.spot;
                if (AutoMoveOpen(sp)) { list.Add((Loc.T(it.ko, it.en), it.c, () => StartAutoMove(spot))); continue; }
                list.Add((Loc.T("🔒 " + it.ko + " — 길이 끊김", "🔒 " + it.en + " — road broken"), new Color(0.66f, 0.66f, 0.70f), () => _hud.Bubble(Loc.T("하늘", "Haneul"),
                    Loc.T("가는 길 중간이 끊어져 있어. 주황 고깔 사이 끊어진 길을 도구 「🛣 도로 깔기」(한 칸 500G)로 이어야 자동이동할 수 있어!",
                          "The road there is broken. Fill the gap between the orange cones with the Road tool (500G a tile) to auto-move!"))));
            }
            list.Add((Loc.T("↩ 분류로 돌아가기", "↩ Back to categories"), new Color(0.70f, 0.72f, 0.78f), AutoMoveMenu));
            _hud.Choice("🧭 " + title, Loc.T("고르면 알아서 걸어가서 들어간다.", "Walks there and enters on its own."), list.ToArray());
        }

        void LotWalkMenu()
        {
            var list = new List<(string, Color, Action)>();
            for (int i = 0; i < VillageLand.Lots.Length; i++)
            {
                var L = VillageLand.Lots[i]; bool own = VillageLand.Owns(Save, i); string id = "lot_" + i;
                list.Add((own ? Loc.T("🏡 내 임대 주택 · " + L.ko, "🏡 My rental · " + L.en) : Loc.T($"🏷 매물 · {L.ko} ({L.price:N0}G)", $"🏷 For sale · {L.en} ({L.price:N0}G)"), own ? new Color(0.45f, 0.75f, 0.50f) : new Color(0.85f, 0.55f, 0.45f), () => StartAutoMove(id)));
            }
            _hud.Choice(Loc.T("🧭 땅으로 가기", "🧭 Go to land"), null, list.ToArray());
        }

        /// 190차: 우리집·알바나라는 늘 열림, 나머지는 도로망으로 이어져야(8 m 안이면 바로)
        bool AutoMoveOpen(Spot sp)
        {
            if (sp.id == "hero" || sp.id == "job") return true;
            var flat = sp.pos - _player.position; flat.y = 0f; if (flat.magnitude < 8f) return true;
            return VillageRoad.Route(Save, _player.position, sp.pos, out var _) != null;
        }
        void StartAutoMove(string spotId)
        {
            var sp = _spots.Find(s => s.id == spotId); if (sp == null) return;
            // 187차(사용자: 「도로가 이어진 곳만 자동이동」): 큰길·방목장길 + 내가 깐 도로가 내 자리와 목적지를 이어야 간다
            List<Vector3> route;
            var flat = sp.pos - _player.position; flat.y = 0f;
            if (flat.magnitude < 8f) route = new List<Vector3> { sp.pos };
            else
            {
                route = VillageRoad.Route(Save, _player.position, sp.pos, out var why);
                if (route == null && (spotId == "hero" || spotId == "job")) route = Route(_player.position, sp.pos);   // 190차: 우리집·알바나라는 늘 간다(옛 큰길 경유)
                if (route == null)
                {
                    string msg = why == "start" ? Loc.T("🛣 지금 있는 곳이 도로에서 멀어 자동이동을 못 해요. 도로 위로 가거나 도로를 이어 깔아 주세요.", "🛣 You're too far from any road. Walk to a road or lay one.")
                               : why == "goal" ? Loc.T($"🛣 {sp.title} 까지 도로가 없어요. 도구 「도로 깔기」로 한 칸({VillageRoad.Price}G)씩 이어 주세요.", $"🛣 No road reaches {sp.title}. Lay tiles ({VillageRoad.Price}G each) with the Road tool.")
                               : Loc.T("🛣 가는 길 중간이 끊어져 있어요. 주황 고깔 사이 끊어진 길을 「도로 깔기」로 이어 주세요.", "🛣 The road is broken — fill the gap between the orange cones.");
                    CoastToast.Show(msg); return;
                }
            }
            if (_autoCo != null) StopCoroutine(_autoCo);
            if (_autoHunt) SetAutoHunt(false);
            _autoDest = spotId; _autoCo = StartCoroutine(AutoMoveCo(sp, route)); _hud.SetButtonOn("AutoMove", true);   // 187차: 켜짐 표시
        }

        /// 큰길(Path)·방목장 갈림길(RanchPath)을 따라가는 경유점 — 가까우면 바로
        List<Vector3> Route(Vector3 from, Vector3 to)
        {
            var pts = new List<Vector3>();
            Vector2 f = new Vector2(from.x, from.z), t = new Vector2(to.x, to.z);
            bool clear = !Physics.SphereCast(from + Vector3.up * 0.9f, 0.4f, (to - from).normalized, out var _, Vector3.Distance(from, to), ~0, QueryTriggerInteraction.Ignore);
            if (Vector2.Distance(f, t) < 12f || clear) { pts.Add(to); return pts; }
            bool fr = f.x < -20f && f.y > 18f, tr = t.x < -20f && t.y > 18f;   // 방목장 쪽
            int Near(Vector2 p) { int bi = 0; float bd = 1e9f; for (int i = 0; i < VillageWorld.Path.Length; i++) { float d = Vector2.Distance(p, VillageWorld.Path[i]); if (d < bd) { bd = d; bi = i; } } return bi; }
            var rp = VillageWorld.RanchPath;
            if (fr) for (int i = rp.Length - 1; i >= 0; i--) pts.Add(VillageWorld.Ground(rp[i].x, rp[i].y));
            int i0 = fr ? 1 : Near(f), i1 = tr ? 1 : Near(t);
            if (!(fr && tr))
            {
                int step = i1 >= i0 ? 1 : -1;
                for (int i = i0; ; i += step) { var p = VillageWorld.Path[i]; pts.Add(VillageWorld.Ground(p.x, p.y)); if (i == i1) break; }
            }
            if (tr && !fr) for (int i = 0; i < rp.Length; i++) pts.Add(VillageWorld.Ground(rp[i].x, rp[i].y));
            // 첫 경유점이 뒤쪽이면(이미 지나침) 건너뛴다
            if (pts.Count > 1 && Vector3.Distance(from, pts[1]) < Vector3.Distance(pts[0], pts[1])) pts.RemoveAt(0);
            pts.Add(to);
            return pts;
        }

        IEnumerator AutoMoveCo(Spot sp, List<Vector3> route)
        {
            CoastToast.Show(Loc.T("🧭 자동 이동 중… (조이스틱을 밀면 멈춤)", "🧭 Auto-moving… (push the stick to stop)"));
            int stuck = 0; float side = 1f;
            for (int w = 0; w < route.Count; w++)
            {
                var target = route[w]; bool last = w == route.Count - 1;
                float reach = last ? Mathf.Max(0.8f, sp.radius * 0.55f) : 1.6f;
                var lastPos = _player.position; float checkT = 0f, guard = 0f;
                while (true)
                {
                    while (_busy || _hud.Locked) { _walkTo = null; yield return null; }
                    var d = target - _player.position; d.y = 0f;
                    if (d.magnitude < reach) break;
                    _walkTo = target; checkT += Time.deltaTime; guard += Time.deltaTime;
                    if (checkT > 0.7f)
                    {
                        checkT = 0f;
                        if (Vector3.Distance(lastPos, _player.position) < 0.35f)
                        {
                            // 막혔다 — 옆으로 비켜 섰다가 다시
                            stuck++; side = -side;
                            var right = Vector3.Cross(Vector3.up, d.normalized) * side;
                            _walkTo = _player.position + right * 2.5f + d.normalized * 0.5f;
                            float t2 = 0f; while (t2 < 0.8f && !_busy) { t2 += Time.deltaTime; yield return null; }
                            if (stuck >= 5) break;
                        }
                        lastPos = _player.position;
                    }
                    if (guard > 40f) { stuck = 99; break; }
                    yield return null;
                }
                if (stuck >= 5) break;
            }
            _walkTo = null;
            if (stuck >= 5)
            {
                // 길이 막혔다 — 문 앞으로 살짝 순간이동(화면 암전)
                _busy = true; yield return FadeScreen(true, 0.25f); Teleport(sp.pos + (_player.position - sp.pos).normalized * 1.2f); yield return FadeScreen(false, 0.25f); _busy = false;
            }
            _autoCo = null; _hud.SetButtonOn("AutoMove", false);
            // 도착 — 그 장소의 행동(들어가기·메뉴)을 바로 연다. 마이룸은 곧장 집 안으로
            _doorCooldown = Time.time + 3f;
            if (sp.id == "hero" && VillageWorld.HeroHouse != null) EnterHouse(VillageWorld.HeroHouse, Loc.T("우리집", "Our home"), VillageWorld.HeroHouse.TransformPoint(new Vector3(0f, 0f, 3.3f)));
            else sp.on?.Invoke();
        }

        // ── ① 자동사냥 ─────────────────────────────────────────────────
        const int HuntMinHp = 10;   // 187차: 12%(24) → HP 10 — 체력이 낮은 세이브에서 켜자마자 꺼지던 것(사용자: 「자동사냥이 안 된다」)
        void ToggleAutoHunt()
        {
            if (_busy || _hud.Locked) return;
            if (!_autoHunt && Save != null && Save.stats.stamina <= HuntMinHp) { _hud.Bubble(Loc.T("하늘", "Haneul"), Loc.T($"체력이 {Save.stats.stamina} 밖에 없어서 자동사냥은 무리야… 밥 먹고 자고 오자!", $"Only {Save.stats.stamina} HP — too weak to auto-hunt. Eat and sleep first!")); return; }
            SetAutoHunt(!_autoHunt); CoastToast.Show(_autoHunt ? Loc.T("⚔ 자동사냥 ON — 가까운 벌레·정령·산적을 알아서 잡는다 (말·귀신 제외)", "⚔ Auto-hunt ON") : Loc.T("⚔ 자동사냥 OFF", "⚔ Auto-hunt OFF"));
        }
        void SetAutoHunt(bool on)
        {
            _autoHunt = on;
            if (_huntBtnT != null) _huntBtnT.text = on ? Loc.T("자동 ON", "Auto ON") : Loc.T("자동", "Auto");
            _hud.SetButtonOn("AutoHunt", on);   // 187차: 켜짐 표시
            if (_huntCo != null) { StopCoroutine(_huntCo); _huntCo = null; }
            _walkTo = null;
            if (on) { if (_autoCo != null) { StopCoroutine(_autoCo); _autoCo = null; _hud.SetButtonOn("AutoMove", false); } _huntCo = StartCoroutine(AutoHuntCo()); }
        }
        IEnumerator AutoHuntCo()
        {
            float wanderT = 0f, idleSaid = -99f;
            while (_autoHunt)
            {
                if (_busy || _hud.Locked || _interior != null || Save == null) { _walkTo = null; yield return new WaitForSeconds(0.3f); continue; }
                if (Save.stats.stamina <= HuntMinHp) { SetAutoHunt(false); CoastToast.Show(Loc.T("💤 체력이 얼마 없어 자동사냥을 멈췄다. 밥 먹고 쉬자!", "💤 Low HP — auto-hunt stopped. Eat and rest!")); yield break; }
                if (_creatures != null && _creatures.FindHuntTarget(_player.position, 22f, out var pos, out bool bat))
                {
                    if (_bat != bat || _axe || _pick || _rod) { _rod = false; _axe = false; _pick = false; _bat = bat; ApplyTool(); }
                    var d = pos - _player.position; d.y = 0f;
                    if (d.magnitude > 1.6f) { _walkTo = pos; yield return new WaitForSeconds(0.15f); continue; }
                    _walkTo = null; if (d.sqrMagnitude > 0.01f) _player.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
                    Swing(); var msg = _creatures.Swing(_bat); if (!string.IsNullOrEmpty(msg)) CoastToast.Show(msg);
                    wanderT = 0f; yield return new WaitForSeconds(0.55f); continue;
                }
                // 사냥감이 없으면 근처를 어슬렁(바다·마을 밖으로는 안 간다)
                wanderT -= 0.3f;
                if (wanderT <= 0f)
                {
                    wanderT = 3.5f;
                    var p = _player.position + new Vector3(UnityEngine.Random.Range(-10f, 10f), 0f, UnityEngine.Random.Range(-10f, 10f));
                    p.x = Mathf.Clamp(p.x, -38f, 38f); p.z = Mathf.Clamp(p.z, -14f, 40f);
                    if (VillageWorld.Height(p.x, p.z) > VillageWorld.SeaLevel + 0.6f) _walkTo = VillageWorld.Ground(p.x, p.z);
                    if (Time.time - idleSaid > 25f) { idleSaid = Time.time; CoastToast.Show(Loc.T("⚔ 사냥감을 찾는 중…", "⚔ Looking for prey…")); }
                }
                yield return new WaitForSeconds(0.3f);
            }
        }

        // ── ③ 말 사냥 · 경찰 ────────────────────────────────────────────
        void HorseKilled()
        {
            if (Save == null) return;
            Save.ranchHorseWeek = Save.week; Save.ranchHorseDead++;
            LifeItems.Add(Save, "ing_meat", 3);
            Save.horseHeat++; Save.horseHeatWeek = Save.week;
            _gm.Persist(); RefreshStatus(); CoastAudioManager.PlayAnywhere(CoastSfx.Coin);
            if (Save.horseHeat <= 1)
                CoastToast.Show(Loc.T("🍖 말을 잡았다! 고기 +3 … 목장 주인이 수상하게 본다. 또 잡으면 경찰을 부를 거야!", "🍖 Got a horse! Meat +3 … the rancher is watching. Do it again and he'll call the police!"));
            else
            {
                int lv = Mathf.Clamp(Save.horseHeat - 1, 1, 3);
                CoastToast.Show(Loc.T($"🍖 고기 +3 — 🚨 경찰 출동! (수배 {lv}단계) 달려서 도망치거나 방망이로 버텨!", $"🍖 Meat +3 — 🚨 Police! (wanted Lv {lv}) Run or fight!"));
                if (_creatures != null && _creatures.PoliceCount == 0) _creatures.SpawnPolice(lv);
            }
        }
        void PoliceHit(int dmg)
        {
            if (Save == null || _busy) return;
            Save.stats.stamina = Mathf.Max(0, Save.stats.stamina - dmg);
            int fine = Mathf.Min(2000, Mathf.RoundToInt(Save.stats.money * 0.08f));   // 186차 상한 500→2000 Save.stats.money = Mathf.Max(0, Save.stats.money - fine);
            _gm.Persist(); RefreshStatus(); CoastPrefs.VibrateEvent(); StartCoroutine(HitFlash());
            VillagePang.Burst(_player.position + Vector3.up * 0.9f, new Color(0.35f, 0.45f, 0.95f), new Color(1f, 0.85f, 0.4f), 1.3f);
            if (Save.stats.stamina <= 0)
            {
                // 체포 — 경찰은 돌아가고 수배는 풀린다(대신 병원비·벌금)
                if (_autoHunt) SetAutoHunt(false);
                if (_creatures != null) _creatures.ClearPolice(); Save.horseHeat = 0; _gm.Persist();
                StartCoroutine(LaterToast(2.2f, Loc.T("🚓 경찰에게 체포됐다… 벌금을 내고 풀려났다. 수배가 풀렸다.", "🚓 Arrested… paid the fine and got released. Wanted level cleared.")));
                StartCoroutine(Hospital()); return;
            }
            CoastToast.Show(Loc.T($"🚓 경찰에게 붙잡혔다! HP −{dmg} · 벌금 {fine}G — 달리면 따돌릴 수 있다", $"🚓 Caught by the police! HP −{dmg} · fine {fine}G — run to lose them"));
        }
        void PoliceDown(bool all)
        {
            if (Save == null) return;
            Save.stats.money += 200;   // 186차 60→200
            if (all) { Save.horseHeat = 0; }
            _gm.Persist(); RefreshStatus();
        }

        // ── ④ 팔기 · 땅 · 월세 ───────────────────────────────────────────
        void ShopCounterMenu()
        {
            if (_busy || _hud.Locked) return;
            VillageDex.Talk(Save, VillageDex.NpcIndex("shop"), 1);   // 195차
            _hud.Choice(Loc.T("🛍 상점 아줌마", "🛍 Shopkeeper"), Loc.T("사고, 팔고! 벌레·물고기·작물·고기 뭐든 사 줄게. 땅도 소개해 줄 수 있어.", "Buy or sell! I'll buy bugs, fish, crops, meat. I also sell land."),
                new (string, Color, Action)[] {
                    (Loc.T("🛒 장보기", "🛒 Buy"), new Color(0.98f, 0.62f, 0.72f), OpenShop),
                    (Loc.T("💰 물건 팔기", "💰 Sell"), new Color(0.98f, 0.80f, 0.30f), SellMenu),
                    (Loc.T($"🏡 땅 사기 · 월세 (보유 {VillageLand.OwnedCount(Save)}곳)", $"🏡 Land · rent ({VillageLand.OwnedCount(Save)} lots)"), new Color(0.45f, 0.75f, 0.50f), LandMenu),
                });
        }

        void SellMenu()
        {
            if (Save == null) return;
            if (_dayNight != null && !_dayNight.ShopOpen) { _hud.Bubble(Loc.T("상점", "Shop"), Loc.T("지금은 문을 닫았어. 팔기는 오전 8시 ~ 오후 7시.", "Closed now. Selling is 8 AM – 7 PM.")); return; }
            var items = VillageSell.Sellable(Save);
            if (items.Count == 0) { _hud.Bubble(Loc.T("상점 아줌마", "Shopkeeper"), Loc.T("팔 게 없네. 벌레·물고기·작물·고기·조개를 모아 와!", "Nothing to sell. Bring bugs, fish, crops, meat, shells!")); return; }
            int total = 0; foreach (var it in items) total += it.n * it.price;
            var rows = new List<(string, Color, Action)>();
            rows.Add((Loc.T($"💰 전부 팔기  +{total:N0}G", $"💰 Sell all  +{total:N0}G"), new Color(0.98f, 0.75f, 0.25f), () =>
            {
                int got = 0; foreach (var it in VillageSell.Sellable(Save)) got += VillageSell.Sell(Save, it.id, it.n, it.price);
                SoldToast(got);
            }));
            items.Sort((x, y) => (y.n * y.price).CompareTo(x.n * x.price));
            if (items.Count > 8) items.RemoveRange(8, items.Count - 8);   // 한 화면에 8 줄까지(나머지는 「전부 팔기」에 포함)
            foreach (var it in items)
            {
                var cap = it; var def = LifeItems.Get(cap.id); string nm = def.HasValue ? LifeItems.Name(def.Value) : cap.id;
                rows.Add((Loc.T($"{nm} ×{cap.n} · 개당 {cap.price}G  (+{cap.n * cap.price:N0}G)", $"{nm} ×{cap.n} · {cap.price}G each  (+{cap.n * cap.price:N0}G)"), new Color(0.95f, 0.88f, 0.70f), () =>
                {
                    int got = VillageSell.Sell(Save, cap.id, cap.n, cap.price); SoldToast(got);
                    if (VillageSell.Sellable(Save).Count > 0) StartCoroutine(ReopenSell());
                }));
            }
            _hud.Choice(Loc.T("💰 물건 팔기", "💰 Sell items"), Loc.T($"가방에서 팔 수 있는 것 — 모두 {total:N0}G. 돈을 모아 땅을 사면 매달 월세가 들어온다!", $"Sellable — {total:N0}G in all. Save up for land and earn monthly rent!"), rows.ToArray());
        }
        IEnumerator ReopenSell() { yield return null; yield return null; SellMenu(); }
        void SoldToast(int got)
        {
            if (got <= 0) return;
            _gm.Persist(); RefreshStatus(); CoastAudioManager.PlayAnywhere(CoastSfx.Coin);
            CoastToast.Show(Loc.T($"💰 팔았다! +{got:N0}G (지금 {Save.stats.money:N0}G)", $"💰 Sold! +{got:N0}G (now {Save.stats.money:N0}G)"));
        }

        string LotTitle(int i)
        {
            var L = VillageLand.Lots[i];
            return VillageLand.Owns(Save, i)
                ? Loc.T($"🏡 내 임대 주택 · 월세 {L.rent}G (다음 입금 {VillageLand.WeeksToRent(Save)}주 뒤)", $"🏡 My rental · {L.rent}G/mo (next in {VillageLand.WeeksToRent(Save)}w)")
                : Loc.T($"🏷 {L.ko} 사기 · {L.price:N0}G (월세 {L.rent}G)", $"🏷 Buy {L.en} · {L.price:N0}G ({L.rent}G/mo)");
        }
        Action LotAct(int i) => () => LotPopup(i);
        void LotPopup(int i)
        {
            if (_busy || _hud.Locked || Save == null) return;
            var L = VillageLand.Lots[i];
            if (VillageLand.Owns(Save, i))
            {
                _hud.Bubble(Loc.T("세입자", "Tenant"), Loc.T($"집이 아주 좋아요! 월세 {L.rent}G 는 {VillageLand.WeeksToRent(Save)}주 뒤에 꼬박꼬박 넣을게요.", $"Love the place! {L.rent}G rent coming in {VillageLand.WeeksToRent(Save)} week(s)."));
                return;
            }
            bool can = Save.stats.money >= L.price;
            _hud.Choice(Loc.T($"🏷 {L.ko}", $"🏷 {L.en}"), Loc.T($"값 {L.price:N0}G · 사면 임대 주택이 서고 4주(한 달)마다 월세 {L.rent}G 가 들어온다. 지금 {Save.stats.money:N0}G", $"{L.price:N0}G · a rental goes up and pays {L.rent}G every 4 weeks. You have {Save.stats.money:N0}G"),
                new (string, Color, Action)[] {
                    (can ? Loc.T($"✅ 산다 (−{L.price:N0}G)", $"✅ Buy (−{L.price:N0}G)") : Loc.T($"💸 돈이 모자란다 ({L.price - Save.stats.money:N0}G 더)", $"💸 Not enough ({L.price - Save.stats.money:N0}G short)"), can ? new Color(0.45f, 0.78f, 0.50f) : new Color(0.6f, 0.6f, 0.66f), can ? (Action)(() => BuyLot(i)) : null),
                    (Loc.T("아직", "Not yet"), new Color(0.6f, 0.6f, 0.66f), null),
                });
        }
        void BuyLot(int i)
        {
            if (!VillageLand.Buy(Save, i)) return;
            _gm.Persist(); RefreshStatus(); CoastAudioManager.PlayAnywhere(CoastSfx.Coin);
            VillageLand.BuildLot(_world, Save, i);
            var sp = _spots.Find(s => s.id == "lot_" + i); if (sp != null) sp.title = LotTitle(i);
            VillagePang.Burst(VillageLand.Ground(i) + Vector3.up * 1.5f, new Color(0.55f, 0.85f, 0.55f), new Color(1f, 0.95f, 0.6f), 2.0f);
            CoastToast.Show(Loc.T($"🎉 {VillageLand.Lots[i].ko}을(를) 샀다! 임대 주택이 섰다 — 4주마다 월세 {VillageLand.Lots[i].rent}G", $"🎉 Bought {VillageLand.Lots[i].en}! Rent {VillageLand.Lots[i].rent}G every 4 weeks"));
        }
        void LandMenu()
        {
            if (Save == null) return;
            var rows = new List<(string, Color, Action)>();
            for (int i = 0; i < VillageLand.Lots.Length; i++)
            {
                int k = i; var L = VillageLand.Lots[i]; bool own = VillageLand.Owns(Save, i);
                rows.Add((own ? Loc.T($"🏡 {L.ko} — 내 땅 · 월세 {L.rent}G", $"🏡 {L.en} — owned · {L.rent}G/mo") : Loc.T($"🏷 {L.ko} — {L.price:N0}G (월세 {L.rent}G)", $"🏷 {L.en} — {L.price:N0}G ({L.rent}G/mo)"),
                    own ? new Color(0.45f, 0.75f, 0.50f) : new Color(0.85f, 0.55f, 0.45f), () => LotPopup(k)));
            }
            rows.Add((Loc.T("🧭 매물로 걸어가 보기", "🧭 Walk to a lot"), new Color(0.40f, 0.70f, 0.95f), AutoMoveMenu));
            string sub = VillageLand.OwnedCount(Save) > 0
                ? Loc.T($"한 달(4주) 월세 {VillageLand.MonthlyRent(Save):N0}G · 다음 입금 {VillageLand.WeeksToRent(Save)}주 뒤 · 지금 {Save.stats.money:N0}G", $"Rent {VillageLand.MonthlyRent(Save):N0}G / 4 weeks · next in {VillageLand.WeeksToRent(Save)}w · {Save.stats.money:N0}G")
                : Loc.T($"벌레·물고기·작물을 팔아 돈을 모아 땅을 사자. 사면 4주마다 월세! 지금 {Save.stats.money:N0}G", $"Sell bugs, fish, crops to buy land — then rent every 4 weeks! {Save.stats.money:N0}G");
            _hud.Choice(Loc.T("🏡 땅 · 월세", "🏡 Land · rent"), sub, rows.ToArray());
        }
    }
}

namespace CoastRun.Village
{
    public partial class VillageHub
    {
        // ── 183차 개발용 ──
        public void DevAutoMove(string spot) => StartAutoMove(spot);
        public void DevAutoHunt(bool on) => SetAutoHunt(on);
        public void DevWalkIntoHeroDoor() { var hh = VillageWorld.HeroHouse; if (hh == null) return; foreach (var hs in VillageWorld.Houses) if (hs.house == hh) { Teleport(hs.door + hh.forward * 2.2f, hh.eulerAngles.y + 180f); _doorCooldown = 0f; _walkTo = hs.door; Debug.LogWarning("[185] walking into hero door " + hs.door); } }
        public void DevFaceSea() { _devSeaYaw = (_devSeaYaw + 90f) % 360f; Teleport(_player.position, _devSeaYaw); Debug.LogWarning("[Sea] yaw=" + _devSeaYaw + " pos=" + _player.position); }  float _devSeaYaw = 90f;   // 187차: 바다(남쪽) 보기
        // ── 187차: 도로 깔기(도구) ─────────────────────────────────────
        Transform _roadRoot; GameObject _roadGhost; MeshFilter _roadGhostMf; int _roadGhostKey = int.MinValue;
        int RoadTargetKey() { var p = _player.position + _player.forward * 1.7f; return VillageRoad.KeyAt(p); }
        string RoadBlock(int k)
        {
            if (VillageRoad.IsRoad(Save, k)) return Loc.T("이미 도로가 있는 칸이야.", "There's already a road here.");
            var c = VillageRoad.Center(k);
            if (c.y < VillageWorld.SeaLevel + 0.35f) return Loc.T("물 위에는 도로를 깔 수 없어.", "Can't lay road on water.");
            foreach (var h in Physics.OverlapBox(c + Vector3.up * 1.0f, new Vector3(0.8f, 0.8f, 0.8f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                if (h.transform != _player && !h.transform.IsChildOf(_player) && h.bounds.size.y > 0.6f && !(h is TerrainCollider) && !(h is MeshCollider mc && mc.name.Contains("Terrain"))) return Loc.T("건물·나무 같은 게 있는 칸엔 못 깔아.", "Something's in the way.");
            return null;
        }
        void TickRoadGhost(bool locked)
        {
            bool show = _road && !locked && _interior == null && Save != null;
            if (!show) { if (_roadGhost != null && _roadGhost.activeSelf) _roadGhost.SetActive(false); return; }
            if (_roadRoot == null) _roadRoot = VillageRoad.BuildAll(_world, Save);
            if (_roadGhost == null)
            {
                _roadGhost = new GameObject("RoadGhost", typeof(MeshFilter), typeof(MeshRenderer)); _roadGhost.transform.SetParent(_world, false);
                _roadGhostMf = _roadGhost.GetComponent<MeshFilter>(); var mr = _roadGhost.GetComponent<MeshRenderer>(); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            }
            if (!_roadGhost.activeSelf) _roadGhost.SetActive(true);
            int k = RoadTargetKey();
            if (k != _roadGhostKey)
            {
                _roadGhostKey = k; if (_roadGhostMf.sharedMesh != null) Destroy(_roadGhostMf.sharedMesh);
                _roadGhostMf.sharedMesh = VillageRoad.TileMesh(k, 0.16f);
                _roadGhost.GetComponent<MeshRenderer>().sharedMaterial = VillageRoad.GhostMat(RoadBlock(k) == null && Save.stats.money >= VillageRoad.Price);
            }
        }
        void PlaceRoad()
        {
            if (Save == null) return;
            if (_roadRoot == null) _roadRoot = VillageRoad.BuildAll(_world, Save);
            int k = RoadTargetKey(); var why = RoadBlock(k);
            if (why != null) { CoastToast.Show("🛣 " + why); return; }
            if (Save.stats.money < VillageRoad.Price) { CoastToast.Show(Loc.T($"🛣 돈이 모자라 (한 칸 {VillageRoad.Price}G, 지금 {Save.stats.money:N0}G)", $"🛣 Not enough money ({VillageRoad.Price}G a tile)")); return; }
            Save.stats.money -= VillageRoad.Price;
            if (Save.roadCells == null) Save.roadCells = new List<int>();
            Save.roadCells.Add(k); _gm.Persist(); RefreshStatus();
            VillageRoad.Spawn(_roadRoot, k);
            _roadGhostKey = int.MinValue;   // 미리보기 색 갱신
            Swing();
            VillagePang.Burst(VillageRoad.Center(k) + Vector3.up * 0.3f, new Color(0.95f, 0.85f, 0.60f), Color.white, 0.9f);
            CoastToast.Show(Loc.T($"🛣 도로 한 칸 −{VillageRoad.Price}G (깐 도로 {Save.roadCells.Count}칸 · 남은 돈 {Save.stats.money:N0}G)", $"🛣 Road tile −{VillageRoad.Price}G ({Save.roadCells.Count} tiles · {Save.stats.money:N0}G left)"));
        }
        /// 개발: 모든 장소가 도로로 이어지는지 로그
        public void DevRoadReport()
        {
            var sb = new System.Text.StringBuilder("[Road] from=" + _player.position + " tiles=" + (Save.roadCells != null ? Save.roadCells.Count : 0) + " |");
            foreach (var sp in _spots) { var r = VillageRoad.Route(Save, _player.position, sp.pos, out var why); sb.Append($" {sp.id}:{(r != null ? "ok" + r.Count : why)}"); }
            Debug.LogWarning(sb.ToString());
        }
        public void DevRoadTool() { DevTool(5); }
        /// 개발: 가장 가까운 NPC 앞 4.5 m 로 가서(자동 대화 2.4 m 밖) 그 NPC 의 화면 좌표(아래 기준 0~1)를 로그 → unity tap 으로 탭 대화 시험
        public void DevNpcTapSetup()
        {
            if (_creatures == null) return; int ni = _creatures.NearestNpcIndex(999f); if (ni < 0) return;
            var np = _creatures.NpcPos(ni); var dir = (_player.position - np); dir.y = 0f; if (dir.sqrMagnitude < 0.01f) dir = Vector3.back; dir.Normalize();
            Teleport(np + dir * 4.5f, Quaternion.LookRotation(-dir).eulerAngles.y);
            StartCoroutine(DevNpcScreenLater(ni));
        }
        IEnumerator DevNpcScreenLater(int ni)
        {
            yield return new WaitForSeconds(0.3f);
            var sp = _cam.WorldToScreenPoint(_creatures.NpcPos(ni) + Vector3.up * 0.8f);
            Debug.LogWarning($"[NpcTap] npc={_creatures.NpcName(ni)} idx={ni} nx={sp.x / Screen.width:F3} ny={sp.y / Screen.height:F3} dist={Vector3.Distance(_creatures.NpcPos(ni), _player.position):F1}");
        }
        /// 개발: x −4..34, z −14..46 막힘 지도(1 m, # = 콜라이더, = = 도로망, S = 장소) → Tools/_obsmap.txt
        /// 189차 진단: 바닷가 격자(4 m)마다 순간이동 → 가장 가까운 큰길 점으로 2.5초 걸어 보고, 1 m 도 못 가까워지면 막힘으로 기록
        public void DevBeachReach() { StartCoroutine(DevBeachReachCo()); }
        IEnumerator DevBeachReachCo()
        {
            var sb = new System.Text.StringBuilder(); var hl = _player.GetComponent<CcHitLog>(); if (hl == null) hl = _player.gameObject.AddComponent<CcHitLog>();
            Time.timeScale = 3f; int n = 0, bad = 0;
            for (int z = -16; z >= -64; z -= 4)
                for (int x = -28; x <= 16; x += 4)
                {
                    var g = VillageWorld.Ground(x, z);
                    if (g.y < VillageWorld.SeaLevel - 0.6f) continue;
                    bool blocked = false; foreach (var h in Physics.OverlapCapsule(g + Vector3.up * 0.4f, g + Vector3.up * 1.4f, 0.35f, ~0, QueryTriggerInteraction.Ignore)) if (!h.name.Contains("Terrain") && h.transform != _player && !h.transform.IsChildOf(_player)) { blocked = true; break; }
                    if (blocked) continue;
                    // 가장 가까운 큰길 점
                    Vector2 p = new Vector2(x, z), best = Vector2.zero; float bd = 1e9f; var P = VillageWorld.Path;
                    for (int i = 0; i < P.Length - 1; i++) { var a = P[i]; var ab = P[i + 1] - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude); var q = a + ab * t; float d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } }
                    if (Mathf.Sqrt(bd) < 2.5f) continue;
                    var target = VillageWorld.Ground(best.x, best.y);
                    if (_interior != null) { _spots.RemoveAll(s => s.id == "exit" || s.id.StartsWith("home_") || s.id == "counter" || s.id.StartsWith("hosp_") || s.id.StartsWith("in_")); Destroy(_interior.gameObject); _interior = null; _interiorKind = null; }
                    _hud.ClosePopup(); _doorCooldown = Time.time + 999f;
                    Teleport(g, Quaternion.LookRotation(new Vector3(best.x - x, 0f, best.y - z)).eulerAngles.y); yield return null;
                    var start = _player.position; float d0 = Vector2.Distance(new Vector2(start.x, start.z), best);
                    hl.Last = "-"; float t2 = 0f;
                    while (t2 < 2.5f) { if (_hud.Locked) _hud.ClosePopup(); _doorCooldown = Time.time + 999f; _walkTo = target; t2 += Time.deltaTime; yield return null; }
                    _walkTo = null;
                    float d1 = Vector2.Distance(new Vector2(_player.position.x, _player.position.z), best); n++;
                    if (d0 - d1 < 1.0f) { bad++; sb.AppendLine($"STUCK from ({x},{z}) h={g.y:F2} → path ({best.x:F1},{best.y:F1}) moved {d0 - d1:F2} now ({_player.position.x:F1},{_player.position.z:F1}) hit={hl.Last}"); }
                }
            Time.timeScale = 1f; _doorCooldown = Time.time + 2f;
            sb.Insert(0, $"[BeachReach] tested={n} stuck={bad}\n");
            System.IO.File.WriteAllText(System.IO.Path.Combine(Application.dataPath, "../Tools/_beachreach.txt"), sb.ToString());
            Debug.LogWarning($"[BeachReach] tested={n} stuck={bad}");
        }
        public void DevColliderDump()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var c in FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (!c.enabled || c.isTrigger) continue; var b = c.bounds; if (b.center.z > -12f || b.center.z < -80f) continue;
                string path = c.name; var t = c.transform.parent; for (int i = 0; i < 3 && t != null; i++, t = t.parent) path = t.name + "/" + path;
                sb.AppendLine($"{path} [{c.GetType().Name}] c=({b.center.x:F1},{b.center.y:F1},{b.center.z:F1}) s=({b.size.x:F1},{b.size.y:F1},{b.size.z:F1})");
            }
            System.IO.File.WriteAllText(System.IO.Path.Combine(Application.dataPath, "../Tools/_colliders_beach.txt"), sb.ToString());
        }
        public void DevObstacleMap() => DevObstacleMap(-4, 34, -14, 46, "_obsmap.txt");
        /// 189차: 범위 지정 + ^ = 가파름(1 m 에 0.9 m 넘게 오르내림 → 캐릭터가 못 오름)
        public void DevObstacleMap(int x0, int x1, int z0, int z1, string file)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var sp in _spots) sb.AppendLine($"spot {sp.id} {sp.pos.x:F1},{sp.pos.z:F1}");
            sb.AppendLine($"x {x0}..{x1} (col 0 = x {x0})");
            for (int z = z1; z >= z0; z--)
            {
                sb.Append($"{z,4} ");
                for (int x = x0; x <= x1; x++)
                {
                    var g = VillageWorld.Ground(x + 0.5f, z + 0.5f); char ch = '.';
                    if (VillageRoad.IsBase(VillageRoad.KeyAt(g))) ch = '=';
                    foreach (var h in Physics.OverlapBox(g + Vector3.up * 1.0f, new Vector3(0.45f, 0.7f, 0.45f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                        if (h.transform != _player && !h.transform.IsChildOf(_player) && !h.name.Contains("Terrain") && h.bounds.size.x < 60f) { ch = '#'; break; }
                    if (ch == '.') { float hx = VillageWorld.Height(x + 1.5f, z + 0.5f), hz = VillageWorld.Height(x + 0.5f, z + 1.5f), hx0 = VillageWorld.Height(x - 0.5f, z + 0.5f), hz0 = VillageWorld.Height(x + 0.5f, z - 0.5f); if (Mathf.Max(Mathf.Abs(hx - g.y), Mathf.Abs(hz - g.y), Mathf.Abs(hx0 - g.y), Mathf.Abs(hz0 - g.y)) > 0.9f) ch = '^'; }
                    if (g.y < VillageWorld.SeaLevel + 0.3f) ch = '~';
                    foreach (var sp in _spots) if (Mathf.Abs(sp.pos.x - (x + 0.5f)) < 0.5f && Mathf.Abs(sp.pos.z - (z + 0.5f)) < 0.5f) ch = 'S';
                    sb.Append(ch);
                }
                sb.AppendLine();
            }
            System.IO.File.WriteAllText(System.IO.Path.Combine(Application.dataPath, "../Tools/" + file), sb.ToString());
            Debug.LogWarning("[ObsMap] written");
        }
        /// 개발: 큰길(0,3) 동쪽으로 3칸 이어 깔기
        public void DevRoadDemo() { StartCoroutine(DevRoadDemoCo()); }
        IEnumerator DevRoadDemoCo()
        {
            DevTool(5); Save.stats.money = Mathf.Max(Save.stats.money, VillageRoad.Price * 3);
            var p = VillageWorld.Ground(1.5f, 3f); Teleport(p, 90f); yield return null;
            for (int i = 0; i < 3; i++) { PlaceRoad(); yield return new WaitForSeconds(0.4f); p += Vector3.right * 2f; Teleport(p, 90f); yield return null; }
            DevRoadReport();
        }
        public void DevPlaceRoad() { Save.stats.money = Mathf.Max(Save.stats.money, VillageRoad.Price); PlaceRoad(); }

        public void DevHeal() { Save.stats.stamina = PlayerStats.StatMax; _gm.Persist(); RefreshStatus(); }
        public bool DevAutoMoving => _autoCo != null;
        public void DevPolice(int lv) { if (_creatures != null) _creatures.SpawnPolice(lv); }
        public void DevGiveSellables() { if (Save == null) return; LifeItems.Add(Save, "bug_butterfly", 3); LifeItems.Add(Save, "bug_bigbeetle", 1); LifeItems.Add(Save, "gath_shell", 5); _gm.Persist(); }
        public void DevSellAll() { int got = 0; foreach (var it in VillageSell.Sellable(Save)) got += VillageSell.Sell(Save, it.id, it.n, it.price); SoldToast(got); Debug.LogWarning($"[Sell] got={got} money={Save.stats.money}"); }
        public void DevBuyLot(int i) { if (Save.stats.money < VillageLand.Lots[i].price) Save.stats.money = VillageLand.Lots[i].price; BuyLot(i); Debug.LogWarning($"[Land] mask={Save.landMask} rentWeek={Save.landRentWeek} money={Save.stats.money}"); }
        public void DevRentSim(int weeks) { int m0 = Save.stats.money; Save.week += weeks; int pay = VillageLand.CollectRent(Save, out int months); Save.week -= weeks; Save.landRentWeek -= weeks; Debug.LogWarning($"[Rent] +{weeks}w months={months} pay={pay} money {m0}->{Save.stats.money}"); }
        public string DevStateLine() => $"money={Save?.stats.money} heat={Save?.horseHeat} dead={Save?.ranchHorseDead} horses={VillageRanch.Horses.Count} police={(_creatures != null ? _creatures.PoliceCount : -1)} meat={LifeItems.Count(Save, "ing_meat")} hp={Save?.stats.stamina} auto={_autoCo != null} hunt={_autoHunt} pos={_player.position}";
        public void DevBatNearestHorse()
        {
            RanchHorse best = null; float bd = 1e9f;
            foreach (var h in VillageRanch.Horses) { if (h == null) continue; float d = Vector3.Distance(h.transform.position, _player.position); if (d < bd) { bd = d; best = h; } }
            if (best == null) return;
            var p = best.transform.position - best.transform.forward * 1.2f; Teleport(p, best.transform.eulerAngles.y);
            _rod = false; _axe = false; _pick = false; _bat = true; ApplyTool();
            Swing(); var msg = _creatures.Swing(true); if (!string.IsNullOrEmpty(msg)) CoastToast.Show(msg);
            Debug.LogWarning($"[HorseBat] d={Vector3.Distance(best.transform.position, _player.position):F2} hp={best.Hp} dead={best.Dead} msg={msg}");
        }
        public void DevSpawnSpiritBat()
        {
            _rod = false; _axe = false; _pick = false; _bat = true; ApplyTool();
            _creatures.DevSpawnSpirit(); Swing(); var msg = _creatures.Swing(true); Debug.LogWarning("[SpiritBat] " + msg);
        }
    }
}
