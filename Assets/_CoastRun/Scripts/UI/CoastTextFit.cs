using UnityEngine;
using UnityEngine.UI;

namespace CoastRun.UI
{
    /// 136차: uGUI Text의 Best Fit은 horizontalOverflow = Overflow(한 줄 라벨)일 때 **가로 폭을 무시**해서
    /// 글자가 상자 밖으로 새어 나갔다(돈 알약, 펫 이름, 카드 미리보기 등 전수조사에서 발견).
    /// 이 컴포넌트는 그런 텍스트를 넘겨받아 Best Fit 대신 직접 글자 크기를 폭·높이에 맞게 줄인다(한 줄 유지, 줄바꿈 없음).
    /// CoastHudLayout.MakeText 가 모든 텍스트에 붙이며, Best Fit + Overflow 조합일 때만 동작한다.
    [DisallowMultipleComponent]
    public sealed class CoastTextFit : MonoBehaviour
    {
        static readonly TextGenerator Gen = new TextGenerator();
        Text _t; string _last; float _lastW = -1f, _lastH = -1f; int _min, _max; bool _armed;

        void Awake() { _t = GetComponent<Text>(); }

        void LateUpdate()
        {
            if (_t == null) return;
            if (!_armed)
            {
                if (!_t.resizeTextForBestFit || _t.horizontalOverflow != HorizontalWrapMode.Overflow) return;
                _min = Mathf.Max(1, _t.resizeTextMinSize); _max = Mathf.Max(_min, _t.resizeTextMaxSize);
                _t.resizeTextForBestFit = false;
                _armed = true;
            }
            else if (_t.resizeTextForBestFit)
            {
                // 나중에 코드가 다시 Best Fit을 켰으면(크기 바꿈) 새 범위를 받아 다시 맡는다
                if (_t.horizontalOverflow != HorizontalWrapMode.Overflow) { _armed = false; return; }
                _min = Mathf.Max(1, _t.resizeTextMinSize); _max = Mathf.Max(_min, _t.resizeTextMaxSize);
                _t.resizeTextForBestFit = false; _last = null;
            }
            var r = _t.rectTransform.rect;
            if (r.width < 2f || r.height < 2f) return;
            if (ReferenceEquals(_t.text, _last) && Mathf.Abs(r.width - _lastW) < 0.5f && Mathf.Abs(r.height - _lastH) < 0.5f) return;
            _last = _t.text; _lastW = r.width; _lastH = r.height;
            Fit(r);
        }

        void Fit(Rect r)
        {
            if (string.IsNullOrEmpty(_t.text)) return;
            _t.fontSize = _max;
            var settings = _t.GetGenerationSettings(Vector2.zero);
            settings.horizontalOverflow = HorizontalWrapMode.Overflow;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            float ppu = Mathf.Max(0.01f, _t.pixelsPerUnit);
            float w = Gen.GetPreferredWidth(_t.text, settings) / ppu;
            float h = Gen.GetPreferredHeight(_t.text, settings) / ppu;
            if (w <= 0f || h <= 0f) return;
            float k = Mathf.Min(r.width / w, r.height / h);
            if (k < 1f)
            {
                int size = Mathf.Clamp(Mathf.FloorToInt(_max * k), _min, _max);
                _t.fontSize = size;
            }
        }

        /// 텍스트를 바꾼 직후 같은 프레임에 맞추고 싶을 때.
        public void Refit() { _last = null; }
    }
}
