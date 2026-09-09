using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 28차: 방 꾸미기 — 방 안 장식 표시 + [방 꾸미기] 팝업(보유 장식 배치/치우기, 상점 장식 구매, 러닝 드롭 안내).
    public partial class RaisingUI
    {
        private RectTransform _decoLayer;
        private GameObject _decoModal;
        private int _decoToastMask = -1;

        // ── 방 안 표시 ─────────────────────────────────────────────────

        private void RefreshRoomDeco()
        {
            if (_decoLayer == null || _gm == null) return;
            var p = _gm.Profile;
            HomeData.Ensure(p);
            for (int i = _decoLayer.childCount - 1; i >= 0; i--) Destroy(_decoLayer.GetChild(i).gameObject);
            // 30차: 집 화면과 같은 정규화 좌표. 벽걸이 먼저, 바닥은 먼 것(y 큰 것)부터.
            var items = new System.Collections.Generic.List<HomeItem>(p.homeItems);
            items.Sort((a, b) =>
            {
                var da = HomeData.Find(a.id); var db = HomeData.Find(b.id);
                bool wa = da != null && HomeData.IsWall(da), wb = db != null && HomeData.IsWall(db);
                if (wa != wb) return wa ? -1 : 1;
                return b.y.CompareTo(a.y);
            });
            foreach (var h in items)
            {
                var d = HomeData.Find(h.id); if (d == null) continue;
                var size = HomeData.Size(d) * 0.9f;
                var go = HomeUI.DecoVisual(_decoLayer, d, size, !HomeData.IsWall(d));
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(h.x, h.y);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = size;
            }
            // 러닝에서 새 장식을 얻었으면 한 번만 알림
            if (RoomDeco.AnyNew(p) && p.decoNewMask != _decoToastMask)
            {
                _decoToastMask = p.decoNewMask;
                Toast(Loc.T("새 장식을 얻었어! [방 꾸미기]에서 놓아 봐.", "New decor! Place it in [Decorate]."));
            }
        }

        // ── 집 화면(30차: HomeUI) ─────────────────────────────────────────

        public void OpenRoomDeco()
        {
            if (_busy || Save == null || _gm == null) return;
            _busy = true;
            HomeUI.Open(_gm, _charImage != null ? _charImage.sprite : null, () =>
            {
                _busy = false;
                RoomDeco.ClearAllNew(_gm.Profile);
                _gm.WriteProfileNow();
                Refresh();
            });
        }
    }
}
