using System;
using System.Linq;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Progression;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed partial class BackpackPrototypeController
    {
        [Header("Run Rules")]
        [SerializeField, Min(1)] private int _startingLives = 3;
        private PrototypeLifeHearts _lifeHearts;
        private RectTransform _stageAbandonModal;
        private Button _stageAbandonButton;
        private bool IsStageAbandonOpen => _stageAbandonModal != null && _stageAbandonModal.gameObject.activeSelf;
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

        private void BuildRunControls(Transform canvas)
        {
            var life = new GameObject("Run Lives", typeof(RectTransform), typeof(PrototypeLifeHearts));
            life.transform.SetParent(canvas, false);
            _lifeHearts = life.GetComponent<PrototypeLifeHearts>();
            _lifeHearts.raycastTarget = false;
            SetRect(_lifeHearts.rectTransform, Vector2.one, new Vector2(-149, -36), new Vector2(176, 28));

            _stageAbandonButton = SelectionUi.Button(_shopScreenRoot, "Abandon Stage Button", "스테이지 포기",
                new Vector2(-405f, -283f), new Vector2(310f, 44f), OpenStageAbandon);
            _stageAbandonButton.GetComponent<Image>().color = new Color(.58f, .19f, .23f);

            var backdrop = SelectionUi.Image(canvas, "Abandon Stage Modal", new Color(0f, 0f, 0f, .72f));
            backdrop.raycastTarget = true;
            _stageAbandonModal = backdrop.rectTransform;
            Stretch(_stageAbandonModal);
            var panel = SelectionUi.Image(backdrop.transform, "Abandon Stage Panel", new Color(.10f, .13f, .19f));
            SelectionUi.Place(panel.rectTransform, Vector2.zero, new Vector2(500, 200));
            SelectionUi.Label(panel.transform, "Abandon Stage Message",
                "스테이지를 포기하면 모든 보상을 잃고 메인 화면으로 나가게 됩니다. 정말로 포기하시겠습니까?",
                21, new Vector2(0, 32), new Vector2(442, 110));
            var yes = SelectionUi.Button(panel.transform, "Confirm Abandon Stage", "예", new Vector2(-114, -55), new Vector2(190, 43), ConfirmStageAbandon);
            yes.GetComponent<Image>().color = new Color(.64f, .22f, .25f);
            var no = SelectionUi.Button(panel.transform, "Cancel Abandon Stage", "아니오", new Vector2(114, -55), new Vector2(190, 43), CancelStageAbandon);
            no.GetComponent<Image>().color = new Color(.31f, .33f, .37f);
            _stageAbandonModal.gameObject.SetActive(false);
        }

        private void OpenStageAbandon()
        {
            if (!CanEditLoadout || _draggingItem != null) return;
            _itemTooltip.HideAll();
            ClearPlacementOverlay();
            EventSystem.current?.SetSelectedGameObject(null);
            _stageAbandonModal.gameObject.SetActive(true);
            RefreshCombatInterface();
            _stageAbandonModal.SetAsLastSibling();
        }

        private void CancelStageAbandon()
        {
            if (!IsStageAbandonOpen) return;
            _stageAbandonModal.gameObject.SetActive(false);
            EventSystem.current?.SetSelectedGameObject(null);
            RefreshCombatInterface();
        }

        private void ConfirmStageAbandon()
        {
            if (!IsStageAbandonOpen || IsMainMenu || _run.Phase != PrototypePhase.Preparation) return;
            _stageAbandonModal.gameObject.SetActive(false);
            _trackProgress = false;
            ClearRunContent();
            ShowMainMenu();
        }

        private PrototypeRunState CreateRunState() => new(_startingGold, _stageData?.StageId,
            GetCharacterStats(_characterData),
            _characterData != null ? _characterData.DisplayName : "플레이어", Mathf.Max(1, _startingLives), _stageData?.DisplayName,
            _stageData?.ClearExperience ?? 0, _stageData?.ClearOriginShards ?? 0,
            _stageData?.IsOriginMine ?? false, _stageData?.ClearBlueprints ?? 0, AvailableGear.Select(gear => gear.GearId),
            _characterData?.Passive?.Capture())
        {
            StartingGear = EquippedGear(_characterData).Select(gear => new StartingGear(gear.instance, gear.definition))
                .ToDictionary(gear => gear.InstanceId)
        };

        private void InitializeRunContent()
        {
            RefreshEncounterPreview(); CreateStartingItems(); PopulateShop(); UpdateEconomyText(); RefreshCombatInterface();
            RecordProgress();
            SetStatus("상점 아이템을 가방이나 자유 보관함으로 드래그해 구매하세요.");
        }

        public bool RequestRunRestart()
        {
            if (IsMainMenu || IsSettingsOpen || !_run.IsFinished || _restartRequested) return false;
            if (!TrySettleRunReward()) return false;
            if (RestartRequested == null) return TryRestartRun();
            _restartRequested = true;
            _runResultScreen.SetRestartPending();
            RestartRequested.Invoke(_run.Result);
            return true;
        }

        /// <summary>Starts a fresh run in the currently configured stage without rebuilding the scene/UI.
        /// Only terminal runs can be restarted; repeated clicks cannot duplicate starting content.</summary>
        public bool TryRestartRun()
        {
            if (IsMainMenu || IsSettingsOpen || !_run.IsFinished) return false;
            if (!TrySettleRunReward()) return false;
            return StartNewRun();
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
            _pendingStageReward = null; _runRewardSettled = false;
            EventSystem.current?.SetSelectedGameObject(null);
        }

        private bool StartNewRun()
        {
            if (!CheckStageAdmission()) return false;
            // Capture editable outside-stage data at entry, never during an active run.
            PrototypeRunState nextRun = CreateRunState();
            _trackProgress = _progressStore != null && (_trackProgress || IsMainMenu);
            if (_trackProgress)
            {
                var candidate = PlayerProgress.FromData(_progress.Export());
                if (!string.IsNullOrWhiteSpace(_characterData?.CharacterId)) candidate.SelectCharacter(_characterData.CharacterId);
                candidate.SelectStage(_stageData.StageId);
                candidate.RefreshMineTickets(DateTimeOffset.UtcNow);
                if (!_progressStore.TrySave(candidate, out _saveNotice)) { ShowEconomyError(_saveNotice); return false; }
                _progress = candidate;
            }
            ClearRunContent();
            _shopRandom = new System.Random(Environment.TickCount);
            _run = nextRun; _restartRequested = false;
            IsMainMenu = false; CloseSelectionScreens(); _mainMenu.Hide();
            _mainNavigation.Hide();
            InitializeRunContent();
            return true;
        }
    }
}
