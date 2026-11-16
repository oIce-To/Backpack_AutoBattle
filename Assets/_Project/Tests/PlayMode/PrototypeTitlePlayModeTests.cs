using System;
using System.Collections;
using System.IO;
using System.Linq;
using BackpackAutoBattle.Prototype;
using BackpackAutoBattle.Progression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class PrototypeTitlePlayModeTests
    {
        private string _oldProfile, _oldSettings, _folder, _profile, _settings;
        private BackpackPrototypeController _controller;
        [SetUp]
        public void Isolate()
        {
            _oldProfile = Environment.GetEnvironmentVariable("BACKPACK_PROFILE_PATH");
            _oldSettings = Environment.GetEnvironmentVariable("BACKPACK_SETTINGS_PATH");
            _folder = Path.Combine(Path.GetTempPath(), "BackpackTitle_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            _profile = Path.Combine(_folder, "profile.json");
            _settings = Path.Combine(_folder, "settings.json");
            Environment.SetEnvironmentVariable("BACKPACK_PROFILE_PATH", _profile);
            Environment.SetEnvironmentVariable("BACKPACK_SETTINGS_PATH", _settings);
        }
        [TearDown]
        public void Restore()
        {
            if (_controller != null) _controller.CloseSettings();
            Environment.SetEnvironmentVariable("BACKPACK_PROFILE_PATH", _oldProfile);
            Environment.SetEnvironmentVariable("BACKPACK_SETTINGS_PATH", _oldSettings);
            foreach (var file in Directory.GetFiles(_folder)) File.Delete(file);
            Directory.Delete(_folder);
        }
        [UnityTest]
        public IEnumerator TitleWaitsForStart_UsesSavedVolume_AndSharesSettingsWithMainMenu()
        {
            PrototypeCombatFixture.PrepareLobbyProfile();
            File.WriteAllText(_settings, "{\"master\":50,\"music\":40,\"effects\":60}");
            yield return LoadTitle();
            var music = Find("Background Music").GetComponent<AudioSource>();
            Assert.That(music.clip.name, Is.EqualTo("TitleScene_BGM"));
            Assert.That(music.volume, Is.EqualTo(.2f).Within(.001f));
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Title));
            Assert.That(_controller.TryStartBattle(), Is.False);
            Assert.That(_controller.TryStartRun(), Is.False);
            Assert.That(Find("Shop Screen").gameObject.activeInHierarchy, Is.False);
            Assert.That(_controller.BattleController.ScreenRoot.gameObject.activeInHierarchy, Is.False);
            Capture("TitleScreen.png");
            int clicks = 0;
            _controller.SoundPlayed += sound => { if (sound == PrototypeSound.ButtonClick) clicks++; };
            Click("Title Settings Button");
            Assert.That(clicks, Is.EqualTo(1));
            Assert.That(_controller.IsSettingsOpen, Is.True);
            Assert.That(_controller.TryStartFromTitle(), Is.False);
            Assert.That(_controller.IsMusicPaused, Is.False);
            Find("Settings Volume 1").GetComponent<Slider>().value = 20;
            Assert.That(music.volume, Is.EqualTo(.1f).Within(.001f));
            Capture("TitleSettings.png");
            Click("Settings Close");
            Assert.That(File.ReadAllText(_settings), Does.Contain("\"music\": 20"));
            Click("Title Start Button");
            Assert.That(_controller.TryStartFromTitle(), Is.False, "Repeated start must be rejected during the fade.");
            yield return new WaitForSecondsRealtime(.9f);
            Assert.That(_controller.IsMainMenu, Is.True);
            Assert.That(_controller.IsTitleScreen, Is.False);
            Assert.That(music.clip.name, Is.EqualTo("MainScene_BGM"));
            Assert.That(music.volume, Is.EqualTo(.1f).Within(.001f));
            Assert.That(Find("Title Screen").gameObject.activeSelf, Is.False);
        }
        [UnityTest]
        public IEnumerator FirstLaunchDoesNotStartTutorialUntilTitleStart_ThenUsesExistingOnboarding()
        {
            yield return LoadTitle();
            Assert.That(File.Exists(_profile), Is.False, "Waiting at the title must not create or advance player progress.");
            Assert.That(_controller.CurrentStageId, Is.Not.EqualTo("tutorial"));
            Assert.That(_controller.TryStartFromTitle(), Is.True);
            yield return new WaitForSecondsRealtime(.9f);
            Assert.That(_controller.CurrentStageId, Is.EqualTo("tutorial"));
            Assert.That(_controller.IsMainMenu, Is.False);
            Assert.That(_controller.Progress.Onboarding, Is.EqualTo(OnboardingState.TutorialRequired));
            Assert.That(Find("Background Music").GetComponent<AudioSource>().clip.name, Is.EqualTo("ShopPhase_BGM"));
        }
        private IEnumerator LoadTitle()
        {
            yield return SceneManager.LoadSceneAsync("Title", LoadSceneMode.Single);
            float deadline = Time.realtimeSinceStartup + 15;
            do { yield return null; _controller = Object.FindFirstObjectByType<BackpackPrototypeController>(); }
            while ((_controller == null || !_controller.IsTitleScreen) && Time.realtimeSinceStartup < deadline);
            Assert.That(_controller != null && _controller.IsTitleScreen, Is.True);
            _controller.BattleController.AutoTick = false;
            yield return null;
        }
        private Transform Find(string name) => _controller.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
        private void Click(string name)
        {
            var target = Find(name);
            ExecuteEvents.Execute(target.gameObject, new PointerEventData(EventSystem.current)
                { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
        }
        private void Capture(string name) => PrototypeScreenCapture.CaptureIfRequested(_controller.GetComponentInChildren<Canvas>(), name);
    }
}
