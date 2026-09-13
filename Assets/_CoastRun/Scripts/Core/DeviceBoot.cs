using UnityEngine;

namespace CoastRun
{
    /// 67차: 기기 공통 부트 설정.
    ///   6) 게임 중 화면 절전(자동 꺼짐) 방지 — Android 는 별도 권한 없이 Screen.sleepTimeout 으로 충분(WAKE_LOCK 은 유니티가 자동 추가).
    ///   1) 빌드에서 MeshCollider 등이 엔진 코드 스트리핑으로 빠져 `CreatePrimitive` 가 실패하던 문제 — 코드에서 형식을 직접 참조해 남긴다
    ///      (BuildMenu 는 stripEngineCode 도 끄고, Assets/link.xml 도 둔다 — 삼중 안전).
    public static class DeviceBoot
    {
        private static readonly System.Type[] KeepTypes =
        {
            typeof(MeshCollider), typeof(BoxCollider), typeof(SphereCollider), typeof(CapsuleCollider),
            typeof(MeshFilter), typeof(MeshRenderer), typeof(Rigidbody), typeof(ParticleSystem), typeof(TrailRenderer), typeof(LineRenderer),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            if (KeepTypes.Length == 0) Debug.Log("[DeviceBoot] keep");
        }
    }
}
