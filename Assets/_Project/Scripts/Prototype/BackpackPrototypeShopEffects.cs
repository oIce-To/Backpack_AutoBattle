using System.Linq;
using BackpackAutoBattle.Items;
using UnityEngine.EventSystems;

namespace BackpackAutoBattle.Prototype
{
    public sealed partial class BackpackPrototypeController
    {
        private PrototypePiggyBankPresentation _piggyBankPresentation;
        private bool IsShopReturnPresenting => _piggyBankPresentation != null || IsFusionPresenting;

        private static int RoundIncome(int round) => round <= 4 ? 5 : round <= 8 ? 7 : 10;

        private (int freeRerolls, float saleBonus, ItemData raritySource, int rarityShift, double shiftChance) GetShopEffects()
        {
            var items = _itemViews.Values.Where(view => _grid.TryGetOccupiedCells(view.InstanceId, out _))
                .Select(view => view.Data).ToArray();
            int cards = items.Count(item => item.StageEffect == StageItemEffect.PlatinumCard);
            var platinum = items.FirstOrDefault(item => item.StageEffect == StageItemEffect.PlatinumClover);
            var source = items.FirstOrDefault(item => item.StageEffect == StageItemEffect.CursedClover)
                ?? platinum ?? items.FirstOrDefault(item => item.StageEffect == StageItemEffect.Clover);
            var (shift, chance) = source?.StageEffect switch
            {
                StageItemEffect.CursedClover => (-1, .15),
                StageItemEffect.PlatinumClover => (1, .25),
                StageItemEffect.Clover => (1, .10),
                _ => (0, 0d)
            };
            return (platinum != null ? 2 : cards, platinum != null ? .10f : cards * .05f, source, shift, chance);
        }

        private void FinishBattleShopEntry(int income)
        {
            var banks = _itemViews.Values.Where(view => view.Data.StageEffect == StageItemEffect.PiggyBank
                && _grid.TryGetOccupiedCells(view.InstanceId, out _)).ToArray();
            _run.Gold += income + banks.Length;
            _run.FreeShopRerolls = GetShopEffects().freeRerolls;
            if (banks.Length == 0)
            {
                BeginFusionPresentation();
                return;
            }

            _itemTooltip.HideAll();
            ClearPlacementOverlay();
            EventSystem.current?.SetSelectedGameObject(null);
            _piggyBankPresentation = new PrototypePiggyBankPresentation(_shopScreenRoot, banks);
        }

        internal void TickPiggyBankPresentation(float delta)
        {
            if (_piggyBankPresentation == null || !_piggyBankPresentation.Tick(delta)) return;
            DisposePiggyBankPresentation();
            BeginFusionPresentation();
            RefreshCombatInterface();
        }

        private void DisposePiggyBankPresentation()
        {
            _piggyBankPresentation?.Dispose();
            _piggyBankPresentation = null;
        }
    }
}
