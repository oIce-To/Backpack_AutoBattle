using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BackpackAutoBattle.Progression;
using UnityEditor;
using UnityEngine;

namespace BackpackAutoBattle.Editor
{
    public static class PlayerProgressResetTool
    {
        private const string MenuPath = "Tools/Backpack AutoBattle/Reset Player Progress";
        private const string Title = "플레이어 진행도 초기화";

        [MenuItem(MenuPath, true)]
        private static bool CanReset() => !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem(MenuPath)]
        private static void Reset()
        {
            if (!CanReset()) return;
            try
            {
                var store = new PlayerProgressStore(PlayerProgressStore.ResolveLocalPath());
                if (!ProgressFiles(store.FilePath).Any(File.Exists))
                {
                    EditorUtility.DisplayDialog(Title, "초기화할 진행도 파일이 없습니다.", "확인");
                    return;
                }

                if (!EditorUtility.DisplayDialog(Title,
                    "실행 중인 게임 빌드를 종료한 뒤 진행해 주세요.\n\n"
                    + "스테이지 기록, 캐릭터 레벨, 경험치와 장비를 초기화합니다. 환경설정은 유지됩니다.\n"
                    + "기존 진행도 파일은 백업 폴더로 옮깁니다.\n\n" + store.FilePath,
                    "백업 후 초기화", "취소")) return;

                string backup = ArchiveProgress(store);
                string message = backup == null ? "초기화할 진행도 파일이 없습니다."
                    : $"진행도를 초기화했습니다. 다음 Play부터 새 진행도로 시작합니다.\n\n백업 폴더\n{backup}";
                EditorUtility.DisplayDialog(Title, message, "확인");
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
            {
                Debug.LogException(error);
                EditorUtility.DisplayDialog(Title, $"진행도를 초기화하지 못했습니다.\n\n{error.Message}", "확인");
            }
        }

        public static string ArchiveProgress(PlayerProgressStore store)
        {
            string[] files = ProgressFiles(store.FilePath).Where(File.Exists).ToArray();
            if (files.Length == 0) return null;

            string backup = Path.Combine(Path.GetDirectoryName(store.FilePath),
                $"progress-reset-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(backup);
            var moved = new Stack<(string original, string archived)>();
            try
            {
                foreach (string file in files)
                {
                    string archived = Path.Combine(backup, Path.GetFileName(file));
                    File.Move(file, archived);
                    moved.Push((file, archived));
                }
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                var errors = new List<Exception> { error };
                foreach (var (original, archived) in moved)
                {
                    try { File.Move(archived, original); }
                    catch (Exception rollbackError) when (rollbackError is IOException or UnauthorizedAccessException)
                    { errors.Add(rollbackError); }
                }

                if (errors.Count > 1)
                    throw new IOException($"일부 파일을 원래 위치로 되돌리지 못했습니다. 백업 폴더를 확인해 주세요.\n{backup}",
                        new AggregateException(errors));
                throw;
            }

            return backup;
        }

        // Move the active profile last so a failure on a sidecar cannot leave it missing.
        private static string[] ProgressFiles(string path) => new[] { path + ".tmp", path + ".bak", path };
    }
}
