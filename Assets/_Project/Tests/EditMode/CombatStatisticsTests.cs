using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BackpackAutoBattle.Tests.Combat
{
    public sealed class CombatStatisticsTests
    {
        private readonly List<Object> _temporary = new();
        [TearDown] public void Cleanup() { foreach (var item in _temporary) Object.DestroyImmediate(item); _temporary.Clear(); }
        [Test]
        public void Armor50And40_Block60_AllocateAtTheMomentOfAbsorption()
        {
            var c = Battle(Unit(100, Armor(50), Armor(40)), Unit(100)); c.Advance(1);
            c.ApplyNonAttackDamage(CombatSide.Enemy, CombatSide.Player, 60, CombatDamageKind.Other);
            Assert.That(Row(c, CombatSide.Player, "i0").DamageBlocked, Is.EqualTo(60d * 50 / 90).Within(1e-9));
            Assert.That(Row(c, CombatSide.Player, "i1").DamageBlocked, Is.EqualTo(60d * 40 / 90).Within(1e-9));
            Assert.That(c.PlayerArmor, Is.EqualTo(30)); Assert.That(c.PlayerHealth, Is.EqualTo(100));
            Assert.That(Side(c, CombatSide.Player).Sum(r => r.DamageBlocked), Is.EqualTo(60).Within(1e-9));
        }
        [Test]
        public void ArmorAddedLater_CannotTakeCreditForEarlierAbsorption()
        {
            var c = Battle(Unit(100, Armor(50), Armor(40, 2)), Unit(100)); c.Advance(1);
            c.ApplyNonAttackDamage(CombatSide.Enemy, CombatSide.Player, 30, CombatDamageKind.Other);
            c.PlayerItems[0].Cycle.SetBaseInterval(100); c.Advance(1);
            c.ApplyNonAttackDamage(CombatSide.Enemy, CombatSide.Player, 30, CombatDamageKind.Other);
            Assert.That(Row(c, CombatSide.Player, "i0").DamageBlocked, Is.EqualTo(40).Within(1e-9));
            Assert.That(Row(c, CombatSide.Player, "i1").DamageBlocked, Is.EqualTo(20).Within(1e-9));
            Assert.That(c.PlayerArmor, Is.EqualTo(30));
        }
        [Test]
        public void UnusedArmorCountsZero_AndManyFractionalHitsPreserveTotals()
        {
            var c = Battle(Unit(100, Armor(50), Armor(40)), Unit(100)); c.Advance(1);
            Assert.That(Side(c, CombatSide.Player).Sum(r => r.DamageBlocked), Is.Zero);
            for (int i = 0; i < 600; i++) c.ApplyNonAttackDamage(CombatSide.Enemy, CombatSide.Player, .1, CombatDamageKind.Other);
            Assert.That(Row(c, CombatSide.Player, "i0").DamageBlocked, Is.EqualTo(100d / 3).Within(1e-8));
            Assert.That(Row(c, CombatSide.Player, "i1").DamageBlocked, Is.EqualTo(80d / 3).Within(1e-8));
            Assert.That(c.PlayerArmor, Is.EqualTo(30).Within(1e-8));
        }
        [Test]
        public void DirectDamageIncludesArmorAndHp_ButNotOverkill_AndSnapshotsDoNotMutate()
        {
            var c = Battle(Unit(100, Weapon(100)), Unit(10, Armor(5, .5f)));
            var before = c.CaptureStatistics(); c.Advance(1);
            var row = Row(c, CombatSide.Player, "i0");
            Assert.That(row.DamageDealt, Is.EqualTo(15)); Assert.That(row.HealthDamage, Is.EqualTo(10));
            Assert.That(row.ArmorDamage, Is.EqualTo(5)); Assert.That(row.DirectDamage, Is.EqualTo(15));
            Assert.That(Row(c, CombatSide.Enemy, "i0").DamageBlocked, Is.EqualTo(5));
            Assert.That(before.Items.First(r => r.Side == CombatSide.Player).DamageDealt, Is.Zero);
            Assert.That(c.CaptureStatistics().Result, Is.EqualTo(CombatResult.Victory));
            c.Advance(10); Assert.That(Row(c, CombatSide.Player, "i0").DamageDealt, Is.EqualTo(15));
        }
        [Test]
        public void BurnSourcesShareDamage_AndDecayDoesNotTurnOneStackIntoMultipleTicks()
        {
            var c = Battle(Unit(100, Passive(), Passive()), Unit(100));
            var ticks = new List<PeriodicStatusTickEvent>(); c.StatusTicked += ticks.Add;
            c.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Burn, 2, CombatSide.Player, "i0");
            c.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Burn, 1, CombatSide.Player, "i1");
            c.Advance(2);
            Assert.That(ticks, Has.Count.EqualTo(2), "Item attribution must not split the physical damage event.");
            Assert.That(Row(c, CombatSide.Player, "i0").StatusDamage, Is.EqualTo(3.6).Within(1e-5));
            Assert.That(Row(c, CombatSide.Player, "i1").StatusDamage, Is.EqualTo(2.4).Within(1e-5));
            Assert.That(c.EnemyHealth, Is.EqualTo(94).Within(1e-5));
        }
        [Test]
        public void LethalPoison_IsSharedByActiveStacks_WithoutOrderingBiasOrOverkill()
        {
            var c = Battle(Unit(100, Passive(), Passive()), Unit(2));
            int damageEvents = 0; c.DamageApplied += _ => damageEvents++;
            c.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Poison, 1, CombatSide.Player, "i0");
            c.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Poison, 3, CombatSide.Player, "i1"); c.Advance(1);
            Assert.That(damageEvents, Is.EqualTo(1));
            Assert.That(Row(c, CombatSide.Player, "i0").StatusDamage, Is.EqualTo(.5).Within(1e-9));
            Assert.That(Row(c, CombatSide.Player, "i1").StatusDamage, Is.EqualTo(1.5).Within(1e-9));
            Assert.That(c.Result, Is.EqualTo(CombatResult.Victory));
        }
        [Test]
        public void CleanseMaintainsExistingSideOrderAndRemovesOnlyTheCorrectItemOrigins()
        {
            var c = Battle(Unit(100, Passive(), Passive()), Unit(100, Passive(), Passive()));
            c.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Burn, 1, CombatSide.Player, "i0");
            c.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Burn, 1, CombatSide.Enemy, "i0");
            c.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Burn, 2, CombatSide.Player, "i1");
            c.RemoveStatus(CombatSide.Enemy, StatusEffectKind.Burn, 2); c.Advance(2);
            Assert.That(Row(c, CombatSide.Player, "i0").StatusDamage, Is.Zero);
            Assert.That(Row(c, CombatSide.Player, "i1").StatusDamage, Is.EqualTo(1.2).Within(1e-5));
            Assert.That(Row(c, CombatSide.Enemy, "i0").StatusDamage, Is.EqualTo(2.4).Within(1e-5));
        }
        [Test]
        public void LifestealAndThornsBelongToGrantingItems_NotTheTriggeringWeapon()
        {
            var c = Battle(Unit(100, Weapon(5), Grant(StatusEffectKind.Lifesteal, 2), Grant(StatusEffectKind.Lifesteal, 3)),
                Unit(100, Grant(StatusEffectKind.Thorns, 1), Grant(StatusEffectKind.Thorns, 3)));
            c.ApplyNonAttackDamage(CombatSide.Enemy, CombatSide.Player, 10, CombatDamageKind.Other); c.Advance(1);
            Assert.That(Row(c, CombatSide.Player, "i0").Healing, Is.Zero);
            Assert.That(Row(c, CombatSide.Player, "i1").Healing, Is.EqualTo(2).Within(1e-5));
            Assert.That(Row(c, CombatSide.Player, "i2").Healing, Is.EqualTo(3).Within(1e-5));
            Assert.That(Row(c, CombatSide.Enemy, "i0").OtherDamage, Is.EqualTo(1).Within(1e-5));
            Assert.That(Row(c, CombatSide.Enemy, "i1").OtherDamage, Is.EqualTo(3).Within(1e-5));
            Assert.That(c.PlayerHealth, Is.EqualTo(91).Within(1e-5));
        }
        [Test]
        public void OverhealingIsExcluded_AndRemainingStackOriginsSurviveRemoval()
        {
            var c = Battle(Unit(100, Weapon(5), Grant(StatusEffectKind.Lifesteal, 1), Grant(StatusEffectKind.Lifesteal, 3)), Unit(100));
            c.ApplyNonAttackDamage(CombatSide.Enemy, CombatSide.Player, 1, CombatDamageKind.Other); c.Advance(1);
            Assert.That(Row(c, CombatSide.Player, "i1").Healing, Is.EqualTo(.25).Within(1e-9));
            Assert.That(Row(c, CombatSide.Player, "i2").Healing, Is.EqualTo(.75).Within(1e-9));
            c.RemoveStatus(CombatSide.Player, StatusEffectKind.Lifesteal, 3);
            c.ApplyNonAttackDamage(CombatSide.Enemy, CombatSide.Player, 10, CombatDamageKind.Other); c.Advance(1);
            Assert.That(Row(c, CombatSide.Player, "i1").Healing, Is.EqualTo(.25).Within(1e-9));
            Assert.That(Row(c, CombatSide.Player, "i2").Healing, Is.EqualTo(1.75).Within(1e-5));
        }
        [Test]
        public void FullyBlockedAttackAndReflection_AreAttributedWithoutTriggeringAnotherReaction()
        {
            var c = Battle(Unit(100, Weapon(5), Armor(20, .5f), Grant(StatusEffectKind.Lifesteal, 3)),
                Unit(100, Armor(20, .5f), Grant(StatusEffectKind.Thorns, 3)));
            c.ApplyNonAttackDamage(CombatSide.Enemy, CombatSide.Player, 1, CombatDamageKind.Other); c.Advance(1);
            Assert.That(Row(c, CombatSide.Player, "i0").ArmorDamage, Is.EqualTo(5));
            Assert.That(Row(c, CombatSide.Enemy, "i1").ArmorDamage, Is.EqualTo(3).Within(1e-5));
            Assert.That(Row(c, CombatSide.Player, "i2").Healing, Is.EqualTo(1));
            Assert.That(Row(c, CombatSide.Player, "i1").DamageBlocked, Is.EqualTo(3).Within(1e-5));
            Assert.That(Row(c, CombatSide.Enemy, "i0").DamageBlocked, Is.EqualTo(5));
        }
        [Test]
        public void UnknownSourcesHaveTheirOwnBucket_AndInstancesAndSidesNeverMerge()
        {
            var c = Battle(Unit(100, Weapon(5)), Unit(100, Weapon(2)));
            c.ApplyNonAttackDamage(CombatSide.Player, CombatSide.Enemy, 1, CombatDamageKind.Other); c.Advance(1);
            Assert.That(Row(c, CombatSide.Player, "i0").DamageDealt, Is.EqualTo(5));
            Assert.That(Row(c, CombatSide.Enemy, "i0").DamageDealt, Is.EqualTo(2));
            Assert.That(Row(c, CombatSide.Player, null).OtherDamage, Is.EqualTo(1));
            var fresh = Battle(c.Player, c.Enemy);
            Assert.That(fresh.CaptureStatistics().Items.Sum(i => i.DamageDealt), Is.Zero);
            Assert.That(fresh.CaptureStatistics().Items, Has.Count.EqualTo(2));
        }
        [Test]
        public void FramePartitionsAndRepeatedSnapshotsCannotChangeResultsOrDoubleCount()
        {
            var p = Unit(500, Weapon(5), Armor(2, 1.5f), Grant(StatusEffectKind.Lifesteal, 2));
            var e = Unit(500, Weapon(3), Grant(StatusEffectKind.Thorns, 1));
            var a = Battle(p, e); var b = Battle(p, e);
            a.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Poison, 2, CombatSide.Player, "i0");
            b.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Poison, 2, CombatSide.Player, "i0");
            a.Advance(6); for (int i = 0; i < 60; i++) { b.Advance(.1); _ = b.CaptureStatistics(); }
            var x = a.CaptureStatistics(); var y = b.CaptureStatistics();
            for (int i = 0; i < x.Items.Count; i++)
            {
                Assert.That(y.Items[i].DamageDealt, Is.EqualTo(x.Items[i].DamageDealt).Within(1e-8));
                Assert.That(y.Items[i].Healing, Is.EqualTo(x.Items[i].Healing).Within(1e-8));
                Assert.That(y.Items[i].DamageBlocked, Is.EqualTo(x.Items[i].DamageBlocked).Within(1e-8));
            }
            Assert.That(a.PlayerHealth, Is.EqualTo(b.PlayerHealth).Within(1e-8));
        }
        private ItemData Passive()
        {
            var item = ScriptableObject.CreateInstance<ItemData>(); _temporary.Add(item);
            Set(item, "_displayName", "Test"); return item;
        }
        private ItemData Weapon(float damage)
        {
            var item = Passive(); Set(item, "_category", ItemCategory.Weapon);
            Set(item, "_weaponStats", new WeaponStats(damage, 1, 100, 0, 200));
            Set(item, "_activation", new ItemActivationData(1, new ItemActivationEffect(ItemEffectKind.Attack))); return item;
        }
        private ItemData Armor(float amount, float interval = 1)
        {
            var item = Passive(); Set(item, "_activation", new ItemActivationData(interval, new ItemActivationEffect(ItemEffectKind.GainArmor, amount))); return item;
        }
        private ItemData Grant(StatusEffectKind kind, int stacks)
        {
            var item = Passive(); Set(item, "_battleStartStatuses", new List<StatusApplication> { new(kind, stacks) }); return item;
        }
        private static CombatantSnapshot Unit(double health, params ItemData[] items) => new("Test", health,
            new CombatLoadoutSnapshot(items.Select((item, i) => new CombatItemSnapshot("i" + i, item, new Vector2Int(i, 0), ItemRotation.None, item.OccupiedCells))));
        private static CombatSimulation Battle(CombatantSnapshot p, CombatantSnapshot e) => new(p, e, new ConstantRandom());
        private static CombatItemStatistics Row(CombatSimulation c, CombatSide side, string id) => c.CaptureStatistics().Items.Single(r => r.Side == side && r.InstanceId == id);
        private static CombatItemStatistics[] Side(CombatSimulation c, CombatSide side) => c.CaptureStatistics().Items.Where(r => r.Side == side).ToArray();
        private static void Set(object obj, string field, object value) => obj.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, value);
        private sealed class ConstantRandom : System.Random { public override double NextDouble() => .5; }
    }
}
