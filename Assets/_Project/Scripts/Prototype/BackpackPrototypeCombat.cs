using System;
using System.Collections.Generic;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Enemies;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Shop;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    // The shop hands snapshots to a separate battle controller; no combat view uses shop item views.
    public sealed partial class BackpackPrototypeController
    {
        [Header("Combat Prototype")]
        [SerializeField, Min(1)] private int _playerMaxHealth = 40;
        [SerializeField] private EnemyData _enemyData;
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

        public PrototypePhase Phase => _run.Phase;
        public CombatSimulation ActiveCombat => _battleController?.Simulation;
        public PrototypeBattleController BattleController => _battleController;
        public EnemyData EnemyData => _enemyData;
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
            if (_enemyData == null)
            {
                SetStatus("Enemy Data가 연결되지 않았습니다. 프로토타입 데이터 갱신을 실행하세요.");
                return false;
            }

            CombatantSnapshot enemy;
            try { enemy = _enemyData.CreateCombatantSnapshot(); }
            catch (InvalidOperationException exception)
            {
                SetStatus($"적 가방 배치를 확인하세요: {exception.Message}");
                return false;
            }

            var player = new CombatantSnapshot("플레이어", _playerMaxHealth, CaptureCombatSnapshot());
            _itemTooltip.HideAll();
            ClearPlacementOverlay();
            _battleController.BeginBattle(player, enemy, _run.Round, _roundIncome, _maximumBattleSeconds, _statusEffects);
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
                CombatResult.Victory => _run.Round == ShopRarityTableData.MaximumRound
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
                _debugNextRoundButton.interactable = CanEditLoadout && _run.Round < ShopRarityTableData.MaximumRound;
            if (CanEditLoadout) _preparationPanel.Render(_run.Round, _enemyData);
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
