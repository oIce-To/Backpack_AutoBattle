using System;
using System.Collections.Generic;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Enemies;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Shop;
using BackpackAutoBattle.Stages;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace BackpackAutoBattle.Prototype
{
    // The shop hands snapshots to a separate battle controller; no combat view uses shop item views.
    public sealed partial class BackpackPrototypeController
    {
        [Header("Combat Prototype")]
        [SerializeField] private CharacterData _characterData;
        [SerializeField] private StageData _stageData;
        [Tooltip("Optional test encounter. Leave empty for normal stage/round progression. Ignores round stat overrides.")]
        [FormerlySerializedAs("_enemyData")]
        [SerializeField] private EnemyEncounterData _debugEnemyOverride;
        [SerializeField] private StatusEffectData[] _statusEffects = Array.Empty<StatusEffectData>();
        [SerializeField] private bool _enableDebugRoundAdvance = true;

        private PrototypeRunState _run;
        private PrototypeBattleController _battleController;
        private PrototypePreparationPanel _preparationPanel;
        private PrototypeItemTooltip _itemTooltip;
        private RectTransform _shopScreenRoot;
        private CanvasGroup _shopInputGroup;
        private CanvasGroup _gridInputGroup;
        private CanvasGroup _storageInputGroup;
        private Button _debugNextRoundButton;
        private CombatantSnapshot _preparedEnemy;
        private string _enemyConfigurationError;

        public PrototypePhase Phase => _run.Phase;
        public CombatSimulation ActiveCombat => _battleController?.Simulation;
        public PrototypeBattleController BattleController => _battleController;
        public StageData Stage => _stageData;
        public int StageRoundCount => _stageData?.RoundCount ?? 0;
        public EnemyEncounterData CurrentEnemyEncounter => _debugEnemyOverride != null ? _debugEnemyOverride
            : _stageData != null && CurrentRound >= 1 && CurrentRound <= StageRoundCount ? _stageData.GetRound(CurrentRound).Encounter : null;
        public CombatantSnapshot PreparedEnemy => _preparedEnemy;
        public string EnemyConfigurationError => _enemyConfigurationError;
        public PrototypeItemTooltip ItemTooltip => _itemTooltip;
        public bool CanEditLoadout => _run.Phase == PrototypePhase.Preparation;
        public CharacterStatsSnapshot StageEntryStats => _run.PlayerStats;

        public CombatLoadoutSnapshot CaptureCombatSnapshot()
        {
            var snapshots = new List<CombatItemSnapshot>();
            foreach (BackpackItemView view in _itemViews.Values)
            {
                if (_grid.TryGetOccupiedCells(view.InstanceId, out var occupiedCells))
                    snapshots.Add(new CombatItemSnapshot(view.InstanceId, view.Data, view.Origin, view.Rotation, occupiedCells, ownerStats: _run.PlayerStats));
            }
            return new CombatLoadoutSnapshot(snapshots, GridSize, GridSize);
        }

        public bool TryStartBattle()
        {
            if (!CanEditLoadout || _draggingItem != null) return false;
            if (_preparedEnemy == null)
            {
                SetStatus(_enemyConfigurationError ?? "스테이지의 적 설정을 확인하세요.");
                return false;
            }

            var player = new CombatantSnapshot(_run.PlayerName, _run.PlayerStats.MaximumHealth, CaptureCombatSnapshot(), _run.PlayerStats.Attack);
            CapturePendingFusions();
            if (!_run.TryBeginBattle()) return false;
            _itemTooltip.HideAll();
            ClearPlacementOverlay();
            // Use exactly the same resolved snapshot shown in preparation, never the source ItemData values.
            _battleController.BeginBattle(player, _preparedEnemy, _run.Round, _roundIncome, statusEffects: _statusEffects);
            _selectedItem = null;
            _sellPreview.gameObject.SetActive(false);
            RefreshCombatInterface();
            return true;
        }

        public void TickCombat(float deltaSeconds) => _battleController.TickCombat(deltaSeconds);

        public bool ReturnToPreparation()
        {
            if (!_run.TryReturnToPreparation()) return false;
            _battleController.EndBattle();
            BeginFusionPresentation();
            RefreshCombatInterface();
            SetStatus($"남은 라이프 {_run.Lives}. 가방을 재배치하고 재도전하세요. 라운드와 골드는 유지됩니다.");
            return true;
        }

        public void HandleCombatAction()
        {
            if (Phase == PrototypePhase.Preparation) TryStartBattle();
            else if (Phase == PrototypePhase.Victory) AdvanceRound();
            else if (Phase == PrototypePhase.Defeat || Phase == PrototypePhase.Draw) ReturnToPreparation();
        }

        private void InitializeBattleScreen(Transform canvas)
        {
            var controllerObject = new GameObject("Battle Controller", typeof(PrototypeBattleController));
            controllerObject.transform.SetParent(transform, false);
            _battleController = controllerObject.GetComponent<PrototypeBattleController>();
            _battleController.Initialize(canvas, OnBattleFinished, HandleCombatAction, _itemTooltip);
        }

        private void OnBattleFinished(CombatResult result)
        {
            if (!_run.TryCompleteBattle(result, StageRoundCount, ActiveCombat.ElapsedSeconds)) return;
            _battleController.ShowResult(Phase);
            UpdateEconomyText();
            if (_run.IsFinished)
            {
                _pendingFusions = Array.Empty<FusionPlan>();
                _itemTooltip.HideAll(); _fusionHoverOwner = null; ClearPlacementOverlay();
                _runResultScreen.Show(_run.Result);
            }
            RefreshCombatInterface();
            if (_run.IsFinished) RunEnded?.Invoke(_run.Result);
        }

        private void RefreshCombatInterface()
        {
            bool showShop = CanEditLoadout || Phase == PrototypePhase.Fusion;
            if (_shopScreenRoot.gameObject.activeSelf != showShop) _itemTooltip.HideAll();
            _shopScreenRoot.gameObject.SetActive(showShop);
            _battleController.SetVisible(!showShop);
            _battleController.SetLives(_run.Lives, _run.StartingLives);
            SetInputEnabled(_shopInputGroup, CanEditLoadout);
            SetInputEnabled(_gridInputGroup, CanEditLoadout);
            SetInputEnabled(_storageInputGroup, CanEditLoadout);
            if (_debugNextRoundButton != null)
                _debugNextRoundButton.interactable = CanEditLoadout && _preparedEnemy != null && _run.Round < StageRoundCount;
            if (showShop) RenderEncounterPreview();
            _preparationPanel.SetInputEnabled(CanEditLoadout && _preparedEnemy != null);
        }

        /// <summary>Explicitly reload edited SO values during preparation. Does not spend gold or advance the round.</summary>
        public bool RefreshEncounterPreview()
        {
            if (!CanEditLoadout || _draggingItem != null) return false;
            bool valid = TryResolveEnemy(_run.Round, out _preparedEnemy, out _enemyConfigurationError);
            RenderEncounterPreview();
            if (_debugNextRoundButton != null) _debugNextRoundButton.interactable = valid && _run.Round < StageRoundCount;
            return valid;
        }

        private bool TryResolveEnemy(int round, out CombatantSnapshot enemy, out string error)
        {
            enemy = null;
            try
            {
                if (_stageData == null) throw new InvalidOperationException("Stage Data를 지정하세요.");
                _stageData.ValidateOrThrow();
                StageRoundData definition = _stageData.GetRound(round);
                enemy = _debugEnemyOverride != null ? _debugEnemyOverride.CreateCombatantSnapshot() : definition.CreateEnemySnapshot();
                error = null;
                return true;
            }
            catch (InvalidOperationException exception) { error = exception.Message; return false; }
        }

        private void RenderEncounterPreview()
        {
            _preparationPanel.Render(_stageData?.DisplayName, _run.Round, _preparedEnemy, _enemyConfigurationError,
                _preparedEnemy != null && _stageData != null && _run.Round <= StageRoundCount && _stageData.GetRound(_run.Round).IsBoss,
                _debugEnemyOverride != null);
        }

        public void ShowItemTooltip(Component owner, ItemData item, PointerEventData pointer)
        {
            if (!CanEditLoadout || _draggingItem != null || item == null) return;
            _fusionHoverOwner = owner;
            _itemTooltip.Show(owner, () => ItemTooltipContent.FromItem(item, _run.PlayerStats), pointer.position, pointer.enterEventCamera);
            ShowPreparationPlacement(owner);
        }

        public void MoveItemTooltip(Component owner, PointerEventData pointer)
        {
            if (_itemTooltip != null) _itemTooltip.Move(owner, pointer.position, pointer.enterEventCamera);
        }

        public void HideItemTooltip(Component owner)
        {
            if (_fusionHoverOwner == owner) _fusionHoverOwner = null;
            if (_itemTooltip != null) _itemTooltip.Hide(owner);
            if (_draggingItem == null && _placementHoverOwner == owner) ClearPlacementOverlay();
        }

        private static void SetInputEnabled(CanvasGroup group, bool enabled)
        {
            if (group == null) return;
            group.interactable = enabled;
            group.blocksRaycasts = enabled;
        }

        private void OnValidate()
        {
            _startingLives = Mathf.Max(1, _startingLives);
        }
    }
}
