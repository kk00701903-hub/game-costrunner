// 23차: Claude MCP ↔ Unity 에디터 브릿지. 화면 제어 권한 없이 에디터를 다룬다.
// 127.0.0.1:47001 에서 한 줄 JSON 명령을 받아 메인 스레드에서 실행하고 한 줄 JSON으로 답한다.
// 명령: ping / status / refresh / play / stop / pause / menu <path> / scene <name> / shot [name] / log [n]
//       warp / clear / retry / hit / lane <-1|1> / jump / crouch / tap <nx> <ny> / key <B|...> / setres <w> <h>
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace CoastRun.Editor
{
    [InitializeOnLoad]
    public static class CoastRemote
    {
        const int Port = 47001;
        static TcpListener _listener;
        static Thread _thread;
        static readonly object _lock = new object();
        static readonly Queue<Req> _queue = new Queue<Req>();
        static readonly List<string> _log = new List<string>();
        static readonly List<string> _compileErrors = new List<string>();
        static bool _compiling;

        class Req { public string line; public string reply; public ManualResetEvent done = new ManualResetEvent(false); }

        static CoastRemote()
        {
            Application.logMessageReceivedThreaded += OnLog;
            CompilationPipeline.compilationStarted += _ => { _compiling = true; lock (_lock) _compileErrors.Clear(); };
            CompilationPipeline.assemblyCompilationFinished += (asm, msgs) =>
            {
                lock (_lock) foreach (var m in msgs) if (m.type == CompilerMessageType.Error) _compileErrors.Add($"{Path.GetFileName(m.file)}({m.line}): {m.message}");
            };
            CompilationPipeline.compilationFinished += _ => _compiling = false;
            EditorApplication.update += Pump;
            EditorApplication.quitting += Stop;
            AppDomain.CurrentDomain.DomainUnload += (_, __) => Stop();
            // 24차-4: 플레이 진입 등 도메인 리로드 때 DomainUnload가 안 불려 옛 리스너가 47001을 물고 있었다
            // ("각 소켓 주소는 하나만 사용할 수 있습니다") → 리로드 직전에 명시적으로 닫고, 그래도 실패하면 잠시 뒤 재시도.
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            Start();
        }

        static int _startRetries;

        static void OnLog(string cond, string stack, LogType type)
        {
            if (type == LogType.Log) return;
            lock (_lock)
            {
                _log.Add($"[{type}] {cond}" + (type == LogType.Exception ? "\n" + (stack ?? "").Split('\n')[0] : ""));
                if (_log.Count > 200) _log.RemoveRange(0, _log.Count - 200);
            }
        }

        static void Start()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Loopback, Port);
                // 24차-4b: 리로드 직전에 받아 둔 연결(Serve 스레드, 최대 20초 대기)이 같은 포트를 물고 있어 재바인드가 실패했다.
                _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listener.Start();
                _thread = new Thread(Accept) { IsBackground = true, Name = "CoastRemote" };
                _thread.Start();
                _startRetries = 0;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[CoastRemote] listen failed: " + e.Message + (_startRetries < 60 ? " — 1초 뒤 재시도" : ""));
                _listener = null;
                if (_startRetries++ < 60)
                {
                    double at = EditorApplication.timeSinceStartup + 1.0;
                    EditorApplication.CallbackFunction retry = null;
                    retry = () => { if (EditorApplication.timeSinceStartup < at) return; EditorApplication.update -= retry; if (_listener == null) Start(); };
                    EditorApplication.update += retry;
                }
            }
        }

        static readonly List<TcpClient> _clients = new List<TcpClient>();

        static void Stop()
        {
            try { _listener?.Stop(); } catch { }
            _listener = null;
            lock (_clients)
            {
                foreach (var c in _clients) { try { c.Close(); } catch { } }
                _clients.Clear();
            }
            lock (_lock) { foreach (var r in _queue) r.done.Set(); _queue.Clear(); }
        }

        static void Accept()
        {
            while (_listener != null)
            {
                TcpClient c;
                try { c = _listener.AcceptTcpClient(); } catch { break; }
                lock (_clients) _clients.Add(c);
                ThreadPool.QueueUserWorkItem(_ => Serve(c));
            }
        }

        static void Serve(TcpClient c)
        {
            try
            {
                using (c)
                using (var s = c.GetStream())
                using (var r = new StreamReader(s, Encoding.UTF8))
                using (var w = new StreamWriter(s, new UTF8Encoding(false)) { AutoFlush = true })
                {
                    string line = r.ReadLine();
                    if (string.IsNullOrEmpty(line)) return;
                    var req = new Req { line = line };
                    lock (_lock) _queue.Enqueue(req);
                    if (!req.done.WaitOne(20000)) req.reply = J("error", "timeout (editor busy/compiling?)");
                    w.WriteLine(req.reply ?? J("error", "reload"));
                }
            }
            catch { }
            finally { lock (_clients) _clients.Remove(c); }
        }

        static void Pump()
        {
            Req req = null;
            lock (_lock) if (_queue.Count > 0) req = _queue.Dequeue();
            if (req == null) return;
            try { req.reply = Handle(req.line.Trim()); }
            catch (Exception e) { req.reply = J("error", e.GetType().Name + ": " + e.Message); }
            req.done.Set();
        }

        static string J(string k, string v) => "{\"" + k + "\":\"" + Esc(v) + "\"}";
        static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");

        static string Status()
        {
            string errs, logs;
            lock (_lock) { errs = string.Join("\n", _compileErrors); logs = string.Join("\n", _log.GetRange(Math.Max(0, _log.Count - 30), Math.Min(30, _log.Count))); }
            return "{\"ok\":true,\"compiling\":" + (_compiling || EditorApplication.isCompiling ? "true" : "false") +
                   ",\"updating\":" + (EditorApplication.isUpdating ? "true" : "false") +
                   ",\"playing\":" + (EditorApplication.isPlaying ? "true" : "false") +
                   ",\"paused\":" + (EditorApplication.isPaused ? "true" : "false") +
                   ",\"scene\":\"" + Esc(SceneManager.GetActiveScene().name) + "\"" +
                   ",\"compileErrors\":\"" + Esc(errs) + "\",\"log\":\"" + Esc(logs) + "\"}";
        }

        static string Handle(string line)
        {
            var parts = line.Split(new[] { ' ' }, 2);
            string cmd = parts[0].ToLowerInvariant();
            string arg = parts.Length > 1 ? parts[1].Trim() : "";
            switch (cmd)
            {
                case "ping": return "{\"ok\":true,\"unity\":\"" + Application.unityVersion + "\",\"project\":\"" + Esc(Application.productName) + "\"}";
                case "status": return Status();
                case "refresh": AssetDatabase.Refresh(); return J("ok", "refresh requested");
                case "play":
                    if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
                    return J("ok", "play");
                case "stop":
                    if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
                    return J("ok", "stop");
                case "pause": EditorApplication.isPaused = !EditorApplication.isPaused; return J("ok", "paused=" + EditorApplication.isPaused);
                case "menu": return J(EditorApplication.ExecuteMenuItem(arg) ? "ok" : "error", "menu " + arg);
                case "scene":
                    if (EditorApplication.isPlaying) return J("error", "stop play first");
                    {
                        var guids = AssetDatabase.FindAssets("t:Scene " + arg);
                        if (guids.Length == 0) return J("error", "scene not found: " + arg);
                        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
                        return J("ok", path);
                    }
                case "shot":
                    {
                        string name = string.IsNullOrEmpty(arg) ? "shot_" + DateTime.Now.ToString("HHmmss") : arg;
                        Directory.CreateDirectory("Tools/_shots");
                        string path = Path.GetFullPath("Tools/_shots/" + name + ".png");
                        if (File.Exists(path)) File.Delete(path);
                        ScreenCapture.CaptureScreenshot(path, 1);
                        // 게임 뷰가 다음 프레임을 그려야 파일이 나온다 — 플레이 중이 아니면 한 번 강제 리페인트
                        if (!EditorApplication.isPlaying) { var gv = GetGameView(); gv?.Repaint(); }
                        return "{\"ok\":true,\"path\":\"" + Esc(path) + "\"}";
                    }
                case "log":
                    {
                        int n = 40; int.TryParse(arg, out n); if (n <= 0) n = 40;
                        string logs; lock (_lock) logs = string.Join("\n", _log.GetRange(Math.Max(0, _log.Count - n), Math.Min(n, _log.Count)));
                        return J("log", logs);
                    }
                case "clearlog": lock (_lock) _log.Clear(); return J("ok", "cleared");
                case "warp": Play(); StageManager.Instance?.DebugWarpToFinish(); return J("ok", "warp");
                case "clear": Play(); StageManager.Instance?.DebugClear(); return J("ok", "clear");
                case "retry": Play(); StageManager.Instance?.RetryCurrent(); return J("ok", "retry");
                case "hit": Play(); UnityEngine.Object.FindAnyObjectByType<PlayerController>()?.SoftHit(); return J("ok", "hit");
                case "lane": Play(); Inject(int.Parse(arg), false, false); return J("ok", "lane " + arg);
                case "jump": Play(); Inject(0, true, false); return J("ok", "jump");
                case "crouch": Play(); Inject(0, false, true); return J("ok", "crouch");
                case "tap":
                    {
                        Play();
                        var xy = arg.Split(' ');
                        float nx = float.Parse(xy[0], System.Globalization.CultureInfo.InvariantCulture), ny = float.Parse(xy[1], System.Globalization.CultureInfo.InvariantCulture);
                        return J("ok", Tap(nx, ny));
                    }
                case "key":
                    CoastRemoteKeys.Press(arg); return J("ok", "key " + arg);
                case "timescale": Time.timeScale = float.Parse(arg, System.Globalization.CultureInfo.InvariantCulture); return J("ok", "timescale " + arg);
                case "gameview":
                    { var gv = GetGameView(); gv?.Focus(); return J("ok", gv != null ? "focused" : "no gameview"); }
                default: return J("error", "unknown command: " + cmd);
            }
        }

        static void Play() { if (!EditorApplication.isPlaying) throw new InvalidOperationException("not in play mode"); }

        static void Inject(int lane, bool jump, bool crouch)
        {
            var inp = UnityEngine.Object.FindAnyObjectByType<MobileSwipeInput>();
            if (inp == null) throw new InvalidOperationException("no MobileSwipeInput");
            inp.Inject(lane, jump, crouch);
        }

        /// 정규화 좌표(0..1, 왼쪽 아래 원점)로 UI 클릭 — 게임 뷰 크기 기준.
        static string Tap(float nx, float ny)
        {
            var es = EventSystem.current; if (es == null) return "no EventSystem";
            var pos = new Vector2(nx * Screen.width, ny * Screen.height);
            var pd = new PointerEventData(es) { position = pos, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>();
            es.RaycastAll(pd, hits);
            foreach (var h in hits)
            {
                var target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(h.gameObject);
                if (target == null) continue;
                pd.pointerPress = target; pd.pointerPressRaycast = h; pd.pointerCurrentRaycast = h;
                ExecuteEvents.Execute(target, pd, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(target, pd, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(target, pd, ExecuteEvents.pointerClickHandler);
                return "clicked " + target.name;
            }
            return "no handler at " + pos + " (screen " + Screen.width + "x" + Screen.height + ")";
        }

        static EditorWindow GetGameView()
        {
            var t = Type.GetType("UnityEditor.GameView,UnityEditor");
            return t != null ? EditorWindow.GetWindow(t, false, null, false) : null;
        }
    }
}
#endif
