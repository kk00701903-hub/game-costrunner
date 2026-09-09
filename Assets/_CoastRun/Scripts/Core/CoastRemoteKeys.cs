using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// 23차: 에디터 원격(CoastRemote, MCP)에서 디버그 키를 주입하기 위한 얇은 래퍼.
    /// `Input.GetKeyDown(k)` 대신 `CoastRemoteKeys.Down(k)` — 실제 키 또는 원격으로 눌린 키(한 번 소비).
    public static class CoastRemoteKeys
    {
        private static readonly Dictionary<KeyCode, float> _pending = new Dictionary<KeyCode, float>();

        public static void Press(string name)
        {
            if (System.Enum.TryParse<KeyCode>(name, true, out var k)) _pending[k] = Time.realtimeSinceStartup;
        }

        public static bool Down(KeyCode k)
        {
            if (Input.GetKeyDown(k)) return true;
            if (_pending.TryGetValue(k, out var t))
            {
                _pending.Remove(k);
                return Time.realtimeSinceStartup - t < 2f;   // 2초 넘게 안 소비된 키는 버린다
            }
            return false;
        }
    }
}
