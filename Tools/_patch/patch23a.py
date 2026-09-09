# 23차 patch A: JuiceDirector(꽈당·펫 120%·콘페티), PickupFloat(Impact), RunnerCameraRig(고정 카메라), PlayerController(FeetPosition), SkaterRig(손목)
import re, sys, io
ROOT = 'Assets/_CoastRun/Scripts/'
def rw(path, pairs):
    p = ROOT + path
    s = io.open(p, encoding='utf-8').read()
    for old, new in pairs:
        if old not in s:
            print('MISSING in', path, ':', old[:70]); sys.exit(1)
        s = s.replace(old, new, 1)
    io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
    print('ok', path)

# ── JuiceDirector ────────────────────────────────────────────────
rw('Visual/JuiceDirector.cs', [
('''            cameraRig?.Shake(0.25f, 0.3f);
            PunchSaturation(-40f, 0.4f);
            player?.FreezeInput(0.3f);
            cameraRig?.FovKick(-6f, 0.2f);
            // ★ BGM never stops — SFX only.
            audio?.PlaySfx(CoastSfx.SoftHit);''',
'''            // 23차-2: '꽈당' — 공격당했다는 불쾌한 충격이 화면에 와야 피하고 싶어진다.
            // 순간 정지(0.10 s) → 큰 흔들림 → 화면이 기울며 붉게 번쩍 + "꽈당!" + 채도 뚝.
            if (_hitStopRoutine != null) StopCoroutine(_hitStopRoutine);
            _hitStopRoutine = StartCoroutine(HitStop(0.03f, 0.10f));
            cameraRig?.Shake(0.55f, 0.38f);
            PunchSaturation(-70f, 0.55f);
            player?.FreezeInput(0.3f);
            cameraRig?.FovKick(-9f, 0.28f);
            PickupFloat.Impact(Loc.T("꽈당!", "OUCH!"));
            // ★ BGM never stops — SFX only.
            audio?.PlaySfx(CoastSfx.SoftHit);'''),
('''            if (amount > 0)
            {
                PickupFloat.Text(popPos, "+" + amount, tint, amount >= 2 ? 1.25f : 1f);
                if (tint == CoastPalette.CoinYellow) PickupFloat.FlyCoin(popPos, amount >= 2 ? 3 : 1);
            }''',
'''            if (amount > 0)
            {
                PickupFloat.Text(popPos, "+" + amount, tint, amount >= 2 ? 1.25f : 1f);
                if (tint == CoastPalette.CoinYellow) PickupFloat.FlyCoin(popPos, amount >= 2 ? 3 : 1);
                // 23차-4: 펫 코인 보너스가 눈에 보이게 — 배율이 있으면 1.1초에 한 번 "120%"가 함께 떠오른다(펫 색).
                if (tint == CoastPalette.CoinYellow && PetCompanion.CoinBonus > 1.001f && now - _lastPetTagTime > 1.1f)
                {
                    _lastPetTagTime = now;
                    PickupFloat.Text(popPos + Vector3.up * 0.55f, Mathf.RoundToInt(PetCompanion.CoinBonus * 100f) + "%", new Color(0.55f, 0.95f, 1f), 0.8f);
                }
            }'''),
('''        private int _pickupStreak; private float _lastPickupTime = -10f; private Coroutine _bodySquash;''',
'''        private int _pickupStreak; private float _lastPickupTime = -10f; private float _lastPetTagTime = -10f; private Coroutine _bodySquash;

        /// 23차-3: 골인 콘페티 — 리본이 끊기는 순간 게이트 위에서 색종이가 쏟아진다(별·하트 파티클 재사용).
        public void OnFinishConfetti(Vector3 top, float halfWidth)
        {
            EnsurePopBursts();
            Color[] cols = { new Color(1f, 0.35f, 0.45f), new Color(1f, 0.85f, 0.3f), new Color(0.45f, 0.75f, 1f), new Color(0.6f, 0.9f, 0.5f), Color.white };
            for (int i = 0; i < 7; i++)
            {
                float x = Mathf.Lerp(-halfWidth, halfWidth, (i + 0.5f) / 7f);
                var p = top + Vector3.right * x;
                SpawnPop(i % 2 == 0 ? _popStar : _popHeart, p, cols[i % cols.Length], 8);
            }
            cameraRig?.Shake(0.12f, 0.2f);
            audio?.PlaySfx(CoastSfx.NearMiss);
        }'''),
])

# ── PickupFloat: Impact ─────────────────────────────────────────
rw('Visual/PickupFloat.cs', [
('''        public static void Text(Vector3 world, string text, Color color, float size = 1f)''',
'''        // 23차-2: 꽈당 — 붉은 비네트 플래시 + 화면 기울기 + 큰 글자
        private Image _vignette, _flash; private Text _slam;
        public static void Impact(string word)
        {
            var f = Ensure();
            f.StartCoroutine(f.ImpactSeq(word));
        }

        private IEnumerator ImpactSeq(string word)
        {
            if (_vignette == null)
            {
                _vignette = MakeFull("HitVignette", VignetteTex());
                _flash = MakeFull("HitFlash", null);
                _slam = CoastHudLayout.MakeText(_root, "Slam", "", 120, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-400f, -120f), new Vector2(400f, 120f));
                _slam.fontStyle = FontStyle.Bold; _slam.raycastTarget = false;
                CoastUiArt.OutlineText(_slam, new Color(0.35f, 0.02f, 0.05f, 1f), 5f);
            }
            _vignette.gameObject.SetActive(true); _flash.gameObject.SetActive(true); _slam.gameObject.SetActive(true);
            _slam.text = word;
            var srt = _slam.rectTransform;
            float t = 0f; const float dur = 0.55f;
            Quaternion r0 = _root.localRotation; Vector2 p0 = _root.anchoredPosition;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / dur);
                // 흰 번쩍(0.06 s) → 붉은 비네트가 남았다가 사라진다
                _flash.color = new Color(1f, 0.95f, 0.9f, Mathf.Clamp01(1f - t / 0.06f) * 0.7f);
                _vignette.color = new Color(0.9f, 0.05f, 0.08f, (1f - u) * (1f - u) * 0.85f);
                // 화면 전체가 기우뚱(감쇠 진동)
                float wob = Mathf.Sin(t * 42f) * (1f - u) * (1f - u) * 5f;
                _root.localRotation = Quaternion.Euler(0f, 0f, wob);
                _root.anchoredPosition = p0 + new Vector2(Mathf.Sin(t * 60f) * 22f, Mathf.Cos(t * 50f) * 14f) * (1f - u) * (1f - u);
                // 글자: 크게 튀어나왔다가 자리 잡고 흐려진다
                float pop = u < 0.12f ? Mathf.Lerp(2.2f, 0.95f, u / 0.12f) : Mathf.Lerp(0.95f, 1.05f, (u - 0.12f) / 0.88f);
                srt.localScale = Vector3.one * pop;
                srt.localRotation = Quaternion.Euler(0f, 0f, -9f + wob * 0.5f);
                srt.anchoredPosition = new Vector2(0f, 140f + 40f * u);
                _slam.color = new Color(1f, 0.92f, 0.3f, u < 0.65f ? 1f : 1f - (u - 0.65f) / 0.35f);
                yield return null;
            }
            _root.localRotation = r0; _root.anchoredPosition = p0;
            _vignette.gameObject.SetActive(false); _flash.gameObject.SetActive(false); _slam.gameObject.SetActive(false);
        }

        private Image MakeFull(string name, Texture2D tex)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root, false);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            if (tex != null) img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(-80f, -80f); rt.offsetMax = new Vector2(80f, 80f);
            img.color = new Color(1f, 1f, 1f, 0f);
            go.SetActive(false);
            return img;
        }

        private static Texture2D _vig;
        private static Texture2D VignetteTex()
        {
            if (_vig != null) return _vig;
            const int N = 128; _vig = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float px = (x + 0.5f) / N * 2f - 1f, py = (y + 0.5f) / N * 2f - 1f;
                float r = Mathf.Sqrt(px * px * 0.8f + py * py * 0.55f);
                float a = Mathf.Clamp01((r - 0.35f) / 0.6f); a = a * a;
                _vig.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            _vig.Apply(); return _vig;
        }

        public static void Text(Vector3 world, string text, Color color, float size = 1f)'''),
])

# ── RunnerCameraRig: 고정 '사진사' 카메라 ────────────────────────
rw('Camera/RunnerCameraRig.cs', [
('''            Vector3 p0 = transform.position; Quaternion r0 = transform.rotation;
            float t = 0f;
            while (_finishFrame)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / swing));
                Quaternion frame = target.PathRotation;
                Vector3 pp = target.transform.position;
                Vector3 pos = pp + frame * new Vector3(0.35f, 1.55f, -3.6f);
                Quaternion rot = Quaternion.LookRotation((pp + Vector3.up * 1.05f - pos).normalized, Vector3.up);
                transform.position = Vector3.Lerp(p0, pos, u);
                transform.rotation = Quaternion.Slerp(r0, rot, u);
                if (u >= 1f) { p0 = pos; r0 = rot; }
                yield return null;
            }''',
'''            Vector3 p0 = transform.position; Quaternion r0 = transform.rotation;
            float t = 0f;
            // 23차-1: 카메라가 위로 붕 떠서 '하늘로 올라가는' 느낌이 났다 → 결승선 옆에 선 사진사처럼 **눈높이(발 기준 1.15 m)에 고정**.
            // 위치는 리본 통과 시점의 자리에서 살짝 뒤·옆으로 잡고 그 뒤로는 움직이지 않는다. 주인공만 몇 걸음 더 가서 돌아선다.
            Quaternion frame = target.PathRotation;
            Vector3 feet0 = target.FeetPosition;
            Vector3 anchor = feet0 + frame * new Vector3(0.55f, 1.15f, -2.2f);
            while (_finishFrame)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / swing));
                Vector3 feet = target.FeetPosition;
                Vector3 pos = anchor;
                Quaternion rot = Quaternion.LookRotation((feet + Vector3.up * 1.0f - pos).normalized, Vector3.up);
                transform.position = Vector3.Lerp(p0, pos, u);
                transform.rotation = Quaternion.Slerp(r0, rot, u);
                if (u >= 1f) { p0 = pos; r0 = rot; }
                yield return null;
            }'''),
])

# ── PlayerController: FeetPosition ─────────────────────────────
rw('Player/PlayerController.cs', [
('''        public void FinishRun()
        {
            _state = SkateState.Finish;
            _gliding = false; _hop = 0f; _laneT = 1f; _laneFrom = _lane * config.laneOffset;
        }''',
'''        public void FinishRun()
        {
            _state = SkateState.Finish;
            _gliding = false; _laneT = 1f; _laneFrom = _lane * config.laneOffset;
            _hop = _groundY + _bodyHeight * 0.5f;   // 23차-1: 0으로 두면 한 프레임 땅 밑으로 꺼졌다 올라온다
            _verticalVelocity = 0f;
        }

        /// 23차-1: 발 위치(월드) — transform은 몸 중심(캡슐)이라 카메라 기준으로는 이게 편하다.
        public Vector3 FeetPosition => transform.position - DownhillPath.Normal * (_bodyHeight * 0.5f);'''),
])

# ── SkaterRig: 손목 정리 + 왼손 옆구리 ──────────────────────────
rw('Player/SkaterRig.cs', [
('''            // 오른손: 위로 쭉(살짝 바깥·앞)
            Aim(rUp, rLo, (up + right * 0.35f + face * 0.10f), k);
            Aim(rLo, rH, (up + right * 0.15f + face * 0.05f), k);
            // 왼손: 위팔은 아래·바깥, 아래팔은 허리 쪽으로 꺾어 손이 옆구리에
            Aim(lUp, lLo, (-up * 0.75f - right * 0.85f + face * 0.10f), k);   // 팔꿈치 바깥으로
            Aim(lLo, lH, (right * 1.0f + up * 0.30f + face * 0.30f), k);      // 아래팔은 허리로 꺾어 손을 옆구리에''',
'''            // 오른손: 위로 쭉(살짝 바깥·앞) — 23차-1: 손목까지 같은 방향으로 펴서 꺾인 손이 없게, 손바닥은 앞
            Aim(rUp, rLo, (up + right * 0.30f + face * 0.08f), k);
            Aim(rLo, rH, (up + right * 0.22f + face * 0.04f), k);
            var rMid = _anim.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
            if (rMid != null) Aim(rH, rMid, (up + right * 0.30f - face * 0.05f), k);
            // 왼손: 위팔은 아래·바깥, 아래팔은 허리 쪽으로 꺾어 손이 옆구리에. 손목은 손등이 바깥을 보게 몸 쪽으로.
            Aim(lUp, lLo, (-up * 0.80f - right * 0.75f + face * 0.05f), k);   // 팔꿈치 바깥으로
            Aim(lLo, lH, (right * 1.0f + up * 0.22f + face * 0.35f), k);      // 아래팔은 허리로 꺾어 손을 옆구리에
            var lMid = _anim.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);
            if (lMid != null) Aim(lH, lMid, (right * 0.6f - up * 0.35f - face * 0.7f), k);   // 손가락은 등 쪽으로 감싸듯'''),
])
print('ALL OK')
