using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class PrototypeBattlePlaybackTests
    {
        private BackpackPrototypeController _controller;
        private PrototypeBattleController Battle => _controller.BattleController;
        private IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single); yield return null;
            _controller = Object.FindFirstObjectByType<BackpackPrototypeController>(); Battle.AutoTick = false;
        }
        private void StartQuietBattle()
        {
            Assert.That(_controller.TryStartBattle(), Is.True);
            foreach (var weapon in Battle.Simulation.PlayerWeapons.Concat(Battle.Simulation.EnemyWeapons)) weapon.Stats.AttackDamage = 0;
        }

        [UnityTest]
        public IEnumerator PauseAndSpeedButtons_FreezeCombatButKeepTooltipsAndResumeAtSelectedSpeed()
        {
            yield return Setup(); StartQuietBattle();
            float timeScale = Time.timeScale;
            var weapon = Battle.Simulation.PlayerWeapons.First(); double interval = weapon.Cycle.IntervalSeconds;
            Click("Combat Speed 0.5 Button"); Battle.TickCombat(2);
            Assert.That(Battle.Simulation.ElapsedSeconds, Is.EqualTo(1));
            Assert.That(Battle.PlaybackSpeed, Is.EqualTo(.5f));
            var view = _controller.GetComponentsInChildren<PrototypeBattleItemView>().First(v => v.Snapshot.InstanceId == weapon.InstanceId);
            float progress = view.CycleProgress;
            Click("Combat Pause Button"); yield return null; Canvas.ForceUpdateCanvases();
            var glyph = _controller.GetComponentInChildren<PrototypePlaybackGlyph>();
            Assert.That(Battle.IsPaused, Is.True); Assert.That(glyph.ShowPlay, Is.True);
            Assert.That(glyph.canvasRenderer.GetMesh().vertexCount, Is.EqualTo(3));
            Assert.That(Find("Combat Paused Label").gameObject.activeInHierarchy, Is.True);
            var before = Battle.Simulation.CaptureStatistics();
            Battle.TickCombat(1000);
            Assert.That(Battle.Simulation.ElapsedSeconds, Is.EqualTo(1)); Assert.That(Battle.Simulation.EnrageStacks, Is.Zero);
            Assert.That(view.CycleProgress, Is.EqualTo(progress));
            Assert.That(Battle.Simulation.CaptureStatistics().Items.Select(i => (i.DamageDealt, i.DefenseTotal)),
                Is.EqualTo(before.Items.Select(i => (i.DamageDealt, i.DefenseTotal))));
            Assert.That(Battle.Simulation.Result, Is.EqualTo(CombatResult.Running));
            view.OnPointerEnter(Pointer(view.transform));
            Assert.That(_controller.ItemTooltip.IsVisible, Is.True); Assert.That(_controller.ItemTooltip.Owner, Is.SameAs(view));
            PrototypeScreenCapture.CaptureIfRequested(_controller.GetComponentInChildren<Canvas>(), "PlaybackPausedTooltip.png");
            view.OnPointerExit(Pointer(view.transform));
            Click("Combat Speed 3 Button"); Assert.That(Battle.IsPaused, Is.True);
            Battle.TickCombat(2); Assert.That(Battle.Simulation.ElapsedSeconds, Is.EqualTo(1));
            Assert.That(Find("Combat Speed 3 Button").GetComponent<Image>().color,
                Is.Not.EqualTo(Find("Combat Speed 1 Button").GetComponent<Image>().color));
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(Pointer(Find("Combat Pause Button")), hits);
            Assert.That(hits.Any(h => h.gameObject == Find("Combat Pause Button").gameObject), Is.True);
            Click("Combat Pause Button"); yield return null; Canvas.ForceUpdateCanvases();
            Assert.That(glyph.ShowPlay, Is.False); Assert.That(glyph.canvasRenderer.GetMesh().vertexCount, Is.EqualTo(8));
            Battle.TickCombat(1); Assert.That(Battle.Simulation.ElapsedSeconds, Is.EqualTo(4));
            Assert.That(weapon.Cycle.IntervalSeconds, Is.EqualTo(interval), "Playback must not change item stats.");
            Assert.That(Time.timeScale, Is.EqualTo(timeScale));
            PrototypeScreenCapture.CaptureIfRequested(_controller.GetComponentInChildren<Canvas>(), "Playback3x.png");
        }

        [UnityTest]
        public IEnumerator EverySpeed_UsesCombatSecondsForEnrageAndForcedDefeat_AndPauseStopsBoth()
        {
            foreach (float speed in PrototypeBattleController.PlaybackSpeeds)
            {
                yield return Setup(); StartQuietBattle();
                Battle.SetPlaybackSpeed(speed); Battle.TickCombat(57f / speed);
                Assert.That(Battle.Simulation.ElapsedSeconds, Is.EqualTo(57).Within(1e-4));
                Assert.That(Battle.Simulation.EnrageStacks, Is.Zero);
                Battle.TogglePause(); Battle.TickCombat(1000);
                Assert.That(Battle.Simulation.ElapsedSeconds, Is.EqualTo(57).Within(1e-4));
                Battle.TogglePause(); Battle.TickCombat(3f / speed);
                Assert.That(Battle.Simulation.EnrageStacks, Is.EqualTo(1));
                Battle.TickCombat(27f / speed); Assert.That(Battle.Simulation.EnrageDamageIncreasePercent, Is.EqualTo(100));
                Battle.TogglePause(); Battle.TickCombat(1000); Assert.That(Battle.Simulation.Result, Is.EqualTo(CombatResult.Running));
                Battle.TogglePause(); Battle.TickCombat(3f / speed);
                Assert.That(Battle.Simulation.ElapsedSeconds, Is.EqualTo(90)); Assert.That(Battle.Simulation.IsForcedDefeat, Is.True);
                Assert.That(_controller.Lives, Is.EqualTo(2)); Assert.That(_controller.CurrentRound, Is.EqualTo(1));
                Battle.TickCombat(1000); Assert.That(_controller.Lives, Is.EqualTo(2));
                Assert.That(Find("Combat Playback Controls").gameObject.activeSelf, Is.False);
                Assert.That(Battle.TogglePause(), Is.False); Assert.That(Battle.SetPlaybackSpeed(1), Is.False);
            }
        }

        [UnityTest]
        public IEnumerator SpeedChanges_KeepCycleProgress_ButEveryRoundRetryAndNewRunStartsAt1x()
        {
            yield return Setup(); StartQuietBattle();
            Battle.TickCombat(.5f); var item = Battle.Simulation.PlayerItems.First();
            double progress = item.Cycle.ProgressAt(Battle.Simulation.ElapsedSeconds);
            Click("Combat Speed 2 Button");
            Assert.That(item.Cycle.ProgressAt(Battle.Simulation.ElapsedSeconds), Is.EqualTo(progress));
            Battle.TickCombat(.5f); Assert.That(Battle.Simulation.ElapsedSeconds, Is.EqualTo(1.5));
            var sword = Battle.Simulation.PlayerWeapons.First(); sword.Stats.AttackDamage = 10000; sword.Stats.AccuracyPercent = 100;
            Battle.TickCombat(1); Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Victory));
            Assert.That(_controller.AdvanceRound(), Is.True); yield return null;
            StartQuietBattle(); Assert.That(Battle.PlaybackSpeed, Is.EqualTo(1)); Assert.That(Battle.IsPaused, Is.False);
            Assert.That(Find("Combat Speed 1 Button").GetComponent<Image>().color,
                Is.Not.EqualTo(Find("Combat Speed 2 Button").GetComponent<Image>().color));
            Battle.TickCombat(1); Assert.That(Battle.Simulation.ElapsedSeconds, Is.EqualTo(1));
            Battle.SetPlaybackSpeed(3);
            Battle.TickCombat(30); Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Defeat));
            Assert.That(_controller.ReturnToPreparation(), Is.True); yield return null;
            StartQuietBattle(); Assert.That(Battle.PlaybackSpeed, Is.EqualTo(1)); Assert.That(Battle.IsPaused, Is.False);
            Battle.SetPlaybackSpeed(.5f);
            Battle.TickCombat(180); _controller.ReturnToPreparation(); yield return null;
            StartQuietBattle(); Assert.That(Battle.PlaybackSpeed, Is.EqualTo(1));
            Battle.SetPlaybackSpeed(2); Battle.TickCombat(45);
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.GameOver));
            Assert.That(_controller.TryRestartRun(), Is.True); yield return null;
            Assert.That(Battle.PlaybackSpeed, Is.EqualTo(1)); Assert.That(Battle.IsPaused, Is.False);
            StartQuietBattle(); Assert.That(Battle.Simulation.ElapsedSeconds, Is.Zero);
            Battle.TickCombat(1); Assert.That(Battle.Simulation.ElapsedSeconds, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SameSeed_AllSpeedsProduceIdenticalEventsHealthWinnerAndStatistics()
        {
            yield return Setup(); _controller.TryStartBattle();
            var player = new CombatantSnapshot("Player", 500, Battle.Simulation.Player.Loadout);
            var enemy = new CombatantSnapshot("Enemy", 500, Battle.Simulation.Enemy.Loadout);
            List<(double, CombatSide, string, bool, bool, double)> expectedEvents = null;
            CombatStatisticsSnapshot expected = null; double playerHealth = 0, enemyHealth = 0;
            foreach (float speed in PrototypeBattleController.PlaybackSpeeds)
            {
                Battle.BeginBattle(player, enemy, 1, 5, random: new System.Random(651));
                Battle.SetPlaybackSpeed(speed);
                Battle.Simulation.ApplyStatus(CombatSide.Player, StatusEffectKind.Lifesteal, 2);
                Battle.Simulation.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Thorns, 2);
                Battle.Simulation.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Poison, 2, CombatSide.Player);
                var events = new List<(double, CombatSide, string, bool, bool, double)>();
                Battle.Simulation.ItemActivated += e => events.Add((e.Time, e.Side, e.InstanceId, e.Hit, e.Critical, e.Damage));
                // Binary-exact frame durations avoid introducing float partition differences into the fixture.
                for (int frame = 0; frame < 2000 && Battle.Simulation.Result == CombatResult.Running; frame++) Battle.TickCombat(.125f);
                var stats = Battle.Simulation.CaptureStatistics(); Assert.That(stats.Result, Is.Not.EqualTo(CombatResult.Running));
                if (expected == null)
                {
                    expected = stats; expectedEvents = events;
                    playerHealth = Battle.Simulation.PlayerHealth; enemyHealth = Battle.Simulation.EnemyHealth;
                }
                else
                {
                    Assert.That(events, Is.EqualTo(expectedEvents));
                    Assert.That(stats.Result, Is.EqualTo(expected.Result)); Assert.That(stats.Seconds, Is.EqualTo(expected.Seconds));
                    Assert.That(Battle.Simulation.PlayerHealth, Is.EqualTo(playerHealth)); Assert.That(Battle.Simulation.EnemyHealth, Is.EqualTo(enemyHealth));
                    Assert.That(stats.Items.Select(i => (i.Side, i.InstanceId, i.DirectDamage, i.BurnDamage, i.PoisonDamage, i.ThornsDamage, i.Healing, i.DamageBlocked)),
                        Is.EqualTo(expected.Items.Select(i => (i.Side, i.InstanceId, i.DirectDamage, i.BurnDamage, i.PoisonDamage, i.ThornsDamage, i.Healing, i.DamageBlocked))));
                }
            }
        }

        [UnityTest]
        public IEnumerator InvalidControlsAreRejected_AndAutomaticUpdatesUseRealTimeWithoutGlobalPause()
        {
            yield return Setup();
            Assert.That(Battle.TogglePause(), Is.False); Assert.That(Battle.SetPlaybackSpeed(2), Is.False);
            StartQuietBattle();
            foreach (float invalid in new[] { 0f, -1f, 1.5f, float.NaN, float.PositiveInfinity })
                Assert.That(Battle.SetPlaybackSpeed(invalid), Is.False);
            float previousScale = Time.timeScale;
            try
            {
                Time.timeScale = 0; Battle.AutoTick = true;
                yield return new WaitForSecondsRealtime(.15f);
                Assert.That(Battle.Simulation.ElapsedSeconds, Is.GreaterThan(0));
                Battle.TogglePause(); double time = Battle.Simulation.ElapsedSeconds;
                yield return new WaitForSecondsRealtime(.15f);
                Assert.That(Battle.Simulation.ElapsedSeconds, Is.EqualTo(time));
                Assert.That(Time.timeScale, Is.Zero);
            }
            finally { Battle.AutoTick = false; Time.timeScale = previousScale; }
        }

        private Transform Find(string name) => _controller.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
        private void Click(string name) => Find(name).GetComponent<Button>().onClick.Invoke();
        private static PointerEventData Pointer(Transform target) => new(EventSystem.current)
        { position = RectTransformUtility.WorldToScreenPoint(null, target.GetComponent<RectTransform>().TransformPoint(target.GetComponent<RectTransform>().rect.center)) };
    }
}
