using System.Collections;
using System.Linq;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class PrototypeIllustrationHitFeedbackTests
    {
        [TestCase(ItemCategory.Weapon, ItemEffectKind.Attack, true, 5, 0, true)]
        [TestCase(ItemCategory.Weapon, ItemEffectKind.Attack, true, 0, 5, true)]
        [TestCase(ItemCategory.Pet, ItemEffectKind.Attack, true, 5, 0, true)]
        [TestCase(ItemCategory.Weapon, ItemEffectKind.Attack, false, 0, 0, false)]
        [TestCase(ItemCategory.Weapon, ItemEffectKind.Attack, true, 0, 0, false)]
        [TestCase(ItemCategory.Weapon, ItemEffectKind.ApplyStatus, true, 5, 0, false)]
        [TestCase(ItemCategory.Accessory, ItemEffectKind.Attack, true, 5, 0, false)]
        public void OnlyDirectAppliedDamageShakes_WithoutChangingArtwork(ItemCategory category, ItemEffectKind kind,
            bool hit, double health, double armor, bool expected)
        {
            var obj = new GameObject("Hit feedback test", typeof(RectTransform), typeof(Image));
            var image = obj.GetComponent<Image>(); var rect = image.rectTransform;
            var texture = new Texture2D(2, 2);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * .5f);
            image.sprite = sprite; rect.anchoredPosition = new Vector2(30, -40);
            rect.sizeDelta = new Vector2(224, 350); rect.localScale = new Vector3(-1, 1, 1);
            var data = ScriptableObject.CreateInstance<ItemData>();
            var snapshot = new CombatItemSnapshot("test", data, Vector2Int.zero, ItemRotation.None, new[] { Vector2Int.zero });
            var item = new CombatItem(snapshot, () => 2);
            var feedback = new PrototypeIllustrationHitFeedback(rect);
            try
            {
                feedback.Show(new CombatItemEvent(2, CombatSide.Player, item, kind, hit,
                    healthDamage: health, armorAbsorbed: armor), category);
                feedback.Tick(2.015);
                Assert.That(rect.anchoredPosition.x, Is.EqualTo(expected ? 35 : 30), "Armor and health hits use the same amplitude.");
                Assert.That(rect.anchoredPosition.y, Is.EqualTo(-40));
                Assert.That(image.sprite, Is.SameAs(sprite));
                Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(224, 350)));
                Assert.That(rect.localScale, Is.EqualTo(new Vector3(-1, 1, 1)));
                feedback.Tick(2.2); Assert.That(rect.anchoredPosition, Is.EqualTo(new Vector2(30, -40)));
                feedback.Show(new CombatItemEvent(3, CombatSide.Player, item, ItemEffectKind.Attack, healthDamage: 1), ItemCategory.Weapon);
                feedback.Tick(3.015); feedback.Reset();
                Assert.That(rect.anchoredPosition, Is.EqualTo(new Vector2(30, -40)));
            }
            finally { Object.DestroyImmediate(obj); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); Object.DestroyImmediate(data); }
        }

        [UnityTest]
        public IEnumerator BattleKeepsOriginalSpritesAndSize_AndHitUsesPausedScaledCombatClock()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single); yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            var battle = controller.BattleController; battle.AutoTick = false;
            Assert.That(controller.TryStartBattle(), Is.True);
            Image Find(string name) => battle.ScreenRoot.GetComponentsInChildren<Image>().Single(i => i.name == name);
            var player = Find("Player Character Illustration"); var enemy = Find("Enemy Character Illustration");
            Vector2 rest = enemy.rectTransform.anchoredPosition;
            foreach (var weapon in battle.Simulation.PlayerWeapons.Concat(battle.Simulation.EnemyWeapons))
            { weapon.Stats.AttackDamage = 1; weapon.Stats.AccuracyPercent = 100; }
            CombatItemEvent last = null;
            battle.Simulation.ItemActivated += e =>
            { if (e.Kind == ItemEffectKind.Attack && e.Side == CombatSide.Player && e.Hit && e.HealthDamage + e.ArmorAbsorbed > 0) last = e; };
            for (int i = 0; i < 2000 && last == null; i++) battle.TickCombat(.01f);
            Assert.That(last, Is.Not.Null);
            // Sample relative to the event, not the end of the simulation step (which can land at a zero crossing).
            battle.TickCombat((float)System.Math.Max(0, last.Time + .015 - battle.Simulation.ElapsedSeconds));
            Assert.That(enemy.rectTransform.anchoredPosition.x, Is.Not.EqualTo(rest.x));
            var shakingPosition = enemy.rectTransform.anchoredPosition;
            battle.TogglePause(); battle.TickCombat(5); yield return null;
            Assert.That(enemy.rectTransform.anchoredPosition, Is.EqualTo(shakingPosition));
            Assert.That(player.sprite, Is.SameAs(battle.Simulation.Player.Illustration));
            Assert.That(enemy.sprite, Is.SameAs(battle.Simulation.Enemy.Illustration));
            Assert.That(player.rectTransform.sizeDelta, Is.EqualTo(new Vector2(224, 350)));
            Assert.That(enemy.rectTransform.sizeDelta, Is.EqualTo(new Vector2(224, 350)));
            Assert.That(player.rectTransform.localScale.x, Is.EqualTo(battle.Simulation.Player.IllustrationFacesLeft ? -1 : 1));
            Assert.That(enemy.rectTransform.localScale.x, Is.EqualTo(battle.Simulation.Enemy.IllustrationFacesLeft ? 1 : -1));
            PrototypeScreenCapture.CaptureIfRequested(controller.GetComponentInChildren<Canvas>(), "OriginalIllustrationsHitOnly.png");
            battle.TogglePause(); battle.SetPlaybackSpeed(2); battle.TickCombat(.1f);
            Assert.That(enemy.rectTransform.anchoredPosition, Is.EqualTo(rest));
            var simulation = battle.Simulation;
            battle.BeginBattle(simulation.Player, simulation.Enemy, 1, 0);
            Assert.That(enemy.rectTransform.anchoredPosition, Is.EqualTo(rest));
        }
    }
}
