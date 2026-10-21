using System.Collections;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Enemies;
using BackpackAutoBattle.Prototype;
using BackpackAutoBattle.Stages;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class PrototypeStagePlayModeTests
    {
        [UnityTest]
        public IEnumerator SameGoblin_NextRoundChangesHealthAttackAndPreviewWithoutMutatingDefinitions()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            controller.BattleController.AutoTick = false;
            CombatantSnapshot first = controller.PreparedEnemy;
            Assert.That(controller.CurrentStageId, Is.EqualTo("stage_01"));
            Assert.That(first.MaximumHealth, Is.EqualTo(30d));
            Assert.That(controller.AdvanceRound(), Is.True);
            CombatantSnapshot second = controller.PreparedEnemy;
            Assert.That(controller.CurrentRound, Is.EqualTo(2));
            Assert.That(second.MaximumHealth, Is.EqualTo(35d));
            Assert.That(second.Loadout.Items.Single().BaseWeaponStats.AttackDamage, Is.EqualTo(6f));
            string preview = Text(controller, "Next Enemy").text;
            Assert.That(preview, Does.Contain("체력 35"));
            Assert.That(preview, Does.Contain("공격력 6"));
            Assert.That(preview, Does.Contain("6×6"));
            PrototypeScreenCapture.CaptureIfRequested(controller.GetComponentInChildren<Canvas>(), "StageRound2Preparation.png");
            Assert.That(controller.TryStartBattle(), Is.True);
            Assert.That(controller.ActiveCombat.Enemy, Is.SameAs(second));
            Assert.That(controller.ActiveCombat.EnemyMaxHealth, Is.EqualTo(35d));
            var view = controller.GetComponentsInChildren<PrototypeBattleItemView>().Single(item => item.Side == CombatSide.Enemy);
            view.OnPointerEnter(new PointerEventData(EventSystem.current)
                { position = RectTransformUtility.WorldToScreenPoint(null, view.transform.position) });
            Assert.That(Text(controller, "Attack Value").text, Is.EqualTo("6"));
            Assert.That(Text(controller, "Attack Value").color, Is.EqualTo(new Color(0.91f, 0.94f, 0.98f)),
                "Round scaling is the enemy's baseline, not an in-battle buff.");
            PrototypeScreenCapture.CaptureIfRequested(controller.GetComponentInChildren<Canvas>(), "StageRound2BattleTooltip.png");
            controller.TickCombat(2f);
            Assert.That(controller.ActiveCombat.PlayerHealth, Is.EqualTo(46.5d), "6 attack minus the player's 2.5 armor must deal 3.5 HP damage.");
            Assert.That(first.MaximumHealth, Is.EqualTo(30d));
            Assert.That(first.Loadout.Items.Single().BaseWeaponStats.AttackDamage, Is.EqualTo(5f));
            Assert.That(controller.CurrentEnemyEncounter.MaximumHealth, Is.EqualTo(30));
            Assert.That(controller.CurrentEnemyEncounter.Items[0].Item.BaseWeaponStats.AttackDamage, Is.EqualTo(5f));
        }

        [UnityTest]
        public IEnumerator LaterRound_ChangesLoadoutAndKeepsBothBattleBagsAtSixBySix()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            controller.BattleController.AutoTick = false;
            for (int index = 0; index < 4; index++) Assert.That(controller.AdvanceRound(), Is.True);
            Assert.That(controller.CurrentRound, Is.EqualTo(5));
            Assert.That(controller.PreparedEnemy.MaximumHealth, Is.EqualTo(50d));
            Assert.That(controller.PreparedEnemy.Loadout.Items.Count, Is.EqualTo(2));
            Assert.That(controller.TryStartBattle(), Is.True);
            foreach (string side in new[] { "Player", "Enemy" })
                Assert.That(controller.GetComponentsInChildren<Transform>().Single(transform => transform.name == side + " Battle Backpack")
                    .GetComponentsInChildren<Transform>().Count(transform => transform.name.StartsWith("Battle Grid Cell ")), Is.EqualTo(36));
            controller.TickCombat(2f);
            Assert.That(controller.ActiveCombat.EnemyArmor, Is.EqualTo(2.5d));
            PrototypeScreenCapture.CaptureIfRequested(controller.GetComponentInChildren<Canvas>(), "StageRound5GuardBattle.png");
        }

        [UnityTest]
        public IEnumerator AnotherStage_UsesItsOwnGoblinHealthAndAttackFromTheFirstRound()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var previous = Object.FindFirstObjectByType<BackpackPrototypeController>();
            StageData stage = Object.Instantiate(previous.Stage);
            Set(stage, "_stageId", "stage_08_playtest");
            Set(stage, "_displayName", "8스테이지 · 검증");
            var encounter = previous.CurrentEnemyEncounter;
            Set(stage, "_rounds", Enumerable.Range(0, 12).Select(index => new StageRoundData(encounter,
                new EnemyEncounterOverrides(180 + index * 10, new EnemyItemOverride("club", new(attackDamage: 25 + index))), index == 11)).ToList());
            var catalog = previous.ItemCatalog.ToArray();
            var rarity = previous.ShopRarityTable;
            var statuses = Get(previous, "_statusEffects");
            Object.Destroy(previous.gameObject);
            yield return null;
            var host = new GameObject("Stage selection test");
            host.SetActive(false);
            var controller = host.AddComponent<BackpackPrototypeController>();
            Set(controller, "_itemCatalog", catalog);
            Set(controller, "_shopRarityTable", rarity);
            Set(controller, "_statusEffects", statuses);
            Set(controller, "_stageData", stage);
            host.SetActive(true);
            yield return null;
            try
            {
                controller.BattleController.AutoTick = false;
                Assert.That(controller.CurrentStageId, Is.EqualTo("stage_08_playtest"));
                Assert.That(controller.CurrentRound, Is.EqualTo(1));
                Assert.That(Text(controller, "Next Enemy").text, Does.Contain("체력 180"));
                Assert.That(Text(controller, "Next Enemy").text, Does.Contain("공격력 25"));
                Assert.That(controller.TryStartBattle(), Is.True);
                Assert.That(controller.ActiveCombat.Enemy.DisplayName, Is.EqualTo("고블린"));
                Assert.That(controller.ActiveCombat.EnemyMaxHealth, Is.EqualTo(180d));
                Assert.That(controller.ActiveCombat.EnemyWeapons.Single().Stats.AttackDamage, Is.EqualTo(25f));
                Assert.That(controller.ActiveCombat.Enemy.Loadout.Width, Is.EqualTo(6));
                PrototypeScreenCapture.CaptureIfRequested(controller.GetComponentInChildren<Canvas>(), "Stage8GoblinBattle.png");
            }
            finally { Object.Destroy(host); Object.Destroy(stage); }
        }

        [UnityTest]
        public IEnumerator InvalidStage_DisablesBattleAndDoesNotPayOrAdvanceUntilFixed()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            StageData original = controller.Stage;
            StageData copy = Object.Instantiate(original);
            try
            {
                var rounds = original.Rounds.ToList();
                rounds[1] = new StageRoundData(null);
                Set(copy, "_rounds", rounds);
                Set(controller, "_stageData", copy);
                int gold = controller.Gold;
                Assert.That(controller.RefreshEncounterPreview(), Is.False);
                Assert.That(controller.PreparedEnemy, Is.Null);
                Assert.That(controller.EnemyConfigurationError, Does.Contain("ROUND 2"));
                Assert.That(Text(controller, "Next Enemy").text, Does.Contain("적 설정 오류"));
                Assert.That(controller.GetComponentsInChildren<Button>().Single(button => button.name == "Start Battle Button").interactable, Is.False);
                Assert.That(controller.TryStartBattle(), Is.False);
                Assert.That(controller.AdvanceRound(), Is.False);
                Assert.That(controller.Gold, Is.EqualTo(gold));
                Assert.That(controller.CurrentRound, Is.EqualTo(1));
                Set(controller, "_stageData", original);
                Assert.That(controller.RefreshEncounterPreview(), Is.True);
                Assert.That(controller.TryStartBattle(), Is.True);
            }
            finally { Set(controller, "_stageData", original); Object.Destroy(copy); }
        }

        private static Text Text(BackpackPrototypeController controller, string name)
            => controller.GetComponentsInChildren<Text>(true).Single(text => text.name == name);
        private static void Set(object value, string name, object content)
            => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(value, content);
        private static object Get(object value, string name)
            => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value);
    }
}
