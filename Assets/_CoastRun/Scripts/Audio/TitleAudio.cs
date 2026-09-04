using UnityEngine;

namespace CoastRun
{
    /// Title-screen BGM + UI SFX (procedural until real clips land).
    public class TitleAudio : MonoBehaviour
    {
        private AudioSource _bgm;
        private AudioSource _sfx;
        private AudioClip _click;
        private AudioClip _start;
        private bool _cleared;

        public void PlayMenu(bool cleared)
        {
            _cleared = cleared;
            Ensure();
            // Real track from Resources/CoastRun/BGM when it exists, procedural bed until then.
            var real = CoastBgmLibrary.Load(CoastBgmLibrary.Menu(cleared));
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
            if (_bgm == null)
            {
                var go = new GameObject("TitleBgm");
                go.transform.SetParent(transform, false);
                _bgm = go.AddComponent<AudioSource>();
                _bgm.playOnAwake = false;
                _bgm.spatialBlend = 0f;
            }

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
