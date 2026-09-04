using UnityEngine;

namespace CoastRun
{
    /// Retired. TitleSceneDriver owns 01_Title, and the legacy MainMenu.unity this used
    /// to boot no longer exists.
    ///
    /// The component survives only because copies of it are still saved inside
    /// 00_Boot / 01_Title / 03_Cutscene / 04_Ending — the five flow scenes were
    /// duplicated from one template and it rode along. 03_Cutscene loads additively
    /// over the running game, so an unguarded copy raised a full title menu on top of
    /// gameplay every time a chapter cutscene played.
    ///
    /// It now removes itself on sight. Run Coast Run/Fix Scene Bootstraps to strip the
    /// saved copies, then this file can go too.
    [DefaultExecutionOrder(-200)]
    public class MainMenuBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Destroy(gameObject);
        }
    }
}
