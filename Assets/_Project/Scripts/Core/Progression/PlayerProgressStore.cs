using System;
using System.IO;
using UnityEngine;

namespace BackpackAutoBattle.Progression
{
    /// <summary>Versioned local JSON with atomic replacement and a last-good backup.</summary>
    public sealed class PlayerProgressStore
    {
        public string FilePath { get; }
        public bool CanWrite { get; private set; } = true;
        private bool _recoveredBackup;
        public PlayerProgressStore(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A save path is required.", nameof(path));
            FilePath = Path.GetFullPath(path);
        }

        public static string ResolveLocalPath()
        {
            string path = Environment.GetEnvironmentVariable("BACKPACK_PROFILE_PATH");
            return string.IsNullOrWhiteSpace(path) ? Path.Combine(Application.persistentDataPath, "player-progress.json") : path;
        }

        public PlayerProgress Load(out string warning)
        {
            warning = null; CanWrite = true; _recoveredBackup = false;
            if (!File.Exists(FilePath) && !File.Exists(FilePath + ".bak")) return new PlayerProgress();
            try { return Read(FilePath); }
            catch (NotSupportedException)
            {
                CanWrite = false;
                warning = "더 최신 버전의 진행도 파일입니다. 기존 파일을 보존합니다.";
                return new PlayerProgress();
            }
            catch (Exception exception) when (IsReadFailure(exception))
            {
                try
                {
                    var recovered = Read(FilePath + ".bak"); _recoveredBackup = true;
                    warning = "백업 파일에서 진행도를 복구했습니다.";
                    return recovered;
                }
                catch (Exception backupException) when (IsReadFailure(backupException) || backupException is NotSupportedException)
                {
                    // Preserve unreadable files; do not silently replace the user's old progress.
                    CanWrite = false;
                    warning = "진행도 파일을 읽을 수 없습니다. 기존 파일을 보존합니다.";
                    return new PlayerProgress();
                }
            }
        }
        public bool TrySave(PlayerProgress progress, out string error)
        {
            error = null;
            if (!CanWrite) { error = "기존 진행도 파일을 보호하기 위해 저장하지 못했습니다."; return false; }
            try
            {
                string json = JsonUtility.ToJson(progress.Export(), true);
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                string temporary = FilePath + ".tmp";
                File.WriteAllText(temporary, json);
                if (File.Exists(FilePath)) File.Replace(temporary, FilePath, _recoveredBackup ? null : FilePath + ".bak");
                else File.Move(temporary, FilePath);
                _recoveredBackup = false;
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            { error = "진행도를 저장하지 못했습니다. 저장 경로를 확인해주세요."; return false; }
        }
        private static PlayerProgress Read(string path) => PlayerProgress.FromData(JsonUtility.FromJson<ProfileSaveData>(File.ReadAllText(path)));
        private static bool IsReadFailure(Exception exception) => exception is IOException || exception is UnauthorizedAccessException
            || exception is ArgumentException || exception is FormatException;
    }
}
