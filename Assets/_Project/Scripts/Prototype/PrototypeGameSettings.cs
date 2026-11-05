using System;
using System.IO;
using UnityEngine;

namespace BackpackAutoBattle.Prototype
{
    [Serializable]
    internal sealed class PrototypeGameSettings
    {
        public int master = 100;
        public int music = 100;
        public int effects = 100;
        public int width = 1280;
        public int height = 720;
        public bool fullScreen;
        public float battleSpeed = PrototypeBattleController.DefaultPlaybackSpeed;
        public bool damageNumbers = true;

        public static string SavePath()
        {
            string path = Environment.GetEnvironmentVariable("BACKPACK_SETTINGS_PATH");
            if (!string.IsNullOrWhiteSpace(path)) return Path.GetFullPath(path);
            string profile = Environment.GetEnvironmentVariable("BACKPACK_PROFILE_PATH");
            return string.IsNullOrWhiteSpace(profile)
                ? Path.Combine(Application.persistentDataPath, "settings.json")
                : Path.GetFullPath(profile) + ".settings.json";
        }

        public static PrototypeGameSettings Load(string path)
        {
            var settings = new PrototypeGameSettings();
            try
            {
                if (File.Exists(path)) JsonUtility.FromJsonOverwrite(File.ReadAllText(path), settings);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
            {
                settings = new PrototypeGameSettings();
            }
            settings.master = Mathf.Clamp(settings.master, 0, 100);
            settings.music = Mathf.Clamp(settings.music, 0, 100);
            settings.effects = Mathf.Clamp(settings.effects, 0, 100);
            if (!PrototypeBattleController.SupportsSpeed(settings.battleSpeed))
                settings.battleSpeed = PrototypeBattleController.DefaultPlaybackSpeed;
            return settings;
        }

        public bool TrySave(string path)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temporary = path + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(this, true));
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
                return true;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
