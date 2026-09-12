using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SecondWind.SpaceStation.Editor
{
    // Editor-only capture harness. Uses the real runtime views and legal game states.
    // Install in an isolated project copy under Assets/SpaceStation/Editor to regenerate.
    [InitializeOnLoad]
    public static class StoreCapture
    {
        const int Width = 1080, Height = 1920;
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        static readonly string[] Names = { "01-supply", "02-facilities", "03-lumi-ai", "04-ship-order", "05-mission-result", "06-home" };
        static RenderTexture target;
        static int phase;
        static double next;
        static StoreCapture() { EditorApplication.update += Tick; }
        public static void Start()
        {
            SessionState.SetBool("Station.StoreCapture", true);
            SessionState.SetFloat("Station.StoreCaptureStart", (float)EditorApplication.timeSinceStartup);
            EditorApplication.isPlaying = true;
        }
        static void Set(StationApp app, string name, object value) { typeof(StationApp).GetField(name, Private).SetValue(app, value); }
        static object Call(StationApp app, string name, params object[] args) { return typeof(StationApp).GetMethod(name, Private).Invoke(app, args); }
        static StationState Apply(StationState state, StationAction action)
        {
            if (!StationRules.TryApply(state, action, out var nextState, out string error)) throw new Exception(error);
            return nextState;
        }
        static void Prepare(StationApp app)
        {
            app.enabled = false; // Stop timers and input while capturing; UI Toolkit still renders.
            Call(app, "CloseSheet", false);
            Set(app, "lockedUntil", 0f);
            Set(app, "aiAt", -1f);
            app.SetEditorPreview(1);
            if (phase == 0) Call(app, "Select", new StationAction("supply", "A-0"));
            else if (phase == 1)
            {
                var state = Apply(Apply(StationRules.NewGame(seed: 1234), new StationAction("build", "solar")), new StationAction("collect", null, 0));
                Set(app, "state", state); Call(app, "Render"); Call(app, "BuildSheet");
            }
            else if (phase == 2)
            {
                var state = StationRules.NewGame(true, "normal", true, 8842, first: 0);
                for (int i = 0; i < 10; i++) state = Apply(state, StationRules.ChooseAI(state));
                if (state.actor == 1) state = Apply(state, StationRules.ChooseAI(state));
                Set(app, "state", state); Call(app, "Render");
                if (state.actor == 0) Call(app, "Select", StationRules.ChooseAI(state));
            }
            else if (phase == 3) Call(app, "OrderDetail", "A-0");
            else if (phase == 4) app.SetEditorPreview(2);
            else app.SetEditorPreview(0);
        }
        static void Tick()
        {
            if (!SessionState.GetBool("Station.StoreCapture", false)) return;
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat("Station.StoreCaptureStart", 0) > 150)
            { Finish(false, "Store capture timed out"); return; }
            if (!EditorApplication.isPlaying) return;
            var app = UnityEngine.Object.FindFirstObjectByType<StationApp>();
            if (app == null || app.Document == null) return;
            try
            {
                if (target == null)
                {
                    target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32); target.Create();
                    app.Document.panelSettings.targetTexture = target;
                    phase = 0; Prepare(app); next = EditorApplication.timeSinceStartup + 4; return;
                }
                if (EditorApplication.timeSinceStartup < next) return;
                var previous = RenderTexture.active; RenderTexture.active = target;
                var pixels = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); pixels.Apply();
                string folder = Path.Combine("output", "StoreScreenshots"); Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder, Names[phase] + ".png"), pixels.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(pixels); RenderTexture.active = previous;
                Debug.Log("Store capture: " + Names[phase] + " 1080x1920 RGB");
                phase++;
                if (phase < Names.Length) { Prepare(app); next = EditorApplication.timeSinceStartup + 3; return; }
                app.Document.panelSettings.targetTexture = null;
                target.Release(); UnityEngine.Object.DestroyImmediate(target); target = null;
                Finish(true, "6 native UI Toolkit captures at 1080x1920; real runtime UI; legal game states; no persistence writes.");
            }
            catch (Exception e) { Finish(false, e.ToString()); }
        }
        static void Finish(bool success, string message)
        {
            SessionState.SetBool("Station.StoreCapture", false);
            Directory.CreateDirectory("output/StoreScreenshots");
            File.WriteAllText("output/StoreScreenshots/Capture-report.txt", (success ? "PASS\n" : "FAIL\n") + message);
            if (!success) Debug.LogError(message);
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => EditorApplication.Exit(success ? 0 : 1);
        }
    }
}
