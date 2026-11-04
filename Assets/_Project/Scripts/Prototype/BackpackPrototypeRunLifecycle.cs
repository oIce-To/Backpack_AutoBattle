using System;
using BackpackAutoBattle.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed partial class BackpackPrototypeController
    {
        [Header("Run Rules")]
        [SerializeField, Min(1)] private int _startingLives = 3;
        private Text _livesText;
        private PrototypeRunResultScreen _runResultScreen;
        private bool _restartRequested;

        public int Lives => _run.Lives;
        public int StartingLives => _run.StartingLives;
        public PrototypeRunResult RunResult => _run.Result;
        public bool IsRunFinished => _run.IsFinished;
        public event Action<PrototypeRunResult> RunEnded;

        /// <summary>Optional navigation boundary. With no subscriber the prototype starts a new run.
        /// A future main-screen coordinator can subscribe and take ownership of navigation/rewards.</summary>
        public event Action<PrototypeRunResult> RestartRequested;

        private PrototypeRunState CreateRunState() => new(_startingGold, _stageData?.StageId,
            _characterData?.CaptureStats(_progress.GetCharacterLevel(_characterData.CharacterId)),
            _characterData != null ? _characterData.DisplayName : "플레이어", Mathf.Max(1, _startingLives), _stageData?.DisplayName,
            _stageData?.ClearExperience ?? 0);

        private void InitializeRunContent()
        {
            RefreshEncounterPreview(); CreateStartingItems(); PopulateShop(); UpdateEconomyText(); RefreshCombatInterface();
            RecordProgress();
            SetStatus("상점 아이템을 가방이나 자유 보관함으로 드래그해 구매하세요.");
        }

        public bool RequestRunRestart()
        {
            if (IsMainMenu || !_run.IsFinished || _restartRequested) return false;
            _restartRequested = true;
            _runResultScreen.SetRestartPending();
            if (RestartRequested != null) { RestartRequested.Invoke(_run.Result); return true; }
            return TryRestartRun();
        }

        /// <summary>Starts a fresh run in the currently configured stage without rebuilding the scene/UI.
        /// Only terminal runs can be restarted; repeated clicks cannot duplicate starting content.</summary>
        public bool TryRestartRun()
        {
            if (IsMainMenu || !_run.IsFinished) return false;
            StartNewRun();
            return true;
        }

        private void ClearRunContent()
        {
            _itemTooltip.HideAll(); _fusionHoverOwner = null;
            FinishDrag();
            _pendingFusions = Array.Empty<FusionPlan>(); _fusionLocks.Clear(); _fusionOverlay?.Clear();
            _fusionAnimation?.Dispose(); _fusionAnimation = null;
            _battleController.EndBattle();
            _battleController.ResetPlayback();
            foreach (var view in _itemViews.Values) { view.gameObject.SetActive(false); Destroy(view.gameObject); }
            foreach (var view in _storageViews.Values) { view.gameObject.SetActive(false); Destroy(view.gameObject); }
            _itemViews.Clear(); _storageViews.Clear(); _storedItems.Clear(); _selectedItem = null;
            foreach (var slot in _shopSlots) slot.Bind(null); // Discard purchases AND locks/discounts from the old run.
            _currentShopItems.Clear();
            _grid = new BackpackGrid(GridSize, GridSize);
            _preparedEnemy = null; _enemyConfigurationError = null;
            _runResultScreen.Hide();
            EventSystem.current?.SetSelectedGameObject(null);
        }

        private void StartNewRun()
        {
            // Capture editable outside-stage data at entry, never during an active run.
            PrototypeRunState nextRun = CreateRunState();
            _trackProgress = _progressStore != null && (_trackProgress || IsMainMenu);
            if (_trackProgress)
            {
                if (!string.IsNullOrWhiteSpace(_characterData?.CharacterId)) _progress.SelectCharacter(_characterData.CharacterId);
                _progress.SelectStage(_stageData.StageId);
                SaveProgress();
            }
            ClearRunContent();
            _shopRandom = new System.Random(Environment.TickCount);
            _run = nextRun; _restartRequested = false;
            IsMainMenu = false; CloseSelectionScreens(); _mainMenu.Hide();
            _mainNavigation.Hide();
            InitializeRunContent();
        }
    }
}
