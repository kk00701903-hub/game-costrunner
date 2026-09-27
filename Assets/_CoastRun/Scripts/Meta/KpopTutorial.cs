using System.Collections;
using UnityEngine;

namespace CoastRun
{
    /// 194차(사용자): K-POP 러닝 튜토리얼 챕터 — 처음 한 번 자동 + 챕터 화면 「튜토리얼 다시보기」.
    /// 1챕터 코스 위에서 장애물·아이템을 하나씩 대본대로 놓고, 설명하는 동안은 속도를 확 낮춘다.
    /// 끝나면 평소 1챕터로 그대로 이어 달린다.
    public class KpopTutorial : MonoBehaviour
    {
        const string PrefDone = "CoastRun_KpopTutorialDone";
        public static bool Done => PlayerPrefs.GetInt(PrefDone, 0) == 1;
        public static void MarkDone() { PlayerPrefs.SetInt(PrefDone, 1); PlayerPrefs.Save(); }

        /// 다음 K-POP 시작을 튜토리얼로(다시보기).
        public static bool Pending;
        /// 보스전 등 — 이번 시작은 튜토리얼 금지.
        public static bool SuppressOnce;

        public static bool Active { get; private set; }
        static bool _slow, _feverLesson, _energyTaught;
        static KpopTutorial _inst;

        /// 설명 중 0.45, 레슨 사이 0.7, 아니면 1.
        public static float SpeedMul => Active ? (_slow ? 0.45f : 0.7f) : 1f;
        /// 피버 제안은 피버 레슨 때만.
        public static bool HoldFever => Active && !_feverLesson;
        /// 204차(사용자: 튜토리얼 문제): 에너지 설명 전까지는 달리기만으로 에너지가 줄지 않는다.
        public static bool HoldDrain => Active && !_energyTaught;

        /// ArcadeRun.StartKpop 머리에서: 이번 판을 튜토리얼로 돌릴지.
        public static bool ShouldRun()
        {
            if (SuppressOnce) { SuppressOnce = false; Pending = false; return false; }
            return Pending || !Done;
        }

        public static void Begin()
        {
            Pending = false;
            if (_inst != null) Destroy(_inst.gameObject);
            var go = new GameObject("KpopTutorial");
            DontDestroyOnLoad(go);
            _inst = go.AddComponent<KpopTutorial>();
            Active = true; _slow = false; _feverLesson = false; _energyTaught = false;
            Debug.LogWarning("[KpopTutorial] begin");
        }

        /// 재도전(스테이지 다시 시작) — 대본을 처음부터.
        public static void OnStageBegin()
        {
            if (_inst == null) return;
            _inst.StopAllCoroutines();
            Active = true; _slow = false; _feverLesson = false; _energyTaught = false;
            _inst._running = true;
            _inst.StartCoroutine(_inst.Run());
        }

        bool _running;
        void Start() { if (!_running) { _running = true; StartCoroutine(Run()); } }

        void OnDestroy()
        {
            if (_inst == this) { _inst = null; Active = false; _slow = false; _feverLesson = false; _energyTaught = false; }
        }

        void Update()
        {
            // 런을 빠져나가면(메인으로·결과) 끝.
            if (!ArcadeRun.KpopMode) Destroy(gameObject);
        }

        PlayerController _p;
        float Z => _p != null ? _p.PathDistance : 0f;
        int Lane => _p != null ? _p.Lane : 0;

        IEnumerator Run()
        {
            _p = null;
            float guard = 0f;
            while ((_p == null || _p.Speed < 0.5f) && guard < 30f)
            {
                if (_p == null) _p = FindAnyObjectByType<PlayerController>();
                guard += Time.unscaledDeltaTime;
                yield return null;
            }
            if (_p == null) { Destroy(gameObject); yield break; }
            yield return Wait(0.8f);

            // 0) 조작
            yield return Say("Obs_Jelly_Strawberry", Loc.T("튜토리얼 · 달리는 법", "Tutorial · How to run"),
                Loc.T("화면을 왼쪽·오른쪽으로 밀면 줄을 바꿔요.\n위로 밀면 점프, 아래로 밀면 숙이기!", "Swipe left/right to change lanes.\nSwipe up to jump, down to slide!"), 5.5f);

            // 1) 말랑이 젤리
            float jz = Z + 24f; int jl = Lane;
            for (int i = 0; i < 8; i++) JellySpawner.Instance?.SpawnGlideReward(PickupKind.Jelly, jz + i * 1.6f, jl, 0.5f);
            yield return Lesson(jz + 11f, "Obs_Jelly_Strawberry", Loc.T("말랑이 젤리 = 점수", "Jelly = score"),
                Loc.T("쭉 달리면서 먹어요. 모은 젤리와 돈은\n스토리 모드(마을)로 그대로 가져가요!", "Eat them as you run. Jelly and money\ncarry over to Story mode!"));

            // 2) 장애물
            float oz = Z + 26f;
            SpawnObstacle(ObstacleId.TrafficCone, oz, Lane);
            yield return Lesson(oz + 1.5f, "Obs_Cone", Loc.T("이건 장애물! (콘)", "Obstacle! (cone)"),
                Loc.T("부딪히면 에너지가 깎여요.\n옆 줄로 피하거나 위로 밀어 점프!", "Hitting it costs energy.\nDodge sideways or swipe up to jump!"));
            float bz = Z + 28f;
            SpawnObstacle(ObstacleId.OverheadBar, bz, Lane);
            yield return Lesson(bz + 1.5f, "Obs_OverheadBar", Loc.T("허들은 숙이기", "Bar: slide under"),
                Loc.T("머리 높이 막대는 아래로 밀어서 숙이거나\n옆 줄로 피해요.", "Swipe down to slide under,\nor change lanes."));
            float cz = Z + 30f; int free = Lane == 1 ? 0 : Lane + 1;
            for (int l = -1; l <= 1; l++) if (l != free) SpawnObstacle(ObstacleId.Barrier, cz, l);
            yield return Lesson(cz + 1.5f, "Obs_Barrier", Loc.T("빈 줄을 찾아요", "Find the open lane"),
                Loc.T("장애물이 두 줄을 막으면\n비어 있는 줄로 옮겨 가요.", "When two lanes are blocked,\nmove to the open one."));

            // 3) 에너지 + 하트
            var hs = HealthSystem.Instance;
            if (hs != null && hs.Normalized > 0.6f) hs.SetFraction(0.55f);
            _energyTaught = true;   // 차오르는 걸 보여 주려고 살짝 비워 둔다
            yield return Say("Obs_Heart", Loc.T("에너지(체력)", "Energy"),
                Loc.T("왼쪽 위 게이지가 에너지예요.\n달리는 동안 조금씩 줄고, 0이 되면 끝!", "The top-left gauge is your energy.\nIt drains as you run — 0 ends the run!"), 4.5f);
            float hz = Z + 24f;
            JellySpawner.Instance?.SpawnGlideReward(PickupKind.Heart, hz, Lane, 0.45f);
            yield return Lesson(hz + 1f, "Obs_Heart", Loc.T("하트 = 에너지 +15", "Heart = energy +15"),
                Loc.T("하트를 먹으면 에너지가 차올라요!", "Hearts refill your energy!"));
            float pz = Z + 24f;
            JellySpawner.Instance?.SpawnGlideReward(PickupKind.Potion, pz, Lane, 0.4f);
            yield return Lesson(pz + 1f, "Obs_Potion", Loc.T("물약 = 에너지 +30", "Potion = energy +30"),
                Loc.T("물약은 하트보다 두 배 많이 채워 줘요.", "Potions refill twice as much."));

            // 4) 거대화
            float gz = Z + 24f;
            JellySpawner.Instance?.SpawnGlideReward(PickupKind.Giant, gz, Lane, 0.45f);
            yield return Lesson(gz + 1f, "Obs_Star", Loc.T("주황 별 = 거대화!", "Orange star = GIANT!"),
                Loc.T("먹으면 10초 동안 두 배로 커져요.\n커진 동안엔 장애물을 부수고 지나가요!", "You grow 2× for 10 seconds\nand smash through obstacles!"));
            if (GiantMode.Active)
            {
                float sz = Z + 22f;
                for (int i = 0; i < 3; i++) SpawnObstacle(ObstacleId.TrafficCone, sz + i * 7f, Lane);
                PickupFloat.Banner(Loc.T("부수고 가요!", "Smash!"), new Color(1f, 0.6f, 0.2f), 2f);
                float t = 0f;
                while (GiantMode.Active && t < 12f) { t += Time.deltaTime; yield return null; }
            }
            yield return Wait(1f);

            // 5) 피버
            Debug.LogWarning($"[KpopTutorial] fever offer z={Z:F0}");
            _feverLesson = true;
            FeverMode.Ensure().ForceOffer();
            _slow = true;
            PickupFloat.InfoStrip("UI_Face_Butler", Loc.T("꼬마 얼굴 = 피버 타임!", "Face button = FEVER!"),
                Loc.T("오른쪽에 꼬마 얼굴이 뜨면 톡 눌러요.\n6초 동안 무적 + 젤리가 쏙쏙 빨려 와요!", "Tap the face on the right.\n6 seconds of invincibility + jelly magnet!"), new Color(1f, 0.85f, 0.35f), 7f);
            float ft = 0f;
            while (!FeverMode.Active && ft < 14f)
            {
                ft += Time.deltaTime;
                if (ft > 7.2f && ft < 7.3f) FeverMode.Ensure().ForceOffer();   // 놓쳤으면 한 번 더
                yield return null;
            }
            if (!FeverMode.Active)   // 끝까지 못 눌렀으면 한 번 보여 준다
            {
                FeverMode.Ensure().Trigger("tutorial");
                PickupFloat.Banner(Loc.T("피버 타임!", "FEVER!"), new Color(1f, 0.5f, 0.8f), 2f);
            }
            _slow = false;
            Debug.LogWarning($"[KpopTutorial] fever lesson active={FeverMode.Active} t={ft:F1}");
            while (FeverMode.Active) yield return null;
            _feverLesson = false;
            yield return Wait(1f);

            // 6) 보너스 별
            float stz = Z + 24f;
            JellySpawner.Instance?.SpawnGlideReward(PickupKind.BonusStar, stz, Lane, 0.5f);
            yield return Lesson(stz + 1f, "Obs_Star", Loc.T("노란 별 = 보너스 타임", "Yellow star = Bonus Time"),
                Loc.T("장애물이 사라지고 큰 젤리 카펫이 깔려요.\n마음껏 먹어요!", "Obstacles vanish and big jellies\ncover the road. Feast!"));
            float bt = 0f;
            while (BonusTimeDirector.IsActive && bt < 15f) { bt += Time.deltaTime; yield return null; }
            yield return Wait(0.6f);

            // 끝
            MarkDone();
            PickupFloat.Banner(Loc.T("튜토리얼 끝!", "Tutorial done!"), new Color(1f, 0.55f, 0.75f), 2.2f);
            PickupFloat.InfoStrip("Obs_Star", Loc.T("이제 진짜 달려 봐요 ♪", "Now run for real ♪"),
                Loc.T("곡이 끝날 때까지 달리면 챕터 클리어!\n튜토리얼은 챕터 화면에서 다시 볼 수 있어요.", "Run to the end of the song to clear!\nReplay the tutorial from the chapter screen."), new Color(1f, 0.85f, 0.35f), 5f);
            // 204차: 튜토리얼에서 쓴 에너지는 채워 주고 진짜 달리기를 시작한다.
            if (HealthSystem.Instance != null) HealthSystem.Instance.SetFraction(1f);
            Debug.LogWarning($"[KpopTutorial] done hp={(HealthSystem.Instance != null ? HealthSystem.Instance.Normalized : -1f):F2}");
            Active = false; _slow = false;
            Destroy(gameObject, 0.1f);
        }

        IEnumerator Wait(float s) { float t = 0f; while (t < s) { t += Time.deltaTime; yield return null; } }

        /// 아이템 없이 설명만(느리게).
        IEnumerator Say(string icon, string title, string body, float seconds)
        {
            Debug.LogWarning($"[KpopTutorial] say {title} z={Z:F0}");
            _slow = true;
            PickupFloat.InfoStrip(icon, title, body, new Color(1f, 0.85f, 0.35f), seconds);
            yield return new WaitForSecondsRealtime(seconds);
            _slow = false;
        }

        /// 설명 띄우고, 주인공이 passZ 를 지날 때까지 느리게. 그다음 잠깐 쉬었다가 다음 레슨.
        IEnumerator Lesson(float passZ, string icon, string title, string body)
        {
            Debug.LogWarning($"[KpopTutorial] lesson {title} z={Z:F0} hp={(HealthSystem.Instance != null ? HealthSystem.Instance.Normalized : -1f):F2}");
            _slow = true;
            PickupFloat.InfoStrip(icon, title, body, new Color(1f, 0.85f, 0.35f), 6.5f);
            float t = 0f;
            while (_p != null && Z < passZ && t < 16f) { t += Time.deltaTime; yield return null; }
            _slow = false;
            yield return Wait(1.4f);
        }

        void SpawnObstacle(ObstacleId id, float z, int lane)
        {
            var sp = FindAnyObjectByType<ObstacleSpawner>();
            Transform root = sp != null ? sp.Root : null;
            const float lw = 2.2f;
            var go = ObstacleCatalog.Spawn(id, root, RoadPlacement.OnRoad(z, lane * lw), lane);
            if (go != null) RoadPlacement.Snap(go, z, lane * lw);
            JellySpawner.Instance?.RemoveNear(z, lane, 2.5f);
        }
    }
}
