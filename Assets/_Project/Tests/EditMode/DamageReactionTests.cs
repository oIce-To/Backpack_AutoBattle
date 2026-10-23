using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BackpackAutoBattle.Tests.Combat
{
    public sealed class DamageReactionTests
    {
        private readonly List<Object> _temporary = new();
        [TearDown] public void Cleanup() { foreach (var obj in _temporary) Object.DestroyImmediate(obj); _temporary.Clear(); }

        [TestCase(false)]
        [TestCase(true)]
        public void ArmorBlockedAttack_StillHealsAndReflects_UsingEachOwnersExternalAttack(bool enemyAttacks)
        {
            var attacker = Unit(50, 20, Weapon(5, 2));
            var defender = Unit(50, 40, Armor(100, 1));
            var combat = enemyAttacks ? new CombatSimulation(defender, attacker, new ConstantRandom())
                : new CombatSimulation(attacker, defender, new ConstantRandom());
            var source = enemyAttacks ? CombatSide.Enemy : CombatSide.Player;
            var target = enemyAttacks ? CombatSide.Player : CombatSide.Enemy;
            combat.ApplyNonAttackDamage(target, source, 10, CombatDamageKind.Other);
            combat.ApplyStatus(source, StatusEffectKind.Lifesteal, 3);
            combat.ApplyStatus(target, StatusEffectKind.Thorns, 2);
            var reactions = new List<CombatReactionEvent>(); combat.ReactionTriggered += reactions.Add;
            var attacks = combat.Advance(2).Where(e => e.Kind == ItemEffectKind.Attack).ToArray();
            Assert.That(attacks.Single().HealthDamage, Is.Zero);
            Assert.That(attacks.Single().ArmorAbsorbed, Is.EqualTo(5));
            Assert.That(reactions.Select(r => r.Kind), Is.EqualTo(new[] { CombatReactionKind.HealSelf, CombatReactionKind.ReflectDamage }));
            Assert.That(reactions[0].Amount, Is.EqualTo(6).Within(1e-5));
            Assert.That(reactions[0].HealthRestored, Is.EqualTo(6).Within(1e-5));
            Assert.That(reactions[1].Amount, Is.EqualTo(8).Within(1e-5));
            Assert.That(reactions[1].Owner, Is.EqualTo(target));
            Assert.That(reactions[1].Target, Is.EqualTo(source));
            Assert.That(enemyAttacks ? combat.EnemyHealth : combat.PlayerHealth, Is.EqualTo(38).Within(1e-5));
            Assert.That(enemyAttacks ? combat.PlayerHealth : combat.EnemyHealth, Is.EqualTo(50));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void KillingBlow_StillReflects_ButAttackerWinsDoubleDeath(bool enemyAttacks)
        {
            var attacker = Unit(1, 10, Weapon(5));
            var defender = Unit(5, 10);
            var combat = enemyAttacks ? new CombatSimulation(defender, attacker, new ConstantRandom())
                : new CombatSimulation(attacker, defender, new ConstantRandom());
            var target = enemyAttacks ? CombatSide.Player : CombatSide.Enemy;
            combat.ApplyStatus(target, StatusEffectKind.Thorns, 3);
            var reactions = new List<CombatReactionEvent>(); combat.ReactionTriggered += reactions.Add;
            combat.Advance(20);
            Assert.That(combat.PlayerHealth, Is.Zero); Assert.That(combat.EnemyHealth, Is.Zero);
            Assert.That(reactions.Single().Damage.HealthDamage, Is.EqualTo(1));
            Assert.That(combat.Result, Is.EqualTo(enemyAttacks ? CombatResult.Defeat : CombatResult.Victory));
            Assert.That(combat.ElapsedSeconds, Is.EqualTo(1), "No later item or status event may overwrite the attacker's result.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OnlyAttackerDiesToThorns_SurvivingDefenderWins(bool enemyAttacks)
        {
            var combat = enemyAttacks ? new CombatSimulation(Unit(50, 10), Unit(1, 10, Weapon()), new ConstantRandom())
                : new CombatSimulation(Unit(1, 10, Weapon()), Unit(50, 10), new ConstantRandom());
            combat.ApplyStatus(enemyAttacks ? CombatSide.Player : CombatSide.Enemy, StatusEffectKind.Thorns, 3);
            combat.Advance(1);
            Assert.That(combat.Result, Is.EqualTo(enemyAttacks ? CombatResult.Victory : CombatResult.Defeat));
        }

        [Test]
        public void Lifesteal_UsesFlatStackAmount_NotCriticalDamageOrWeaponDamage_AndCapsHealth()
        {
            var combat = new CombatSimulation(Unit(50, 10, Weapon(20, critical: 100)), Unit(1000, 10), new ConstantRandom());
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Lifesteal, 3);
            combat.ApplyNonAttackDamage(CombatSide.Enemy, CombatSide.Player, 1.5, CombatDamageKind.Other);
            combat.PlayerWeapons.Single().Stats.AttackDamage = 200;
            var reactions = new List<CombatReactionEvent>(); combat.ReactionTriggered += reactions.Add;
            Assert.That(combat.Advance(1).Single().Damage, Is.EqualTo(400));
            Assert.That(reactions.Single().Amount, Is.EqualTo(3).Within(1e-5));
            Assert.That(reactions.Single().HealthRestored, Is.EqualTo(1.5));
            Assert.That(combat.PlayerHealth, Is.EqualTo(50));
            combat.Advance(1);
            Assert.That(reactions.Last().HealthRestored, Is.Zero);
            Assert.That(combat.PlayerHealth, Is.EqualTo(50));
        }

        [Test]
        public void HealPrecedesThorns_AndDoesNotResurrectAfterRetaliation()
        {
            var combat = new CombatSimulation(Unit(10, 10, Weapon()), Unit(100, 10), new ConstantRandom());
            combat.ApplyNonAttackDamage(CombatSide.Enemy, CombatSide.Player, 9, CombatDamageKind.Other);
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Lifesteal, 3);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Thorns, 5);
            var reactions = new List<CombatReactionEvent>(); combat.ReactionTriggered += reactions.Add;
            combat.Advance(1);
            Assert.That(reactions[0].HealthRestored, Is.EqualTo(3).Within(1e-5));
            Assert.That(reactions[1].Damage.HealthDamage, Is.EqualTo(4).Within(1e-5));
            Assert.That(combat.PlayerHealth, Is.Zero); Assert.That(combat.Result, Is.EqualTo(CombatResult.Defeat));
        }

        [Test]
        public void ReflectionUsesArmor_CannotBeEvadedOrCrit_AndNeverChainsReactions()
        {
            var random = new Rolls(0, .99, .99);
            var combat = new CombatSimulation(Unit(50, 10, Armor(20, .5f), Weapon(5)), Unit(50, 10), random);
            foreach (CombatSide side in Enum.GetValues(typeof(CombatSide)))
            {
                combat.ApplyStatus(side, StatusEffectKind.Thorns, 3);
                combat.ApplyStatus(side, StatusEffectKind.Lifesteal, 3);
                combat.ApplyStatus(side, StatusEffectKind.Agility, 100);
            }
            var reactions = new List<CombatReactionEvent>(); var damage = new List<CombatDamageEvent>();
            combat.ReactionTriggered += reactions.Add; combat.DamageApplied += damage.Add;
            combat.Advance(1);
            Assert.That(random.Remaining, Is.Zero, "Only the initial attack consumes accuracy/evasion/critical rolls.");
            Assert.That(reactions, Has.Count.EqualTo(2));
            Assert.That(damage.Select(d => d.Kind), Is.EqualTo(new[] { CombatDamageKind.AttackItem, CombatDamageKind.Other }));
            Assert.That(damage.Last().Resolution.ArmorAbsorbed, Is.EqualTo(3).Within(1e-5));
            Assert.That(damage.Last().Resolution.HealthDamage, Is.Zero);
            Assert.That(combat.PlayerHealth, Is.EqualTo(50));
            Assert.That(combat.EnemyHealth, Is.EqualTo(45));
            Assert.That(combat.GetAvoidanceCount(CombatSide.Player), Is.Zero);
        }

        [TestCase(AttackOutcome.AccuracyMiss)]
        [TestCase(AttackOutcome.Evaded)]
        public void MissAndEvade_DoNotHealOrReflect(AttackOutcome outcome)
        {
            var random = outcome == AttackOutcome.AccuracyMiss ? new Rolls(.99) : new Rolls(0, 0);
            var combat = new CombatSimulation(Unit(50, 10, Weapon(5, accuracy: 70)), Unit(50, 10), random);
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Lifesteal, 3);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Thorns, 3);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Agility, 30);
            var reactions = new List<CombatReactionEvent>(); combat.ReactionTriggered += reactions.Add;
            Assert.That(combat.Advance(1).Single().AttackOutcome, Is.EqualTo(outcome));
            Assert.That(reactions, Is.Empty); Assert.That(random.Remaining, Is.Zero);
        }

        [Test]
        public void PeriodicAndOtherDamage_DoNotReact_AndStacksHaveNoDecayOrCap()
        {
            var combat = new CombatSimulation(Unit(1000, 10), Unit(1000, 20));
            foreach (CombatSide side in Enum.GetValues(typeof(CombatSide)))
            {
                combat.ApplyStatus(side, StatusEffectKind.Thorns, 1000);
                combat.ApplyStatus(side, StatusEffectKind.Lifesteal, 1000);
            }
            var reactions = new List<CombatReactionEvent>(); combat.ReactionTriggered += reactions.Add;
            combat.ApplyNonAttackDamage(CombatSide.Player, CombatSide.Enemy, 3, CombatDamageKind.Other);
            combat.ApplyNonAttackDamage(CombatSide.Player, CombatSide.Enemy, 3, CombatDamageKind.StatusEffect);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Burn, 2, CombatSide.Player);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Poison, 2, CombatSide.Player);
            combat.Advance(5);
            Assert.That(reactions, Is.Empty);
            Assert.That(combat.GetStatusStacks(CombatSide.Enemy, StatusEffectKind.Thorns), Is.EqualTo(1000));
            Assert.That(combat.GetStatusReactionAmount(CombatSide.Enemy, StatusEffectKind.Thorns), Is.EqualTo(2000).Within(1e-3));
        }

        [Test]
        public void ZeroDamageHit_DoesNotTriggerDamageReactions()
        {
            var combat = new CombatSimulation(Unit(50, 10, Weapon(0)), Unit(50, 10), new ConstantRandom());
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Lifesteal, 3);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Thorns, 3);
            int count = 0; combat.ReactionTriggered += _ => count++;
            combat.Advance(1); Assert.That(count, Is.Zero);
        }

        [Test]
        public void DefinitionsAreFrozenPerBattle_AndTooltipsShowFinalValuesOnly()
        {
            var data = ScriptableObject.CreateInstance<StatusEffectData>(); _temporary.Add(data);
            Set(data, "_kind", StatusEffectKind.Thorns); Set(data, "_damageScaling", new StatScaling(StatReference.Attack, .1f));
            var combat = new CombatSimulation(Unit(50, 10), Unit(50, 20), statusEffects: new[] { data });
            Set(data, "_damageScaling", new StatScaling(StatReference.Attack, .9f));
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Thorns, 3);
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Lifesteal, 3);
            Assert.That(combat.GetStatusReactionAmount(CombatSide.Enemy, StatusEffectKind.Thorns), Is.EqualTo(6).Within(1e-5));
            var thorns = StatusTooltipContent.FromCombat(combat, CombatSide.Enemy, StatusEffectKind.Thorns);
            Assert.That(thorns.Description, Does.Contain("6 피해").And.Not.Contain("%").And.Not.Contain("공격력"));
            Assert.That(StatusTooltipContent.FromCombat(combat, CombatSide.Player, StatusEffectKind.Lifesteal).Description, Does.Contain("3 회복"));
            Assert.That(StatusEffectRules.IsDebuff(StatusEffectKind.Thorns), Is.False);
            Assert.That(thorns.Color, Is.EqualTo(new Color(.36f, .88f, .66f)));
        }

        [Test]
        public void SampleItemsGrantOnlyAtBattleStart_AndNewBattleDoesNotInheritStacks()
        {
            var armor = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/Item_ThornArmor.asset");
            var charm = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/Item_VampiricCharm.asset");
            var unit = Unit(50, 10, armor, charm, charm);
            var combat = new CombatSimulation(unit, Unit(50, 10));
            Assert.That(combat.PlayerItems, Is.Empty, "Passive start-grant items do not need their own clock.");
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Thorns), Is.EqualTo(3));
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Lifesteal), Is.EqualTo(6));
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Thorns, 100);
            Assert.That(new CombatSimulation(unit, Unit(50, 10)).GetStatusStacks(CombatSide.Player, StatusEffectKind.Thorns), Is.EqualTo(3));
        }

        [Test]
        public void MultipleWeaponsAndFramePartitions_ProduceTheSameReactionSequence()
        {
            var unit = Unit(200, 10, Weapon(5, 1), Weapon(2, 1.5f));
            CombatSimulation Build()
            {
                var c = new CombatSimulation(unit, Unit(200, 10), new ConstantRandom());
                c.ApplyStatus(CombatSide.Player, StatusEffectKind.Lifesteal, 2);
                c.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Thorns, 3);
                return c;
            }
            var whole = Build(); var split = Build();
            var a = new List<CombatReactionEvent>(); var b = new List<CombatReactionEvent>();
            whole.ReactionTriggered += a.Add; split.ReactionTriggered += b.Add;
            whole.Advance(6); for (int i = 0; i < 60; i++) split.Advance(.1);
            Assert.That(a.Select(e => (e.Time, e.Kind, e.TriggeringItemId)), Is.EqualTo(b.Select(e => (e.Time, e.Kind, e.TriggeringItemId))));
            Assert.That(a, Has.Count.EqualTo(20));
            Assert.That(split.PlayerHealth, Is.EqualTo(whole.PlayerHealth).Within(1e-5));
            Assert.That(split.EnemyHealth, Is.EqualTo(whole.EnemyHealth).Within(1e-5));
        }

        private ItemData Weapon(float damage = 5, float interval = 1, float accuracy = 100, float critical = 0)
        {
            var item = ScriptableObject.CreateInstance<ItemData>(); _temporary.Add(item);
            Set(item, "_category", ItemCategory.Weapon);
            Set(item, "_weaponStats", new WeaponStats(damage, interval, accuracy, critical, 200));
            Set(item, "_activation", new ItemActivationData(interval, new ItemActivationEffect(ItemEffectKind.Attack)));
            return item;
        }
        private ItemData Armor(float amount, float interval)
        {
            var item = ScriptableObject.CreateInstance<ItemData>(); _temporary.Add(item);
            Set(item, "_activation", new ItemActivationData(interval, new ItemActivationEffect(ItemEffectKind.GainArmor, amount)));
            return item;
        }
        private static CombatantSnapshot Unit(double hp, double attack, params ItemData[] items) => new("Test", hp,
            new CombatLoadoutSnapshot(items.Select((item, index) => new CombatItemSnapshot($"item_{index}", item,
                new Vector2Int(index, 0), ItemRotation.None, item.OccupiedCells, ownerStats: new CharacterStatsSnapshot(hp, attack)))), attack);
        private static void Set(object obj, string field, object value) => obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
        private sealed class ConstantRandom : System.Random { public override double NextDouble() => .5; }
        private sealed class Rolls : System.Random
        {
            private readonly Queue<double> _values;
            public int Remaining => _values.Count;
            public Rolls(params double[] values) { _values = new(values); }
            public override double NextDouble() => _values.Dequeue();
        }
    }
}
