using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace SymmetryBreakStudio.TastyGrassShader.Editor
{
    public static class UpdateHandler
    {
        public const string ThisVersion = "2.5.2";
        const string UpdateUrl =
            "https://raw.githubusercontent.com/SymmetryBreakStudio/TastyGrassShader/refs/heads/main/version-unity.txt";

        // Attempt limit, because of https://github.com/SymmetryBreakStudio/TastyGrassShader/issues/51
        private static int _attempts = 0;
        private const float AttemptPause = 0.5f;
        private const int MaxAttempts = 25;
        
        public static string NewVersionStr { get; private set; } = string.Empty;
        static bool _newVersionAvailable;
        static IEnumerator _downloader;
        static DownloadState _dlState = DownloadState.None;
        static string _downloadErrorMessage = string.Empty;

        [InitializeOnLoadMethod]
        static void CheckForUpdate()
        {
            EditorApplication.update += EditorUpdate;
        }

        static void EditorUpdate()
        {
            switch (_dlState)
            {
                case DownloadState.None:
                    _dlState = DownloadState.Running;
                    _downloader = GetUpdateFile();
                    break;
                case DownloadState.Running:
                    _downloader.MoveNext();
                    break;
                case DownloadState.FailedConnection:
                case DownloadState.FailedInternally:
                    // Unity bugs out a bit when running the web request just after loading,
                    // so we need to hammer it a few times...
                    _dlState = DownloadState.Running;
                    _downloader.Reset();
                    _downloader = GetUpdateFile();
                    break;
                case DownloadState.Success:
                case DownloadState.TooManyAttempts:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static void DisplayUpdateBox()
        {
            if (_newVersionAvailable)
            {
                EditorGUILayout.HelpBox(
                    $"A new version of Tasty Grass Shader is available.\n\nNew: {NewVersionStr}\nThis: {ThisVersion}\n\nPlease update via the Unity Package Manager.\nRemember to backup your project before updating and delete any previous installations.",
                    MessageType.Warning);
            }

            if (_dlState == DownloadState.FailedConnection)
            {
                EditorGUILayout.HelpBox(
                    $"Unable to check for updates.\nMake sure that you are connected to the internet and this is most recent version of Tasty Grass Shader.\n\nError: {_downloadErrorMessage}",
                    MessageType.Warning);
            }

            if (_dlState == DownloadState.TooManyAttempts)
            {
                EditorGUILayout.HelpBox(
                    $"Unable to check for updates.\nToo many attempts to connect failed. Please check your internet connection. {_attempts}\n\nError: {_downloadErrorMessage}",
                    MessageType.Warning);
            }
            
            EditorGUILayout.Space();
        }

        static IEnumerator GetUpdateFile()
        {
            _attempts++;
            if (_attempts < MaxAttempts)
            {
                yield return new WaitForSecondsRealtime(AttemptPause);
                UnityWebRequest www = UnityWebRequest.Get(UpdateUrl);
                www.timeout = 30;
                yield return www.SendWebRequest();
                while( www.isDone == false )
                    yield return null;
                if (www.result != UnityWebRequest.Result.Success)
                {
                    _dlState = string.IsNullOrEmpty(www.error)
                        ? DownloadState.FailedInternally
                        : DownloadState.FailedConnection;
                    _downloadErrorMessage = www.error;
                }
                else
                {
                    NewVersionStr = www.downloadHandler.text.Trim();
                    _newVersionAvailable = NewVersionStr != ThisVersion;
                    _dlState = DownloadState.Success;
                }
            }
            else
            {
                _dlState = DownloadState.TooManyAttempts;
            }
        }

        enum DownloadState
        {
            None,
            Running,
            TooManyAttempts,
            FailedInternally,
            FailedConnection,
            Success
        }
    }
}