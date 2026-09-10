using System;
using System.IO;
using UnityEngine;

namespace SecondWind.SpaceStation
{
    public static class StationSave
    {
        public static string DirectoryPath { get { return Path.Combine(Application.persistentDataPath, "SpaceStation"); } }
        public static string SavePath { get { return Path.Combine(DirectoryPath, "run.json"); } }
        public static StationState Load(out bool recovered)
        { return LoadAt(SavePath, out recovered); }
        public static StationState LoadAt(string destination, out bool recovered)
        {
            recovered = false;
            foreach (string path in new[] { destination, destination + ".backup" })
            {
                try { if (!File.Exists(path)) continue; var s = StationRules.Restore(File.ReadAllText(path)); if (s != null) { recovered = path.EndsWith(".backup"); return s; } }
                catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
            return null;
        }
        public static bool Write(StationState state, out string error)
        { return WriteAt(SavePath, state, out error); }
        public static bool WriteAt(string destination, StationState state, out string error)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                string temporary = destination + ".pending";
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                { var bytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(state)); stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                if (File.Exists(destination)) File.Copy(destination, destination + ".backup", true);
                // Atomic rename preserves either the old or complete new action boundary.
                if (File.Exists(destination)) File.Replace(temporary, destination, null); else File.Move(temporary, destination);
                error = null; return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException)
            { error = "저장 공간에 기록하지 못했습니다. 공간을 확인해 주세요."; Debug.LogWarning("Station save failed: " + e.GetType().Name); return false; }
        }
    }
}
