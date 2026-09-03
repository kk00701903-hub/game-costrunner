using System;
using UnityEngine;

namespace CoastRun
{
    /// Thin legacy wrapper — prefers CutsceneController + CutsceneTable.
    public class CutsceneHost : MonoBehaviour
    {
        private static CutsceneHost _stub;

        public static CutsceneHost EnsureStub()
        {
            if (_stub != null)
                return _stub;
            var go = new GameObject("CutsceneHost_Stub");
            DontDestroyOnLoad(go);
            _stub = go.AddComponent<CutsceneHost>();
            return _stub;
        }

        public void Play(CutsceneKind kind, int chapter, Action onFinished)
        {
            var ctrl = CutsceneController.Ensure();
            ctrl.PlayKind(kind, chapter, c =>
            {
                var flow = GameDirector.Instance != null ? GameDirector.Instance.Flow : null;
                if (flow != null)
                    flow.OnCutsceneControllerFinished(c);
                else
                    onFinished?.Invoke();
            });
        }
    }
}
