using System;
using System.Collections;
using System.IO;
using System.Linq;
using BackpackAutoBattle.Prototype;
using BackpackAutoBattle.Progression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class PrototypeTutorialTests
    {
        private string _oldPath, _directory, _path;
        private BackpackPrototypeController _controller;

        [SetUp]
        public void SetUp()
        {
            _oldPath = Environment.GetEnvironmentVariable("BACKPACK_PROFILE_PATH");
            _directory = Path.Combine(Path.GetTempPath(), "BackpackTutorialTest_" + Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_directory, "test-progress.json");
            Environment.SetEnvironmentVariable("BACKPACK_PROFILE_PATH", _path);
        }

        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable("BACKPACK_PROFILE_PATH", _oldPath);
            foreach (string suffix in new[] { "", ".bak", ".tmp" }) File.Delete(_path + suffix);
            if (Directory.Exists(_directory)) Directory.Delete(_directory);
        }

        [UnityTest]
        public IEnumerator NewProfile_LearnsFusionAndSavesItsNameAfterTwoBattles()
        {
            yield return LoadApplication();
            Assert.That(_controller.CurrentStageId, Is.EqualTo("tutorial"));
            Assert.That(_controller.StageRoundCount, Is.EqualTo(2));
            Assert.That(_controller.CurrentShopItems.Select(item => item.ItemId),
                Is.EqualTo(new[] { "wooden_sword", "wooden_shield", "whetstone", "healing_potion" }));
            Assert.That(_controller.TryRerollShop(), Is.False);
            Assert.That(_controller.AdvanceRound(), Is.False);
            Assert.That(_controller.TryStartBattle(), Is.False);
            Capture("TutorialLockShop.png");
            var potion = Shop("healing_potion");
            potion.OnPointerClick(Pointer(potion.transform, PointerEventData.InputButton.Right));
            Assert.That(potion.Offer.IsLocked, Is.True);
            potion.OnPointerClick(Pointer(potion.transform, PointerEventData.InputButton.Right));
            Assert.That(potion.Offer.IsLocked, Is.False);
            int startGold = _controller.Gold;
            Buy("wooden_sword", "Grid Cell 0,0");
            Assert.That(_controller.Gold, Is.EqualTo(startGold));
            Buy("wooden_sword", "Grid Cell 1,1");
            Buy("wooden_shield", "Grid Cell 3,1");
            Assert.That(_controller.CaptureCombatSnapshot().Items.Count, Is.EqualTo(1));
            Capture("TutorialRotateShield.png");
            yield return null;
            Buy("wooden_shield", "Grid Cell 3,1", rotate: true);
            Buy("whetstone", "Grid Cell 2,2");
            Assert.That(_controller.GetReadyFusions().Single().Result.ItemId, Is.EqualTo("steel_sword"));

            BackpackItemView sword = _controller.GetComponentsInChildren<BackpackItemView>().Single(view => view.Data.ItemId == "wooden_sword");
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftShift));
                InputSystem.Update();
                var rightClick = Pointer(sword.transform, PointerEventData.InputButton.Right);
                sword.OnPointerClick(rightClick);
                Assert.That(_controller.IsFusionLocked(sword.InstanceId), Is.True);
                Assert.That(_controller.GetReadyFusions(), Is.Empty);
                sword.OnPointerClick(rightClick);
                Assert.That(_controller.IsFusionLocked(sword.InstanceId), Is.False);
            }
            finally { InputSystem.RemoveDevice(keyboard); }

            Assert.That(_controller.TryStartBattle(), Is.True);
            _controller.TickCombat(30);
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Victory));
            _controller.HandleCombatAction();
            Assert.That(_controller.IsFusionPresenting, Is.True);
            _controller.TickFusionPresentation(10);
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Preparation));
            Assert.That(_controller.TryStartBattle(), Is.False);
            var steel = _controller.GetComponentsInChildren<PrototypeStorageItemView>().Single();
            Assert.That(steel.StoredItem.ItemData.ItemId, Is.EqualTo("steel_sword"));
            Capture("TutorialEquipSteelSword.png");
            var drag = Pointer(steel.transform);
            steel.OnBeginDrag(drag);
            drag.position = Pointer(Find("Grid Cell 1,1")).position;
            steel.OnDrag(drag);
            steel.OnEndDrag(drag);
            Assert.That(_controller.TryStartBattle(), Is.True);
            _controller.TickCombat(30);
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Completed));
            Assert.That(new PlayerProgressStore(_path).Load(out _).Onboarding, Is.EqualTo(OnboardingState.NameRequired));
            _controller.HandleCombatAction();
            Assert.That(Find("Tutorial Name Popup").gameObject.activeInHierarchy, Is.True);

            // Leaving the application at this point resumes name entry, without replaying either battle.
            yield return LoadApplication();
            Assert.That(Find("Tutorial Name Popup").gameObject.activeInHierarchy, Is.True);
            var field = Find("Tutorial Name Input").GetComponent<InputField>();
            var confirm = Find("Tutorial Name Confirm").GetComponent<Button>();
            field.text = "잘못된 이름";
            confirm.onClick.Invoke();
            Assert.That(_controller.IsMainMenu, Is.False);
            Assert.That(Find("Tutorial Name Error").GetComponent<Text>().text, Is.Not.Empty);
            field.text = "";
            Assert.That(field.isFocused, Is.True);
            var textKeyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                textKeyboard.OnIMECompositionChanged(new IMECompositionString("모"));
                Assert.That(EventSystem.current.currentInputModule.input.compositionString, Is.EqualTo("모"));
                Assert.That(field.text, Is.Empty);
                Assert.That(field.textComponent.text, Does.Contain("모"));
                foreach (char c in "모험가42") InputSystem.QueueTextEvent(textKeyboard, c);
                InputSystem.Update();
                Assert.That(field.text, Is.EqualTo("모험가42"));
            }
            finally { InputSystem.RemoveDevice(textKeyboard); }
            Capture("TutorialUserName.png");
            confirm.onClick.Invoke();
            Assert.That(_controller.IsMainMenu, Is.True);
            Assert.That(Find("Main Profile Name").GetComponent<Text>().text, Is.EqualTo("모험가42"));
            Assert.That(_controller.Progress.SharedExperience, Is.Zero);
            Assert.That(_controller.Progress.OriginShards, Is.Zero);
            Assert.That(_controller.Progress.GetStage("stage_01").HighestRound, Is.Zero);
            Capture("MainSavedUserName.png");
            yield return LoadApplication();
            Assert.That(_controller.IsMainMenu, Is.True);
            Assert.That(_controller.Progress.UserName, Is.EqualTo("모험가42"));
            Assert.That(_controller.TryStartRun(), Is.True);
            Assert.That(_controller.CurrentStageId, Is.EqualTo("stage_01"));
            Assert.That(_controller.StageRoundCount, Is.EqualTo(12));
        }

        private IEnumerator LoadApplication()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            float deadline = Time.realtimeSinceStartup + 10;
            do { yield return null; _controller = Object.FindFirstObjectByType<BackpackPrototypeController>(); }
            while (_controller == null && Time.realtimeSinceStartup < deadline);
            Assert.That(_controller, Is.Not.Null);
            _controller.BattleController.AutoTick = false;
            _controller.AutoTickFusionPresentation = false;
            yield return null;
        }

        private void Buy(string id, string cell, bool rotate = false)
        {
            var slot = Shop(id);
            var drag = Pointer(slot.transform);
            slot.OnBeginDrag(drag);
            if (rotate)
                _controller.RotateItem(_controller.GetComponentsInChildren<BackpackItemView>().Single(view => view.Data.ItemId == id));
            drag.position = Pointer(Find(cell)).position;
            slot.OnDrag(drag);
            slot.OnEndDrag(drag);
        }

        private Transform Find(string name)
            => _controller.GetComponentsInChildren<Transform>(true).Single(child => child.name == name);
        private PrototypeShopSlotView Shop(string id)
            => _controller.GetComponentsInChildren<PrototypeShopSlotView>().Single(slot => slot.ItemData?.ItemId == id);
        private static PointerEventData Pointer(Transform transform, PointerEventData.InputButton button = PointerEventData.InputButton.Left)
            => new(EventSystem.current)
            {
                button = button,
                position = RectTransformUtility.WorldToScreenPoint(null,
                    transform.TransformPoint(((RectTransform)transform).rect.center))
            };
        private void Capture(string name) => PrototypeScreenCapture.CaptureIfRequested(_controller.GetComponentInChildren<Canvas>(), name);
    }
}
