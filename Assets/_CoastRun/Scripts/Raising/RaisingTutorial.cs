using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 31차: 육성 화면 튜토리얼 — 꼬마 집사(도담)가 메뉴를 하나씩 하이라이트하며 소개한다.
    /// 화면 위에 어두운 막(4장)으로 대상만 밝게 남기고, 대상 둘레에 맥동하는 테두리, 집사 흉상 + 말풍선 + [다음]/[건너뛰기].
    /// 처음 육성 화면에 들어왔을 때 1회(PlayerPrefs CoastRun_RaisingTut). 에디터 F1 로 다시 볼 수 있다.
    public class RaisingTutorial : MonoBehaviour
    {
        public const string PrefKey = "CoastRun_RaisingTut";

        public class Step { public string target; public string[] targets; public string ko, en; public bool butlerTop; }

        private RectTransform _uiRoot, _root;
        private Canvas _canvas;
        private List<Step> _steps;
        private int _i = -1;
        private Action _onDone;
        private readonly Image[] _dim = new Image[4];
        private Image _ring, _ring2;
        private RectTransform _butler, _bubble;
        private Text _text, _count;
        private float _t;

        public static RaisingTutorial Open(RectTransform uiRoot, List<Step> steps, Action onDone)
        {
            var go = new GameObject("RaisingTutorial");
            var t = go.AddComponent<RaisingTutorial>();
            t._uiRoot = uiRoot; t._steps = steps; t._onDone = onDone;
            t.Build();
            t.Next();
            return t;
        }

        public static List<Step> DefaultSteps()
        {
            return new List<Step>
            {
                new Step { target = null, ko = "아가씨, 처음이시죠? 집사 도담입니다.\n이 방에서 뭘 할 수 있는지 하나씩 보여 드릴게요.", en = "Miss, first time here? I'm Dodam, your butler.\nLet me show you around." },
                new Step { target = "Date", ko = "여기가 지금 <b>주차와 계절</b>이에요. 탭하면 챕터 달력이 열려요.\n챕터가 끝나는 주까지 체력을 키워야 러닝을 갈 수 있어요.", en = "This is the <b>week & season</b>. Tap to open the chapter calendar.\nRaise stamina before the chapter's last week to unlock the run." },
                new Step { target = "Money", ko = "<b>G</b>는 상점·씨앗·가구에 써요.\n알바와 러닝, 베란다 꽃 판매로 벌 수 있어요.", en = "<b>G</b> buys pets, seeds and furniture.\nEarn it from jobs, runs and selling flowers." },
                new Step { target = "Level", ko = "지금 <b>챕터</b>예요. 챕터의 마지막 주에 러닝이 열리고,\n체력이 모자라면 한 주가 더 늘어나요.", en = "The current <b>chapter</b>. The run opens on its last week;\nnot enough stamina means one more week." },
                new Step { target = "Cond", ko = "<b>컨디션</b>이에요. 스트레스가 체력을 넘으면 부상!\n휴식 카드로 회복하세요.", en = "<b>Condition</b>. Stress above stamina means injury —\nrest to recover." },
                new Step { target = "HeartsPlate", ko = "<b>말랑이 하트</b>. 러닝에서 젤리를 모으면 차고,\n많이 모을수록 챕터 등급이 올라가요.", en = "<b>Hearts</b> fill as you collect jellies on runs —\nmore hearts, better chapter grade." },
                new Step { target = "ArcadeBtnSlot", ko = "<b>달리기</b>: 챕터와 상관없이 무한 러닝·오늘의 런을 바로 달려요.\n기록과 도장을 모으세요.", en = "<b>Run</b>: endless and daily runs anytime, independent of the story." },
                new Step { target = "TimelineBtnSlot", ko = "<b>챕터</b>: 지나온 챕터를 다시 보고, 남은 주차를 확인해요.", en = "<b>Chapters</b>: revisit past chapters and check weeks left." },
                new Step { target = "CollBtnSlot", ko = "<b>컬렉션</b>: 포토카드·앨범·업적이 모여요.", en = "<b>Album</b>: photocards, albums and achievements." },
                new Step { target = "ShopBtnSlot", ko = "<b>상점</b>: 펫을 데려오면 러닝에서 코인을 끌어오거나 막아 줘요.", en = "<b>Shop</b>: pets help on runs — magnets, shields and more." },
                new Step { target = "RhythmBtnSlot", ko = "<b>리듬</b>: 빡세게 / 보통 / 여유.\n성장은 빠르지만 스트레스도 빨라요 — 컨디션을 보며 고르세요.", en = "<b>Pace</b>: hard / normal / easy. Faster growth, faster stress." },
                new Step { target = "TitleBtnSlot", ko = "<b>홈</b>: 타이틀로 돌아가요. 진행은 자동 저장되니 걱정 마세요.", en = "<b>Home</b>: back to the title. Progress autosaves." },
                new Step { target = "TabBody", ko = "<b>스탯</b>이에요. 체력은 러닝 HP, 순발력은 속도가 돼요.\n옆으로 밀면 정신/생활 스탯도 보여요.", en = "<b>Stats</b>. Stamina becomes run HP, agility becomes speed.\nSwipe for mind & life stats.", butlerTop = true },
                new Step { targets = new[] { "Chip0", "Chip1", "Chip2" }, ko = "이번 주 <b>스케줄 칸 3개</b>예요.\n알바·자기계발·휴식 카드를 넣으면 한 주가 굴러가요.", en = "This week's <b>three schedule slots</b>.\nFill them with job, growth and rest cards.", butlerTop = true },
                new Step { target = "ScheduleHit", ko = "<b>[스케줄]</b>에서 카드를 골라 칸을 채워요.\n비워 두면 제가 알아서 채워 드려요.", en = "<b>[Plan]</b> to pick cards. Leave it empty and I'll fill it in.", butlerTop = true },
                new Step { target = "RunHit", ko = "<b>[실행]</b>하면 한 주가 지나가고 결과가 나와요.\n챕터 마지막 주엔 여기서 러닝이 시작돼요!", en = "<b>[Go]</b> plays the week. On the chapter's last week, the run starts here!", butlerTop = true },
                new Step { target = "StoryHit", ko = "<b>[방 꾸미기]</b> — 우리 집이에요.\n가구를 놓고, 베란다에서 꽃을 키워 팔고, 미니게임으로 G를 벌어요.", en = "<b>[Decorate]</b> — our home. Furniture, balcony flowers to sell, and mini-games for G.", butlerTop = true },
                new Step { target = null, ko = "이 정도면 충분해요. 그럼 첫 주, 시작해 볼까요?\n(에디터에선 F1 로 다시 볼 수 있어요)", en = "That's the tour. Shall we start the first week?" },
            };
        }

        private void Build()
        {
            _canvas = CoastUiCanvas.Create("TutorialCanvas", 140, transform);
            _root = CoastUiCanvas.Root(_canvas);
            // 입력 막(투명) — 튜토리얼 중엔 아래 UI를 못 누른다
            var block = CoastHudLayout.MakeImage(_root, "Block", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-2000f, -3000f), new Vector2(2000f, 3000f), new Color(0f, 0f, 0f, 0.03f));   // 알파 0.03: RaycastWatchdog(투명 막 자동 해제)에 안 걸리게
            block.raycastTarget = true;
            for (int i = 0; i < 4; i++)
            {
                _dim[i] = CoastHudLayout.MakeImage(_root, "Dim" + i, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.08f, 0.04f, 0.10f, 0.72f));
                _dim[i].rectTransform.pivot = Vector2.zero; _dim[i].raycastTarget = true;
            }
            _ring = CoastUiArt.Panel(_root, "Ring", new Color(1f, 0.85f, 0.35f, 0.95f), 18);
            _ring.rectTransform.anchorMin = _ring.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); _ring.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _ring2 = CoastUiArt.Panel(_ring.transform, "Hole", new Color(0.08f, 0.04f, 0.10f, 0f), 14);
            _ring2.rectTransform.anchorMin = Vector2.zero; _ring2.rectTransform.anchorMax = Vector2.one; _ring2.rectTransform.offsetMin = new Vector2(5f, 5f); _ring2.rectTransform.offsetMax = new Vector2(-5f, -5f);
            // 링 안쪽은 어두운 막이 없어야 하므로 링은 '테두리만' — 안쪽 Hole 을 투명이 아니라 '막을 뚫는' 용도로는 못 쓰니, 링 자체를 얇은 액자 4장으로 만든다.
            _ring2.enabled = false;

            // 집사 + 말풍선
            _butler = new GameObject("Butler", typeof(RectTransform)).GetComponent<RectTransform>();
            _butler.SetParent(_root, false);
            _butler.sizeDelta = new Vector2(200f, 240f);
            var tex = ArtAssets.LoadTexture("UI_Butler_Bust") ?? ArtAssets.LoadTexture("UI_Butler_Boy");
            var img = new GameObject("Img", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(_butler, false);
            img.rectTransform.anchorMin = Vector2.zero; img.rectTransform.anchorMax = Vector2.one; img.rectTransform.offsetMin = img.rectTransform.offsetMax = Vector2.zero;
            img.preserveAspect = true; img.raycastTarget = false;
            if (tex != null) img.sprite = CoastUiArt.AsSprite(RaisingUI.ChromaKeyed(tex)); else img.color = new Color(0.25f, 0.2f, 0.35f);

            var pill = CoastUiArt.CutePill(_root, "Bubble", new Color(1f, 0.99f, 0.95f), 20, 4);
            _bubble = pill.rectTransform;
            _bubble.sizeDelta = new Vector2(470f, 210f);
            var tag = CoastHudLayout.MakeText(_bubble, "Tag", Loc.T("집사 도담", "Dodam"), 13, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -30f), new Vector2(-12f, -8f));
            tag.color = new Color(0.85f, 0.4f, 0.3f); tag.fontStyle = FontStyle.Bold;
            _count = CoastHudLayout.MakeText(_bubble, "Count", "", 12, TextAnchor.UpperRight, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -30f), new Vector2(-16f, -8f));
            _count.color = new Color(0.55f, 0.5f, 0.6f);
            _text = CoastHudLayout.MakeText(_bubble, "Text", "", 15, TextAnchor.UpperLeft, Vector2.zero, Vector2.one, new Vector2(18f, 60f), new Vector2(-14f, -34f));
            _text.color = new Color(0.25f, 0.15f, 0.12f); _text.horizontalOverflow = HorizontalWrapMode.Wrap; _text.supportRichText = true;
            _text.resizeTextForBestFit = true; _text.resizeTextMinSize = 14; _text.resizeTextMaxSize = CoastHudLayout.Scaled(15);
            Btn(_bubble, "Next", Loc.T("다음", "Next"), new Color(1f, 0.44f, 0.57f), new Vector2(1f, 0f), new Vector2(-12f, 10f), new Vector2(130f, 44f), Next);
            Btn(_bubble, "Skip", Loc.T("건너뛰기", "Skip"), new Color(0.62f, 0.63f, 0.70f), new Vector2(0f, 0f), new Vector2(12f, 10f), new Vector2(130f, 44f), Finish);
        }

        private static Button Btn(Transform parent, string name, string label, Color color, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick)
        {
            var pill = CoastUiArt.CutePill(parent, name, color, 14, 3);
            pill.rectTransform.anchorMin = pill.rectTransform.anchorMax = anchor; pill.rectTransform.pivot = anchor;
            pill.rectTransform.anchoredPosition = pos; pill.rectTransform.sizeDelta = size; pill.raycastTarget = true;
            var b = pill.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { CoastPrefs.Vibrate(); onClick?.Invoke(); });
            var t = CoastHudLayout.MakeText(pill.transform, "T", label, 14, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.35f), 1.5f);
            return b;
        }

        private void Next()
        {
            _i++;
            if (_i >= _steps.Count) { Finish(); return; }
            Show(_steps[_i]);
        }

        private void Finish()
        {
            PlayerPrefs.SetInt(PrefKey, 1); PlayerPrefs.Save();
            var cb = _onDone; _onDone = null;
            Destroy(gameObject);
            cb?.Invoke();
        }

        private RectTransform FindTarget(string name)
        {
            if (string.IsNullOrEmpty(name) || _uiRoot == null) return null;
            foreach (var rt in _uiRoot.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == name) return rt;
            return null;
        }

        /// 대상 RectTransform 의 사각형을 튜토리얼 캔버스 루트 로컬 좌표로.
        private Rect LocalRect(RectTransform target)
        {
            var c = new Vector3[4]; target.GetWorldCorners(c);
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 4; i++)
            {
                var sp = RectTransformUtility.WorldToScreenPoint(null, c[i]);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, sp, null, out var l);
                min = Vector2.Min(min, l); max = Vector2.Max(max, l);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private void Show(Step s)
        {
            _count.text = $"{_i + 1} / {_steps.Count}";
            _text.text = Loc.T(s.ko, s.en);
            Rect? hi = null;
            var names = s.targets ?? (s.target != null ? new[] { s.target } : null);
            if (names != null)
                foreach (var n in names)
                {
                    var t = FindTarget(n); if (t == null) continue;
                    var r = LocalRect(t);
                    hi = hi.HasValue ? Rect.MinMaxRect(Mathf.Min(hi.Value.xMin, r.xMin), Mathf.Min(hi.Value.yMin, r.yMin), Mathf.Max(hi.Value.xMax, r.xMax), Mathf.Max(hi.Value.yMax, r.yMax)) : r;
                }
            const float big = 3000f, pad = 8f;
            if (hi.HasValue)
            {
                var r = hi.Value; r.xMin -= pad; r.yMin -= pad; r.xMax += pad; r.yMax += pad;
                // 위 / 아래 / 왼 / 오 — 네 장으로 구멍
                SetDim(0, -big, r.yMax, 2f * big, big);            // 위
                SetDim(1, -big, -big, 2f * big, big + r.yMin);      // 아래
                SetDim(2, -big, r.yMin, big + r.xMin, r.height);    // 왼
                SetDim(3, r.xMax, r.yMin, big, r.height);           // 오
                _ring.enabled = true;
                _ring.rectTransform.anchoredPosition = r.center; _ring.rectTransform.sizeDelta = r.size + new Vector2(10f, 10f);
                // 링은 액자: 안쪽을 다시 밝히려면 안쪽에 같은 크기의 투명 구멍이 필요 → sliced 스프라이트의 테두리만 남기는 대신 4각 띠로
                BuildRingBands(r);
                // 집사·말풍선 위치: 대상이 화면 위쪽이면 집사는 아래, 아래쪽이면 위. Step.butlerTop 이 있으면 우선.
                bool top = s.butlerTop || r.center.y < -80f;
                PlaceButler(top);
            }
            else
            {
                SetDim(0, -big, -big, 2f * big, 2f * big); SetDim(1, 0, 0, 0, 0); SetDim(2, 0, 0, 0, 0); SetDim(3, 0, 0, 0, 0);
                _ring.enabled = false; ClearBands();
                PlaceButler(false, center: true);
            }
            _t = 0f;
        }

        private readonly List<Image> _bands = new List<Image>();
        private void ClearBands() { foreach (var b in _bands) if (b != null) Destroy(b.gameObject); _bands.Clear(); }
        private void BuildRingBands(Rect r)
        {
            ClearBands(); _ring.enabled = false;
            const float w = 5f;
            var col = new Color(1f, 0.85f, 0.35f, 0.95f);
            _bands.Add(Band(new Rect(r.xMin - w, r.yMax, r.width + 2f * w, w), col));
            _bands.Add(Band(new Rect(r.xMin - w, r.yMin - w, r.width + 2f * w, w), col));
            _bands.Add(Band(new Rect(r.xMin - w, r.yMin, w, r.height), col));
            _bands.Add(Band(new Rect(r.xMax, r.yMin, w, r.height), col));
        }
        private Image Band(Rect r, Color c)
        {
            var im = CoastUiArt.Panel(_root, "Band", c, 3);
            im.rectTransform.anchorMin = im.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); im.rectTransform.pivot = Vector2.zero;
            im.rectTransform.anchoredPosition = r.min; im.rectTransform.sizeDelta = r.size;
            return im;
        }

        private void SetDim(int i, float x, float y, float w, float h)
        {
            var rt = _dim[i].rectTransform;
            rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(Mathf.Max(0f, w), Mathf.Max(0f, h));
        }

        private void PlaceButler(bool top, bool center = false)
        {
            var rr = _root.rect;
            _butler.anchorMin = _butler.anchorMax = new Vector2(0.5f, 0.5f); _butler.pivot = new Vector2(0f, 0f);
            _bubble.anchorMin = _bubble.anchorMax = new Vector2(0.5f, 0.5f); _bubble.pivot = new Vector2(0f, 0f);
            float y = center ? -60f : top ? rr.yMax - 300f : rr.yMin + 30f;
            _butler.anchoredPosition = new Vector2(rr.xMin + 10f, y);
            _bubble.anchoredPosition = new Vector2(rr.xMin + 210f, y + 20f);
            _butler.SetAsLastSibling(); _bubble.SetAsLastSibling();
        }

        private void Update()
        {
            _t += Time.unscaledDeltaTime;
            float p = 0.75f + 0.25f * Mathf.Abs(Mathf.Sin(_t * 3f));
            foreach (var b in _bands) if (b != null) b.color = new Color(1f, 0.85f, 0.35f, p);
            if (_butler != null) _butler.localScale = new Vector3(1f, 1f + 0.02f * Mathf.Sin(_t * 2.2f), 1f);
            // 엔터가 EventSystem Submit 으로 아래 화면의 마지막 선택 버튼을 누르지 않게 선택을 비운다
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null && es.currentSelectedGameObject != null) es.SetSelectedGameObject(null);
            if (CoastRemoteKeys.Down(KeyCode.Return) || CoastRemoteKeys.Down(KeyCode.Space)) Next();
        }
    }
}
