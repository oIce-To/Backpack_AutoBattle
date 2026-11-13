using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Stages;
using BackpackAutoBattle.Progression;
using UnityEngine;

namespace BackpackAutoBattle.Prototype
{
    public sealed partial class BackpackPrototypeController
    {
        private PrototypeMainMenuScreen _mainMenu;
        private PrototypeCharacterSelectionScreen _characterSelection;
        private PrototypeStageSelectionScreen _stageSelection;
        private PrototypeMainNavigation _mainNavigation;
        public MainMenuTab ActiveMainTab { get; private set; } = MainMenuTab.Home;
        [SerializeField] private AdventureCatalogData _adventureCatalog;
        private PlayerProgress _progress = new();
        private PlayerProgressStore _progressStore;
        private string _saveNotice;
        private bool _trackProgress;
        public bool IsMainMenu { get; private set; }
        public CharacterData SelectedCharacter => _characterData;
        public PlayerProgress Progress => _progress;
        public string ProgressSavePath => _progressStore?.FilePath;
        public IReadOnlyList<CharacterData> AvailableCharacters => CatalogCharacters.Where(c => _progress.HasCharacter(c.CharacterId)).ToArray();
        public IReadOnlyList<StageData> AvailableStages => (_adventureCatalog?.Stages ?? new[] { _stageData })
            .Where(s => s != null).Distinct().ToArray();
        public IReadOnlyList<StageData> AvailableMineStages => (_adventureCatalog?.OriginMines ?? Array.Empty<StageData>())
            .Where(s => s != null).Distinct().ToArray();
        private IEnumerable<StageData> AllStages => AvailableStages.Concat(AvailableMineStages);
        public bool IsStageUnlocked(string id)
        {
            var stage = AllStages.FirstOrDefault(s => s.StageId == id);
            return stage != null && _progress.IsStageUnlocked(id,
                (stage.IsOriginMine ? AvailableMineStages : AvailableStages).Select(s => s.StageId).ToArray());
        }

        private void EnsureProgressLoaded()
        {
            if (_progressStore != null) return;
            _progressStore = new PlayerProgressStore(PlayerProgressStore.ResolveLocalPath());
            _progress = _progressStore.Load(out _saveNotice);
            AcquireInitialCharacters();
            _progress.RefreshMineTickets(DateTimeOffset.UtcNow);
            if (string.IsNullOrEmpty(_saveNotice))
                _progressStore.TrySave(_progress, out _saveNotice);
            _characterData = AvailableCharacters.FirstOrDefault(c => c.CharacterId == _progress.SelectedCharacterId)
                ?? AvailableCharacters.FirstOrDefault();
            _stageData = AllStages.FirstOrDefault(s => s.StageId == _progress.SelectedStageId && IsStageUnlocked(s.StageId))
                ?? AvailableStages.FirstOrDefault(s => IsStageUnlocked(s.StageId));
        }

        public bool ShowMainMenuTab(MainMenuTab tab)
        {
            if (!IsMainMenu || IsSettingsOpen || _rewardPopup.IsOpen || _mainMenu.IsPopupOpen || _stageSelection.IsEntryOpen
                || !Enum.IsDefined(typeof(MainMenuTab), tab)) return false;
            _mainMenu.Hide(); _characterSelection.Hide(); _stageSelection.Hide(); _equipment.Hide();
            switch (tab)
            {
                case MainMenuTab.Home: RefreshMainMenu(); break;
                case MainMenuTab.Characters: _characterSelection.Show(AvailableCharacters, _characterData, _progress); break;
                case MainMenuTab.Stages: _stageSelection.Show(AvailableStages, _stageData, _progress, AvailableMineStages); break;
                case MainMenuTab.Equipment: _equipment.Show(); break;
            }
            ActiveMainTab = tab; _mainNavigation.Show(tab, _progress.SharedExperience); RefreshEconomyDisplay(); return true;
        }
        public bool ShowCharacterSelection() => ShowMainMenuTab(MainMenuTab.Characters);
        public bool ShowStageSelection() => ShowMainMenuTab(MainMenuTab.Stages);
        public void CloseSelectionScreens()
        {
            if (IsMainMenu) ShowMainMenuTab(MainMenuTab.Home);
            else { _characterSelection.Hide(); _stageSelection.Hide(); _equipment.Hide(); }
        }

        public bool TrySelectCharacter(string id)
        {
            if (!IsMainMenu || IsSettingsOpen || _rewardPopup.IsOpen || _mainMenu.IsPopupOpen || _stageSelection.IsEntryOpen) return false;
            CharacterData character = AvailableCharacters.FirstOrDefault(c => c.CharacterId == id);
            if (character == null || string.IsNullOrWhiteSpace(id)) return false;
            _characterData = character; _progress.SelectCharacter(id);
            SaveProgress(); ShowMainMenuTab(MainMenuTab.Home); return true;
        }
        public bool TrySelectStage(string id)
        {
            if (!IsMainMenu || IsSettingsOpen || _rewardPopup.IsOpen || _mainMenu.IsPopupOpen || _stageSelection.IsEntryOpen) return false;
            StageData stage = AllStages.FirstOrDefault(s => s.StageId == id);
            if (stage == null || !IsStageUnlocked(id) || stage.GetValidationErrors().Count > 0) return false;
            _stageData = stage; _progress.SelectStage(id);
            SaveProgress(); ShowMainMenuTab(MainMenuTab.Home); return true;
        }

        public bool TryLevelUpCharacter(string id)
        {
            if (!IsMainMenu || IsSettingsOpen || _rewardPopup.IsOpen || _stageSelection.IsEntryOpen || _progressStore == null) return false;
            CharacterData character = AvailableCharacters.FirstOrDefault(c => c.CharacterId == id);
            if (character == null) return false;
            var candidate = PlayerProgress.FromData(_progress.Export());
            if (!candidate.TryLevelUp(id, character.ExperienceTable)) return false;
            // Save the debit and new level together. A failed write must not spend the user's experience.
            if (!_progressStore.TrySave(candidate, out _saveNotice))
            {
                _characterSelection.ShowError(_saveNotice);
                if (ActiveMainTab == MainMenuTab.Home) _mainMenu.ShowError(_saveNotice);
                return false;
            }
            _progress = candidate;
            _characterSelection.RefreshProgress(_progress);
            _mainNavigation.SetExperience(_progress.SharedExperience);
            if (ActiveMainTab == MainMenuTab.Home) ShowMainMenuTab(MainMenuTab.Home);
            return true;
        }

        private void SaveProgress()
        {
            if (_progressStore == null) return;
            _progressStore.TrySave(_progress, out _saveNotice);
            if (IsMainMenu && !string.IsNullOrEmpty(_saveNotice)) _mainMenu.ShowError(_saveNotice);
        }
        private void RecordProgress()
        {
            if (!_trackProgress || _progressStore == null || _stageData == null) return;
            if (_progress.RecordRound(_run.StageId, _run.Round, StageRoundCount)) SaveProgress();
        }
        private void RefreshMainMenu()
        {
            _mainMenu.Show(_characterData, _progress, GetCharacterStats(_characterData), AvailableGear);
            if (!string.IsNullOrEmpty(_saveNotice)) _mainMenu.ShowError(_saveNotice);
        }

        private void ShowStageEntry(StageData stage)
        {
            if (!IsMainMenu || ActiveMainTab != MainMenuTab.Stages || IsSettingsOpen || _rewardPopup.IsOpen
                || _stageSelection.IsEntryOpen || !IsStageUnlocked(stage?.StageId)) return;
            if (_characterData == null)
            {
                _stageSelection.ShowError("출전할 캐릭터를 선택해주세요.");
                return;
            }
            _itemTooltip.HideAll();
            _stageSelection.ShowEntry(stage, _characterData, _progress, FindGearDefinition);
        }

        public bool TryStartRun() => !_stageSelection.IsEntryOpen && TryEnterStage(_stageData);

        private bool TryEnterStage(StageData stage)
        {
            if (!IsMainMenu || IsSettingsOpen || _rewardPopup.IsOpen || _mainMenu.IsPopupOpen) return false;
            if (!IsStageUnlocked(stage?.StageId))
            {
                ShowEconomyError("이전 스테이지를 먼저 클리어해주세요.");
                return false;
            }
            StageData previous = _stageData;
            _stageData = stage;
            if (!TryResolveEnemy(1, out _, out string error))
            {
                _stageData = previous;
                ShowEconomyError(error);
                return false;
            }
            if (StartNewRun()) return true;
            _stageData = previous;
            return false;
        }

        public bool TryReturnToMainMenu()
        {
            if (IsMainMenu || IsSettingsOpen || !_run.IsFinished || _restartRequested) return false;
            if (!TrySettleRunReward()) return false;
            ClearRunContent();
            ShowMainMenu();
            return true;
        }

        private void ShowMainMenu()
        {
            EnsureProgressLoaded();
            IsMainMenu = true;
            ShowMainMenuTab(MainMenuTab.Home);
            RefreshCombatInterface();
        }

        private void QuitGame()
        {
            if (!IsMainMenu) return;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
