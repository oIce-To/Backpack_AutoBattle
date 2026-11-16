using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class PrototypeBattlePresentationTests
    {
        private static object Field(object target, string name) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        [UnityTest]
        public IEnumerator HealthFillAndValue_StayInsideFrameAtEmptyFullAndExcessHealth()
        {
            yield return PrototypeCombatFixture.LoadWithTrainingKit();
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            controller.BattleController.AutoTick = false;
            Assert.That(controller.TryStartBattle(), Is.True);
            var screen = (PrototypeBattleScreen)Field(controller.BattleController, "_screen");
            var setHealth = typeof(PrototypeBattleScreen).GetMethod("SetHealth", BindingFlags.Static | BindingFlags.NonPublic);
            foreach (string side in new[] { "player", "enemy" })
            {
                var fill = (Image)Field(screen, $"_{side}HealthFill");
                var label = (Text)Field(screen, $"_{side}HealthText");
                var inset = (RectTransform)fill.transform.parent;
                Assert.That(inset.GetComponent<RectMask2D>(), Is.Not.Null);
                Assert.That(label.transform.parent, Is.EqualTo(inset));
                Assert.That(inset.offsetMin, Is.EqualTo(new Vector2(8, 6)));
                Assert.That(inset.offsetMax, Is.EqualTo(new Vector2(-8, -6)));
                foreach (double health in new[] { -10d, 0d, 50d, 100d, 125d })
                {
                    setHealth.Invoke(null, new object[] { fill, label, health, 100d });
                    Canvas.ForceUpdateCanvases();
                    Assert.That(fill.rectTransform.rect.width,
                        Is.EqualTo(inset.rect.width * Mathf.Clamp01((float)health / 100)).Within(.01f));
                    Assert.That(label.rectTransform.rect.size, Is.EqualTo(inset.rect.size));
                }
            }
            screen.Render(controller.ActiveCombat);
        }

        [UnityTest]
        public IEnumerator DamageAndHealing_RiseFadeAndExpire_CriticalPops_AndEveryStatusIsVisible()
        {
            yield return PrototypeCombatFixture.LoadWithTrainingKit();
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            controller.BattleController.AutoTick = false;
            Assert.That(controller.TryStartBattle(), Is.True);
            foreach (var weapon in controller.ActiveCombat.PlayerWeapons.Concat(controller.ActiveCombat.EnemyWeapons))
                weapon.Stats.AttackDamage = 0;
            controller.TickCombat(60); // Enrage is a timed status, not an externally applied stack.
            yield return null;
            var screen = (PrototypeBattleScreen)Field(controller.BattleController, "_screen");
            var item = controller.ActiveCombat.PlayerItems.First();
            foreach (StatusEffectKind kind in Enum.GetValues(typeof(StatusEffectKind)))
                controller.ActiveCombat.ApplyStatus(StatusEffectRules.IsDebuff(kind) ? CombatSide.Enemy : CombatSide.Player, kind, 3);
            controller.TickCombat(0);
            Canvas.ForceUpdateCanvases();
            Assert.That(controller.GetComponentsInChildren<PrototypeStatusIcon>().Length,
                Is.EqualTo(Enum.GetValues(typeof(StatusEffectKind)).Length));
            foreach (var icon in controller.GetComponentsInChildren<PrototypeStatusIcon>())
            {
                var glyph = icon.GetComponentInChildren<PrototypeStatusGlyph>();
                Assert.That(glyph.color.maxColorComponent, Is.GreaterThan(.65f), icon.Kind.ToString());
                Assert.That(icon.GetComponent<Image>().sprite, Is.Null);
                Assert.That(icon.GetComponent<Image>().color.a, Is.Zero);
                Assert.That(glyph.raycastTarget, Is.False);
                Assert.That(glyph.canvasRenderer.GetMesh().vertexCount, Is.GreaterThan(0));
            }
            foreach (var scroll in controller.GetComponentsInChildren<ScrollRect>())
            {
                var icons = scroll.content.GetComponentsInChildren<PrototypeStatusIcon>()
                    .OrderByDescending(icon => ((RectTransform)icon.transform).anchoredPosition.y).ToArray();
                for (int index = 1; index < icons.Length; index++)
                    Assert.That(Mathf.Abs(((RectTransform)icons[index].transform).anchoredPosition.y
                        - ((RectTransform)icons[index - 1].transform).anchoredPosition.y), Is.EqualTo(48f));
            }
            screen.ShowAttack(new CombatItemEvent(0, CombatSide.Player, item, ItemEffectKind.Attack,
                damage: 12.5, critical: true, targetSide: CombatSide.Enemy));
            screen.ShowAttack(new CombatItemEvent(0, CombatSide.Player, item, ItemEffectKind.Heal,
                healthRestored: 3.25, targetSide: CombatSide.Player));
            var popups = controller.GetComponentsInChildren<PrototypeDamageNumber>();
            var critical = popups.Single(p => p.Text == "-12.5");
            var heal = popups.Single(p => p.Text == "+3.25");
            Assert.That(heal.color, Is.EqualTo(new Color(.38f, 1f, .52f)));
            Assert.That(heal.transform.parent.name, Is.EqualTo("Player Damage"));
            Assert.That(critical.color, Is.EqualTo(new Color(1f, .81f, .28f)));
            screen.ShowAttack(new CombatItemEvent(0, CombatSide.Player, item, ItemEffectKind.Heal,
                healthRestored: 0, targetSide: CombatSide.Player));
            Assert.That(controller.GetComponentsInChildren<PrototypeDamageNumber>().Length, Is.EqualTo(popups.Length),
                "Full-health heals must not display +0.");
            Vector2 criticalOrigin = critical.rectTransform.anchoredPosition, healOrigin = heal.rectTransform.anchoredPosition;
            double now = Time.unscaledTimeAsDouble;
            void Animate(double offset)
            {
                foreach (string name in new[] { "_playerDamage", "_enemyDamage" })
                {
                    object pool = Field(screen, name);
                    pool.GetType().GetMethod("Tick").Invoke(pool, new object[] { now + offset });
                }
                Canvas.ForceUpdateCanvases();
            }
            Animate(.12);
            Assert.That(critical.rectTransform.anchoredPosition.y, Is.GreaterThan(criticalOrigin.y));
            Assert.That(critical.rectTransform.localScale.x, Is.GreaterThan(1.1f));
            Assert.That(heal.rectTransform.anchoredPosition.y, Is.GreaterThan(healOrigin.y));
            Assert.That(heal.canvasRenderer.GetMesh().vertexCount, Is.GreaterThan(0));
            var canvas = controller.GetComponentInChildren<Canvas>();
            PrototypeScreenCapture.CaptureIfRequested(canvas, "BattlePixelCriticalAndHeal.png");
            Animate(.78);
            Assert.That(heal.canvasRenderer.GetAlpha(), Is.InRange(.01f, .8f));
            Assert.That(critical.canvasRenderer.GetAlpha(), Is.LessThan(1f));
            foreach (var popup in popups)
                Assert.That(((RectTransform)popup.transform.parent).rect.Contains(popup.rectTransform.anchoredPosition), Is.True);
            PrototypeScreenCapture.CaptureIfRequested(canvas, "BattlePixelFading.png");
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BACKPACK_QA_CAPTURE_DIR")))
                for (int frame = 0; frame <= 36; frame++)
                {
                    Animate(frame / 30d);
                    PrototypeScreenCapture.CaptureIfRequested(canvas, $"BattleMotion-{frame:000}.png");
                }
            Animate(2);
            Assert.That(controller.GetComponentsInChildren<PrototypeDamageNumber>(), Is.Empty);
        }
    }
}
