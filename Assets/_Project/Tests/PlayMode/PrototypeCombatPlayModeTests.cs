using System.Collections;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Prototype;
using BackpackAutoBattle.Shop;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class PrototypeCombatPlayModeTests
    {
        [UnityTest]
        public IEnumerator ScreenSwitch_PreservesStoragePositionsAndRotatedShopItems()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            BackpackPrototypeController controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            controller.BattleController.AutoTick = false;
            var pointer = new PointerEventData(Object.FindFirstObjectByType<EventSystem>())
            { button = PointerEventData.InputButton.Left };
            var potion = controller.ItemCatalog.Single(item => item.ItemId == "healing_potion");
            PrototypeShopSlotView slot = Object.FindFirstObjectByType<PrototypeShopSlotView>();
            slot.Bind(new ShopOffer(potion, false));
            pointer.position = ScreenPoint(slot.transform);
            slot.OnBeginDrag(pointer);
            pointer.position = ScreenPoint(Find(controller, "Freeform Storage")) + new Vector2(95f, 0f);
            slot.OnDrag(pointer);
            slot.OnEndDrag(pointer);
            yield return null;

            PrototypeStorageItemView stored = Object.FindFirstObjectByType<PrototypeStorageItemView>();
            Vector2 storagePosition = stored.StoredItem.StoragePosition;
            BackpackItemView sword = Object.FindObjectsByType<BackpackItemView>(FindObjectsSortMode.None)
                .Single(view => view.InstanceId == "starter_training_blade");
            pointer.position = ScreenPoint(sword.transform);
            sword.OnBeginDrag(pointer);
            pointer.position = ScreenPoint(Find(controller, "Grid Cell 0,3"));
            sword.OnDrag(pointer);
            sword.OnEndDrag(pointer);
            controller.RotateItem(sword);
            Assert.That(sword.Rotation, Is.EqualTo(ItemRotation.Clockwise90));
            Vector2Int origin = sword.Origin;
            Vector2 shopViewPosition = sword.RectTransform.anchoredPosition;
            int goldBefore = controller.Gold;
            controller.SelectItem(sword);
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            PrototypeScreenCapture.CaptureIfRequested(canvas, "Shop.png");

            Assert.That(controller.TryStartBattle(), Is.True);
            Assert.That(controller.GetComponentsInChildren<PrototypeBattleItemView>()
                .All(view => view.Snapshot.InstanceId != stored.StoredItem.InstanceId), Is.True);
            CombatWeapon weapon = controller.ActiveCombat.PlayerWeapons.Single();
            weapon.Stats.AccuracyPercent = 100f;
            weapon.Stats.CriticalChancePercent = 0f;
            controller.TickCombat(2f);
            Assert.That(controller.ActiveCombat.PlayerHealth, Is.EqualTo(36d));
            PrototypeBattleItemView enemyView = controller.GetComponentsInChildren<PrototypeBattleItemView>()
                .Single(view => view.Side == CombatSide.Enemy);
            enemyView.OnPointerClick(pointer);
            Assert.That(Find(controller, "Battle Item Details").GetComponent<Text>().text, Does.Contain("고블린 몽둥이"));
            Assert.That((object)enemyView, Is.Not.InstanceOf<IBeginDragHandler>());
            PrototypeScreenCapture.CaptureIfRequested(canvas, "Battle.png");

            weapon.Stats.AttackDamage = 100f;
            controller.TickCombat(2f);
            Assert.That(controller.Phase, Is.EqualTo(PrototypePhase.Victory));
            PrototypeScreenCapture.CaptureIfRequested(canvas, "Victory.png");
            Assert.That(controller.AdvanceRound(), Is.True);
            Assert.That(controller.StoredItemCount, Is.EqualTo(1));
            Assert.That(stored.StoredItem.StoragePosition, Is.EqualTo(storagePosition));
            Assert.That(stored.gameObject.activeInHierarchy, Is.True);
            Assert.That(sword.Origin, Is.EqualTo(origin));
            Assert.That(sword.Rotation, Is.EqualTo(ItemRotation.Clockwise90));
            Assert.That(sword.RectTransform.anchoredPosition, Is.EqualTo(shopViewPosition));
            Assert.That(controller.Gold, Is.EqualTo(goldBefore + 5));
            Assert.That(controller.CurrentShopItems.All(item => item.ItemId != "goblin_club"), Is.True);
        }

        [UnityTest]
        public IEnumerator Battle_AutomaticallyTicksAndRendersHealthAndLogs()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            BackpackPrototypeController controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            Assert.That(controller.TryStartBattle(), Is.True);
            CombatWeapon weapon = controller.ActiveCombat.PlayerWeapons.Single();
            weapon.Stats.AttackDamage = 100f;
            weapon.Stats.AccuracyPercent = 100f;
            weapon.Stats.CriticalChancePercent = 0f;
            float previousTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 10f;
                float deadline = Time.realtimeSinceStartup + 6f;
                while (controller.Phase == PrototypePhase.Battle && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                Assert.That(controller.Phase, Is.EqualTo(PrototypePhase.Victory));
                Assert.That(Find(controller, "Enemy Health Text").GetComponent<Text>().text, Does.Contain("0 / 30"));
                Assert.That(Find(controller, "Combat Log").GetComponent<Text>().text, Does.Contain("명중"));
                Assert.That(Find(controller, "Enemy Health").GetChild(0).GetComponent<RectTransform>().anchorMax.x, Is.Zero);
            }
            finally
            {
                Time.timeScale = previousTimeScale;
            }
        }

        [UnityTest]
        public IEnumerator Battle_UsesOnlyBackpackWeaponsLocksPreparationAndPaysNextRoundOnce()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            BackpackPrototypeController controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            controller.enabled = false;
            controller.BattleController.AutoTick = false; // Advance deterministic test time instead of rendering frames.
            SetField(controller, "_enableDebugRoundAdvance", false);
            Assert.That(controller.AdvanceRound(), Is.False);

            var dagger = controller.ItemCatalog.Single(item => item.ItemId == "iron_dagger");
            PrototypeShopSlotView slot = Object.FindFirstObjectByType<PrototypeShopSlotView>();
            slot.Bind(new ShopOffer(dagger, false));
            var pointer = new PointerEventData(Object.FindFirstObjectByType<EventSystem>())
            {
                button = PointerEventData.InputButton.Left,
                position = ScreenPoint(slot.transform)
            };
            slot.OnBeginDrag(pointer);
            Assert.That(controller.TryStartBattle(), Is.False);
            pointer.position = ScreenPoint(Find(controller, "Freeform Storage"));
            slot.OnDrag(pointer);
            slot.OnEndDrag(pointer);
            yield return null;

            CombatLoadoutSnapshot beforePlacement = controller.CaptureCombatSnapshot();
            Assert.That(beforePlacement.Items, Has.Count.EqualTo(3));
            Assert.That(beforePlacement.Items.Count(item => item.Category == BackpackAutoBattle.Items.ItemCategory.Weapon), Is.EqualTo(1));
            PrototypeStorageItemView stored = Object.FindFirstObjectByType<PrototypeStorageItemView>();
            string daggerInstanceId = stored.StoredItem.InstanceId;
            Assert.That(beforePlacement.Items.All(item => item.InstanceId != daggerInstanceId), Is.True);

            pointer.position = ScreenPoint(stored.transform);
            stored.OnBeginDrag(pointer);
            pointer.position = ScreenPoint(Find(controller, "Grid Cell 5,4"));
            stored.OnDrag(pointer);
            stored.OnEndDrag(pointer);
            yield return null;

            Assert.That(controller.TryStartBattle(), Is.True);
            CombatSimulation battle = controller.ActiveCombat;
            Assert.That(battle.Loadout.Items, Has.Count.EqualTo(4));
            Assert.That(battle.PlayerWeapons, Has.Count.EqualTo(2));
            Assert.That(battle.Loadout.Items.Single(item => item.InstanceId == daggerInstanceId).Origin,
                Is.EqualTo(new Vector2Int(5, 4)));
            Assert.That(controller.Phase, Is.EqualTo(PrototypePhase.Battle));
            Assert.That(Find(controller, "Shop Screen").gameObject.activeSelf, Is.False);
            Assert.That(Find(controller, "Battle Screen").gameObject.activeSelf, Is.True);
            Assert.That(battle.Enemy.DisplayName, Is.EqualTo("고블린"));
            Assert.That(battle.EnemyMaxHealth, Is.EqualTo(30d));
            Assert.That(battle.Enemy.Loadout.Width, Is.EqualTo(3));
            Assert.That(battle.Enemy.Loadout.Height, Is.EqualTo(3));
            Assert.That(Find(controller, "Enemy Battle Backpack").GetComponentsInChildren<Transform>()
                .Count(child => child.name.StartsWith("Battle Grid Cell ")), Is.EqualTo(9));
            Assert.That(Find(controller, "Player Battle Backpack").GetComponentsInChildren<Transform>()
                .Count(child => child.name.StartsWith("Battle Grid Cell ")), Is.EqualTo(36));
            PrototypeBattleItemView[] battleViews = controller.GetComponentsInChildren<PrototypeBattleItemView>();
            Assert.That(battleViews.Count(view => view.Side == CombatSide.Player), Is.EqualTo(4));
            Assert.That(battleViews.Count(view => view.Side == CombatSide.Enemy), Is.EqualTo(1));
            Assert.That(battleViews.Where(view => view.Side == CombatSide.Enemy).Single().Snapshot.DisplayName,
                Is.EqualTo("고블린 몽둥이"));
            Assert.That(controller.CanEditLoadout, Is.False);
            Assert.That(controller.TryStartBattle(), Is.False);
            Assert.That(controller.TryRerollShop(), Is.False);
            Assert.That(controller.AdvanceRound(), Is.False);

            BackpackItemView sword = controller.GetComponentsInChildren<BackpackItemView>(true)
                .Single(item => item.InstanceId == "starter_training_blade");
            Vector2Int originalOrigin = sword.Origin;
            ItemRotation originalRotation = sword.Rotation;
            pointer.position = ScreenPoint(sword.transform);
            sword.OnBeginDrag(pointer);
            pointer.position = ScreenPoint(Find(controller, "Prototype Shop"));
            sword.OnDrag(pointer);
            sword.OnEndDrag(pointer);
            controller.RotateItem(sword);
            Assert.That(sword.Origin, Is.EqualTo(originalOrigin));
            Assert.That(sword.Rotation, Is.EqualTo(originalRotation));
            Assert.That(Find(controller, "Backpack Grid").GetComponent<CanvasGroup>().blocksRaycasts, Is.False);

            CombatWeapon combatSword = battle.PlayerWeapons.Single(weapon => weapon.InstanceId == sword.InstanceId);
            combatSword.Stats.AttackDamage = 100f;
            combatSword.Stats.AccuracyPercent = 100f;
            combatSword.Stats.CriticalChancePercent = 100f;
            int goldBeforeVictory = controller.Gold;
            controller.TickCombat(2f);
            Assert.That(controller.Phase, Is.EqualTo(PrototypePhase.Victory));
            Assert.That(battle.EnemyHealth, Is.Zero);
            Assert.That(controller.Gold, Is.EqualTo(goldBeforeVictory));
            Text combatLog = Find(controller, "Combat Log").GetComponent<Text>();
            Assert.That(combatLog.text, Does.Contain("치명타"));
            Assert.That(sword.Data.BaseWeaponStats.AttackDamage, Is.EqualTo(combatSword.BaseStats.AttackDamage));
            Assert.That(sword.Data.BaseWeaponStats.AttackDamage, Is.Not.EqualTo(100f));

            Find(controller, "Combat Action Button").GetComponent<Button>().onClick.Invoke();
            Assert.That(controller.Phase, Is.EqualTo(PrototypePhase.Preparation));
            Assert.That(controller.CurrentRound, Is.EqualTo(2));
            Assert.That(Find(controller, "Shop Screen").gameObject.activeSelf, Is.True);
            Assert.That(Find(controller, "Battle Screen").gameObject.activeSelf, Is.False);
            Assert.That(sword.Origin, Is.EqualTo(originalOrigin));
            Assert.That(sword.Rotation, Is.EqualTo(originalRotation));
            Assert.That(controller.StoredItemCount, Is.Zero);
            Assert.That(controller.Gold, Is.EqualTo(goldBeforeVictory + 5));
            Assert.That(controller.CurrentShopItems, Has.Count.EqualTo(4));
            Assert.That(controller.AdvanceRound(), Is.False);
            Assert.That(controller.Gold, Is.EqualTo(goldBeforeVictory + 5));
            Assert.That(Find(controller, "Backpack Grid").GetComponent<CanvasGroup>().blocksRaycasts, Is.True);

            Assert.That(controller.TryStartBattle(), Is.True);
            Assert.That(controller.ActiveCombat.PlayerWeapons.Single(weapon => weapon.InstanceId == sword.InstanceId).Stats.AttackDamage,
                Is.EqualTo(sword.Data.BaseWeaponStats.AttackDamage));
            Assert.That(controller.ActiveCombat.PlayerHealth, Is.EqualTo(40d));
        }

        [UnityTest]
        public IEnumerator Defeat_ReturnsToPreparationWithoutIncomeOrRoundChange()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            BackpackPrototypeController controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            controller.enabled = false;
            controller.BattleController.AutoTick = false;
            int goldBefore = controller.Gold;
            Assert.That(controller.TryStartBattle(), Is.True);
            controller.ActiveCombat.EnemyWeapons.Single().Stats.AttackDamage = 100f;
            controller.ActiveCombat.EnemyWeapons.Single().Stats.AccuracyPercent = 100f;
            controller.ActiveCombat.EnemyWeapons.Single().Stats.CriticalChancePercent = 0f;
            controller.TickCombat(3f);
            Assert.That(controller.Phase, Is.EqualTo(PrototypePhase.Defeat));
            Assert.That(controller.ActiveCombat.PlayerHealth, Is.Zero);
            Assert.That(controller.AdvanceRound(), Is.False);
            Assert.That(controller.ReturnToPreparation(), Is.True);
            Assert.That(controller.Phase, Is.EqualTo(PrototypePhase.Preparation));
            Assert.That(controller.CurrentRound, Is.EqualTo(1));
            Assert.That(controller.Gold, Is.EqualTo(goldBefore));
            Assert.That(controller.TryStartBattle(), Is.True);
            Assert.That(controller.ActiveCombat.PlayerHealth, Is.EqualTo(40d));
        }

        [UnityTest]
        public IEnumerator TwelveVictories_CompleteRunWithoutThirteenthRoundOrExtraIncome()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            BackpackPrototypeController controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            controller.enabled = false;
            controller.BattleController.AutoTick = false;
            SetField(controller, "_enableDebugRoundAdvance", false);
            for (int round = 1; round <= 12; round++)
            {
                Assert.That(controller.CurrentRound, Is.EqualTo(round));
                Assert.That(controller.TryStartBattle(), Is.True);
                CombatWeapon weapon = controller.ActiveCombat.PlayerWeapons.Single();
                weapon.Stats.AttackDamage = 100f;
                weapon.Stats.AccuracyPercent = 100f;
                controller.TickCombat(2f);
                if (round < 12)
                {
                    Assert.That(controller.Phase, Is.EqualTo(PrototypePhase.Victory));
                    Assert.That(controller.AdvanceRound(), Is.True);
                    Assert.That(controller.AdvanceRound(), Is.False);
                }
            }

            Assert.That(controller.Phase, Is.EqualTo(PrototypePhase.Completed));
            Assert.That(controller.CurrentRound, Is.EqualTo(12));
            Assert.That(controller.Gold, Is.EqualTo(20 + 11 * 5));
            Assert.That(controller.AdvanceRound(), Is.False);
            Assert.That(controller.TryStartBattle(), Is.False);
            Assert.That(Find(controller, "Combat Action Button").GetComponent<Button>().interactable, Is.False);
        }

        private static void SetField(BackpackPrototypeController controller, string name, object value)
        {
            typeof(BackpackPrototypeController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, value);
        }

        private static Transform Find(BackpackPrototypeController controller, string name)
        {
            return controller.GetComponentsInChildren<Transform>(true).Single(child => child.name == name);
        }

        private static Vector2 ScreenPoint(Transform target) => RectTransformUtility.WorldToScreenPoint(null, target.position);
    }
}
