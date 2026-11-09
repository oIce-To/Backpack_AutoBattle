using System;
using BackpackAutoBattle.Progression;
using BackpackAutoBattle.Prototype;
using UnityEditor;
using UnityEngine;

namespace BackpackAutoBattle.Editor
{
    public static class MineTicketRefillTool
    {
        private const string MenuPath = "Tools/Backpack AutoBattle/Refill Mine Tickets";

        [MenuItem(MenuPath, true)]
        private static bool CanRefill() => !EditorApplication.isCompiling
            && (!EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying);

        [MenuItem(MenuPath)]
        private static void Refill()
        {
            if (!CanRefill()) return;
            bool success;
            string notice;
            if (EditorApplication.isPlaying)
            {
                var controller = UnityEngine.Object.FindFirstObjectByType<BackpackPrototypeController>();
                if (controller == null) { EditorUtility.DisplayDialog("광산 티켓 충전", "게임의 메인 화면에 진입한 뒤 사용해 주세요.", "확인"); return; }
                success = controller.RefillMineTicketsForTesting(out notice);
            }
            else success = RefillSavedTickets(new PlayerProgressStore(PlayerProgressStore.ResolveLocalPath()), out notice);
            EditorUtility.DisplayDialog("광산 티켓 충전", success ? "기원의 광산 티켓을 5 / 5로 충전했습니다." : notice, "확인");
        }

        public static bool RefillSavedTickets(PlayerProgressStore store, out string error)
        {
            var progress = store.Load(out error);
            if (!store.CanWrite) return false;
            progress.RefillMineTickets(DateTimeOffset.UtcNow);
            return store.TrySave(progress, out error);
        }
    }
}
