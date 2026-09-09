using System;

namespace CoastRun
{
    /// 38차: 37차의 회전 레코드판 화면은 시안(컬렉션 › 레코드 리스트)으로 통합됐다. 옛 호출처 호환용.
    public static class RecordsUI
    {
        public static bool IsOpen => CollectionUI.IsOpen;
        public static void Open(Action onClose) => CollectionUI.Open(onClose, 0);
    }
}
