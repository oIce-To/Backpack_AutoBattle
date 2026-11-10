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
    public sealed class CharacterPassiveTests
    {
        private readonly List<Object> _temporary = new();
        private static CharacterPassiveSnapshot Allen => new(CharacterPassiveKind.LostLifeArmor, "allen", "굴하지 않는 자", 1, 10);
        private static CharacterPassiveSnapshot Serena => new(CharacterPassiveKind.RangedAttackFocus, "serena", "궁수의 집중", focusChancePercent: 30, focusStacks: 1);
        [TearDown] public void Cleanup() { foreach (var item in _temporary) Object.DestroyImmediate(item); _temporary.Clear(); }

        [TestCase(0, 50, 0)] [TestCase(1, 50, 5)] [TestCase(2, 50, 10)] [TestCase(3, 50, 15)]
        [TestCase(1, 52.5, 5.25)] [TestCase(2, 1000, 200)]
        public void LostLifeArmor_UsesCurrentMissingLivesAndScaledMaxHealth_OncePerBattle(int lost, double health, double expected)
        {
            var player = Unit(health, Allen, lost);
            var combat = new CombatSimulation(player, Unit(100));
            Assert.That(combat.PlayerArmor, Is.EqualTo(expected).Within(1e-9));
            combat.Advance(5); Assert.That(combat.PlayerArmor, Is.EqualTo(expected).Within(1e-9));
            var row = combat.CaptureStatistics().Items.Single(item => item.InstanceId == CombatSimulation.CharacterPassiveSourceId);
            Assert.That(row.DisplayName, Does.Contain("캐릭터 패시브").And.Contain("굴하지 않는 자"));
            Assert.That(row.DamageBlocked, Is.Zero);
            if (expected > 0)
            {
                combat.ApplyNonAttackDamage(CombatSide.Enemy, CombatSide.Player, expected / 2, CombatDamageKind.Other);
                Assert.That(combat.CaptureStatistics().Items.Single(item => item.InstanceId == row.InstanceId).DamageBlocked,
                    Is.EqualTo(expected / 2).Within(1e-9));
            }
            Assert.That(new CombatSimulation(player, Unit(100)).PlayerArmor, Is.EqualTo(expected).Within(1e-9));
        }

        [Test]
        public void BonusLife_IsAddedOnceAtEntry_AndLostLifeCountFollowsRetries()
        {
            var run = new PrototypeRunState(20, startingLives: 3, passive: Allen);
            Assert.That(run.Lives, Is.EqualTo(4));
            for (int lost = 1; lost <= 4; lost++)
            {
                Assert.That(run.TryBeginBattle(), Is.True);
                Assert.That(run.TryCompleteBattle(CombatResult.Defeat, 12, 1), Is.True);
                Assert.That(run.Lives, Is.EqualTo(4 - lost));
                if (lost < 4) Assert.That(run.TryReturnToPreparation(), Is.True);
            }
            Assert.That(run.IsFinished, Is.True);
            Assert.That(new PrototypeRunState(20, passive: Serena).StartingLives, Is.EqualTo(3));
        }

        [Test]
        public void RangedFocus_UsesIndependentThirtyPercentRoll_AfterTheCurrentAttack()
        {
            var rng = new SequenceRandom(.01, .5, .2999, .01, .5, .30);
            var combat = new CombatSimulation(Unit(100, Serena, items: new[] { Weapon(ItemCategory.Weapon, ItemTags.Ranged, 90) }), Unit(100), rng);
            Assert.That(combat.PlayerWeapons.Single().Stats.AccuracyPercent, Is.EqualTo(90));
            combat.Advance(1);
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Focus), Is.EqualTo(1));
            Assert.That(combat.PlayerWeapons.Single().Stats.AccuracyPercent, Is.EqualTo(91));
            combat.Advance(1);
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Focus), Is.EqualTo(1));
            Assert.That(rng.Calls, Is.EqualTo(6));
        }

        [TestCase(false)] [TestCase(true)]
        public void MissedAndEvadedRangedAttacks_CanStillGrantFocus(bool evade)
        {
            var random = evade ? new SequenceRandom(.01, .01, .01) : new SequenceRandom(.99, .01);
            var combat = new CombatSimulation(Unit(100, Serena, items: new[] { Weapon(ItemCategory.Weapon, ItemTags.Ranged, 30) }), Unit(100), random);
            if (evade) combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Agility, 70);
            var attack = combat.Advance(1).Single(item => item.Kind == ItemEffectKind.Attack);
            Assert.That(attack.AttackOutcome, Is.EqualTo(evade ? AttackOutcome.Evaded : AttackOutcome.AccuracyMiss));
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Focus), Is.EqualTo(1));
            Assert.That(combat.EnemyHealth, Is.EqualTo(100));
        }

        [Test]
        public void FullyBlockedRangedAttack_GrantsFocus_ButPeriodicAndOtherDamageCannot()
        {
            var combat = new CombatSimulation(Unit(100, Serena, items: new[] { Weapon(ItemCategory.Weapon, ItemTags.Ranged) }),
                Unit(100, Allen, 1), new SequenceRandom(.01, .5, .01));
            combat.Advance(1);
            Assert.That(combat.EnemyArmor, Is.EqualTo(5)); Assert.That(combat.EnemyHealth, Is.EqualTo(100));
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Focus), Is.EqualTo(1));
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Poison, 1);
            combat.ApplyNonAttackDamage(CombatSide.Player, CombatSide.Enemy, 1, CombatDamageKind.Other);
            combat.PlayerItems[0].Cycle.SetBaseInterval(100);
            combat.Advance(2);
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Focus), Is.EqualTo(1));
        }

        [TestCase(ItemCategory.Weapon, ItemTags.Melee)]
        [TestCase(ItemCategory.Weapon, ItemTags.Magic)]
        [TestCase(ItemCategory.Pet, ItemTags.Ranged)]
        public void OnlyRangedWeapons_TriggerFocus(ItemCategory category, ItemTags tags)
        {
            var random = new SequenceRandom(.01, .5);
            var combat = new CombatSimulation(Unit(100, Serena, items: new[] { Weapon(category, tags) }), Unit(100), random);
            combat.Advance(1);
            Assert.That(combat.GetStatusStacks(CombatSide.Player, StatusEffectKind.Focus), Is.Zero);
            Assert.That(random.Calls, Is.EqualTo(2));
        }

        [Test]
        public void PassiveSo_IsCapturedWithoutLiveReferences_AndAssetsHaveTheirArtwork()
        {
            foreach (string character in new[] { "Character_Default", "Character_Serena" })
            {
                var data = AssetDatabase.LoadAssetAtPath<CharacterData>($"Assets/_Project/Data/{character}.asset");
                Assert.That(data.Passive, Is.Not.Null); Assert.That(data.Passive.Icon, Is.Not.Null);
            }
            var source = AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/_Project/Data/Character_Default.asset").Passive;
            var copy = Object.Instantiate(source); _temporary.Add(copy);
            var captured = copy.Capture();
            Set(copy, "_bonusLives", 9); Set(copy, "_armorPercentPerLostLife", 99f);
            Assert.That(captured.BonusLives, Is.EqualTo(1)); Assert.That(captured.ArmorPercentPerLostLife, Is.EqualTo(10));
        }

        [Test]
        public void PlaybackPartitioning_DoesNotChangeProcCountOrBattleOutcome()
        {
            var player = Unit(500, Serena, items: new[] { Weapon(ItemCategory.Weapon, ItemTags.Ranged, 70) });
            var a = new CombatSimulation(player, Unit(500), new System.Random(1234));
            var b = new CombatSimulation(player, Unit(500), new System.Random(1234));
            a.Advance(10); for (int i = 0; i < 100; i++) b.Advance(.1);
            Assert.That(b.GetStatusStacks(CombatSide.Player, StatusEffectKind.Focus), Is.EqualTo(a.GetStatusStacks(CombatSide.Player, StatusEffectKind.Focus)));
            Assert.That(b.EnemyHealth, Is.EqualTo(a.EnemyHealth));
        }

        private ItemData Weapon(ItemCategory category, ItemTags tags, float accuracy = 100)
        {
            var data = ScriptableObject.CreateInstance<ItemData>(); _temporary.Add(data);
            Set(data, "_category", category); Set(data, "_tags", tags);
            Set(data, "_weaponStats", new WeaponStats(5, 1, accuracy, 0, 200));
            Set(data, "_activation", new ItemActivationData(1, new ItemActivationEffect(ItemEffectKind.Attack)));
            return data;
        }
        private static CombatantSnapshot Unit(double health, CharacterPassiveSnapshot passive = null, int lost = 0, params ItemData[] items)
            => new("Test", health, new CombatLoadoutSnapshot(items.Select((item, i) => new CombatItemSnapshot("i" + i, item,
                new Vector2Int(i, 0), ItemRotation.None, item.OccupiedCells))), passive: passive, lostLives: lost);
        private static void Set(object target, string field, object value)
            => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private sealed class SequenceRandom : System.Random
        {
            private readonly Queue<double> _rolls;
            internal int Calls { get; private set; }
            internal SequenceRandom(params double[] rolls) => _rolls = new Queue<double>(rolls);
            public override double NextDouble() { Calls++; return _rolls.Count > 0 ? _rolls.Dequeue() : .5; }
        }
    }
}
