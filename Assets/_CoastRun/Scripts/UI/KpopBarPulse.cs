using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 29차: 타이틀 K-POP 러닝모드 바 — 네온이 숨 쉬듯 살짝 커졌다 작아지고(1.0~1.03), 누르면 눌리는 스쿼시.
    /// 그림(UI_KpopBar)에 글로우가 구워져 있으므로 스케일 펄스만으로 '빛이 맥동'하는 느낌이 난다.
    public class KpopBarPulse : MonoBehaviour
    {
        private RectTransform _rt;
        private Button _btn;
        private float _press;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _btn = GetComponent<Button>();
            if (_btn != null) _btn.onClick.AddListener(() => _press = 1f);
        }

        private void Update()
        {
            float t = Time.unscaledTime;
            float pulse = 1f + 0.03f * (0.5f + 0.5f * Mathf.Sin(t * 2.6f));
            _press = Mathf.MoveTowards(_press, 0f, Time.unscaledDeltaTime * 5f);
            float sq = 1f - 0.08f * Mathf.Sin(_press * Mathf.PI);
            _rt.localScale = new Vector3(pulse * (2f - sq), pulse * sq, 1f);
        }
    }
}
