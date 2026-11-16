using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    public sealed class PrototypeAudioPlayModeTests
    {
        private string _oldProfile, _oldSettings, _directory;
        private BackpackPrototypeController _controller;
        private readonly List<PrototypeSound> _sounds = new();

        [SetUp]
        public void IsolateSaves()
        {
            _oldProfile = Environment.GetEnvironmentVariable("BACKPACK_PROFILE_PATH");
            _oldSettings = Environment.GetEnvironmentVariable("BACKPACK_SETTINGS_PATH");
            _directory = Path.Combine(Path.GetTempPath(), "BackpackAudio_" + Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable("BACKPACK_PROFILE_PATH", Path.Combine(_directory, "profile.json"));
            Environment.SetEnvironmentVariable("BACKPACK_SETTINGS_PATH", Path.Combine(_directory, "settings.json"));
            _sounds.Clear();
        }

        [TearDown]
        public void RestoreSaves()
        {
            if (_controller != null) _controller.CloseSettings();
            Environment.SetEnvironmentVariable("BACKPACK_PROFILE_PATH", _oldProfile);
            Environment.SetEnvironmentVariable("BACKPACK_SETTINGS_PATH", _oldSettings);
            if (Directory.Exists(_directory))
            {
                foreach (string file in Directory.GetFiles(_directory)) File.Delete(file);
                Directory.Delete(_directory);
            }
        }

        [UnityTest]
        public IEnumerator MusicFollowsScreens_AndRespectsBothPauseReasons()
        {
            yield return LoadMenu();
            var music = Find("Background Music").GetComponent<AudioSource>();
            Assert.That(music.clip.name, Is.EqualTo("MainScene_BGM"));
            Assert.That(music.loop, Is.True);
            foreach (PrototypeSound sound in Enum.GetValues(typeof(PrototypeSound)))
                Assert.That(Resources.Load<AudioClip>("Audio/" + sound), Is.Not.Null, sound.ToString());
            _controller.ShowCharacterSelection();
            Assert.That(music.clip.name, Is.EqualTo("MainScene_BGM"));
            Assert.That(_controller.TryStartRun(), Is.True);
            Assert.That(music.clip.name, Is.EqualTo("ShopPhase_BGM"));
            Assert.That(_controller.TryOpenSettings(), Is.True);
            Assert.That(_controller.IsMusicPaused, Is.False, "Shop music keeps playing in settings.");
            _controller.CloseSettings();
            Assert.That(_controller.TryStartBattle(), Is.True);
            Assert.That(music.clip.name, Is.EqualTo("Battle_BGM"));
            Assert.That(_controller.BattleController.TogglePause(), Is.True);
            Assert.That(_controller.IsMusicPaused, Is.True);
            int pausedSample = music.timeSamples;
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(music.timeSamples, Is.EqualTo(pausedSample));
            Assert.That(_controller.TryOpenSettings(), Is.True);
            _controller.CloseSettings();
            Assert.That(_controller.IsMusicPaused, Is.True, "Closing settings must preserve manual pause.");
            _controller.BattleController.TogglePause();
            Assert.That(_controller.IsMusicPaused, Is.False);
            Assert.That(_controller.TryOpenSettings(), Is.True);
            Assert.That(_controller.IsMusicPaused, Is.True);
            _controller.CloseSettings();
            Assert.That(_controller.IsMusicPaused, Is.False);
            Assert.That(_controller.BattleController.SetPlaybackSpeed(3), Is.True);
            Assert.That(music.pitch, Is.EqualTo(1f), "Battle speed must not change music speed.");
        }

        [UnityTest]
        public IEnumerator UiClicksPlayOnce_AndRespectDisabledButtonsAndEffectsVolume()
        {
            yield return LoadMenu();
            Assert.That(_controller.GetComponentsInChildren<Button>(true).All(b => b is PrototypeAudioButton), Is.True);
            Click("Main Tab Equipment");
            Assert.That(_sounds, Is.EqualTo(new[] { PrototypeSound.ButtonClick }));
            Assert.That(_controller.GetComponentsInChildren<Button>(true).All(b => b is PrototypeAudioButton), Is.True,
                "Dynamically created item cards must use the same click audio.");
            _sounds.Clear();
            var button = Find("Main Tab Home").GetComponent<Button>();
            button.interactable = false;
            Click("Main Tab Home");
            Assert.That(_sounds, Is.Empty);
            button.interactable = true;
            ExecuteEvents.Execute(button.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            Assert.That(_sounds, Is.EqualTo(new[] { PrototypeSound.ButtonClick }));
            Assert.That(_controller.TryOpenSettings(), Is.True);
            Find("Settings Volume 2").GetComponent<Slider>().value = 0;
            _sounds.Clear();
            Click("Settings Close");
            Assert.That(_sounds, Is.Empty);
            Assert.That(Find("Background Music").GetComponent<AudioSource>().volume, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator DragSoundsOnlyCommitOnSuccessfulPurchasesPlacementsAndSales()
        {
            yield return LoadTraining();
            var potion = _controller.ItemCatalog.Single(i => i.ItemId == "healing_potion");
            Set("_itemCatalog", new[] { potion });
            _controller.RefreshShopForTesting();
            var slot = _controller.GetComponentsInChildren<PrototypeShopSlotView>().First();
            var pointer = Pointer(slot.transform);
            slot.OnBeginDrag(pointer);
            Assert.That(_sounds, Is.EqualTo(new[] { PrototypeSound.ItemPick }));
            pointer.position = new Vector2(-1000, -1000);
            slot.OnDrag(pointer); slot.OnEndDrag(pointer);
            Assert.That(_sounds, Is.EqualTo(new[] { PrototypeSound.ItemPick }), "Rejected drop must not buy or place.");
            yield return null;
            _sounds.Clear();
            DragShop(slot, Find("Freeform Storage"));
            Assert.That(_sounds, Is.EqualTo(new[] { PrototypeSound.ItemPick, PrototypeSound.Buying, PrototypeSound.ItemPlace }));
            yield return null;
            var storage = _controller.GetComponentsInChildren<PrototypeStorageItemView>().Single();
            _sounds.Clear();
            pointer = Pointer(storage.transform); storage.OnBeginDrag(pointer);
            pointer.position = Pointer(Find("Grid Cell 5,5")).position;
            storage.OnDrag(pointer); storage.OnEndDrag(pointer);
            Assert.That(_sounds, Is.EqualTo(new[] { PrototypeSound.ItemPick, PrototypeSound.ItemPlace }));
            yield return null;
            var item = _controller.GetComponentsInChildren<BackpackItemView>().Single(i => i.Data == potion);
            _sounds.Clear();
            pointer = Pointer(item.transform); item.OnBeginDrag(pointer);
            pointer.position = Pointer(Find("Prototype Shop")).position;
            item.OnDrag(pointer); item.OnEndDrag(pointer);
            Assert.That(_sounds, Is.EqualTo(new[] { PrototypeSound.ItemPick, PrototypeSound.Selling }));
            _sounds.Clear();
            Assert.That(_controller.TryRerollShop(), Is.True);
            Assert.That(_sounds, Is.EqualTo(new[] { PrototypeSound.Reroll }));
            Set("_rerollCost", int.MaxValue);
            _sounds.Clear();
            Assert.That(_controller.TryRerollShop(), Is.False);
            Assert.That(_sounds, Is.Empty);
        }

        [UnityTest]
        public IEnumerator OnlyWeaponHitsPlayHitAudio_AndRoundAndStageResultsDoNotDuplicate()
        {
            yield return LoadTraining();
            Assert.That(_controller.TryStartBattle(), Is.True);
            var combat = _controller.ActiveCombat;
            foreach (var weapon in combat.PlayerWeapons.Concat(combat.EnemyWeapons))
            { weapon.Stats.AccuracyPercent = 100; weapon.Stats.AttackDamage = 0; }
            var attacks = new List<CombatItemEvent>();
            combat.ItemActivated += attacks.Add;
            // Status damage must not enter the item hit-audio callback.
            combat.ApplyStatus(CombatSide.Player, StatusEffectKind.Burn, 1);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Poison, 1);
            combat.ApplyStatus(CombatSide.Enemy, StatusEffectKind.Thorns, 1);
            _controller.TickCombat(2.1f);
            Assert.That(attacks.Any(a => a.Side == CombatSide.Player && a.Kind == ItemEffectKind.Attack), Is.True);
            Assert.That(attacks.Any(a => a.Side == CombatSide.Enemy && a.Kind == ItemEffectKind.Attack), Is.True);
            Assert.That(_sounds.Count(s => s == PrototypeSound.SimpleHit),
                Is.EqualTo(attacks.Count(a => a.Kind == ItemEffectKind.Attack && a.Hit)));
            _sounds.Clear();
            combat.PlayerWeapons.First().Stats.AttackDamage = 10000;
            _controller.TickCombat(3);
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Victory));
            Assert.That(_sounds.Count(s => s == PrototypeSound.RoundClear), Is.EqualTo(1));
            _controller.TickCombat(3);
            Assert.That(_sounds.Count(s => s == PrototypeSound.RoundClear), Is.EqualTo(1));
            Assert.That(_controller.AdvanceRound(), Is.True);
            Assert.That(Find("Background Music").GetComponent<AudioSource>().clip.name, Is.EqualTo("ShopPhase_BGM"));
            _controller.TryStartBattle();
            foreach (var weapon in _controller.ActiveCombat.PlayerWeapons) weapon.Stats.AttackDamage = 0;
            foreach (var weapon in _controller.ActiveCombat.EnemyWeapons)
            { weapon.Stats.AttackDamage = 10000; weapon.Stats.AccuracyPercent = 100; }
            _sounds.Clear(); _controller.TickCombat(3);
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Defeat));
            Assert.That(_sounds.Count(s => s == PrototypeSound.RoundFail), Is.EqualTo(1));
            Assert.That(_controller.ReturnToPreparation(), Is.True);
            while (_controller.CurrentRound < _controller.StageRoundCount) Assert.That(_controller.AdvanceRound(), Is.True);
            _controller.TryStartBattle();
            foreach (var weapon in _controller.ActiveCombat.PlayerWeapons)
            { weapon.Stats.AttackDamage = 10000; weapon.Stats.AccuracyPercent = 100; }
            foreach (var weapon in _controller.ActiveCombat.EnemyWeapons) weapon.Stats.AttackDamage = 0;
            _sounds.Clear(); _controller.TickCombat(3);
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Completed));
            Assert.That(_sounds.Count(s => s == PrototypeSound.StageClear), Is.EqualTo(1));
            Assert.That(_sounds, Has.No.Member(PrototypeSound.RoundClear));
            _controller.TickCombat(3);
            Assert.That(_sounds.Count(s => s == PrototypeSound.StageClear), Is.EqualTo(1));
            Assert.That(_controller.TryReturnToMainMenu(), Is.True);
            Assert.That(Find("Background Music").GetComponent<AudioSource>().clip.name, Is.EqualTo("MainScene_BGM"));
        }

        private IEnumerator LoadMenu()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            float deadline = Time.realtimeSinceStartup + 10;
            do { yield return null; _controller = Object.FindFirstObjectByType<BackpackPrototypeController>(); }
            while ((_controller == null || !_controller.IsMainMenu) && Time.realtimeSinceStartup < deadline);
            Assert.That(_controller != null && _controller.IsMainMenu, Is.True);
            _controller.BattleController.AutoTick = false;
            _controller.SoundPlayed += _sounds.Add;
            yield return null;
        }
        private IEnumerator LoadTraining()
        {
            yield return PrototypeCombatFixture.LoadWithTrainingKit();
            _controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            _controller.BattleController.AutoTick = false;
            _controller.SoundPlayed += _sounds.Add;
            Canvas.ForceUpdateCanvases();
        }
        private Transform Find(string name) => _controller.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
        private void Set(string name, object value) => typeof(BackpackPrototypeController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_controller, value);
        private static PointerEventData Pointer(Transform target) => new(EventSystem.current)
        { button = PointerEventData.InputButton.Left, position = RectTransformUtility.WorldToScreenPoint(null, target.GetComponent<RectTransform>().TransformPoint(target.GetComponent<RectTransform>().rect.center)) };
        private void Click(string name) => ExecuteEvents.Execute(Find(name).gameObject, Pointer(Find(name)), ExecuteEvents.pointerClickHandler);
        private static void DragShop(PrototypeShopSlotView slot, Transform target)
        {
            var pointer = Pointer(slot.transform); slot.OnBeginDrag(pointer);
            pointer.position = Pointer(target).position; slot.OnDrag(pointer); slot.OnEndDrag(pointer);
        }
    }
}
