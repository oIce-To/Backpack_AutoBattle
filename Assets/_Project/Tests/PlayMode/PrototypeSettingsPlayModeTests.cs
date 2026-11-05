using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Combat;
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
    public sealed class PrototypeSettingsPlayModeTests
    {
        private string _directory, _profilePath, _settingsPath, _oldProfile, _oldSettings;
        private BackpackPrototypeController _controller;

        [SetUp]
        public void IsolateSaves()
        {
            _oldProfile = Environment.GetEnvironmentVariable("BACKPACK_PROFILE_PATH");
            _oldSettings = Environment.GetEnvironmentVariable("BACKPACK_SETTINGS_PATH");
            _directory = Path.Combine(Path.GetTempPath(), "BackpackSettingsTest_" + Guid.NewGuid().ToString("N"));
            _profilePath = Path.Combine(_directory, "profile.json");
            _settingsPath = Path.Combine(_directory, "settings.json");
            Environment.SetEnvironmentVariable("BACKPACK_PROFILE_PATH", _profilePath);
            Environment.SetEnvironmentVariable("BACKPACK_SETTINGS_PATH", _settingsPath);
        }

        [TearDown]
        public void RemoveTestSaves()
        {
            Environment.SetEnvironmentVariable("BACKPACK_PROFILE_PATH", _oldProfile);
            Environment.SetEnvironmentVariable("BACKPACK_SETTINGS_PATH", _oldSettings);
            if (_controller != null) _controller.CloseSettings();
            foreach (string path in new[] { _profilePath, _profilePath + ".bak", _profilePath + ".tmp", _settingsPath, _settingsPath + ".tmp" })
                if (File.Exists(path)) File.Delete(path);
            if (Directory.Exists(_directory)) Directory.Delete(_directory);
        }

        [UnityTest]
        public IEnumerator AudioIsAppliedLive_AndDisplayPersistsOnlyAfterConfirmation()
        {
            yield return LoadMenu();
            Click("Main Settings Button");
            Assert.That(_controller.IsSettingsOpen, Is.True);
            Assert.That(_controller.TryStartRun(), Is.False);
            Find("Settings Volume 0").GetComponent<Slider>().value = 50;
            Find("Settings Volume 1").GetComponent<Slider>().value = 20;
            Find("Settings Volume 2").GetComponent<Slider>().value = 80;
            Assert.That(Find("Background Music").GetComponent<AudioSource>().volume, Is.EqualTo(.1f).Within(.001f));
            Assert.That(Find("Combat Effects").GetComponent<AudioSource>().volume, Is.EqualTo(.4f).Within(.001f));
            Capture("SettingsAudio.png");
            Click("Settings Tab 1");
            var next = Find("Settings Resolution Next").GetComponent<Button>();
            string resolution = Text("Settings Resolution");
            if (next.interactable)
            {
                next.onClick.Invoke();
                Assert.That(Text("Settings Resolution"), Is.Not.EqualTo(resolution));
            }
            Click("Settings Full Screen");
            Click("Settings Display Apply");
            Assert.That(Find("Settings Display Confirmation").gameObject.activeInHierarchy, Is.True);
            Assert.That(Text("Settings Display Countdown"), Does.Contain("15초"));
            Assert.That(File.Exists(_settingsPath), Is.False);
            Capture("SettingsDisplayConfirmation.png");
            Click("Settings Display Revert");
            Assert.That(Text("Settings Resolution"), Is.EqualTo(resolution));

            Click("Settings Full Screen");
            Click("Settings Display Apply");
            Click("Settings Display Confirm");
            string confirmed = File.ReadAllText(_settingsPath);
            Assert.That(confirmed, Does.Contain("\"fullScreen\": true"));
            Capture("SettingsDisplay.png");
            Click("Settings Windowed");
            Click("Settings Display Apply");
            typeof(BackpackPrototypeController).GetField("_displayDeadline", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_controller, Time.realtimeSinceStartupAsDouble - .01);
            yield return null;
            Assert.That(Find("Settings Display Confirmation").gameObject.activeSelf, Is.False);
            Assert.That(File.ReadAllText(_settingsPath), Is.EqualTo(confirmed));
            Assert.That(Find("Settings Display Apply").GetComponent<Button>().interactable, Is.False);
            Click("Settings Close");
            yield return LoadMenu();
            Click("Main Settings Button");
            Assert.That(Find("Settings Volume 1").GetComponent<Slider>().value, Is.EqualTo(20));
            Click("Settings Reset");
            Assert.That(Find("Settings Volume 0").GetComponent<Slider>().value, Is.EqualTo(100));
            Assert.That(Find("Background Music").GetComponent<AudioSource>().volume, Is.EqualTo(1));
            Click("Settings Close");
            Assert.That(File.ReadAllText(_settingsPath), Does.Contain("\"fullScreen\": true"));
        }

        [UnityTest]
        public IEnumerator BattleSettingsPreservePauseAndCurrentSpeed_AndHideOnlyFloatingFeedback()
        {
            yield return LoadMenu();
            Click("Main Settings Button");
            Click("Settings Tab 2");
            Click("Settings Speed 3");
            Find("Settings Damage Toggle").GetComponent<Toggle>().isOn = false;
            Click("Settings Close");
            Assert.That(_controller.TryStartRun(), Is.True);
            Assert.That(_controller.TryStartBattle(), Is.True);
            var battle = _controller.BattleController;
            Assert.That(battle.PlaybackSpeed, Is.EqualTo(3));
            Assert.That(Find("Player Damage").GetComponent<Text>().enabled, Is.False);
            Assert.That(Find("Enemy Damage").GetComponent<Text>().enabled, Is.False);
            battle.Simulation.PlayerWeapons.First().Stats.AccuracyPercent = 100;
            battle.TickCombat(1);
            Assert.That(battle.Simulation.EnemyHealth, Is.LessThan(battle.Simulation.EnemyMaxHealth));
            Assert.That(Text("Combat Log"), Does.Contain("피해"));
            battle.SetPlaybackSpeed(.5f);
            double elapsed = battle.Simulation.ElapsedSeconds;
            yield return null; // Let the newly shown stage HUD receive its first UI frame.
            ClickStageGear();
            Assert.That(_controller.IsSettingsOpen, Is.True);
            battle.TickCombat(20);
            Assert.That(battle.Simulation.ElapsedSeconds, Is.EqualTo(elapsed));
            Assert.That(battle.IsPaused, Is.False);
            Click("Settings Speed 2");
            Find("Settings Damage Toggle").GetComponent<Toggle>().isOn = true;
            Assert.That(Find("Player Damage").GetComponent<Text>().enabled, Is.True);
            Assert.That(Find("Enemy Damage").GetComponent<Text>().enabled, Is.True);
            Assert.That(Text("Player Damage"), Is.Empty);
            Capture("SettingsBattle.png");
            Click("Settings Close");
            Assert.That(battle.PlaybackSpeed, Is.EqualTo(.5f));
            battle.TickCombat(.5f);
            Assert.That(battle.Simulation.ElapsedSeconds, Is.GreaterThan(elapsed));

            battle.TogglePause();
            elapsed = battle.Simulation.ElapsedSeconds;
            ClickStageGear();
            Click("Settings Close");
            battle.TickCombat(20);
            Assert.That(battle.IsPaused, Is.True);
            Assert.That(battle.Simulation.ElapsedSeconds, Is.EqualTo(elapsed));
            battle.TogglePause();
            battle.Simulation.PlayerWeapons.First().Stats.AttackDamage = 10000;
            battle.TickCombat(10);
            Assert.That(battle.Simulation.Result, Is.EqualTo(CombatResult.Victory));
            Assert.That(_controller.AdvanceRound(), Is.True);
            Assert.That(_controller.TryStartBattle(), Is.True);
            Assert.That(battle.PlaybackSpeed, Is.EqualTo(2));
            Assert.That(battle.IsPaused, Is.False);
            yield return LoadMenu();
            Click("Main Settings Button");
            Click("Settings Tab 2");
            Assert.That(Text("Settings Damage State"), Is.EqualTo("ON"));
            Click("Settings Reset");
            Click("Settings Close");
            Assert.That(File.ReadAllText(_settingsPath), Does.Contain("\"battleSpeed\": 1"));
        }

        [UnityTest]
        public IEnumerator StageSettingsCanBeOpenedThroughShopAndResultLayers()
        {
            yield return LoadMenu();
            Assert.That(_controller.TryStartRun(), Is.True);
            Capture("SettingsGearShop.png");
            ClickStageGear();
            Assert.That(_controller.CanEditLoadout, Is.False);
            Assert.That(_controller.AdvanceRound(), Is.False);
            Click("Settings Close");
            Assert.That(_controller.CanEditLoadout, Is.True);

            WinBattle();
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Victory));
            Capture("SettingsGearBattleResult.png");
            ClickStageGear();
            Assert.That(_controller.AdvanceRound(), Is.False);
            Click("Settings Close");
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Victory));

            while (_controller.CurrentRound < _controller.StageRoundCount)
                Assert.That(_controller.AdvanceRound(), Is.True);
            WinBattle();
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Completed));
            Assert.That(Find("Run Result Screen").gameObject.activeInHierarchy, Is.True);
            Capture("SettingsGearRunResult.png");
            ClickStageGear();
            Assert.That(_controller.TryReturnToMainMenu(), Is.False);
            Click("Settings Close");
            Assert.That(_controller.TryReturnToMainMenu(), Is.True);
            Assert.That(Find("Stage Settings Button").gameObject.activeInHierarchy, Is.False);
        }

        private void WinBattle()
        {
            Assert.That(_controller.TryStartBattle(), Is.True);
            var sword = _controller.ActiveCombat.PlayerWeapons.First();
            sword.Stats.AccuracyPercent = 100;
            sword.Stats.AttackDamage = 10000;
            _controller.TickCombat(2);
        }

        private void ClickStageGear()
        {
            Canvas.ForceUpdateCanvases();
            var target = Find("Stage Settings Button");
            var pointer = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, target.position)
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.FirstOrDefault().gameObject, Is.SameAs(target.gameObject),
                $"Phase {_controller.Phase}, pointer {pointer.position}, screen {Screen.width}×{Screen.height}");
            ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(_controller.IsSettingsOpen, Is.True);
        }

        private IEnumerator LoadMenu()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            float deadline = Time.realtimeSinceStartup + 10;
            do
            {
                yield return null;
                _controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            } while ((_controller == null || !_controller.IsMainMenu) && Time.realtimeSinceStartup < deadline);
            Assert.That(_controller != null && _controller.IsMainMenu, Is.True);
            _controller.BattleController.AutoTick = false;
            yield return null;
        }

        private Transform Find(string name) => _controller.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
        private void Click(string name) => Find(name).GetComponent<Button>().onClick.Invoke();
        private string Text(string name) => Find(name).GetComponent<Text>().text;
        private void Capture(string name) => PrototypeScreenCapture.CaptureIfRequested(_controller.GetComponentInChildren<Canvas>(), name);
    }
}
