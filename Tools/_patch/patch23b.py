# 23차 patch B: 리본 통과 시 코인·장애물 정리(StageManager/Spawners), 남은 km 강조 + 얼굴 컷인(StageClearUI), 결승 근처 장애물 금지
import io, sys
ROOT = 'Assets/_CoastRun/Scripts/'
def rw(path, pairs):
    p = ROOT + path
    s = io.open(p, encoding='utf-8').read()
    for old, new in pairs:
        if old not in s:
            print('MISSING in', path, ':', old[:80]); sys.exit(1)
        s = s.replace(old, new, 1)
    io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
    print('ok', path)

# ── StageManager ────────────────────────────────────────────────
rw('Core/StageManager.cs', [
('''        private System.Collections.IEnumerator FinishThenClear(StageDef cleared, bool chapterEnd)
        {
            _ribbon?.Break();
            player?.FinishRun();''',
'''        /// 23차-3: 결승선 앞뒤 이 구간엔 장애물 행을 놓지 않는다(리본이 가려지지 않게).
        public float FinishPathZ => _current != null && _stageActive ? _stageOriginDistance + _current.targetDistance : float.PositiveInfinity;

        private System.Collections.IEnumerator FinishThenClear(StageDef cleared, bool chapterEnd)
        {
            _ribbon?.Break();
            player?.FinishRun();
            // 23차-3: 리본을 지나는 순간 앞에 남은 코인·말랑이·장애물을 싹 치운다 — 무대는 관중과 주인공만.
            float sweepZ = player != null ? player.PathDistance - 1f : 0f;
            foreach (var o in FindObjectsByType<ObstacleSpawner>(FindObjectsSortMode.None)) o.SetSuppressed(true);
            foreach (var c in FindObjectsByType<CoinSpawner>(FindObjectsSortMode.None)) c.ClearAhead(sweepZ);
            foreach (var j in FindObjectsByType<JellySpawner>(FindObjectsSortMode.None)) j.ClearAhead(sweepZ);'''),
('''        private void ResetFinishPresentation()
        {
            _finishRig?.SetFinishPose(false);
            _finishCam?.EndFinishFrame();''',
'''        private void ResetFinishPresentation()
        {
            _finishRig?.SetFinishPose(false);
            _finishCam?.EndFinishFrame();
            foreach (var o in FindObjectsByType<ObstacleSpawner>(FindObjectsSortMode.None)) o.SetSuppressed(false);'''),
])

# ── CoinSpawner / JellySpawner: ClearAhead ──────────────────────
rw('Economy/CoinSpawner.cs', [
('''        private Transform _root;''',
'''        private Transform _root;

        /// 23차-3: 골인 뒤 앞쪽 코인을 전부 거둔다(풀로).
        public void ClearAhead(float z)
        {
            if (_root == null) return;
            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                var c = _root.GetChild(i);
                if (!c.gameObject.activeSelf || DownhillPath.DistanceAlong(c.position) < z) continue;
                var cp = c.GetComponent<CoinPickup>();
                if (cp != null) cp.Recycle(); else Destroy(c.gameObject);
            }
        }'''),
])
rw('Economy/JellySpawner.cs', [
('''        public void ClearAll()
        {''',
'''        /// 23차-3: 골인 뒤 앞쪽 말랑이·물약·별을 전부 치운다.
        public void ClearAhead(float z)
        {
            if (_root == null) return;
            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                var c = _root.GetChild(i);
                if (DownhillPath.DistanceAlong(c.position) >= z) Destroy(c.gameObject);
            }
        }

        public void ClearAll()
        {'''),
])

# ── ObstacleSpawner: 결승 근처 행 금지 + 차 제거 ─────────────────
rw('Economy/ObstacleSpawner.cs', [
('''            _suppressed = on;
            if (!on || _root == null || player == null)
                return;
            float z = player.PathDistance;''',
'''            _suppressed = on;
            if (!on || _root == null || player == null)
                return;
            if (_car != null) { Destroy(_car.gameObject); _car = null; }   // 23차-3: 마주 오던 차도 치운다
            float z = player.PathDistance;'''),
])
s = io.open(ROOT + 'Economy/ObstacleSpawner.cs', encoding='utf-8').read()
import re
m = re.search(r'\n(\s*)private void Update\(\)\s*\n\s*\{\n', s)
if not m: print('no Update'); sys.exit(1)
ins = m.group(1) + '''    // 23차-3: 결승선 앞 14 m ~ 뒤 60 m 구간엔 행을 놓지 않는다(리본·관중이 보이게).
''' + m.group(1) + '''    if (StageManager.Instance != null && _nextSpawnZ > StageManager.Instance.FinishPathZ - 14f && _nextSpawnZ < StageManager.Instance.FinishPathZ + 60f)
''' + m.group(1) + '''    { _nextSpawnZ = StageManager.Instance.FinishPathZ + 60f; }
'''
s = s[:m.end()] + ins + s[m.end():]
io.open(ROOT + 'Economy/ObstacleSpawner.cs', 'w', encoding='utf-8', newline='\n').write(s)
print('ok ObstacleSpawner Update guard')

# ── StageClearUI: 남은 km 강조 + 얼굴 컷인 ──────────────────────
rw('UI/StageClearUI.cs', [
('''            float remainingKm = stages.RemainingJourneyDistance / 1000f;
            _journey.text =
                Loc.T($"송전탑까지 {remainingKm:0.0} km   ·   {ClockAt(stage.lightingTEnd)}", $"{remainingKm:0.0} km to the tower   ·   {ClockAt(stage.lightingTEnd)}") +
                $"   ·   {StageRunStats.FormatTime(seconds)}";''',
'''            float remainingKm = stages.RemainingJourneyDistance / 1000f;
            // 23차-5: 남은 거리는 한 줄로 크게 — 시계·기록은 아래 작은 줄로.
            _journey.text = Loc.T($"송전탑까지  {remainingKm:0.0} km", $"{remainingKm:0.0} km to the tower");
            if (_journeySub != null) _journeySub.text = $"{ClockAt(stage.lightingTEnd)}   ·   {StageRunStats.FormatTime(seconds)}";
            if (_journeyPill != null) StartCoroutine(SimpleTween.PunchScale(_journeyPill.transform, 0.12f, 0.25f));'''),
('''            _journey = FootLabel(host, "Journey", 14, -142f, 24f);
            _journey.color = new Color(1f, 1f, 1f, 0.85f);''',
'''            // 23차-5: "송전탑까지 N km"가 버튼에 가려 안 보였다 → 주황 알약 안에 22px 굵게, 그 아래 시계·기록.
            _journeyPill = CoastUiArt.CutePill(host, "JourneyPill", new Color(1f, 0.55f, 0.28f, 1f), 16, 3);
            var jrt = _journeyPill.rectTransform; jrt.anchorMin = new Vector2(0.5f, 1f); jrt.anchorMax = new Vector2(0.5f, 1f); jrt.pivot = new Vector2(0.5f, 1f);
            jrt.anchoredPosition = new Vector2(0f, -140f); jrt.sizeDelta = new Vector2(420f, 44f); _journeyPill.raycastTarget = false;
            var tower = new GameObject("Tower", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            tower.transform.SetParent(_journeyPill.transform, false); tower.raycastTarget = false;
            var tex = CoastUiArt.TowerIcon; if (tex != null) tower.sprite = CoastUiArt.AsSprite(tex);
            tower.preserveAspect = true; tower.color = Color.white;
            var trt2 = tower.rectTransform; trt2.anchorMin = trt2.anchorMax = new Vector2(0f, 0.5f); trt2.pivot = new Vector2(0f, 0.5f);
            trt2.anchoredPosition = new Vector2(10f, 0f); trt2.sizeDelta = new Vector2(32f, 32f);
            _journey = CoastHudLayout.MakeText(_journeyPill.rectTransform, "Journey", "", 22, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(40f, 0f), new Vector2(-8f, 0f));
            _journey.color = Color.white; _journey.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(_journey, new Color(0.35f, 0.12f, 0.02f, 0.8f), 2f);
            _journeySub = FootLabel(host, "JourneySub", 13, -188f, 20f);
            _journeySub.color = new Color(1f, 1f, 1f, 0.75f);'''),
('''        private RectTransform _card; private Image _gradeBadge; private Text _gradeText;''',
'''        private RectTransform _card; private Image _gradeBadge; private Text _gradeText;
        private Image _journeyPill; private Text _journeySub; private Image _face; private Text _faceBubble;'''),
('''            frt.anchoredPosition = new Vector2(0f, 14f); frt.sizeDelta = new Vector2(-24f, 300f); foot.raycastTarget = false;''',
'''            frt.anchoredPosition = new Vector2(0f, 14f); frt.sizeDelta = new Vector2(-24f, 350f); foot.raycastTarget = false;'''),
('''            _gradeBadge = CoastUiArt.Panel(_banner, "Grade", new Color(1f, 0.80f, 0.25f), 40);''',
'''            // 23차-1: 뒤돌아 포즈 잡을 때 메인페이지의 그 얼굴 — 배너 왼쪽에 동그란 컷인 + 말풍선
            var faceTex = ArtAssets.LoadTexture("UI_Face_Girl");
            if (faceTex != null)
            {
                _face = new GameObject("Face", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                _face.transform.SetParent(_banner, false); _face.raycastTarget = false;
                _face.sprite = CoastUiArt.AsSprite(faceTex); _face.preserveAspect = true;
                var frt0 = _face.rectTransform; frt0.anchorMin = frt0.anchorMax = new Vector2(1f, 0f); frt0.pivot = new Vector2(1f, 1f);
                frt0.anchoredPosition = new Vector2(-10f, -6f); frt0.sizeDelta = new Vector2(170f, 170f);
                var bub = CoastUiArt.CutePill(_banner, "FaceBubble", Color.white, 18, 3);
                var brt2 = bub.rectTransform; brt2.anchorMin = brt2.anchorMax = new Vector2(1f, 0f); brt2.pivot = new Vector2(1f, 1f);
                brt2.anchoredPosition = new Vector2(-186f, -30f); brt2.sizeDelta = new Vector2(190f, 58f); bub.raycastTarget = false;
                _faceBubble = CoastHudLayout.MakeText(brt2, "T", "", 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
                _faceBubble.color = new Color(0.25f, 0.12f, 0.2f); _faceBubble.fontStyle = FontStyle.Bold;
            }
            _gradeBadge = CoastUiArt.Panel(_banner, "Grade", new Color(1f, 0.80f, 0.25f), 40);'''),
('''            // 제목이 '쾅' 들어온다
            yield return PunchIn(_banner, 0.35f);''',
'''            if (_faceBubble != null)
            {
                string[] lines = { Loc.T("해냈다!", "Did it!"), Loc.T("봤지? 나 좀 빨라", "See? I'm fast"), Loc.T("휴, 다 왔다", "Phew, made it"), Loc.T("송전탑 조금만 더!", "Tower, almost!") };
                _faceBubble.text = lines[UnityEngine.Random.Range(0, lines.Length)];
            }
            // 제목이 '쾅' 들어온다
            yield return PunchIn(_banner, 0.35f);'''),
])
print('ALL OK')
