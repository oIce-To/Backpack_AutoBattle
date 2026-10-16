using System.Linq;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Enemies;
using BackpackAutoBattle.Items;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BackpackAutoBattle.Tests.Enemies
{
    public sealed class EnemyDataTests
    {
        [Test]
        public void GoblinAsset_HasRequestedBackpackWeaponAndCombatStats()
        {
            EnemyEncounterData goblin = AssetDatabase.LoadAssetAtPath<EnemyEncounterData>("Assets/_Project/Data/Enemies/Enemy_Goblin.asset");
            Assert.That(goblin, Is.Not.Null);
            Assert.That(goblin.DisplayName, Is.EqualTo("고블린"));
            Assert.That(goblin.MaximumHealth, Is.EqualTo(30));
            Assert.That(goblin.BackpackWidth, Is.EqualTo(6));
            Assert.That(goblin.BackpackHeight, Is.EqualTo(6));
            Assert.That(goblin.Items, Has.Count.EqualTo(1));
            ItemData club = goblin.Items[0].Item;
            Assert.That(club.DisplayName, Is.EqualTo("고블린 몽둥이"));
            Assert.That(club.OccupiedCells, Is.EqualTo(new[] { Vector2Int.zero, new Vector2Int(0, 1) }));
            Assert.That(club.BaseWeaponStats.AttackDamage, Is.EqualTo(5f));
            Assert.That(club.Activation.CooldownSeconds, Is.EqualTo(2f));
            Assert.That(club.BaseWeaponStats.AccuracyPercent, Is.EqualTo(100f));
            Assert.That(club.BaseWeaponStats.CriticalChancePercent, Is.Zero);
            Assert.That(club.BaseWeaponStats.CriticalDamagePercent, Is.EqualTo(200f));

            CombatantSnapshot snapshot = goblin.CreateCombatantSnapshot();
            Assert.That(snapshot.Loadout.Items.Single().OccupiedCells, Has.Count.EqualTo(2));
            var combat = new CombatSimulation(new CombatantSnapshot("Player", 40d,
                new CombatLoadoutSnapshot(System.Array.Empty<CombatItemSnapshot>())), snapshot);
            Assert.That(combat.Advance(1.9d), Is.Empty);
            CombatItemEvent attack = combat.Advance(0.2d).Single();
            Assert.That(attack.Side, Is.EqualTo(CombatSide.Enemy));
            Assert.That(attack.DisplayName, Is.EqualTo("고블린 몽둥이"));
            Assert.That(attack.Damage, Is.EqualTo(5d));
            Assert.That(attack.Hit, Is.True);
            Assert.That(attack.Critical, Is.False);
            Assert.That(combat.PlayerHealth, Is.EqualTo(35d));
        }

        [Test]
        public void InvalidEnemyPlacement_IsRejectedWithoutChangingAsset()
        {
            EnemyEncounterData original = AssetDatabase.LoadAssetAtPath<EnemyEncounterData>("Assets/_Project/Data/Enemies/Enemy_Goblin.asset");
            EnemyEncounterData copy = Object.Instantiate(original);
            try
            {
                var data = new SerializedObject(copy);
                data.FindProperty("_items").GetArrayElementAtIndex(0).FindPropertyRelative("_origin")
                    .vector2IntValue = new Vector2Int(5, 5);
                data.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<System.InvalidOperationException>(() => copy.CreateCombatantSnapshot());
                Assert.That(original.Items[0].Origin, Is.EqualTo(new Vector2Int(1, 0)));
            }
            finally { Object.DestroyImmediate(copy); }
        }
    }
}
