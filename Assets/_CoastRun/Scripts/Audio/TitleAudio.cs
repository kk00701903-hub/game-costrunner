using UnityEngine;

namespace CoastRun
{
    /// Title-screen BGM + UI SFX (procedural until real clips land).
    /// 48차-6(사용자): 타이틀 BGM(M5)은 앱을 켜자마자(00_Boot) 시작하고, 씬이 바뀌어도 살아남는 전역 소스(DontDestroyOnLoad)에서
    /// 계속 나온다. PlayMenu 는 같은 곡이 이미 나오고 있으면 끊지 않는다. StopMenu 는 러닝/육성으로 나갈 때만.
    public class TitleAudio : MonoBehaviour
    {
        private static AudioSource s_bgm;
        private AudioSource _bgm => EnsureBgm();
        private AudioSource _sfx;
        private AudioClip _click;
        private AudioClip _start;
        private bool _cleared;

        private static AudioSource EnsureBgm()
        {
            if (s_bgm != null) return s_bgm;
            var go = new GameObject("TitleBgm(Global)");
            Object.DontDestroyOnLoad(go);
            s_bgm = go.AddComponent<AudioSource>();
            s_bgm.playOnAwake = false;
            s_bgm.spatialBlend = 0f;
            return s_bgm;
        }

        /// 00_Boot 에서 호출 — 리스너·타이틀 UI 가 뜨기 전에 곡부터 튼다.
        public static void PlayMenuEarly()
        {
            var src = EnsureBgm();
            var real = CoastBgmLibrary.Load(CoastBgmLibrary.Menu(false));
            if (real == null || (src.isPlaying && src.clip == real)) return;
            src.clip = real; src.volume = 0.85f; src.loop = true; src.Play();
        }

        public void PlayMenu(bool cleared)
        {
            _cleared = cleared;
            Ensure();
            // Real track from Resources/CoastRun/BGM when it exists, procedural bed until then.
            var real = CoastBgmLibrary.Load(CoastBgmLibrary.Menu(cleared));
            if (real != null && _bgm.isPlaying && _bgm.clip == real) return;   // 이미 나오는 중 — 이어서
            _bgm.clip = real != null
                ? real
                : cleared
                    ? ProceduralAudio.CreateLoop(110f, 0.05f, 8f)   // BGM_Menu_Cleared — darker
                    : ProceduralAudio.CreateLoop(196f, 0.04f, 8f);  // BGM_Menu — warm noon
            _bgm.volume = real != null ? 0.85f : (cleared ? 0.28f : 0.32f);
            _bgm.loop = true;
            if (!_bgm.isPlaying)
                _bgm.Play();
            else
            {
                _bgm.Stop();
                _bgm.Play();
            }
        }

        public void StopMenu()
        {
            if (_bgm != null && _bgm.isPlaying)
                _bgm.Stop();
        }

        public void PlayClick()
        {
            Ensure();
            _sfx.PlayOneShot(_click, 0.4f);
        }

        public void PlayStart()
        {
            Ensure();
            _sfx.PlayOneShot(_start, 0.55f);
        }

        private void Ensure()
        {
            EnsureBgm();

            if (_sfx == null)
            {
                var go = new GameObject("TitleSfx");
                go.transform.SetParent(transform, false);
                _sfx = go.AddComponent<AudioSource>();
                _sfx.playOnAwake = false;
                _sfx.spatialBlend = 0f;
            }

            if (_click == null)
                _click = ProceduralAudio.CreateBlip(660f, 0.04f);
            if (_start == null)
                _start = ProceduralAudio.CreateBlip(440f, 0.12f);
        }
    }
}
