using System;
using System.Linq;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Progression;
using BackpackAutoBattle.Stages;
using UnityEngine;

namespace BackpackAutoBattle.Prototype
{
    public sealed partial class BackpackPrototypeController
    {
        private StageReward _pendingStageReward;
        private bool _runRewardSettled;
        private PrototypeRewardPopup _rewardPopup;
        private float _nextEconomyDisplay;
        private readonly System.Random _rewardRandom = new();
        public System.Collections.Generic.IReadOnlyList<UniqueGearData> AvailableGear
            => (_adventureCatalog?.UniqueGear ?? CatalogCharacters.Select(c => c.StarterGear).ToArray())
                .Where(gear => gear != null && gear.FitsSlot).GroupBy(gear => gear.GearId).Select(group => group.First()).ToArray();

        private StageReward RollReward(long experience, long shards, int blueprintCount,
            System.Collections.Generic.IReadOnlyList<string> pool)
        {
            if (blueprintCount > 0 && pool.Count == 0) throw new InvalidOperationException("설계도 보상 목록이 비어 있습니다.");
            var rolls = Enumerable.Range(0, blueprintCount).Select(_ => pool[_rewardRandom.Next(pool.Count)]).ToArray();
            return new StageReward(experience, shards, rolls);
        }

        private bool CheckStageAdmission()
        {
            _progress.RefreshMineTickets(DateTimeOffset.UtcNow);
            if (_stageData?.IsOriginMine == true && (_progress.MineTickets == 0 || AvailableGear.Count == 0))
            {
                ShowEconomyError(_progress.MineTickets == 0 ? "기원의 광산 입장 티켓이 부족합니다." : "설계도 보상 목록을 확인해주세요.");
                return false;
            }
            return true;
        }

        private bool TrySettleRunReward()
        {
            if (!_trackProgress || !_run.IsFinished || _runRewardSettled) return true;
            var result = _run.Result;
            _pendingStageReward ??= RollReward(result.ExperienceReward, result.OriginShardReward,
                result.BlueprintRewardCount, _run.BlueprintPool);
            var candidate = PlayerProgress.FromData(_progress.Export());
            candidate.RecordRound(_run.StageId, result.Round, result.RoundCount, result.Outcome == PrototypeRunOutcome.Cleared);
            if (!candidate.TryClaimReward(_run.RunId, _pendingStageReward,
                _run.IsOriginMine && result.Outcome == PrototypeRunOutcome.Cleared, DateTimeOffset.UtcNow))
            {
                _saveNotice = "티켓 수량 또는 보상 저장 범위를 확인해주세요.";
                ShowEconomyError(_saveNotice);
                return false;
            }
            if (!_progressStore.TrySave(candidate, out _saveNotice)) { ShowEconomyError(_saveNotice); return false; }
            _progress = candidate;
            _runRewardSettled = true;
            return true;
        }

        public bool TrySweepStage(string stageId)
        {
            if (!IsMainMenu || IsSettingsOpen || _rewardPopup.IsOpen || _stageSelection.IsEntryOpen) return false;
            var stage = AvailableMineStages.FirstOrDefault(s => s.StageId == stageId);
            if (stage == null || !IsStageUnlocked(stageId) || !_progress.GetStage(stageId).Cleared
                || stage.GetValidationErrors().Count > 0 || AvailableGear.Count == 0) return false;
            var candidate = PlayerProgress.FromData(_progress.Export());
            candidate.RefreshMineTickets(DateTimeOffset.UtcNow);
            if (candidate.MineTickets == 0) { ShowEconomyError("기원의 광산 입장 티켓이 부족합니다."); return false; }
            var reward = RollReward(0, stage.ClearOriginShards, stage.ClearBlueprints, AvailableGear.Select(gear => gear.GearId).ToArray());
            if (!candidate.TryClaimReward(Guid.NewGuid().ToString("N"), reward, true, DateTimeOffset.UtcNow)) return false;
            if (!_progressStore.TrySave(candidate, out _saveNotice)) { ShowEconomyError(_saveNotice); return false; }
            _progress = candidate;
            _stageSelection.Show(AvailableStages, stage, _progress, AvailableMineStages);
            RefreshEconomyDisplay();
            _mainNavigation.Root.SetAsLastSibling();
            _rewardPopup.Show("소탕 완료", DescribeReward(reward));
            return true;
        }

        public bool TryCraftUniqueGear(string gearId)
        {
            if (!CanChangeEquipment) return false;
            var candidate = PlayerProgress.FromData(_progress.Export());
            var crafted = candidate.TryCraftGear(FindGearDefinition(gearId));
            if (crafted == null || !SaveEquipment(candidate)) return false;
            _equipment.ShowCraftDetails(gearId);
            return true;
        }

        public bool TryEnhanceUniqueGear(string instanceId)
        {
            if (!CanChangeEquipment) return false;
            var candidate = PlayerProgress.FromData(_progress.Export());
            if (!candidate.TryEnhanceGear(instanceId) || !SaveEquipment(candidate)) return false;
            _equipment.ShowGearDetails(instanceId);
            return true;
        }

        public bool TryPromoteUniqueGear(string instanceId, System.Collections.Generic.IReadOnlyList<string> materialIds)
        {
            if (!CanChangeEquipment) return false;
            var candidate = PlayerProgress.FromData(_progress.Export());
            if (!candidate.TryPromoteGear(instanceId, materialIds) || !SaveEquipment(candidate)) return false;
            _equipment.ShowGearDetails(instanceId);
            return true;
        }

        private string DescribeReward(StageReward reward)
        {
            if (reward == null) return "";
            var lines = new System.Collections.Generic.List<string>();
            if (reward.OriginShards > 0) lines.Add($"기원의 파편 +{reward.OriginShards:N0}");
            lines.AddRange(reward.Blueprints.Select(pair => $"{FindGearDefinition(pair.Key)?.Item.DisplayName ?? pair.Key} 설계도 +{pair.Value:N0}"));
            return string.Join("\n", lines);
        }

        private void ShowEconomyError(string error)
        {
            if (IsMainMenu)
            {
                if (ActiveMainTab == MainMenuTab.Equipment) _equipment.ShowError(error);
                else if (ActiveMainTab == MainMenuTab.Stages) _stageSelection.ShowError(error);
                else _mainMenu.ShowError(error);
            }
            else if (_run.IsFinished) _runResultScreen.ShowNotice(error);
            else SetStatus(error);
        }

        private void RefreshEconomyDisplay()
        {
            if (!IsMainMenu) return;
            _mainNavigation.SetEconomy(_progress, DateTimeOffset.UtcNow);
            _stageSelection.RefreshTickets();
        }

        private void UpdateEconomyDisplay()
        {
            if (!IsMainMenu || Time.unscaledTime < _nextEconomyDisplay) return;
            _nextEconomyDisplay = Time.unscaledTime + .25f;
            RefreshEconomyDisplay();
        }

#if UNITY_EDITOR
        public bool RefillMineTicketsForTesting(out string error)
        {
            error = null;
            if (_progressStore == null) { error = "메인 화면에 한 번 진입한 뒤 사용해 주세요."; return false; }
            var candidate = PlayerProgress.FromData(_progress.Export());
            candidate.RefillMineTickets(DateTimeOffset.UtcNow);
            if (!_progressStore.TrySave(candidate, out error)) return false;
            _progress = candidate;
            _stageSelection.RefreshProgress(candidate);
            RefreshEconomyDisplay();
            return true;
        }
#endif
    }
}
