using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Enemies;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Shop;
using BackpackAutoBattle.Stages;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private PrototypeEnemyPreview _enemyPreview;
        private PrototypeItemTooltip _itemTooltip;
        private RectTransform _shopScreenRoot;
        private CanvasGroup _shopInputGroup;
        private CanvasGroup _gridInputGroup;
        private CanvasGroup _storageInputGroup;
        private CombatantSnapshot _preparedEnemy;
        private string _enemyConfigurationError;

        public PrototypePhase Phase => IsTitleScreen ? PrototypePhase.Title : IsMainMenu ? PrototypePhase.MainMenu : _run.Phase;
        public CombatSimulation ActiveCombat => _battleController?.Simulation;
        public PrototypeBattleController BattleController => _battleController;
        public StageData Stage => _stageData;
        public int StageRoundCount => _tutorialActive ? 2 : _stageData?.RoundCount ?? 0;
        public EnemyEncounterData CurrentEnemyEncounter => _tutorialActive ? AvailableStages.FirstOrDefault()?.GetRound(1).Encounter
            : _debugEnemyOverride != null ? _debugEnemyOverride
            : _stageData != null && CurrentRound >= 1 && CurrentRound <= StageRoundCount ? _stageData.GetRound(CurrentRound).Encounter : null;
        public CombatantSnapshot PreparedEnemy => _preparedEnemy;
        public string EnemyConfigurationError => _enemyConfigurationError;
        public PrototypeItemTooltip ItemTooltip => _itemTooltip;
        public bool CanEditLoadout => Phase == PrototypePhase.Preparation && !IsShopReturnPresenting
            && !IsSettingsOpen && !IsStageAbandonOpen && !IsEnemyPreviewOpen && _tutorialNamePopup?.IsOpen != true;
        public bool IsEnemyPreviewOpen => _enemyPreview?.IsOpen == true;

        public bool TryOpenEnemyPreview()
        {
            if (!CanEditLoadout || _draggingItem != null || _preparedEnemy == null) return false;
            _fusionHoverOwner = null;
            ClearPlacementOverlay();
            _enemyPreview.Show(_preparedEnemy);
            RefreshCombatInterface();
            return true;
        }

        public void CloseEnemyPreview()
        {
            if (!IsEnemyPreviewOpen) return;
            _enemyPreview.Hide();
            if (!IsMainMenu) RefreshCombatInterface();
        }
        public CharacterStatsSnapshot StageEntryStats => _run.PlayerStats;

        public CombatLoadoutSnapshot CaptureCombatSnapshot()
        {
            var snapshots = new List<CombatItemSnapshot>();
            foreach (BackpackItemView view in _itemViews.Values)
            {
                if (_grid.TryGetOccupiedCells(view.InstanceId, out var occupiedCells))
                {
                    _run.StartingGear.TryGetValue(view.InstanceId, out var gear);
                    snapshots.Add(new CombatItemSnapshot(view.InstanceId, view.Data, view.Origin, view.Rotation, occupiedCells,
                        ownerStats: _run.PlayerStats, rarity: gear?.Rarity, gearEffect: gear?.Effect ?? default));
                }
            }
            return new CombatLoadoutSnapshot(snapshots, GridSize, GridSize);
        }

        public bool TryStartBattle()
        {
            if (!CanEditLoadout || _draggingItem != null) return false;
            if (_tutorialActive && (!TutorialCanStartBattle
                || (_run.Round == 1 && !GetReadyFusions().Any(plan => plan.Result.ItemId == "steel_sword")))) return false;
            if (_preparedEnemy == null)
            {
                SetStatus(_enemyConfigurationError ?? "스테이지의 적 설정을 확인하세요.");
                return false;
            }

            var player = new CombatantSnapshot(_run.PlayerName, _run.PlayerStats.MaximumHealth, CaptureCombatSnapshot(), _run.PlayerStats.Attack,
                _characterData?.Portrait, _characterData?.PortraitFacesLeft ?? false, _run.PlayerStats.CriticalDamageBonusPercent,
                _run.Passive, _run.StartingLives - _run.Lives, _run.PlayerStats.CriticalChanceBonusPercent,
                _run.PlayerStats.ArmorGainBonusPercent, _run.PlayerStats.HealingBonusPercent, _run.PlagueStacks);
            CapturePendingFusions();
            if (!_run.TryBeginBattle()) return false;
            _itemTooltip.HideAll();
            ClearPlacementOverlay();
            // Use exactly the same resolved snapshot shown in preparation, never the source ItemData values.
            _battleController.BeginBattle(player, _preparedEnemy, _run.Round, RoundIncome(_run.Round + 1),
                statusEffects: _statusEffects, random: _tutorialActive ? new System.Random(_run.Round) : null,
                isOriginMine: _run.IsOriginMine);
            _selectedItem = null;
            _sellPreview.gameObject.SetActive(false);
            RefreshCombatInterface();
            return true;
        }

        public void TickCombat(float deltaSeconds) => _battleController.TickCombat(deltaSeconds);

        public bool ReturnToPreparation()
        {
            if (IsSettingsOpen || !_run.TryReturnToPreparation()) return false;
            if (_tutorialActive)
            {
                _battleController.EndBattle();
                _pendingFusions = Array.Empty<FusionPlan>();
                RefreshCombatInterface();
                return true;
            }
            int income = RoundIncome(_run.Round);
            _battleController.EndBattle();
            FinishBattleShopEntry(income);
            UpdateEconomyText();
            RefreshCombatInterface();
            SetStatus($"남은 라이프 {_run.Lives}. 같은 라운드에 재도전합니다. 라운드 수입 +{income} G");
            return true;
        }

        public void HandleCombatAction()
        {
            if (_tutorialActive && (IsSettingsOpen || _tutorialNamePopup.IsOpen)) return;
            if (_tutorialActive && Phase == PrototypePhase.Completed) { ShowTutorialName(); return; }
            if (_tutorialActive && Phase == PrototypePhase.GameOver) { StartTutorial(); return; }
            if (Phase == PrototypePhase.Preparation) TryStartBattle();
            else if (Phase == PrototypePhase.Victory) AdvanceRound();
            else if (Phase == PrototypePhase.Defeat || Phase == PrototypePhase.Draw) ReturnToPreparation();
        }

        private void InitializeBattleScreen(Transform canvas)
        {
            var controllerObject = new GameObject("Battle Controller", typeof(PrototypeBattleController));
            controllerObject.transform.SetParent(transform, false);
            _battleController = controllerObject.GetComponent<PrototypeBattleController>();
            _battleController.Initialize(canvas, OnBattleFinished, HandleCombatAction, _itemTooltip, PlayCombatHit);
            _battleController.PlaybackStateChanged += RefreshMusic;
        }

        private void OnBattleFinished(CombatResult result)
        {
            if (!_run.TryCompleteBattle(result, StageRoundCount, ActiveCombat.ElapsedSeconds,
                ActiveCombat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Plague))) return;
            PlaySound(Phase == PrototypePhase.Completed ? PrototypeSound.StageClear
                : result == CombatResult.Victory ? PrototypeSound.RoundClear : PrototypeSound.RoundFail);
            if (_tutorialActive)
            {
                ShowTutorialBattleResult();
                return;
            }
            if (_run.IsFinished) TrySettleRunReward();
            else RecordProgress();
            int returnIncome = RoundIncome(Phase == PrototypePhase.Victory ? _run.Round + 1 : _run.Round);
            _battleController.ShowResult(Phase, returnIncome);
            UpdateEconomyText();
            if (_run.IsFinished)
            {
                _pendingFusions = Array.Empty<FusionPlan>();
                _itemTooltip.HideAll(); _fusionHoverOwner = null; ClearPlacementOverlay();
                _runResultScreen.Show(_run.Result, _trackProgress ? _run.Result.ExperienceReward : (long?)null, _saveNotice,
                    _trackProgress ? DescribeReward(_pendingStageReward) : null);
            }
            RefreshCombatInterface();
            if (_run.IsFinished) RunEnded?.Invoke(_run.Result);
        }

        private void RefreshCombatInterface()
        {
            RefreshMusic();
            bool showShop = Phase is PrototypePhase.Preparation or PrototypePhase.Fusion;
            if (_shopScreenRoot.gameObject.activeSelf != showShop) _itemTooltip.HideAll();
            _shopScreenRoot.gameObject.SetActive(showShop);
            _battleController.SetVisible(!IsMainMenu && !IsTitleScreen && !showShop);
            _lifeHearts.gameObject.SetActive(!IsMainMenu && !IsTitleScreen);
            _lifeHearts.SetLives(_run.Lives, _run.StartingLives);
            _stageSettingsButton.gameObject.SetActive(!IsMainMenu && !IsTitleScreen);
            _stageSettingsButton.interactable = !IsShopReturnPresenting && !IsStageAbandonOpen && !IsEnemyPreviewOpen;
            _stageAbandonButton.interactable = CanEditLoadout;
            _stageAbandonButton.gameObject.SetActive(!_tutorialActive);
            if (!IsMainMenu && !IsSettingsOpen && !IsStageAbandonOpen && !IsEnemyPreviewOpen)
            {
                _lifeHearts.transform.SetAsLastSibling();
                _stageSettingsButton.transform.SetAsLastSibling();
            }
            SetInputEnabled(_shopInputGroup, CanEditLoadout);
            SetInputEnabled(_gridInputGroup, CanEditLoadout);
            SetInputEnabled(_storageInputGroup, CanEditLoadout);
            if (showShop) RenderEncounterPreview();
            RenderTutorialGuide();
        }

        /// <summary>Explicitly reload edited SO values during preparation. Does not spend gold or advance the round.</summary>
        public bool RefreshEncounterPreview()
        {
            if (!CanEditLoadout || _draggingItem != null) return false;
            bool valid = TryResolveEnemy(_run.Round, out _preparedEnemy, out _enemyConfigurationError);
            RenderEncounterPreview();
            return valid;
        }

        private bool TryResolveEnemy(int round, out CombatantSnapshot enemy, out string error)
        {
            enemy = null;
            try
            {
                if (_tutorialActive)
                {
                    var training = AvailableStages.First().GetRound(1).CreateEnemySnapshot();
                    enemy = new CombatantSnapshot(training.DisplayName, round == 1 ? 12d : 18d,
                        training.Loadout, training.Stats.Attack, training.Illustration, training.IllustrationFacesLeft);
                    error = null;
                    return true;
                }
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
            _preparationPanel.Render(_enemyConfigurationError, _characterData);
            _preparationStage.text = _run.StageName ?? "";
            _preparationPanel.SetInputEnabled(CanEditLoadout && _preparedEnemy != null, !_tutorialActive || TutorialCanStartBattle);
            _preparationPanel.RenderTutorial(_tutorialActive);
        }

        public void ShowItemTooltip(Component owner, ItemData item, PointerEventData pointer)
        {
            if (!CanEditLoadout || _draggingItem != null || item == null) return;
            _fusionHoverOwner = owner;
            string id = owner switch
            {
                BackpackItemView bag => bag.InstanceId,
                PrototypeStorageItemView storage => storage.StoredItem.InstanceId,
                _ => null
            };
            StartingGear gear = id != null && _run.StartingGear.TryGetValue(id, out var equipped) ? equipped : null;
            _itemTooltip.Show(owner, () => ItemTooltipContent.FromItem(item, _run.PlayerStats, gear?.Rarity, gear?.Effect ?? default),
                pointer.position, pointer.enterEventCamera);
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
