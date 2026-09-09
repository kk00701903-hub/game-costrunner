using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 23차-7: 집사 꼬마 — 프린세스 메이커의 집사처럼, 기억을 잃은 주인공(과 플레이어)에게 지금 뭘 하면 좋은지 알려준다.
    /// 방 왼쪽 아래에 서 있고, 말풍선에 상황별 조언. 탭하면 다음 조언. 상태를 읽어 우선순위대로 말한다.
    public class RaisingButler
    {
        private readonly RectTransform _root;
        private readonly Image _body;
        private readonly Text _bubble;
        private readonly RectTransform _bubbleRt;
        private readonly List<string> _lines = new List<string>();
        private int _idx;
        private float _bob;
        public bool Visible => _root != null && _root.gameObject.activeSelf;

        public RaisingButler(RectTransform host, System.Action onTapped)
        {
            _root = new GameObject("Butler", typeof(RectTransform)).GetComponent<RectTransform>();
            _root.SetParent(host, false);
            _root.anchorMin = _root.anchorMax = new Vector2(0f, 0f); _root.pivot = new Vector2(0f, 0f);
            _root.anchoredPosition = new Vector2(8f, 14f); _root.sizeDelta = new Vector2(190f, 300f);

            var shadow = CoastHudLayout.MakeImage(_root, "Shadow", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-60f, -4f), new Vector2(60f, 14f), new Color(0f, 0f, 0f, 0.2f));
            shadow.sprite = CoastUiArt.RoundedRect(30); shadow.type = Image.Type.Sliced; shadow.raycastTarget = false;

            var go = new GameObject("Body", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_root, false);
            _body = go.GetComponent<Image>();
            var tex = ArtAssets.LoadTexture("UI_Butler_Boy");
            if (tex != null) _body.sprite = CoastUiArt.AsSprite(tex); else _body.color = new Color(0.2f, 0.2f, 0.3f);
            _body.preserveAspect = true;
            var brt = _body.rectTransform; brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero; brt.pivot = new Vector2(0.5f, 0f);
            var btn = go.GetComponent<Button>(); btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { Next(); onTapped?.Invoke(); });

            // 말풍선(오른쪽 위로), 꼬리
            var pill = CoastUiArt.CutePill(_root, "Bubble", new Color(1f, 0.99f, 0.95f), 18, 3);
            _bubbleRt = pill.rectTransform;
            _bubbleRt.anchorMin = _bubbleRt.anchorMax = new Vector2(1f, 1f); _bubbleRt.pivot = new Vector2(0f, 0f);
            _bubbleRt.anchoredPosition = new Vector2(-40f, -80f); _bubbleRt.sizeDelta = new Vector2(400f, 96f);
            pill.raycastTarget = false;
            var tail = CoastUiArt.Panel(_bubbleRt, "Tail", new Color(1f, 0.99f, 0.95f), 6);
            var trt = tail.rectTransform; trt.anchorMin = trt.anchorMax = new Vector2(0f, 0f); trt.pivot = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = new Vector2(18f, -2f); trt.sizeDelta = new Vector2(26f, 26f); trt.localRotation = Quaternion.Euler(0f, 0f, 45f); tail.raycastTarget = false;
            var tag = CoastHudLayout.MakeText(_bubbleRt, "Tag", Loc.T("집사 도담", "Dodam"), 14, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -24f), new Vector2(-12f, -6f));
            tag.color = new Color(0.85f, 0.4f, 0.3f); tag.fontStyle = FontStyle.Bold; tag.raycastTarget = false;
            _bubble = CoastHudLayout.MakeText(_bubbleRt, "T", "", 19, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(16f, 8f), new Vector2(-12f, -24f));
            _bubble.color = new Color(0.25f, 0.15f, 0.12f); _bubble.horizontalOverflow = HorizontalWrapMode.Wrap; _bubble.raycastTarget = false;
            _bubble.resizeTextForBestFit = true; _bubble.resizeTextMinSize = 14; _bubble.resizeTextMaxSize = 19;
        }

        public void SetVisible(bool on) { _root.gameObject.SetActive(on); }

        /// 상태를 읽어 조언 목록을 다시 만든다(우선순위 순). 처음 문장은 바로 보여준다.
        public void Refresh(SaveData save, bool firstVisit)
        {
            _lines.Clear();
            if (save == null) { _lines.Add(Loc.T("아가씨, 오늘도 제가 옆에 있겠습니다.", "Miss, I'm right here today too.")); Show(0); return; }
            var st = save.stats;
            int week = save.week;
            if (firstVisit)
                _lines.Add(Loc.T("아가씨, 기억은 없어도 몸은 기억합니다. 아래 칸에 이번 주 할 일을 넣어 보세요.", "Miss, your body remembers even if you don't. Fill this week's slots below."));
            if (st.Burnout)
                _lines.Add(Loc.T("지금은 무리입니다. 이번 주는 휴식으로 채우시지요.", "Not now. Fill this week with rest."));
            else if (st.stamina > 0 && st.stress / (float)st.stamina >= 0.7f)
                _lines.Add(Loc.T("스트레스가 높습니다. 휴식 카드 한 장이면 다음 주가 편해집니다.", "Stress is high. One rest card makes next week easier."));
            if (st.money < 60)
                _lines.Add(Loc.T("지갑이 가볍습니다. 알바 한 칸 넣어 두면 교육비를 댈 수 있습니다.", "Purse is light. One job slot pays for lessons."));
            int lowest = Mathf.Min(st.stamina, Mathf.Min(st.agility, st.charm));
            string low = lowest == st.stamina ? Loc.T("체력", "stamina") : lowest == st.agility ? Loc.T("순발력", "agility") : Loc.T("매력", "charm");
            _lines.Add(Loc.T($"지금 가장 낮은 건 {low}입니다. 그쪽 교육을 넣으시면 달리기가 달라집니다.", $"{low} is lowest right now. A lesson there changes the run."));
            var rec = save.CurrentChapter;
            if (rec != null && rec.heartsTarget > 0)
            {
                int left = rec.heartsTarget - save.chapterHearts;
                if (left > 0) _lines.Add(Loc.T($"이번 장 하트가 {left}개 남았습니다. 달리기에서 모아 오시면 됩니다.", $"{left} hearts left this chapter. Collect them on the run."));
            }
            _lines.Add(Loc.T("[자동 배치]를 누르시면 제가 이번 주를 짜 드립니다. 마음에 안 드는 칸만 바꾸세요.", "Tap [Auto plan] and I'll fill the week. Change only what you dislike."));
            _lines.Add(Loc.T("송전탑은 여전히 창밖에 있습니다. 약속한 생일까지, 제가 세어 두겠습니다.", "The tower is still out the window. I'll count the days to the promised birthday."));
            _idx = 0;
            Show(0);
        }

        private void Next()
        {
            if (_lines.Count == 0) return;
            _idx = (_idx + 1) % _lines.Count;
            Show(_idx);
            _bob = 1f;
        }

        private void Show(int i)
        {
            if (_lines.Count == 0) { _bubble.text = ""; return; }
            _bubble.text = _lines[Mathf.Clamp(i, 0, _lines.Count - 1)];
        }

        /// 매 프레임: 살짝 숨 쉬고, 탭하면 콩 뛴다.
        public void Tick(float dt)
        {
            if (!Visible) return;
            float t = Time.unscaledTime;
            float breathe = 1f + Mathf.Sin(t * 2.1f) * 0.012f;
            _bob = Mathf.MoveTowards(_bob, 0f, dt * 3f);
            float hop = Mathf.Sin(Mathf.Clamp01(_bob) * Mathf.PI) * 18f;
            _body.rectTransform.localScale = new Vector3(1f / breathe, breathe, 1f);
            _body.rectTransform.anchoredPosition = new Vector2(0f, hop);
            _bubbleRt.localScale = Vector3.one * (1f + Mathf.Sin(t * 2.1f + 1f) * 0.006f);
        }
    }
}
