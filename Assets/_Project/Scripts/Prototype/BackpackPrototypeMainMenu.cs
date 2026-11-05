using System;
using System.Collections.Generic;
using System.IO;
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
        public IReadOnlyList<CharacterData> AvailableCharacters => (_adventureCatalog?.Characters ?? new[] { _characterData })
            .Where(c => c != null).Distinct().ToArray();
        public IReadOnlyList<StageData> AvailableStages => (_adventureCatalog?.Stages ?? new[] { _stageData })
            .Where(s => s != null).Distinct().ToArray();
        public bool IsStageUnlocked(string id) => _progress.IsStageUnlocked(id, AvailableStages.Select(s => s.StageId).ToArray());

        private void EnsureProgressLoaded()
        {
            if (_progressStore != null) return;
            string path = Environment.GetEnvironmentVariable("BACKPACK_PROFILE_PATH");
            _progressStore = new PlayerProgressStore(string.IsNullOrWhiteSpace(path)
                ? Path.Combine(Application.persistentDataPath, "player-progress.json") : path);
            _progress = _progressStore.Load(out _saveNotice);
            _characterData = AvailableCharacters.FirstOrDefault(c => c.CharacterId == _progress.SelectedCharacterId) ?? _characterData;
            _stageData = AvailableStages.FirstOrDefault(s => s.StageId == _progress.SelectedStageId && IsStageUnlocked(s.StageId))
                ?? AvailableStages.FirstOrDefault(s => IsStageUnlocked(s.StageId));
        }

        public bool ShowMainMenuTab(MainMenuTab tab)
        {
            if (!IsMainMenu || IsSettingsOpen || !Enum.IsDefined(typeof(MainMenuTab), tab)) return false;
            _mainMenu.Hide(); _characterSelection.Hide(); _stageSelection.Hide();
            switch (tab)
            {
                case MainMenuTab.Home: RefreshMainMenu(); break;
                case MainMenuTab.Characters: _characterSelection.Show(AvailableCharacters, _characterData, _progress); break;
                case MainMenuTab.Stages: _stageSelection.Show(AvailableStages, _stageData, _progress); break;
            }
            ActiveMainTab = tab; _mainNavigation.Show(tab, _progress.SharedExperience); return true;
        }
        public bool ShowCharacterSelection() => ShowMainMenuTab(MainMenuTab.Characters);
        public bool ShowStageSelection() => ShowMainMenuTab(MainMenuTab.Stages);
        public void CloseSelectionScreens()
        {
            if (IsMainMenu) ShowMainMenuTab(MainMenuTab.Home);
            else { _characterSelection.Hide(); _stageSelection.Hide(); }
        }

        public bool TrySelectCharacter(string id)
        {
            if (!IsMainMenu || IsSettingsOpen) return false;
            CharacterData character = AvailableCharacters.FirstOrDefault(c => c.CharacterId == id);
            if (character == null || string.IsNullOrWhiteSpace(id)) return false;
            _characterData = character; _progress.SelectCharacter(id);
            SaveProgress(); ShowMainMenuTab(MainMenuTab.Home); return true;
        }
        public bool TrySelectStage(string id)
        {
            if (!IsMainMenu || IsSettingsOpen) return false;
            StageData stage = AvailableStages.FirstOrDefault(s => s.StageId == id);
            if (stage == null || !IsStageUnlocked(id) || stage.GetValidationErrors().Count > 0) return false;
            _stageData = stage; _progress.SelectStage(id);
            SaveProgress(); ShowMainMenuTab(MainMenuTab.Home); return true;
        }

        public bool TryLevelUpCharacter(string id)
        {
            if (!IsMainMenu || IsSettingsOpen || _progressStore == null) return false;
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
        private void RecordProgress(bool cleared = false, long experienceReward = 0)
        {
            if (!_trackProgress || _progressStore == null || _stageData == null) return;
            bool changed = _progress.RecordRound(_run.StageId, _run.Round, StageRoundCount, cleared);
            if (!_progress.TryAddExperience(experienceReward))
            {
                _saveNotice = "경험치가 저장 가능한 범위를 초과했습니다.";
                return;
            }
            if (changed || experienceReward > 0) SaveProgress();
        }
        private void RefreshMainMenu()
        {
            _mainMenu.Show(_characterData, _stageData, _progress.GetStage(_stageData?.StageId), _progress);
            if (!string.IsNullOrEmpty(_saveNotice)) _mainMenu.ShowError(_saveNotice);
        }

        public bool TryStartRun()
        {
            if (!IsMainMenu || IsSettingsOpen) return false;
            if (!IsStageUnlocked(_stageData?.StageId))
            {
                _mainMenu.ShowError("이전 스테이지를 먼저 클리어해주세요.");
                return false;
            }
            if (!TryResolveEnemy(1, out _, out string error))
            {
                _mainMenu.ShowError(error);
                return false;
            }
            StartNewRun();
            return true;
        }

        public bool TryReturnToMainMenu()
        {
            if (IsMainMenu || IsSettingsOpen || !_run.IsFinished || _restartRequested) return false;
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
