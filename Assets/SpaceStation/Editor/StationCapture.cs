using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SecondWind.SpaceStation.Editor
{
    /// <summary>Captures the native UI Toolkit panel, without desktop input automation.</summary>
    [InitializeOnLoad]
    public static class StationCapture
    {
        static double nextFrame;
        static RenderTexture target;
        static int phase;
        static StationCapture() { EditorApplication.update += Tick; }
        public static void Start()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Capture requires starting from edit mode.");
            SessionState.SetBool("Station.CapturePending", true); SessionState.SetFloat("Station.CaptureStarted", (float)EditorApplication.timeSinceStartup); EditorApplication.isPlaying = true;
        }
        static void Tick()
        {
            if (!SessionState.GetBool("Station.CapturePending", false) || !EditorApplication.isPlaying) return;
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat("Station.CaptureStarted", 0) > 90)
            { SessionState.SetBool("Station.CapturePending", false); Debug.LogError("Station UI capture timed out."); if (Application.isBatchMode) EditorApplication.Exit(1); return; }
            var app = UnityEngine.Object.FindFirstObjectByType<StationApp>(); if (app == null || app.Document == null) return;
            if (target == null)
            {
                target = new RenderTexture(390, 844, 24, RenderTextureFormat.ARGB32); target.Create();
                app.Document.panelSettings.targetTexture = target; phase = 0; app.SetEditorPreview(phase); nextFrame = EditorApplication.timeSinceStartup + 3; return;
            }
            if (EditorApplication.timeSinceStartup < nextFrame) return;
            try
            {
                var previous = RenderTexture.active; RenderTexture.active = target;
                var texture = new Texture2D(390, 844, TextureFormat.RGB24, false); texture.ReadPixels(new Rect(0,0,390,844),0,0); texture.Apply();
                Directory.CreateDirectory("output"); File.WriteAllBytes("output/Unity-" + new[] { "Home", "Game", "Result" }[phase] + ".png", texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture); RenderTexture.active = previous;
                if (phase < 2) { phase++; app.SetEditorPreview(phase); nextFrame = EditorApplication.timeSinceStartup + 2; return; }
                app.Document.panelSettings.targetTexture = null; target.Release(); UnityEngine.Object.DestroyImmediate(target); target = null;
                File.WriteAllText("output/Unity-UI-Smoke.txt", "Native Unity UI Toolkit home, game and result panels rendered at 390 x 844.\n" + app.Document.rootVisualElement.childCount + " root elements.");
                SessionState.SetBool("Station.CapturePending", false); EditorApplication.isPlaying = false;
                if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(0);
            }
            catch (Exception e) { SessionState.SetBool("Station.CapturePending", false); Debug.LogException(e); EditorApplication.isPlaying = false; if (Application.isBatchMode) EditorApplication.Exit(1); }
        }
    }
}
