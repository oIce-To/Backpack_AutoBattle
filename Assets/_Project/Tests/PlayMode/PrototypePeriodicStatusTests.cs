using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Enemies;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using BackpackAutoBattle.Shop;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class PrototypePeriodicStatusTests
    {
        [UnityTest]
        public IEnumerator StrongerCharacter_ShowsResolvedNumbersInShopStorageBagAndBattle()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single); yield return null;
            var previous = Object.FindFirstObjectByType<BackpackPrototypeController>();
            var character = Object.Instantiate((CharacterData)Field(previous, "_characterData"));
            Set(character, "_maximumHealth", 100f); Set(character, "_attack", 20f);
            var host = new GameObject("Scaled character test"); host.SetActive(false);
            var controller = host.AddComponent<BackpackPrototypeController>();
            Set(controller, "_characterData", character);
            Set(controller, "_itemCatalog", previous.ItemCatalog.ToArray());
            Set(controller, "_shopRarityTable", previous.ShopRarityTable);
            Set(controller, "_stageData", previous.Stage);
            Set(controller, "_statusEffects", Field(previous, "_statusEffects"));
            previous.gameObject.SetActive(false); host.SetActive(true); yield return null;
            try
            {
                controller.BattleController.AutoTick = false;
                var slot = controller.GetComponentsInChildren<PrototypeShopSlotView>().First();
                slot.Bind(new ShopOffer(controller.ItemCatalog.Single(i => i.ItemId == "training_blade"), false));
                slot.OnPointerEnter(Pointer(slot.transform)); Assert.That(Value(controller, "Attack"), Is.EqualTo("10"));
                var pointer = Pointer(slot.transform); slot.OnBeginDrag(pointer);
                pointer.position = Screen(Find(controller, "Freeform Storage")); slot.OnDrag(pointer); slot.OnEndDrag(pointer);
                yield return null;
                var stored = controller.GetComponentsInChildren<PrototypeStorageItemView>().Single();
                stored.OnPointerEnter(Pointer(stored.transform)); Assert.That(Value(controller, "Attack"), Is.EqualTo("10"));
                var views = controller.GetComponentsInChildren<BackpackItemView>();
                var shield = views.Single(v => v.Data.ItemId == "wooden_shield");
                shield.OnPointerEnter(Pointer(shield.transform)); Assert.That(Description(controller), Does.Contain("방어도를 5").And.Not.Contain("5%"));
                var sword = views.Single(v => v.Data.ItemId == "training_blade");
                sword.OnPointerEnter(Pointer(sword.transform)); Assert.That(Value(controller, "Attack"), Is.EqualTo("10"));
                Buy(controller, "whetstone", 1, 2);
                var stone = controller.GetComponentsInChildren<BackpackItemView>().Single(v => v.Data.ItemId == "whetstone");
                stone.OnPointerEnter(Pointer(stone.transform)); Assert.That(Description(controller), Does.Contain("공격력을 2 증가").And.Not.Contain("10%"));
                // Subsequent profile edits must not change this run's resolved values.
                Set(character, "_attack", 100f);
                controller.TryStartBattle();
                Assert.That(controller.ActiveCombat.PlayerMaxHealth, Is.EqualTo(100));
                Assert.That(controller.ActiveCombat.PlayerWeapons.Single().Stats.AttackDamage, Is.EqualTo(12));
                var view = controller.GetComponentsInChildren<PrototypeBattleItemView>().Single(v => v.Side == CombatSide.Player && v.Snapshot.ItemId == "training_blade");
                view.OnPointerEnter(Pointer(view.transform));
                Assert.That(Value(controller, "Attack"), Is.EqualTo("12"));
                Text value = controller.ItemTooltip.GetComponentsInChildren<Text>().Single(t => t.name == "Attack Value");
                Assert.That(value.color.g, Is.GreaterThan(value.color.r));
                PrototypeScreenCapture.CaptureIfRequested(controller.GetComponentInChildren<Canvas>(), "ScaledCharacterBattleTooltip.png");
            }
            finally { Object.Destroy(host); Object.Destroy(character); previous.gameObject.SetActive(true); }
        }

        [UnityTest]
        public IEnumerator StageEntryStats_RemainFixedAcrossScreensAndRounds_WithoutShowingCoefficients()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single); yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            controller.BattleController.AutoTick = false;
            CharacterData original = (CharacterData)Field(controller, "_characterData");
            var changed = Object.Instantiate(original);
            try
            {
                Set(changed, "_maximumHealth", 500f); Set(changed, "_attack", 100f);
                Set(controller, "_characterData", changed);
                Assert.That(controller.StageEntryStats.MaximumHealth, Is.EqualTo(50));
                Assert.That(controller.StageEntryStats.Attack, Is.EqualTo(10));
                var swordData = controller.ItemCatalog.Single(i => i.ItemId == "training_blade");
                var slot = controller.GetComponentsInChildren<PrototypeShopSlotView>().First();
                slot.Bind(new ShopOffer(swordData, false)); slot.OnPointerEnter(Pointer(slot.transform));
                Assert.That(Value(controller, "Attack"), Is.EqualTo("5"));
                var pointer = Pointer(slot.transform); slot.OnBeginDrag(pointer);
                pointer.position = Screen(Find(controller, "Freeform Storage")); slot.OnDrag(pointer); slot.OnEndDrag(pointer);
                yield return null;
                var stored = controller.GetComponentsInChildren<PrototypeStorageItemView>().Single();
                stored.OnPointerEnter(Pointer(stored.transform)); Assert.That(Value(controller, "Attack"), Is.EqualTo("5"));
                var shield = controller.GetComponentsInChildren<BackpackItemView>().Single(v => v.Data.ItemId == "wooden_shield");
                shield.OnPointerEnter(Pointer(shield.transform));
                Assert.That(Description(controller), Does.Contain("방어도를 2.5").And.Not.Contain("5%"));
                PrototypeScreenCapture.CaptureIfRequested(controller.GetComponentInChildren<Canvas>(), "ScaledShieldTooltip.png");
                controller.TryStartBattle();
                Assert.That(controller.ActiveCombat.PlayerMaxHealth, Is.EqualTo(50));
                var runtime = controller.ActiveCombat.PlayerWeapons.Single();
                Assert.That(runtime.Stats.AttackDamage, Is.EqualTo(5));
                runtime.Stats.AttackDamage = 1000; runtime.Stats.AccuracyPercent = 100;
                controller.TickCombat(3); Assert.That(controller.Phase, Is.EqualTo(PrototypePhase.Victory));
                Assert.That(controller.AdvanceRound(), Is.True); yield return null;
                controller.TryStartBattle();
                Assert.That(controller.ActiveCombat.PlayerMaxHealth, Is.EqualTo(50));
                Assert.That(controller.ActiveCombat.PlayerWeapons.Single().Stats.AttackDamage, Is.EqualTo(5));
            }
            finally { Set(controller, "_characterData", original); Object.Destroy(changed); }
        }

        [UnityTest]
        public IEnumerator PurchasedFireAndPoisonItems_RenderIndependentTicksLiveIconsTooltipsAndChronologicalLogs()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single); yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>(); controller.BattleController.AutoTick = false;
            var enemy = Object.Instantiate(controller.CurrentEnemyEncounter);
            try
            {
                Set(enemy, "_maximumHealth", 300); Set(controller, "_debugEnemyOverride", enemy);
                Assert.That(controller.RefreshEncounterPreview(), Is.True);
                Buy(controller, "fire_staff", 3, 0); Buy(controller, "poison_vial", 4, 4);
                Assert.That(controller.Gold, Is.EqualTo(6));
                controller.TryStartBattle();
                var combat = controller.ActiveCombat;
                var ticks = new List<PeriodicStatusTickEvent>(); combat.StatusTicked += ticks.Add;
                controller.TickCombat(4.3f); yield return null; Canvas.ForceUpdateCanvases();
                Assert.That(ticks.Select(t => t.Kind), Is.EquivalentTo(new[] { StatusEffectKind.Poison, StatusEffectKind.Burn }));
                Assert.That(ticks.Single(t => t.Kind == StatusEffectKind.Burn).Damage, Is.EqualTo(2.4).Within(1e-5));
                Assert.That(ticks.Single(t => t.Kind == StatusEffectKind.Poison).Damage, Is.EqualTo(2).Within(1e-5));
                foreach (var kind in new[] { StatusEffectKind.Burn, StatusEffectKind.Poison })
                {
                    var icon = controller.GetComponentsInChildren<PrototypeStatusIcon>().Single(i => i.Side == CombatSide.Enemy && i.Kind == kind);
                    Assert.That(icon.GetComponentInChildren<PrototypeStatusGlyph>().canvasRenderer.GetMesh().vertexCount, Is.GreaterThan(0));
                    icon.OnPointerEnter(Pointer(icon.transform));
                    Assert.That(Description(controller), Does.Contain(kind == StatusEffectKind.Burn ? "1.2 피해" : "2 피해"));
                    Assert.That(Description(controller), Does.Not.Contain("%").And.Not.Contain("공격력"));
                    PrototypeScreenCapture.CaptureIfRequested(controller.GetComponentInChildren<Canvas>(), kind + "LiveTooltip.png");
                    icon.OnPointerExit(Pointer(icon.transform));
                }
                var log = Find(controller, "Combat Log").GetComponent<Text>().text;
                Assert.That(log, Does.Contain("화상").And.Contain("중독"));
                var times = log.Split('\n').Select(line => float.Parse(line.Split(' ')[0].TrimEnd('s'), CultureInfo.InvariantCulture)).ToArray();
                Assert.That(times, Is.Ordered);
                combat.RemoveStatus(CombatSide.Enemy, StatusEffectKind.Burn, long.MaxValue);
                controller.TickCombat(0);
                Assert.That(controller.GetComponentsInChildren<PrototypeStatusIcon>(true)
                    .Single(i => i.Side == CombatSide.Enemy && i.Kind == StatusEffectKind.Burn).gameObject.activeSelf, Is.False);
            }
            finally { Object.Destroy(enemy); }
        }

        private static void Buy(BackpackPrototypeController controller, string id, int x, int y)
        {
            var slot = controller.GetComponentsInChildren<PrototypeShopSlotView>().First();
            slot.Bind(new ShopOffer(controller.ItemCatalog.Single(i => i.ItemId == id), false));
            var pointer = Pointer(slot.transform); slot.OnBeginDrag(pointer);
            pointer.position = Screen(Find(controller, $"Grid Cell {x},{y}")); slot.OnDrag(pointer); slot.OnEndDrag(pointer);
        }
        private static string Description(BackpackPrototypeController c) => c.ItemTooltip.GetComponentInChildren<TextMeshProUGUI>().text;
        private static string Value(BackpackPrototypeController c, string stat) => c.ItemTooltip.GetComponentsInChildren<Text>().Single(t => t.name == stat + " Value").text;
        private static Transform Find(BackpackPrototypeController c, string name) => c.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
        private static Vector2 Screen(Transform target) => RectTransformUtility.WorldToScreenPoint(null, target.position);
        private static PointerEventData Pointer(Transform target) => new(EventSystem.current) { position = Screen(target), button = PointerEventData.InputButton.Left };
        private static object Field(object target, string field) => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }
}
