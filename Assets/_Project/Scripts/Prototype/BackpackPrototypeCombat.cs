using System;
using System.Collections.Generic;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Enemies;
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
        [SerializeField, Min(1)] private int _playerMaxHealth = 40;
        [SerializeField] private StageData _stageData;
        [Tooltip("Optional test encounter. Leave empty for normal stage/round progression. Ignores round stat overrides.")]
        [FormerlySerializedAs("_enemyData")]
        [SerializeField] private EnemyEncounterData _debugEnemyOverride;
        [SerializeField] private StatusEffectData[] _statusEffects = Array.Empty<StatusEffectData>();
        [SerializeField, Min(1f)] private float _maximumBattleSeconds = 120f;
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

        public CombatLoadoutSnapshot CaptureCombatSnapshot()
        {
            var snapshots = new List<CombatItemSnapshot>();
            foreach (BackpackItemView view in _itemViews.Values)
            {
                if (_grid.TryGetOccupiedCells(view.InstanceId, out var occupiedCells))
                    snapshots.Add(new CombatItemSnapshot(view.InstanceId, view.Data, view.Origin, view.Rotation, occupiedCells));
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

            var player = new CombatantSnapshot("플레이어", _playerMaxHealth, CaptureCombatSnapshot());
            _itemTooltip.HideAll();
            ClearPlacementOverlay();
            // Use exactly the same resolved snapshot shown in preparation, never the source ItemData values.
            _battleController.BeginBattle(player, _preparedEnemy, _run.Round, _roundIncome, _maximumBattleSeconds, _statusEffects);
            _run.Phase = PrototypePhase.Battle;
            _selectedItem = null;
            _sellPreview.gameObject.SetActive(false);
            RefreshCombatInterface();
            return true;
        }

        public void TickCombat(float deltaSeconds) => _battleController.TickCombat(deltaSeconds);

        public bool ReturnToPreparation()
        {
            if (Phase != PrototypePhase.Defeat && Phase != PrototypePhase.Draw) return false;
            _run.Phase = PrototypePhase.Preparation;
            _battleController.EndBattle();
            RefreshCombatInterface();
            SetStatus("가방을 재배치하고 재도전하세요. 라운드·골드·보관함은 유지됩니다.");
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
            _run.Phase = result switch
            {
                CombatResult.Victory => _run.Round == StageRoundCount
                    ? PrototypePhase.Completed : PrototypePhase.Victory,
                CombatResult.Defeat => PrototypePhase.Defeat,
                _ => PrototypePhase.Draw
            };
            _battleController.ShowResult(Phase);
            RefreshCombatInterface();
        }

        private void RefreshCombatInterface()
        {
            if (_shopScreenRoot.gameObject.activeSelf != CanEditLoadout) _itemTooltip.HideAll();
            _shopScreenRoot.gameObject.SetActive(CanEditLoadout);
            _battleController.SetVisible(!CanEditLoadout);
            SetInputEnabled(_shopInputGroup, CanEditLoadout);
            SetInputEnabled(_gridInputGroup, CanEditLoadout);
            SetInputEnabled(_storageInputGroup, CanEditLoadout);
            if (_debugNextRoundButton != null)
                _debugNextRoundButton.interactable = CanEditLoadout && _preparedEnemy != null && _run.Round < StageRoundCount;
            if (CanEditLoadout) RenderEncounterPreview();
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
            _itemTooltip.Show(owner, () => ItemTooltipContent.FromItem(item), pointer.position, pointer.enterEventCamera);
            ShowPreparationPlacement(owner);
        }

        public void MoveItemTooltip(Component owner, PointerEventData pointer)
        {
            if (_itemTooltip != null) _itemTooltip.Move(owner, pointer.position, pointer.enterEventCamera);
        }

        public void HideItemTooltip(Component owner)
        {
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
            _playerMaxHealth = Mathf.Max(1, _playerMaxHealth);
            _maximumBattleSeconds = float.IsNaN(_maximumBattleSeconds) || float.IsInfinity(_maximumBattleSeconds)
                ? 120f : Mathf.Max(1f, _maximumBattleSeconds);
        }
    }
}
