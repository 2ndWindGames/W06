using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace SecondWind.SpaceStation.Editor
{
    [InitializeOnLoad]
    public static class StationProject
    {
        const string ScenePath = "Assets/SpaceStation/Scenes/Station.unity";
        const string LogoPath = "Assets/SpaceStation/Branding/SecondWindGamesLogo.png";
        const string PanelPath = "Assets/SpaceStation/Resources/Station/StationPanel.asset";
        static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        static double nextPoll;
        static bool processing;
        [Serializable] class Command { public string action; }
        [Serializable] class Reply { public bool ok; public string action, message; }
        static StationProject()
        {
            EditorApplication.delayCall += () => { if (!AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath)) Setup(); };
            EditorApplication.update += Poll;
        }
        [MenuItem("Space Station/1. Apply W01 Android Settings")]
        public static void Setup()
        {
            PlayerSettings.companyName = "SecondWindGames";
            PlayerSettings.productName = "우주 정거장 보급소";
            PlayerSettings.bundleVersion = "0.3.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.secondwindgames.spacestation");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.Android.bundleVersionCode = 3;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.Android.minifyRelease = true;
            PlayerSettings.Android.minifyDebug = false;
            PlayerSettings.Android.splitApplicationBinary = false;
            PlayerSettings.Android.startInFullscreen = true;
            PlayerSettings.Android.renderOutsideSafeArea = false;
            PlayerSettings.Android.optimizedFramePacing = true;
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.Android.forceSDCardPermission = false;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.buildAppBundle = true;
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.backgroundColor = Color.white;
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Static;
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.DarkOnLight;
            var logo = AssetDatabase.LoadAllAssetsAtPath(LogoPath).OfType<Sprite>().FirstOrDefault();
            if (logo != null) PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(2f, logo) };
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SpaceStation/Resources/Station/Art/AppIcon.png");
            if (icon != null) PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });
            string referenceProject = Environment.GetEnvironmentVariable("STATION_W01_PROJECT") ?? Path.GetFullPath(Path.Combine(ProjectRoot, "../W01"));
            string keystore = Path.Combine(referenceProject, "user.keystore");
            if (File.Exists(keystore)) { PlayerSettings.Android.useCustomKeystore = true; PlayerSettings.Android.keystoreName = keystore; PlayerSettings.Android.keyaliasName = "secondwindgames"; }
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>(); panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panel.referenceResolution = new Vector2Int(390,844); panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight; panel.match = 0;
                AssetDatabase.CreateAsset(panel, PanelPath);
            }
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var active = EditorSceneManager.GetActiveScene();
                bool single = Application.isBatchMode || (!active.isDirty && string.IsNullOrEmpty(active.path));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, single ? NewSceneMode.Single : NewSceneMode.Additive);
                EditorSceneManager.SaveScene(scene, ScenePath);
                if (!single) { EditorSceneManager.CloseScene(scene, true); if (active.IsValid()) EditorSceneManager.SetActiveScene(active); }
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets(); WriteSettingsReport(); Debug.Log("Space Station: W01 Android and splash settings applied.");
        }
        static void WriteSettingsReport()
        {
            Directory.CreateDirectory("output");
            File.WriteAllText("output/Unity-Settings.txt", "Project: " + ProjectRoot + "\nUnity: " + Application.unityVersion +
                "\nIdentifier: " + PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) +
                "\nBackend: " + PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) + "\nArchitecture: " + PlayerSettings.Android.targetArchitectures +
                "\nMin SDK: " + (int)PlayerSettings.Android.minSdkVersion + "\nTarget SDK: " + PlayerSettings.Android.targetSdkVersion +
                "\nOrientation: " + PlayerSettings.defaultInterfaceOrientation + "\nSplash: white, company logo 2 seconds, Unity logo hidden" +
                "\nMinify release: " + PlayerSettings.Android.minifyRelease + "\nDevelopment: " + EditorUserBuildSettings.development +
                "\nRelease profile: AAB / LZ4HC / W01 keystore reference\nPrototype APK: temporary debug signing, settings restored afterwards\nScene: " + ScenePath);
        }
        [MenuItem("Space Station/2. Open Game Scene")]
        public static void OpenScene()
        { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath); }
        [MenuItem("Space Station/3. Verify Rules")]
        public static void Verify()
        { Directory.CreateDirectory("output"); string result = StationChecks.Run(); File.WriteAllText("output/Unity-Rule-Tests.txt", result); Debug.Log(result); }
        [MenuItem("Space Station/4. Build Prototype APK")]
        public static void BuildApk() { Build(false); }
        [MenuItem("Space Station/5. Build Release AAB")]
        public static void BuildAab() { Build(true); }
        public static void BatchVerify() { Setup(); Verify(); }
        public static void BatchBuild() { Setup(); BuildApk(); }
        static void Build(bool bundle)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before building.");
            Verify();
            bool oldBundle = EditorUserBuildSettings.buildAppBundle, oldKey = PlayerSettings.Android.useCustomKeystore;
            try
            {
                EditorUserBuildSettings.buildAppBundle = bundle;
                if (!bundle) PlayerSettings.Android.useCustomKeystore = false;
                var options = new BuildPlayerOptions { scenes = new[] { ScenePath }, target = BuildTarget.Android,
                    locationPathName = "output/SpaceStation-Unity-v0.3." + (bundle ? "aab" : "apk"), options = BuildOptions.CompressWithLz4HC };
                var report = BuildPipeline.BuildPlayer(options);
                File.WriteAllText("output/Unity-Build-Result.txt", report.summary.result + "\n" + report.summary.totalSize + " bytes\n" + report.summary.totalErrors + " errors\n" + report.summary.totalTime);
                if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Unity Android build failed: " + report.summary.totalErrors + " errors. See Editor.log and output/Unity-Build-Result.txt.");
            }
            finally { EditorUserBuildSettings.buildAppBundle = oldBundle; PlayerSettings.Android.useCustomKeystore = oldKey; AssetDatabase.SaveAssets(); }
        }
        static void Poll()
        {
            if (processing || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 1;
            string path = Path.Combine(ProjectRoot, ".station-command.json"); if (!File.Exists(path)) return;
            processing = true; var reply = new Reply();
            try
            {
                var command = JsonUtility.FromJson<Command>(File.ReadAllText(path)); File.Delete(path); reply.action = command.action;
                if (command.action == "setup") Setup();
                else if (command.action == "verify") Verify();
                else if (command.action == "build-apk") BuildApk();
                else if (command.action == "capture") StationCapture.Start();
                else throw new ArgumentException("Unknown Station command");
                reply.ok = true; reply.message = "Complete";
            }
            catch (Exception e) { reply.ok = false; reply.message = e.ToString(); Debug.LogException(e); }
            finally { processing = false; File.WriteAllText(Path.Combine(ProjectRoot, ".station-response.json"), JsonUtility.ToJson(reply, true)); }
        }
    }
}
