using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 196차(사용자: 「자고난 다음에 컷씬 나오는게 아니고 각 컷씬의 장소(하이라이트로 특정 영역 표시)에 갔을때 컷씬 나오는걸로 · 엔딩까지 스무스하게」):
    /// 이야기 장소표 — 챕터에 닿으면 그 장면의 장소가 빛나고(바닥 고리 + 빛기둥 + 미니맵 별), 꼬마가 앞장서고, 그 자리에 들어가면 컷씬.
    /// 컷씬 순서·자막은 CinematicTable 그대로(바꾸지 않음). 여기선 「어디서」만 정한다.
    public static class VillageStory
    {
        public enum Kind { Cut, Ev, Frag }
        public class Scene
        {
            public string id; public Kind kind; public int index; public int chapter;
            public string placeKo, placeEn;   // 장소 이름(퀘스트 줄·미니맵)
            public string spot;               // VillageHub 스팟 id(자리 = 그 스팟의 pos). null 이면 pos 직접
            public Vector2 pos;               // spot 이 없을 때 · 스팟을 못 찾았을 때 대신
            public string kidKo, kidEn;       // 꼬마의 유도 한마디(자막 문장 아님 — 게임 안내)
            public string after;              // Frag: 이 장면을 본 뒤에 열린다
        }

        // 장소 좌표는 마을 기준(VillageWorld): 송전탑 (15,41) · 담요 자리 (12.4,38.4) · 엄마 집 스팟 (1.2,-18.4) · 가게 (-5.5,-29.5) · 텃밭 (0,-6)
        //   바닷가 (-1,-31) · 등대 (-9.5,-58.2) · 정자 (28,-6) · 우리집 (0,32) · 병원 문 · 브런치 카페 · 버스 정류장(동쪽 차도)
        public static readonly Vector2 Blanket = new Vector2(12.4f, 37.6f);

        public static readonly Scene[] Order = {
            S("CS1", Kind.Cut, 1, 1, "송전탑 아래 담요", "Blanket under the tower", null, Blanket, "", ""),
            S("EV1", Kind.Ev, 1, 2, "버스 정류장", "Bus stop", "bus", Vector2.zero, "누나, 정류장 가자! 거기서 탑까지 달리기야.", "Sis, to the bus stop! We race from there to the tower."),
            S("CS2", Kind.Cut, 2, 3, "마을 가게 앞", "In front of the shop", "shop", new Vector2(-5.5f, -29.5f), "누나, 배고파. 가게 가자.", "Sis, I'm hungry. Let's go to the shop."),
            S("EV3", Kind.Ev, 3, 4, "바닷가 방파제", "Beach breakwater", "beach", new Vector2(-1f, -31f), "바다 가자. 물장구 치고 싶어.", "Let's go to the sea. I want to splash."),
            S("CS3", Kind.Cut, 3, 5, "언덕길 외딴집 (엄마 집)", "The lone house (Mom's)", "mom", new Vector2(1.2f, -18.4f), "저 집… 누나 가 봐. 나는 마당에 있을게.", "That house… go in, sis. I'll wait in the yard."),
            S("EV4", Kind.Ev, 4, 6, "해녀네 앞 바위", "Rocks by the haenyeo house", "house_해녀네", new Vector2(-1f, -31f), "새벽에 아줌마가 바다에 간대. 따라가 보자.", "The lady goes diving at dawn. Let's follow."),
            S("CS4", Kind.Cut, 4, 7, "엄마 집 텃밭", "Mom's vegetable patch", "garden", new Vector2(0f, -6f), "아줌마 텃밭에 가 봐. 파, 상추 있어.", "Go to her patch. Scallions, lettuce."),
            S("EV5", Kind.Ev, 5, 8, "가게 앞 (우유 두 병)", "Shop front (two bottles)", "shop", new Vector2(-5.5f, -29.5f), "그 아저씨 또 가게 왔어. 우유 사러.", "That man's at the shop again. For milk."),
            S("EV6", Kind.Ev, 6, 9, "창가 (브런치 카페)", "By the window (cafe)", "cafe", new Vector2(20f, 0f), "눈 온다! 창에 그림 그리자.", "It's snowing! Let's draw on the window."),
            S("CS5", Kind.Cut, 5, 10, "엄마 집 장롱", "Mom's wardrobe", "mom", new Vector2(1.2f, -18.4f), "아줌마 없대. 집 좀 봐 줄래?", "She's out. Will you watch the house?"),
            S("EV7", Kind.Ev, 7, 11, "정자 (운동장 구석)", "Pavilion (schoolyard corner)", "play", new Vector2(28f, -6f), "정자에 가 봐. 도시락 먹기 좋은 데야.", "Go to the pavilion. Good spot for lunch."),
            S("CS6", Kind.Cut, 6, 12, "등대 아래 바위 불턱", "Stone shelter by the lighthouse", "light", new Vector2(-9.5f, -58.2f), "등대 밑 바위 틈에 뭐가 있어. 가 볼래?", "Something's in the rocks under the lighthouse. Go look?"),
            S("EV8", Kind.Ev, 8, 13, "언덕 위 외딴집 (우리집)", "Lone house on the hill", "hero", new Vector2(0f, 32f), "비 온다… 언덕 위 집으로 가자.", "Rain's coming… to the house on the hill."),
            S("EV9", Kind.Ev, 9, 14, "병원 가는 길 (리어카)", "Road to the hospital (cart)", "hospital", new Vector2(23f, 13.5f), "병원 쪽 길… 누나, 거기 가 봐.", "The road to the hospital… go there, sis."),
            S("EV2", Kind.Ev, 2, 15, "버스 정류장 (검은 차)", "Bus stop (black car)", "bus", Vector2.zero, "정류장… 누나, 차 떠나기 전에!", "The stop… sis, before the car leaves!"),
            S("CS7", Kind.Cut, 7, 17, "엄마 집 마루", "Mom's porch", "mom", new Vector2(1.2f, -18.4f), "누나, 머리띠 들고… 그 집으로 가.", "Sis, take the headband… go to that house."),
            S("EV10", Kind.Ev, 10, 19, "엄마 집 부엌", "Mom's kitchen", "mom", new Vector2(1.2f, -18.4f), "……엄마한테 가.", "……Go to Mom."),
            S("CS8", Kind.Cut, 8, 20, "송전탑 아래", "Under the tower", null, Blanket, "스무 살 생일이야. 탑으로 가자.", "It's your 20th birthday. To the tower."),
        };

        /// 196차(사용자: OPEN 「처음엔 숨기고 기억 조각으로」): 오프닝 「그 약속」 다섯 컷을 세 조각으로 — 자막 그대로, 장소에서 줍는다(잠은 안 막음).
        public static readonly Scene[] Frags = {
            new Scene { id = "OPEN_F1", kind = Kind.Frag, index = 1, placeKo = "기억 조각 · 버스 정류장", placeEn = "Memory · Bus stop", spot = "bus", after = "CS2", kidKo = "정류장에 뭐가 반짝여.", kidEn = "Something glints at the stop." },
            new Scene { id = "OPEN_F2", kind = Kind.Frag, index = 2, placeKo = "기억 조각 · 엄마 집 달력", placeEn = "Memory · Mom's calendar", spot = "mom", pos = new Vector2(1.2f, -18.4f), after = "CS4", kidKo = "아줌마 집 달력… 동그라미 봤어?", kidEn = "The calendar at her house… see the circle?" },
            new Scene { id = "OPEN_F3", kind = Kind.Frag, index = 3, placeKo = "기억 조각 · 송전탑", placeEn = "Memory · The tower", spot = null, pos = Blanket, after = "CS5", kidKo = "탑이 웅웅 울어.", kidEn = "The tower is humming." },
        };

        static Scene S(string id, Kind k, int idx, int ch, string pko, string pen, string spot, Vector2 pos, string kko, string ken)
            => new Scene { id = id, kind = k, index = idx, chapter = ch, placeKo = pko, placeEn = pen, spot = spot, pos = pos, kidKo = kko, kidEn = ken };

        /// 장소 모드 = 마을이 켜진 스토리 모드. 이때 챕터 경계(잠 → TamaRaisingUI)는 컷씬을 틀지 않는다.
        public static bool PlaceMode => VillageHub.Enabled;
        /// 개발용: 이 장면을 (봤어도) 지금 장소 목표로 — 한 번 틀면 풀린다
        public static Scene DevForce;
        public static Scene Find(string id) { foreach (var s in Order) if (s.id == id) return s; foreach (var f in Frags) if (f.id == id) return f; return null; }

        /// 196차: 본 기록은 세이브마다(storySeenMask) — 시네마 목록용 전역 기록(PlayerPrefs)과 따로. 새 게임·2회차면 처음부터 다시 장소에서 본다.
        public static bool Seen(Scene s, SaveData save)
        {
            if (save == null) return true;
            if (s.kind == Kind.Frag) return (save.storyFragMask & (1 << s.index)) != 0;
            int i = System.Array.IndexOf(Order, s); return i >= 0 && (save.storySeenMask & (1 << i)) != 0;
        }
        public static void MarkSeen(Scene s, SaveData save)
        {
            if (save == null) return;
            if (s.kind == Kind.Frag) { save.storyFragMask |= 1 << s.index; return; }
            int i = System.Array.IndexOf(Order, s); if (i >= 0) save.storySeenMask |= 1 << i;
        }
        /// 옛 세이브(장소 모드 전): 이미 지난 챕터의 장면은 챕터 경계에서 봤던 것 — 본 것으로 옮긴다(현재 챕터 장면은 장소에서).
        public static void Migrate(SaveData save)
        {
            if (save == null || save.storyMaskInit) return;
            save.storyMaskInit = true;
            for (int i = 0; i < Order.Length; i++) if (Order[i].chapter < save.chapter) save.storySeenMask |= 1 << i;
        }

        /// 지금 가야 할 본 장면 — 이미 닿은 챕터 가운데 아직 안 본 첫 장면(순서대로). 옛 세이브에서 건너뛴 장면도 여기서 다시 잡힌다.
        public static Scene Pending(SaveData save)
        {
            if (save == null) return null;
            if (!save.prologueSeen) return null;   // 탑 위에서 깨어나는 도입 중
            if (DevForce != null) return DevForce;   // 개발용: 본 장면도 다시
            foreach (var s in Order)
            {
                if (s.chapter > save.chapter) break;
                if (!Seen(s, save)) return s;
            }
            return null;
        }

        /// 열린 기억 조각(본 장면 뒤, 아직 안 주움)
        public static Scene PendingFrag(SaveData save)
        {
            if (save == null || !save.prologueSeen) return null;
            foreach (var f in Frags)
            {
                if (Seen(f, save)) continue;
                if (save.bond214 < VillageHub.FragBond(f.index)) continue;   // 214차: 꼬마와의 약속으로 쌓은 인연이 모자라면 아직 안 보인다
                var a = System.Array.Find(Order, o => o.id == f.after);
                if (a != null && Seen(a, save)) return f;
            }
            return null;
        }

        public static string Title(Scene s)
        {
            if (s.kind == Kind.Cut) return StoryProgress.CutsceneTitle(s.index);
            if (s.kind == Kind.Ev) return StoryProgress.EventTitle(s.index);
            return Loc.T("기억 조각", "Memory fragment");
        }
    }

    /// 이야기 장소 표시: 바닥 빛 고리(숨쉬기) + 반투명 빛기둥 + 떠 있는 별. 기억 조각은 파랑.
    public class StoryBeacon : MonoBehaviour
    {
        Transform _ring, _pillar, _star; Material _ringM, _pillarM; Color _col; float _t;
        public static StoryBeacon Make(Transform parent, Vector3 pos, Color col, float radius)
        {
            var go = new GameObject("StoryBeacon"); go.transform.SetParent(parent, false); go.transform.position = pos;
            var b = go.AddComponent<StoryBeacon>(); b._col = col; b.Build(radius); return b;
        }
        void Build(float r)
        {
            _ringM = CoastMaterials.CreateUnlit(new Color(_col.r, _col.g, _col.b, 0.85f));
            _pillarM = CoastMaterials.CreateUnlit(new Color(_col.r, _col.g, _col.b, 0.28f));
            TryTransparent(_pillarM);
            // 고리: 짧은 상자 24개를 원으로(큰 판은 곡면 셰이더에 휘어 사라지므로 작게 쪼갬)
            _ring = new GameObject("Ring").transform; _ring.SetParent(transform, false);
            int n = 24;
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                var q = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(q.GetComponent<Collider>()); q.name = "Seg"; q.transform.SetParent(_ring, false);
                q.transform.localPosition = new Vector3(Mathf.Cos(a) * r, 0.08f, Mathf.Sin(a) * r);
                q.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                q.transform.localScale = new Vector3(0.14f, 0.06f, r * 2f * Mathf.PI / n * 1.05f);
                q.GetComponent<MeshRenderer>().sharedMaterial = _ringM;
            }
            var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(p.GetComponent<Collider>()); p.name = "Pillar"; _pillar = p.transform; _pillar.SetParent(transform, false);
            _pillar.localPosition = new Vector3(0f, 3.2f, 0f); _pillar.localScale = new Vector3(0.9f, 3.2f, 0.9f); p.GetComponent<MeshRenderer>().sharedMaterial = _pillarM;
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(s.GetComponent<Collider>()); s.name = "Star"; _star = s.transform; _star.SetParent(transform, false);
            _star.localPosition = new Vector3(0f, 2.6f, 0f); _star.localScale = Vector3.one * 0.45f; s.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateUnlit(Color.Lerp(_col, Color.white, 0.5f));
        }
        static void TryTransparent(Material m)
        {
            if (m == null) return;
            // URP Unlit: 투명 표면으로(셰이더 이름이 달라도 실패하지 않게)
            if (m.HasProperty("_Surface")) { m.SetFloat("_Surface", 1f); m.SetOverrideTag("RenderType", "Transparent"); m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000; }
        }
        void Update()
        {
            _t += Time.deltaTime;
            float k = 0.5f + 0.5f * Mathf.Sin(_t * 2.4f);
            if (_ring != null) { _ring.localScale = Vector3.one * (0.92f + 0.12f * k); _ring.Rotate(0f, 20f * Time.deltaTime, 0f); }
            if (_star != null) { _star.localPosition = new Vector3(0f, 2.5f + 0.35f * Mathf.Sin(_t * 1.7f), 0f); }
            if (_ringM != null) _ringM.color = new Color(_col.r, _col.g, _col.b, 0.6f + 0.35f * k);
            if (_pillarM != null) _pillarM.color = new Color(_col.r, _col.g, _col.b, 0.14f + 0.14f * k);
        }
    }
}
