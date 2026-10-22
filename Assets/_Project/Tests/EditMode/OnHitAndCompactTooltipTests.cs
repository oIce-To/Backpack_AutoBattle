using System;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BackpackAutoBattle.Tests.Combat
{
    public sealed class OnHitAndCompactTooltipTests
    {
        private static ItemData Item(string name) => AssetDatabase.LoadAssetAtPath<ItemData>($"Assets/_Project/Data/Items/Item_{name}.asset");
        private static CombatantSnapshot Unit(params ItemData[] items) => new("Test", 1000,
            new CombatLoadoutSnapshot(items.Select((item, i) => new CombatItemSnapshot($"item_{i}", item,
                Vector2Int.zero, ItemRotation.None, item.OccupiedCells, ownerStats: new CharacterStatsSnapshot(1000, 10)))));

        [TestCase(AttackOutcome.AccuracyMiss)]
        [TestCase(AttackOutcome.Evaded)]
        [TestCase(AttackOutcome.Hit)]
        public void FireStaff_BurnRunsExactlyOnceOnlyAfterItsOwnSuccessfulHit(AttackOutcome expected)
        {
            var random = expected == AttackOutcome.AccuracyMiss ? new Rolls(.99)
                : expected == AttackOutcome.Evaded ? new Rolls(0, 0) : new Rolls(0, .99, .99);
            var combat = new CombatSimulation(Unit(Item("FireStaff")), Unit(), random);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Agility, 30);
            var events = combat.Advance(3.5);
            Assert.That(events.First().AttackOutcome, Is.EqualTo(expected));
            Assert.That(events.Count(e => e.Kind == ItemEffectKind.ApplyStatus), Is.EqualTo(expected == AttackOutcome.Hit ? 1 : 0));
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Burn), Is.EqualTo(expected == AttackOutcome.Hit ? 2 : 0));
            if (expected == AttackOutcome.Hit)
            {
                Assert.That(events.Last().InstanceId, Is.EqualTo(events.First().InstanceId));
                Assert.That(events.Last().Time, Is.EqualTo(events.First().Time));
                Assert.That(events.Last().TargetSide, Is.EqualTo(CombatSide.Enemy));
            }
            combat.Advance(1);
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Burn), Is.EqualTo(expected == AttackOutcome.Hit ? 1 : 0));
            Assert.That(random.Remaining, Is.Zero, "The on-hit effect and status tick must not perform extra attack rolls.");
        }

        [Test]
        public void FullyAbsorbedHit_StillAppliesBurn_WithoutRequiringHpDamage()
        {
            var combat = new CombatSimulation(Unit(Item("FireStaff")), Unit(Item("WoodenShield")), new ConstantRandom());
            var events = combat.Advance(3.5);
            var hit = events.Single(e => e.Kind == ItemEffectKind.Attack);
            Assert.That(hit.Hit, Is.True); Assert.That(hit.HealthDamage, Is.Zero); Assert.That(hit.ArmorAbsorbed, Is.EqualTo(12));
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Burn), Is.EqualTo(2));
            Assert.That(combat.EnemyHealth, Is.EqualTo(1000));
        }

        [Test]
        public void OtherWeaponAndStatusDamage_DoNotTriggerStaff_AndTriggerSurvivesCopying()
        {
            var combat = new CombatSimulation(Unit(Item("FireStaff"), Item("TrainingBlade")), Unit(), new ConstantRandom());
            combat.Advance(2);
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Burn), Is.Zero);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Poison, 2);
            combat.Advance(1);
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Burn), Is.Zero);
            var events = combat.Advance(.5);
            Assert.That(events.Count(e => e.Kind == ItemEffectKind.ApplyStatus), Is.EqualTo(1));
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Burn), Is.EqualTo(2));
            var effect = combat.PlayerItems.Single(i => i.Snapshot.ItemId == "fire_staff").Activation.Effects.Last();
            Assert.That(effect.Trigger, Is.EqualTo(ItemEffectTrigger.OnHit));
            Assert.That(effect.Copy().Trigger, Is.EqualTo(ItemEffectTrigger.OnHit));
            Assert.That(effect.Resolve(new CharacterStatsSnapshot(50, 20)).Trigger, Is.EqualTo(ItemEffectTrigger.OnHit));
            Assert.That(ItemTooltipContent.FromItem(Item("FireStaff")).GetDescriptionText(),
                Does.Contain("해당 무기가 적에게 명중할 때마다 상대에게 화상 2중첩을 부여합니다.").And.Not.Contain("3.5초마다"));
        }

        [Test]
        public void KillingHit_DoesNotApplyAStatusToTheAlreadyDeadTarget()
        {
            var deadSoon = new CombatantSnapshot("Target", 1, new CombatLoadoutSnapshot(Array.Empty<CombatItemSnapshot>()));
            var combat = new CombatSimulation(Unit(Item("FireStaff")), deadSoon, new ConstantRandom());
            Assert.That(combat.Advance(3.5).Count(e => e.Kind == ItemEffectKind.ApplyStatus), Is.Zero);
            Assert.That(combat.Result, Is.EqualTo(CombatResult.Victory));
        }

        [Test]
        public void DescriptionNumbers_ColorOnlyChangedValues_AndCooldownUsesInverseDirection()
        {
            var snapshot = new CombatItemSnapshot("shield", Item("WoodenShield"), Vector2Int.zero, ItemRotation.None,
                Item("WoodenShield").OccupiedCells);
            var runtime = new CombatItem(snapshot, () => 0);
            runtime.Cycle.SetBaseInterval(2 / 1.1);
            Assert.That(ItemTooltipContent.FromCombat(snapshot, runtime).GetDescriptionText(),
                Does.Contain("<color=#59E680>1.82</color>초마다 방어도를 2.5 얻습니다."));
            runtime.Cycle.SetBaseInterval(2 / .9);
            Set(runtime.Activation.Effects.Single(), "_amount", 4f);
            Assert.That(ItemTooltipContent.FromCombat(snapshot, runtime).GetDescriptionText(),
                Does.Contain("<color=#FF5C57>2.22</color>초마다 방어도를 <color=#59E680>4</color> 얻습니다."));
            runtime.Cycle.SetBaseInterval(2);
            Set(runtime.Activation.Effects.Single(), "_amount", 1f);
            Assert.That(ItemTooltipContent.FromCombat(snapshot, runtime).GetDescriptionText(),
                Does.Contain("2.0초마다 방어도를 <color=#FF5C57>1</color> 얻습니다."));
            Assert.That(ItemTooltipContent.FromItem(Item("WoodenShield")).GetDescriptionText(), Does.Not.Contain("<color="));
            Assert.That(snapshot.Activation.Effects.Single().Amount, Is.EqualTo(2.5));
        }

        [Test]
        public void PeriodicStatusStackAmount_CanUseInlineColorsToo()
        {
            var data = Item("PoisonVial");
            var snapshot = new CombatItemSnapshot("poison", data, Vector2Int.zero, ItemRotation.None, data.OccupiedCells);
            var runtime = new CombatItem(snapshot, () => 0);
            Set(runtime.Activation.Effects.Single(), "_status", new StatusApplication(StatusEffectKind.Poison, 3, StatusTarget.Opponent));
            Assert.That(ItemTooltipContent.FromCombat(snapshot, runtime).GetDescriptionText(), Does.Contain("중독 <color=#59E680>3</color>중첩"));
        }

        [Test]
        public void AttackStatsVisibility_IsBasedOnActualEffects_NotWeaponOrPetCategory()
        {
            var pet = UnityEngine.Object.Instantiate(Item("FireStaff"));
            var buffPet = UnityEngine.Object.Instantiate(Item("WoodenShield"));
            try
            {
                Set(pet, "_category", ItemCategory.Pet); Set(buffPet, "_category", ItemCategory.Pet);
                Assert.That(ItemTooltipContent.FromItem(pet).HasWeaponStats, Is.True);
                Assert.That(ItemTooltipContent.FromItem(buffPet).HasWeaponStats, Is.False);
                Assert.That(ItemTooltipContent.FromItem(pet).GetDescriptionText(), Does.Contain("해당 소환수가 적에게 명중할 때마다"));
            }
            finally { UnityEngine.Object.DestroyImmediate(pet); UnityEngine.Object.DestroyImmediate(buffPet); }
        }

        private static void Set(object obj, string field, object value) => obj.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, value);
        private sealed class ConstantRandom : System.Random { public override double NextDouble() => .5; }
        private sealed class Rolls : System.Random
        {
            private readonly System.Collections.Generic.Queue<double> _values;
            public int Remaining => _values.Count;
            public Rolls(params double[] values) { _values = new(values); }
            public override double NextDouble() => _values.Dequeue();
        }
    }
}
