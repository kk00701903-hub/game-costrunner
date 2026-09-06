using System;
using System.IO;
using UnityEngine;

namespace CoastRun
{
    /// 플러그인 없는 안드로이드 공유 — PNG를 갤러리(MediaStore, Pictures/JEJU)에 넣고 ACTION_SEND 공유 시트를 연다.
    /// API 29+ 는 MediaStore insert(권한 불필요), 그 아래(min 26)는 외부 Pictures 폴더 + file:// (StrictMode 해제).
    /// 안드로이드가 아니면 파일만 남기고 false.
    public static class NativeShareLite
    {
        public const string AlbumFolder = "JEJU";

        /// 갤러리에 저장하고 공유 시트를 연다. 반환: 갤러리 저장 성공 여부.
        public static bool SharePng(byte[] png, string fileName, string title, string text)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                int sdk = version.GetStatic<int>("SDK_INT");
                AndroidJavaObject uri = sdk >= 29 ? InsertMediaStore(activity, png, fileName) : InsertLegacy(activity, png, fileName);
                if (uri == null) return false;
                SendIntent(activity, uri, title, text);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Share] failed: " + e.Message);
                return false;
            }
#else
            return false;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject InsertMediaStore(AndroidJavaObject activity, byte[] png, string fileName)
        {
            using var resolver = activity.Call<AndroidJavaObject>("getContentResolver");
            using var values = new AndroidJavaObject("android.content.ContentValues");
            values.Call("put", "_display_name", fileName);
            values.Call("put", "mime_type", "image/png");
            values.Call("put", "relative_path", "Pictures/" + AlbumFolder);
            values.Call("put", "is_pending", 1);
            using var images = new AndroidJavaClass("android.provider.MediaStore$Images$Media");
            using var external = images.GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI");
            var uri = resolver.Call<AndroidJavaObject>("insert", external, values);
            if (uri == null) return null;
            using (var os = resolver.Call<AndroidJavaObject>("openOutputStream", uri))
            {
                os.Call("write", png);
                os.Call("flush");
                os.Call("close");
            }
            using var done = new AndroidJavaObject("android.content.ContentValues");
            done.Call("put", "is_pending", 0);
            resolver.Call<int>("update", uri, done, null, null);
            return uri;
        }

        static AndroidJavaObject InsertLegacy(AndroidJavaObject activity, byte[] png, string fileName)
        {
            using var env = new AndroidJavaClass("android.os.Environment");
            string dirType = env.GetStatic<string>("DIRECTORY_PICTURES");
            using var dirObj = env.CallStatic<AndroidJavaObject>("getExternalStoragePublicDirectory", dirType);
            string dir = Path.Combine(dirObj.Call<string>("getAbsolutePath"), AlbumFolder);
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, fileName);
            File.WriteAllBytes(path, png);
            // 갤러리 스캔
            using var scanner = new AndroidJavaClass("android.media.MediaScannerConnection");
            scanner.CallStatic("scanFile", activity, new[] { path }, new[] { "image/png" }, null);
            // file:// 공유 허용 (FileProvider 없이)
            using var builder = new AndroidJavaObject("android.os.StrictMode$VmPolicy$Builder");
            using var policy = builder.Call<AndroidJavaObject>("build");
            using var strict = new AndroidJavaClass("android.os.StrictMode");
            strict.CallStatic("setVmPolicy", policy);
            using var file = new AndroidJavaObject("java.io.File", path);
            using var uriCls = new AndroidJavaClass("android.net.Uri");
            return uriCls.CallStatic<AndroidJavaObject>("fromFile", file);
        }

        static void SendIntent(AndroidJavaObject activity, AndroidJavaObject uri, string title, string text)
        {
            using var intentCls = new AndroidJavaClass("android.content.Intent");
            using var intent = new AndroidJavaObject("android.content.Intent", intentCls.GetStatic<string>("ACTION_SEND"));
            intent.Call<AndroidJavaObject>("setType", "image/png");
            intent.Call<AndroidJavaObject>("putExtra", intentCls.GetStatic<string>("EXTRA_STREAM"), uri);
            if (!string.IsNullOrEmpty(text)) intent.Call<AndroidJavaObject>("putExtra", intentCls.GetStatic<string>("EXTRA_TEXT"), text);
            intent.Call<AndroidJavaObject>("addFlags", intentCls.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION"));
            using var chooser = intentCls.CallStatic<AndroidJavaObject>("createChooser", intent, title);
            chooser.Call<AndroidJavaObject>("addFlags", intentCls.GetStatic<int>("FLAG_ACTIVITY_NEW_TASK"));
            activity.Call("startActivity", chooser);
        }
#endif
    }
}
