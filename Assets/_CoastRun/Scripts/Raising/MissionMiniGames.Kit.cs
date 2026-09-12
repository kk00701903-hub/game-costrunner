using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 58차(사용자): 미니게임 사용성을 10억 다운로드급 캐주얼 게임 문법으로 — 5종 공통 「키트」.
    ///   ① 마당 위 **목표 띠**(한 줄 목표 + 남은 기회 알약 ● ● ○ + 점수) — 언제나 보인다.
    ///   ② **큰 버튼 맥동** + 첫 2판엔 **탭 링·손가락 안내**(「여기를 탭!」) — 글을 안 읽어도 뭘 누를지 안다.
    ///   ③ 시작 「준비 — 시작!」 플래시, 매 시도 결과 **큰 팝 글자**(쏙! / 빗나감…)로 즉각 피드백.
    ///   ④ 진동·효과음은 기존 것 그대로. 텍스트는 짧게, 정보는 위(목표)·아래(조작) 두 곳에만.
    public static partial class MissionMiniGames
    {
        internal class MiniKit : MonoBehaviour
        {
            private RectTransform _yard, _strip;
            private Text _goal, _score;
            private readonly List<Image> _pips = new List<Image>();
            private RectTransform _pipHost;
            private RectTransform _hint, _hintPill; private Text _hintTxt; private RectTransform _ring;
            private string _prefKey;

            public static MiniKit Attach(RectTransform yard, string playKey)
            {
                var go = new GameObject("MiniKit", typeof(RectTransform));
                go.transform.SetParent(yard, false);
                var rt = go.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
                var k = go.AddComponent<MiniKit>(); k._yard = rt; k._prefKey = "CoastRun_MiniPlays_" + playKey;
                k.BuildStrip();
                return k;
            }

            private void BuildStrip()
            {
                _strip = CoastUiArt.CutePill(_yard, "GoalStrip", new Color(0.08f, 0.10f, 0.24f, 0.86f), 18, 3).rectTransform;
                _strip.anchorMin = new Vector2(0f, 1f); _strip.anchorMax = new Vector2(1f, 1f); _strip.pivot = new Vector2(0.5f, 1f);
                _strip.anchoredPosition = new Vector2(0f, -10f); _strip.sizeDelta = new Vector2(-24f, 62f);
                _strip.GetComponent<Image>().raycastTarget = false;
                _goal = CoastHudLayout.MakeText(_strip, "Goal", "", 18, TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(0.62f, 1f), new Vector2(18f, 0f), new Vector2(0f, 0f));
                _goal.color = new Color(1f, 0.95f, 0.75f); _goal.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(_goal, new Color(0f, 0f, 0f, 0.5f), 1.3f);
                _goal.resizeTextForBestFit = true; _goal.resizeTextMinSize = 10; _goal.resizeTextMaxSize = CoastHudLayout.Scaled(18);
                _pipHost = new GameObject("Pips", typeof(RectTransform)).GetComponent<RectTransform>();
                _pipHost.SetParent(_strip, false); _pipHost.anchorMin = new Vector2(0.62f, 0f); _pipHost.anchorMax = new Vector2(0.84f, 1f); _pipHost.offsetMin = _pipHost.offsetMax = Vector2.zero;
                _score = CoastHudLayout.MakeText(_strip, "Score", "", 22, TextAnchor.MiddleRight, new Vector2(0.84f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(-16f, 0f));
                _score.color = new Color(1f, 0.85f, 0.30f); _score.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(_score, new Color(0f, 0f, 0f, 0.5f), 1.5f);
                _score.resizeTextForBestFit = true; _score.resizeTextMinSize = 10; _score.resizeTextMaxSize = CoastHudLayout.Scaled(22);
            }

            /// 목표 한 줄(예: 「5발 중 3발 넣기」).
            public void Goal(string s) { if (_goal != null) _goal.text = s; }
            /// 오른쪽 점수(예: 「1 / 3」).
            public void Score(string s) { if (_score != null) _score.text = s; }
            /// 남은 기회 알약: total 개 중 left 개가 켜짐(색 = 알약 색).
            public void Pips(int total, int left, Color? color = null)
            {
                if (_pipHost == null) return;
                while (_pips.Count < total)
                {
                    var p = CoastUiArt.Panel(_pipHost, "P" + _pips.Count, Color.white, 9); p.raycastTarget = false;
                    p.rectTransform.anchorMin = p.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); p.rectTransform.sizeDelta = new Vector2(18f, 18f);
                    _pips.Add(p);
                }
                var c = color ?? new Color(1f, 0.85f, 0.30f);
                float w = _pipHost.rect.width; if (w < 10f) w = 120f;
                float step = Mathf.Min(24f, w / Mathf.Max(1, total));
                for (int i = 0; i < _pips.Count; i++)
                {
                    bool on = i < total; _pips[i].gameObject.SetActive(on); if (!on) continue;
                    _pips[i].rectTransform.anchoredPosition = new Vector2((i - (total - 1) * 0.5f) * step, 0f);
                    _pips[i].color = i < left ? c : new Color(1f, 1f, 1f, 0.22f);
                }
            }

            /// 시작 플래시 「준비 — 시작!」.
            public void Flash(string big, Color? col = null, float hold = 0.7f) { StartCoroutine(FlashCo(big, col ?? new Color(1f, 0.92f, 0.35f), hold, 0.5f)); }

            /// 시도 결과 팝(마당 가운데 큰 글자가 튀어 올랐다 사라진다).
            public void Pop(string s, bool good) { StartCoroutine(FlashCo(s, good ? new Color(0.45f, 1f, 0.55f) : new Color(1f, 0.45f, 0.45f), 0.55f, 0.35f)); }

            private IEnumerator FlashCo(string s, Color col, float hold, float y01)
            {
                var t = CoastHudLayout.MakeText(_yard, "Pop", s, 52, TextAnchor.MiddleCenter, new Vector2(0f, y01 - 0.12f), new Vector2(1f, y01 + 0.12f), Vector2.zero, Vector2.zero);
                t.color = col; t.fontStyle = FontStyle.Bold; t.raycastTarget = false; CoastUiArt.OutlineText(t, new Color(0.1f, 0.05f, 0.15f, 0.85f), 3f);
                t.resizeTextForBestFit = true; t.resizeTextMinSize = 20; t.resizeTextMaxSize = CoastHudLayout.Scaled(52);
                var rt = t.rectTransform; float e = 0f;
                while (e < 0.18f) { e += Time.unscaledDeltaTime; float k = e / 0.18f; rt.localScale = Vector3.one * (0.4f + 0.75f * Mathf.Sin(k * Mathf.PI * 0.5f) + 0.15f * Mathf.Sin(k * Mathf.PI)); yield return null; }
                rt.localScale = Vector3.one;
                yield return new WaitForSecondsRealtime(hold);
                e = 0f;
                while (e < 0.25f) { e += Time.unscaledDeltaTime; float k = e / 0.25f; t.color = new Color(col.r, col.g, col.b, 1f - k); rt.anchoredPosition += new Vector2(0f, 60f * Time.unscaledDeltaTime); yield return null; }
                if (t != null) Destroy(t.gameObject);
            }

            /// 첫 두 판: 큰 버튼 위 탭 링 + 「여기를 탭!」. 한 번 누르면 사라진다. 판 수는 PlayerPrefs.
            public void TapHint(RectTransform button, string label)
            {
                int plays = PlayerPrefs.GetInt(_prefKey, 0);
                PlayerPrefs.SetInt(_prefKey, plays + 1);
                if (plays >= 2 || button == null) return;
                _hint = new GameObject("TapHint", typeof(RectTransform)).GetComponent<RectTransform>();
                _hint.SetParent(button, false); _hint.anchorMin = Vector2.zero; _hint.anchorMax = Vector2.one; _hint.offsetMin = _hint.offsetMax = Vector2.zero;
                var ring = CoastUiArt.Panel(_hint, "Ring", new Color(1f, 1f, 1f, 0f), 40); ring.raycastTarget = false;
                _ring = ring.rectTransform; _ring.anchorMin = _ring.anchorMax = new Vector2(0.5f, 0.5f); _ring.sizeDelta = new Vector2(90f, 90f);
                var inner = CoastUiArt.Panel(_hint, "Inner", new Color(1f, 1f, 1f, 0.55f), 30); inner.raycastTarget = false;
                inner.rectTransform.anchorMin = inner.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); inner.rectTransform.sizeDelta = new Vector2(60f, 60f);
                // 안내 말풍선은 마당 아래 가운데(조작 패널 글과 안 겹치게)
                _hintPill = CoastUiArt.CutePill(_yard, "HintLabel", new Color(1f, 1f, 1f, 0.96f), 14, 2).rectTransform; _hintPill.GetComponent<Image>().raycastTarget = false;
                var prt = _hintPill; prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0f); prt.pivot = new Vector2(0.5f, 0f); prt.anchoredPosition = new Vector2(0f, 74f); prt.sizeDelta = new Vector2(320f, 48f);
                _hintTxt = CoastHudLayout.MakeText(prt, "T", label, 17, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(4f, 2f), new Vector2(-4f, 0f));
                _hintTxt.color = new Color(0.16f, 0.14f, 0.30f); _hintTxt.fontStyle = FontStyle.Bold;
                _hintTxt.resizeTextForBestFit = true; _hintTxt.resizeTextMinSize = 10; _hintTxt.resizeTextMaxSize = CoastHudLayout.Scaled(17);
                var arrow = CoastHudLayout.MakeText(_hintPill, "Arrow", "▼", 22, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-20f, -26f), new Vector2(20f, -2f));
                arrow.color = Color.white; arrow.fontStyle = FontStyle.Bold; arrow.raycastTarget = false; CoastUiArt.OutlineText(arrow, new Color(0f, 0f, 0f, 0.5f), 1.5f);
                var b = button.GetComponent<Button>();
                if (b != null) b.onClick.AddListener(HideHint);
                StartCoroutine(RingCo(inner));
            }
            private IEnumerator RingCo(Image inner)
            {
                float t = 0f;
                while (_hint != null)
                {
                    t += Time.unscaledDeltaTime; float k = (t % 1.1f) / 1.1f;
                    if (_ring != null) { _ring.localScale = Vector3.one * (0.7f + 0.9f * k); var im = _ring.GetComponent<Image>(); if (im != null) im.color = new Color(1f, 1f, 1f, 0.6f * (1f - k)); }
                    if (inner != null) inner.rectTransform.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(t * 8f));
                    yield return null;
                }
            }
            public void HideHint() { if (_hint != null) { Destroy(_hint.gameObject); _hint = null; } if (_hintPill != null) { Destroy(_hintPill.gameObject); _hintPill = null; } }

            /// 큰 버튼 맥동(대기 중 눈에 띄게). on=false 면 원래 크기.
            public static void Pulse(RectTransform button, bool on)
            {
                if (button == null) return;
                var p = button.GetComponent<ButtonPulse>() ?? button.gameObject.AddComponent<ButtonPulse>();
                p.enabled = on; if (!on) button.localScale = Vector3.one;
            }
            private class ButtonPulse : MonoBehaviour
            {
                private void Update() { float s = 1f + 0.035f * Mathf.Sin(Time.unscaledTime * 4.5f); transform.localScale = new Vector3(s, s, 1f); }
                private void OnDisable() { transform.localScale = Vector3.one; }
            }
        }
    }
}
