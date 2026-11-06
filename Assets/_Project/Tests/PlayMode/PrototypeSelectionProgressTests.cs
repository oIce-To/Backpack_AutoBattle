using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Progression;
using BackpackAutoBattle.Prototype;
using NUnit.Framework;
using TMPro;
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
    public sealed class PrototypeSelectionProgressTests
    {
        private BackpackPrototypeController _controller;
        private string _previousPath, _directory, _savePath;
        [SetUp] public void SetupSave()
        {
            _previousPath = Environment.GetEnvironmentVariable("BACKPACK_PROFILE_PATH");
            _directory = Path.Combine(Path.GetTempPath(), "BackpackSelectionTest_" + Guid.NewGuid().ToString("N"));
            _savePath = Path.Combine(_directory, "profile.json");
            Environment.SetEnvironmentVariable("BACKPACK_PROFILE_PATH", _savePath);
        }
        [TearDown] public void CleanupSave()
        {
            Environment.SetEnvironmentVariable("BACKPACK_PROFILE_PATH", _previousPath);
            foreach (string suffix in new[] { "", ".bak", ".tmp" }) if (File.Exists(_savePath + suffix)) File.Delete(_savePath + suffix);
            if (Directory.Exists(_directory)) Directory.Delete(_directory);
        }
        private IEnumerator LoadMenu()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            float deadline = Time.realtimeSinceStartup + 10;
            do { yield return null; _controller = Object.FindFirstObjectByType<BackpackPrototypeController>(); }
            while ((_controller == null || !_controller.IsMainMenu) && Time.realtimeSinceStartup < deadline);
            Assert.That(_controller, Is.Not.Null); Assert.That(_controller.IsMainMenu, Is.True);
            _controller.BattleController.AutoTick = false; yield return null;
        }

        [UnityTest]
        public IEnumerator CharacterAndStagePreview_CanCancelOrConfirm_AndSelectionsDriveActualBattle()
        {
            SeedClearedStages(4);
            yield return LoadMenu();
            Assert.That(_controller.AvailableCharacters.Select(c => c.CharacterId), Is.EquivalentTo(new[] { "allen", "serena" }));
            Assert.That(_controller.AvailableStages.Count, Is.EqualTo(5));
            Click("Main Character Select Button"); yield return null;
            Carousel().Move(1); SettleCarousel();
            Assert.That(Text("Character Preview Name"), Is.EqualTo("세레나"));
            Assert.That(Find("Character Preview Illustration").GetComponent<Image>().sprite,
                Is.SameAs(_controller.AvailableCharacters.Single(c => c.CharacterId == "serena").Portrait));
            Capture("CharacterSelectionSerena.png");
            Click("Character Selection Back Button"); Assert.That(_controller.SelectedCharacter.CharacterId, Is.EqualTo("allen"));
            Click("Main Character Select Button"); yield return null; Carousel().Move(1); SettleCarousel();
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(Pointer(Find("Character Confirm Button")), hits);
            Assert.That(hits.Any(h => h.gameObject == Find("Character Confirm Button").gameObject), Is.True);
            Click("Character Confirm Button");
            Assert.That(Text("Main Character Name"), Is.EqualTo("세레나"));
            Click("Main Stage Select Button"); yield return null;
            Click("Stage Choice stage_05");
            Assert.That(Text("Stage Preview Name"), Does.Contain("5스테이지"));
            Assert.That(Text("Stage Preview Details"), Does.Contain("12라운드").And.Contain("주요 적").And.Contain("보스"));
            Capture("StageSelectionFive.png");
            Click("Stage Selection Back Button"); Assert.That(_controller.CurrentStageId, Is.EqualTo("stage_01"));
            _controller.ShowStageSelection(); yield return null; Click("Stage Choice stage_05"); Click("Stage Confirm Button");
            Assert.That(_controller.CurrentStageId, Is.EqualTo("stage_05"));
            Capture("MainMenuSerena.png");
            Click("Main Start Button");
            Assert.That(_controller.CurrentStageId, Is.EqualTo("stage_05"));
            Assert.That(_controller.PreparedEnemy.MaximumHealth, Is.EqualTo(78));
            Assert.That(_controller.StageEntryStats.MaximumHealth, Is.EqualTo(50)); Assert.That(_controller.StageEntryStats.Attack, Is.EqualTo(10.5));
            var bow = _controller.CaptureCombatSnapshot().Items.Single();
            Assert.That(bow.ItemId, Is.EqualTo("silver_longbow"));
            Assert.That(bow.InventorySprite, Is.Not.Null);
            var bagView = _controller.GetComponentsInChildren<BackpackItemView>().Single();
            Assert.That(bagView.transform.Find("Item Artwork").GetComponent<Image>().sprite, Is.SameAs(bow.InventorySprite));
            Assert.That(bagView.transform.Find("Item Artwork").localScale.x, Is.GreaterThan(1f));
            Assert.That(bagView.transform.Find("Label"), Is.Null);
            Assert.That(_controller.TrySelectCharacter("allen"), Is.False); Assert.That(_controller.TrySelectStage("stage_01"), Is.False);
            _controller.TryStartBattle();
            Assert.That(_controller.ActiveCombat.Player.DisplayName, Is.EqualTo("세레나"));
            Assert.That(_controller.ActiveCombat.EnemyMaxHealth, Is.EqualTo(78));
            var portrait = Find("Player Character Illustration").GetComponent<Image>();
            Assert.That(portrait.sprite, Is.SameAs(_controller.SelectedCharacter.Portrait));
            Assert.That(portrait.rectTransform.localScale.x, Is.EqualTo(1));
            Assert.That(_controller.Stage.GetRound(12).Encounter.Enemy.Illustration,
                Is.SameAs(_controller.ActiveCombat.Enemy.Illustration));
            var battleItem = _controller.GetComponentsInChildren<PrototypeBattleItemView>().Single(item => item.Snapshot.ItemId == bow.ItemId);
            Assert.That(battleItem.transform.Find("Label"), Is.Null);
            var fill = battleItem.transform.Find("Artwork Cycle Color").GetComponent<Image>();
            Assert.That(fill.sprite, Is.SameAs(bow.InventorySprite));
            Assert.That(fill.transform.localScale.x, Is.EqualTo(bagView.transform.Find("Item Artwork").localScale.x));
            _controller.TickCombat(1.2f);
            Assert.That(fill.fillAmount, Is.EqualTo(.5f).Within(.01f));
            Capture("BattleSerenaPortrait.png");
        }

        [UnityTest]
        public IEnumerator EquipmentTab_TransfersSharedGearAndPersists_WithoutRegrantingOrRequiringAWeapon()
        {
            yield return LoadMenu();
            var sword = _controller.Progress.GetEquippedGear("allen", UniqueGearSlot.Weapon);
            var bow = _controller.Progress.GetEquippedGear("serena", UniqueGearSlot.Weapon);
            Assert.That(_controller.Progress.UniqueGear, Has.Count.EqualTo(2));
            _controller.ShowCharacterSelection();
            Assert.That(Find("Character Gear Weapon").Find("Gear Badge").gameObject.activeInHierarchy, Is.True);
            Assert.That(Find("Character Gear Weapon").Find("Gear Badge/Gear Sprite").GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(Find("Character Gear Armor").Find("Gear Badge"), Is.Null);
            Click("Main Tab Equipment"); yield return null;
            Assert.That(Text("Equipped Character Stats"), Does.Contain("52.5").And.Contain("10.5"));
            Capture("EquipmentAllen.png");
            var swordTile = (RectTransform)Find("Inventory Gear " + sword.InstanceId);
            Assert.That(swordTile.rect.width, Is.EqualTo(swordTile.rect.height));
            var badge = swordTile.Find("Gear Badge");
            Assert.That(badge.Find("Gear Rank").GetComponent<Text>().text, Is.EqualTo("일반"));
            Assert.That(badge.Find("Gear Enhancement").GetComponent<Text>().text, Is.EqualTo("+0"));
            Assert.That(badge.Find("Gear Equipped").gameObject.activeInHierarchy, Is.True);
            Assert.That(badge.Find("Gear Equipped").GetComponent<Text>().fontSize, Is.EqualTo(28));
            var bowIcon = (RectTransform)Find("Inventory Gear " + bow.InstanceId).Find("Gear Badge/Gear Sprite");
            Assert.That(bowIcon.offsetMin, Is.EqualTo(new Vector2(7, 7)));
            Assert.That(bowIcon.offsetMax, Is.EqualTo(new Vector2(-7, -7)));
            Assert.That(Find("Equipment Details Modal").gameObject.activeSelf, Is.False);
            Click("Equipment Slot Weapon"); yield return null;
            var tooltip = Find("Equipment Item Tooltip");
            Assert.That(tooltip.Find("Tooltip Name").GetComponent<Text>().text, Is.EqualTo("기사의 보검"));
            Assert.That(tooltip.GetComponentInChildren<TextMeshProUGUI>().text, Does.Contain("5%"));
            Assert.That(Find("Equip Unique Gear").gameObject.activeSelf, Is.False);
            Assert.That(Find("Unequip Unique Gear").GetComponent<Button>().interactable, Is.True);
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(Pointer(tooltip), hits);
            Assert.That(hits.First().gameObject, Is.EqualTo(tooltip.gameObject));
            hits.Clear();
            EventSystem.current.RaycastAll(Pointer(Find("Main Tab Home")), hits);
            Assert.That(hits.First().gameObject.name, Is.EqualTo("Equipment Details Backdrop"));
            Capture("EquipmentDetailsAllen.png");
            Click("Equipment Details Backdrop");
            Assert.That(Find("Equipment Details Modal").gameObject.activeSelf, Is.False);
            Assert.That(_controller.TrySelectCharacter("serena"), Is.True);
            Click("Main Tab Equipment"); yield return null;
            Click("Equipment Slot Weapon");
            Assert.That(tooltip.Find("Tooltip Name").GetComponent<Text>().text, Is.EqualTo("은빛 장궁"));
            Assert.That(tooltip.GetComponentInChildren<TextMeshProUGUI>().text, Does.Contain("0.5%"));
            Click("Equipment Details Backdrop");
            Click("Inventory Gear " + sword.InstanceId); yield return null;
            Assert.That(Find("Equip Unique Gear").GetComponent<Button>().interactable, Is.True);
            Assert.That(Find("Unequip Unique Gear").gameObject.activeSelf, Is.False);
            Click("Equip Unique Gear"); yield return null;
            Assert.That(Text("Equipment Transfer Message"), Does.Contain("알렌이 장착"));
            Capture("EquipmentTransferConfirmation.png");
            Click("Cancel Equipment Transfer");
            Assert.That(Find("Equipment Transfer Modal").gameObject.activeSelf, Is.False);
            Assert.That(Find("Equipment Details Modal").gameObject.activeSelf, Is.True);
            Assert.That(_controller.Progress.GetEquippedGear("allen", UniqueGearSlot.Weapon).InstanceId, Is.EqualTo(sword.InstanceId));
            Assert.That(_controller.Progress.GetEquippedGear("serena", UniqueGearSlot.Weapon).InstanceId, Is.EqualTo(bow.InstanceId));
            Click("Equip Unique Gear");
            Click("Confirm Equipment Transfer"); yield return null;
            Assert.That(_controller.Progress.GetEquippedGear("allen", UniqueGearSlot.Weapon), Is.Null);
            Assert.That(_controller.Progress.GetEquippedGear("serena", UniqueGearSlot.Weapon).InstanceId, Is.EqualTo(sword.InstanceId));
            Assert.That(_controller.Progress.UniqueGear.Single(item => item.InstanceId == bow.InstanceId).EquippedBy, Is.Empty);

            Click("Inventory Gear " + bow.InstanceId);
            Click("Equip Unique Gear"); yield return null;
            Assert.That(Find("Equipment Transfer Modal").gameObject.activeSelf, Is.False);
            Assert.That(_controller.Progress.GetEquippedGear("serena", UniqueGearSlot.Weapon).InstanceId, Is.EqualTo(bow.InstanceId));
            Click("Inventory Gear " + sword.InstanceId);
            Click("Equip Unique Gear"); yield return null;
            yield return LoadMenu();
            Assert.That(_controller.Progress.UniqueGear, Has.Count.EqualTo(2));
            Assert.That(_controller.Progress.GetEquippedGear("allen", UniqueGearSlot.Weapon), Is.Null);
            Assert.That(_controller.Progress.GetEquippedGear("serena", UniqueGearSlot.Weapon).InstanceId, Is.EqualTo(sword.InstanceId));

            Assert.That(_controller.TryStartRun(), Is.True);
            yield return null;
            Assert.That(_controller.CurrentShopItems.All(item => !item.IsUniqueGear), Is.True);
            var view = _controller.GetComponentsInChildren<BackpackItemView>().Single();
            Assert.That(view.Data.ItemId, Is.EqualTo("knight_sword"));
            Assert.That(_controller.TryToggleFusionLock(view.InstanceId), Is.False);
            var validDrop = Pointer(view.transform);
            view.OnBeginDrag(validDrop);
            view.OnDrag(validDrop);
            var gridCell = Find("Grid Cell 0,0").GetComponent<Image>();
            Assert.That(gridCell.color.g, Is.GreaterThan(gridCell.color.r + .2f));
            Assert.That(gridCell.rectTransform.rect.width, Is.GreaterThan(BackpackPrototypeController.ItemCellSize));
            Assert.That(view.transform.Find("Cell 0,0").GetComponent<Image>().color.a, Is.Zero);
            view.OnEndDrag(validDrop);
            _controller.RotateItem(view);
            Assert.That(view.Rotation, Is.EqualTo(ItemRotation.Clockwise90));
            Assert.That(view.transform.Find("Item Artwork").localEulerAngles.z, Is.EqualTo(270f).Within(.01f));
            var pointer = Pointer(view.transform);
            view.OnBeginDrag(pointer);
            pointer.position = Pointer(Find("Prototype Shop")).position;
            view.OnDrag(pointer);
            var invalidColor = view.transform.Find("Cell 0,0").GetComponent<Image>().color;
            Assert.That(invalidColor.r, Is.GreaterThan(invalidColor.g + .2f));
            view.OnEndDrag(pointer);
            Assert.That(_controller.Gold, Is.EqualTo(20));
            Assert.That(_controller.CaptureCombatSnapshot().Items, Has.Count.EqualTo(1));
            pointer = Pointer(view.transform);
            view.OnBeginDrag(pointer);
            pointer.position = Pointer(Find("Freeform Storage")).position;
            view.OnDrag(pointer); view.OnEndDrag(pointer);
            yield return null;
            Assert.That(_controller.StoredItemCount, Is.EqualTo(1));
            var stored = _controller.GetComponentsInChildren<PrototypeStorageItemView>().Single();
            Assert.That(stored.transform.Find("Item Artwork").GetComponent<Image>().sprite, Is.SameAs(view.Data.InventorySprite));
            Assert.That(stored.transform.Find("Label"), Is.Null);
            pointer = Pointer(stored.transform);
            stored.OnBeginDrag(pointer);
            pointer.position = Pointer(Find("Prototype Shop")).position;
            stored.OnDrag(pointer); stored.OnEndDrag(pointer);
            Assert.That(_controller.StoredItemCount, Is.EqualTo(1));
            Assert.That(_controller.Gold, Is.EqualTo(20));
            Assert.That(_controller.TryStartBattle(), Is.True);
            Assert.That(_controller.ActiveCombat.PlayerMaxHealth, Is.EqualTo(52.5));
            Assert.That(_controller.ActiveCombat.Player.Stats.Attack, Is.EqualTo(10.5));
            Assert.That(_controller.ActiveCombat.PlayerItems, Is.Empty);

            yield return LoadMenu();
            Assert.That(_controller.TrySelectCharacter("allen"), Is.True);
            Assert.That(_controller.TryStartRun(), Is.True);
            Assert.That(_controller.StageEntryStats.Attack, Is.EqualTo(10));
            Assert.That(_controller.CaptureCombatSnapshot().Items, Is.Empty);
            Assert.That(_controller.TryStartBattle(), Is.True);
        }

        [UnityTest]
        public IEnumerator AbandonStage_CancelPreservesRun_ConfirmLeavesWithoutPartialRewards()
        {
            yield return LoadMenu();
            Assert.That(_controller.TryStartRun(), Is.True);
            for (int i = 0; i < 4; i++) Assert.That(_controller.AdvanceRound(), Is.True);
            Lose();
            Assert.That(Find("Abandon Stage Button").gameObject.activeInHierarchy, Is.False);
            Assert.That(_controller.ReturnToPreparation(), Is.True);
            int gold = _controller.Gold;
            int ended = 0;
            _controller.RunEnded += _ => ended++;
            Click("Abandon Stage Button");
            Assert.That(Find("Abandon Stage Modal").gameObject.activeInHierarchy, Is.True);
            Assert.That(_controller.TryStartBattle(), Is.False);
            Assert.That(_controller.AdvanceRound(), Is.False);
            Assert.That(_controller.TryRerollShop(), Is.False);
            Assert.That(_controller.TryOpenSettings(), Is.False);
            Capture("AbandonStageConfirmation.png");
            Click("Cancel Abandon Stage");
            Assert.That(_controller.CanEditLoadout, Is.True);
            Assert.That(_controller.CurrentRound, Is.EqualTo(5));
            Assert.That(_controller.Lives, Is.EqualTo(2));
            Assert.That(_controller.Gold, Is.EqualTo(gold));
            Assert.That(_controller.CaptureCombatSnapshot().Items, Has.Count.EqualTo(1));
            Click("Abandon Stage Button");
            Click("Confirm Abandon Stage");
            Assert.That(_controller.IsMainMenu, Is.True);
            Assert.That(_controller.Progress.SharedExperience, Is.Zero);
            Assert.That(_controller.Progress.GetStage("stage_01").HighestRound, Is.EqualTo(5));
            Assert.That(_controller.RunResult, Is.Null);
            Assert.That(_controller.CurrentShopItems, Is.Empty);
            Assert.That(ended, Is.Zero);
            Assert.That(_controller.TryStartRun(), Is.True);
            Assert.That(_controller.CurrentRound, Is.EqualTo(1));
            Assert.That(_controller.Lives, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator SelectionsAndHighestRound_SurviveReloadAfterGameOver()
        {
            SeedClearedStages(4);
            yield return LoadMenu(); _controller.TrySelectCharacter("serena"); _controller.TrySelectStage("stage_05");
            _controller.TryStartRun(); _controller.AdvanceRound(); _controller.AdvanceRound();
            Assert.That(_controller.Progress.GetStage("stage_05").HighestRound, Is.EqualTo(3));
            for (int i = 0; i < 3; i++) { Lose(); if (i < 2) _controller.ReturnToPreparation(); }
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.GameOver));
            _controller.TryReturnToMainMenu();
            Assert.That(File.Exists(_savePath), Is.True);
            yield return LoadMenu();
            Assert.That(_controller.SelectedCharacter.CharacterId, Is.EqualTo("serena"));
            Assert.That(_controller.CurrentStageId, Is.EqualTo("stage_05"));
            Assert.That(_controller.Progress.GetStage("stage_05").HighestRound, Is.EqualTo(3));
            Assert.That(_controller.Progress.GetStage("stage_05").Cleared, Is.False);
            Assert.That(Text("Main Stage Summary"), Does.Contain("최고 3라운드"));
            Assert.That(_controller.CurrentShopItems, Is.Empty);
            _controller.TryStartRun();
            Assert.That(_controller.CurrentRound, Is.EqualTo(1)); Assert.That(_controller.Gold, Is.EqualTo(20)); Assert.That(_controller.Lives, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator ClearRecord_IsStageSpecificPersistentAndNeverLostOnAWeakerRetry()
        {
            yield return LoadMenu(); _controller.TrySelectCharacter("serena"); _controller.TryStartRun();
            for (int i = 0; i < 11; i++) _controller.AdvanceRound();
            _controller.TryStartBattle();
            var sword = _controller.ActiveCombat.PlayerWeapons.First(); sword.Stats.AttackDamage = 10000; sword.Stats.AccuracyPercent = 100;
            _controller.TickCombat(2.5f); Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Completed));
            Assert.That(_controller.Progress.GetStage("stage_01").Cleared, Is.True);
            _controller.TickCombat(200); _controller.TryReturnToMainMenu();
            _controller.ShowStageSelection(); yield return null;
            Assert.That(Text("Stage Preview Progress"), Does.Contain("클리어").And.Contain("12 / 12"));
            Capture("StageSelectionCleared.png");
            Assert.That(Find("Stage Choice stage_02").GetComponent<Button>().interactable, Is.True);
            Assert.That(Find("Stage Choice stage_03").GetComponent<Button>().interactable, Is.False);
            Assert.That(_controller.IsStageUnlocked("stage_02"), Is.True);
            _controller.CloseSelectionScreens();
            Assert.That(_controller.TrySelectStage("stage_05"), Is.False); Assert.That(_controller.Progress.GetStage("stage_05").Cleared, Is.False);
            _controller.TrySelectStage("stage_01"); _controller.TryStartRun();
            for (int i = 0; i < 3; i++) { Lose(); if (i < 2) _controller.ReturnToPreparation(); }
            _controller.TryReturnToMainMenu(); yield return LoadMenu();
            Assert.That(_controller.Progress.GetStage("stage_01").Cleared, Is.True);
            Assert.That(_controller.Progress.GetStage("stage_01").HighestRound, Is.EqualTo(12));
            Assert.That(_controller.Progress.GetStage("stage_05").HighestRound, Is.Zero);
            Assert.That(_controller.IsStageUnlocked("stage_02"), Is.True);
            Assert.That(_controller.TrySelectStage("stage_02"), Is.True);
            Assert.That(_controller.TrySelectStage("stage_03"), Is.False);
        }

        [UnityTest]
        public IEnumerator RemovedContentSelections_FallBackToAvailableDefaultsWithoutDeletingOldRecords()
        {
            var progress = new PlayerProgress(); progress.SelectCharacter("removed_character"); progress.SelectStage("stage_08_example");
            progress.RecordRound("stage_08_example", 8, 12);
            Assert.That(new PlayerProgressStore(_savePath).TrySave(progress, out _), Is.True);
            yield return LoadMenu();
            Assert.That(_controller.SelectedCharacter.CharacterId, Is.EqualTo("allen")); Assert.That(_controller.CurrentStageId, Is.EqualTo("stage_01"));
            Assert.That(_controller.Progress.GetStage("stage_08_example").HighestRound, Is.EqualTo(8));
            Assert.That(_controller.TrySelectCharacter("missing"), Is.False); Assert.That(_controller.TrySelectStage("missing"), Is.False);
            _controller.TryStartRun();
            var loaded = new PlayerProgressStore(_savePath).Load(out _);
            Assert.That(loaded.SelectedCharacterId, Is.EqualTo("allen")); Assert.That(loaded.GetStage("stage_08_example").HighestRound, Is.EqualTo(8));
        }

        private void Lose()
        {
            Assert.That(_controller.TryStartBattle(), Is.True);
            foreach (var weapon in _controller.ActiveCombat.PlayerWeapons) weapon.Stats.AttackDamage = 0;
            var enemy = _controller.ActiveCombat.EnemyWeapons.First(); enemy.Stats.AttackDamage = 10000; enemy.Stats.AccuracyPercent = 100;
            _controller.TickCombat(2);
        }
        [UnityTest]
        public IEnumerator MainTabs_AreExclusive_CancelUnconfirmedPreview_AndHideDuringRun()
        {
            yield return LoadMenu();
            Assert.That(((RectTransform)Find("Main Tab Characters")).anchoredPosition.x, Is.LessThan(0));
            Assert.That(((RectTransform)Find("Main Tab Home")).anchoredPosition.x, Is.Zero);
            Assert.That(((RectTransform)Find("Main Tab Stages")).anchoredPosition.x, Is.GreaterThan(0));
            Assert.That(((RectTransform)Find("Main Tabs")).anchoredPosition.y, Is.LessThan(-300));
            var wallet = Find("Main Experience Wallet");
            Assert.That(((RectTransform)wallet).anchoredPosition.y, Is.GreaterThan(300));
            Assert.That(((RectTransform)wallet).anchoredPosition.x, Is.LessThan(0));
            Assert.That(wallet.GetComponent<PrototypeRoundedPanel>().color.a, Is.GreaterThan(0).And.LessThan(1));
            Assert.That(wallet.gameObject.activeInHierarchy, Is.True);
            Assert.That(Find("Main Experience Icon").GetComponent<PrototypeExperienceIcon>(), Is.Not.Null);
            Assert.That(_controller.ActiveMainTab, Is.EqualTo(MainMenuTab.Home));
            Assert.That(Find("Main Navigation").gameObject.activeInHierarchy, Is.True);
            Click("Main Tab Characters"); yield return null;
            Assert.That(_controller.ActiveMainTab, Is.EqualTo(MainMenuTab.Characters));
            Assert.That(Find("Main Menu Screen").gameObject.activeSelf, Is.False);
            Assert.That(Find("Character Selection Screen").gameObject.activeSelf, Is.True);
            Assert.That(wallet.gameObject.activeInHierarchy, Is.True);
            Carousel().Move(1); SettleCarousel();
            Click("Main Tab Stages"); yield return null;
            Assert.That(_controller.ActiveMainTab, Is.EqualTo(MainMenuTab.Stages));
            Assert.That(Find("Character Selection Screen").gameObject.activeSelf, Is.False);
            Assert.That(_controller.SelectedCharacter.CharacterId, Is.EqualTo("allen"));
            Assert.That(Find("Stage Selection Screen").gameObject.activeSelf, Is.True);
            Assert.That(wallet.gameObject.activeInHierarchy, Is.True);
            var fifth = Find("Stage Choice stage_05");
            Assert.That(RectTransformUtility.RectangleContainsScreenPoint((RectTransform)Find("Stage Choices Viewport"), Pointer(fifth).position), Is.True);
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(Pointer(fifth), hits);
            Assert.That(hits.Any(h => h.gameObject == fifth.gameObject), Is.True);
            Capture("MainTabsFiveStages.png");
            Click("Main Tab Home");
            Assert.That(Find("Stage Selection Screen").gameObject.activeSelf, Is.False);
            Assert.That(Find("Main Menu Screen").gameObject.activeSelf, Is.True);
            Click("Main Tab Characters"); Assert.That(Carousel().PreviewedCharacter.CharacterId, Is.EqualTo("allen"));
            Click("Character Selection Back Button");
            _controller.TryStartRun();
            Assert.That(Find("Main Navigation").gameObject.activeSelf, Is.False);
            Assert.That(wallet.gameObject.activeInHierarchy, Is.False);
            Assert.That(_controller.ShowMainMenuTab(MainMenuTab.Characters), Is.False);
        }

        [UnityTest]
        public IEnumerator FreshProgress_LocksStagesAndRejectsClicksAndDirectSelection_WhileLegacyLockedSelectionFallsBack()
        {
            var legacy = new PlayerProgress(); legacy.SelectStage("stage_05"); legacy.RecordRound("stage_05", 6, 12);
            Assert.That(new PlayerProgressStore(_savePath).TrySave(legacy, out _), Is.True);
            yield return LoadMenu();
            Assert.That(_controller.CurrentStageId, Is.EqualTo("stage_01"));
            Assert.That(_controller.Progress.GetStage("stage_05").HighestRound, Is.EqualTo(6));
            _controller.ShowStageSelection(); yield return null;
            Assert.That(Find("Stage Choice stage_01").GetComponent<Button>().interactable, Is.True);
            for (int i = 2; i <= 5; i++)
            {
                string id = $"stage_{i:00}"; var choice = Find("Stage Choice " + id);
                Assert.That(choice.GetComponent<Button>().interactable, Is.False);
                Assert.That(choice.GetComponent<CanvasGroup>().alpha, Is.LessThan(.5f));
                var color = choice.GetComponent<Image>().color;
                Assert.That(color.r, Is.EqualTo(color.g)); Assert.That(color.g, Is.EqualTo(color.b));
                var pointer = Pointer(choice); pointer.button = PointerEventData.InputButton.Left;
                ExecuteEvents.Execute(choice.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                Assert.That(Text("Stage Preview Name"), Does.Contain("1스테이지"));
                Assert.That(_controller.TrySelectStage(id), Is.False);
            }
            Capture("StagesLocked.png");
            Click("Stage Confirm Button");
            Assert.That(_controller.CurrentStageId, Is.EqualTo("stage_01"));
            Assert.That(_controller.TryStartRun(), Is.True);
            Assert.That(_controller.CurrentStageId, Is.EqualTo("stage_01"));
        }

        [UnityTest]
        public IEnumerator Carousel_WheelSideClicksAndKeyboard_AnimateAndUseGreyClippedNeighbours()
        {
            yield return LoadMenu(); Click("Main Tab Characters"); yield return null;
            var carousel = Carousel();
            Assert.That(_controller.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Character Carousel Counter"), Is.False);
            var confirm = (RectTransform)Find("Character Confirm Button");
            var cancel = (RectTransform)Find("Character Selection Back Button");
            var details = (RectTransform)Find("Character Details Panel");
            Assert.That(confirm.sizeDelta, Is.EqualTo(cancel.sizeDelta));
            Assert.That(confirm.anchoredPosition.y, Is.EqualTo(cancel.anchoredPosition.y));
            Assert.That(details.anchoredPosition.y + details.rect.yMin, Is.LessThan(confirm.anchoredPosition.y - confirm.rect.height / 2));
            Color selectionColor = confirm.GetComponent<Image>().color;
            Assert.That(selectionColor.g, Is.GreaterThan(selectionColor.r)); Assert.That(selectionColor.g, Is.GreaterThan(.7f));
            var viewport = (RectTransform)Find("Character Carousel Viewport");
            Assert.That(viewport.GetComponent<RectMask2D>(), Is.Not.Null);
            Image center = Find("Character Preview Illustration").GetComponent<Image>();
            Image previous = Find("Character Carousel Previous").GetComponent<Image>();
            Image next = Find("Character Carousel Next").GetComponent<Image>();
            Assert.That(center.color.a, Is.EqualTo(1)); Assert.That(center.material.GetFloat("_Saturation"), Is.EqualTo(1));
            Assert.That(previous.color.a, Is.LessThan(.3)); Assert.That(previous.material.GetFloat("_Saturation"), Is.Zero);
            Assert.That(next.sprite, Is.SameAs(previous.sprite)); Assert.That(next.sprite, Is.Not.SameAs(center.sprite));
            Capture("CharacterCarouselAllen.png");
            var pointer = Pointer(viewport); pointer.scrollDelta = Vector2.down;
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.scrollHandler);
            Assert.That(carousel.IsAnimating, Is.True); Assert.That(Find("Character Confirm Button").GetComponent<Button>().interactable, Is.False);
            Assert.That(carousel.Position, Is.EqualTo(0));
            carousel.TickAnimation(.025f);
            Assert.That(carousel.Position, Is.GreaterThan(0).And.LessThan(1));
            Assert.That(center.rectTransform.anchoredPosition.x, Is.LessThan(0).And.GreaterThan(-PrototypeCharacterCarousel.Spacing));
            Capture("CharacterCarouselTransition.png");
            SettleCarousel(); Assert.That(carousel.PreviewedCharacter.CharacterId, Is.EqualTo("serena"));
            Assert.That(center.sprite, Is.Not.Null); Assert.That(center.sprite.texture.name, Is.EqualTo("serena-standing-approved"));
            Assert.That(Find("Character Confirm Button").GetComponent<Button>().interactable, Is.True);
            Click("Character Carousel Previous"); SettleCarousel(); Assert.That(carousel.PreviewedCharacter.CharacterId, Is.EqualTo("allen"));
            Click("Character Carousel Next"); SettleCarousel(); Assert.That(carousel.PreviewedCharacter.CharacterId, Is.EqualTo("serena"));
            carousel.Move(-1); carousel.TickAnimation(.02f); float position = carousel.Position;
            carousel.Move(1); Assert.That(carousel.Position, Is.EqualTo(position), "Retargeting must not snap an in-flight portrait.");
            SettleCarousel();
            var keyboard = InputSystem.AddDevice<Keyboard>(); float originalScale = Time.timeScale;
            bool originalRunInBackground = Application.runInBackground;
            var settings = InputSystem.settings;
            var backgroundBehavior = settings.backgroundBehavior;
#if UNITY_EDITOR
            var editorInputBehavior = settings.editorInputBehaviorInPlayMode;
#endif
            try
            {
                // Hidden batch-mode editors route keyboard state to the Editor unless the test explicitly opts into game input.
                Application.runInBackground = true;
                settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
                settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
                Time.timeScale = 0;
                Assert.That(carousel.PreviewedCharacter.CharacterId, Is.EqualTo("serena"));
                InputSystem.Update();
                // Drive the Input System's real state-change monitors; OS keyboard events are discarded by hidden native editors.
                InputState.Change(keyboard, new KeyboardState(Key.LeftArrow), InputUpdateType.Dynamic);
                yield return null;
                Assert.That(carousel.PreviewedCharacter.CharacterId, Is.EqualTo("allen"));
                InputState.Change(keyboard, new KeyboardState(), InputUpdateType.Dynamic); yield return null;
                SettleCarousel();
                InputState.Change(keyboard, new KeyboardState(Key.RightArrow), InputUpdateType.Dynamic); yield return null;
                Assert.That(carousel.PreviewedCharacter.CharacterId, Is.EqualTo("serena"));
                InputState.Change(keyboard, new KeyboardState(), InputUpdateType.Dynamic); yield return null;
                SettleCarousel();
            }
            finally
            {
                Time.timeScale = originalScale; InputSystem.RemoveDevice(keyboard);
                settings.backgroundBehavior = backgroundBehavior;
#if UNITY_EDITOR
                settings.editorInputBehaviorInPlayMode = editorInputBehavior;
#endif
                Application.runInBackground = originalRunInBackground;
            }
            Capture("CharacterCarouselSerena.png");
            Assert.That(_controller.SelectedCharacter.CharacterId, Is.EqualTo("allen"), "Browsing is not a saved selection.");
            Click("Character Confirm Button"); Assert.That(_controller.SelectedCharacter.CharacterId, Is.EqualTo("serena"));
            Assert.That(_controller.ActiveMainTab, Is.EqualTo(MainMenuTab.Home));
        }

        private PrototypeCharacterCarousel Carousel() => _controller.GetComponentInChildren<PrototypeCharacterCarousel>(true);

        [UnityTest]
        public IEnumerator ExperienceRewards_PayOnlyAtTerminalResult_ExactlyOnce_EvenForPreviouslyClearedLowStages()
        {
            SeedClearedStages(4); yield return LoadMenu();
            Assert.That(_controller.TrySelectStage("stage_01"), Is.True); _controller.TryStartRun();
            for (int i = 0; i < 2; i++)
            {
                Lose(); Assert.That(_controller.Progress.SharedExperience, Is.Zero);
                Assert.That(_controller.ReturnToPreparation(), Is.True);
            }
            Lose(); Assert.That(_controller.Progress.SharedExperience, Is.EqualTo(10));
            Assert.That(_controller.RunResult.ExperienceReward, Is.EqualTo(10));
            Assert.That(Text("Run Result Experience"), Does.Contain("+10"));
            _controller.TickCombat(200); Assert.That(_controller.Progress.SharedExperience, Is.EqualTo(10));
            Capture("ExperienceGameOver.png");
            _controller.TryReturnToMainMenu(); _controller.TryStartRun();
            for (int i = 0; i < 11; i++) _controller.AdvanceRound();
            Win(); Assert.That(_controller.Progress.SharedExperience, Is.EqualTo(110));
            Assert.That(_controller.RunResult.ExperienceReward, Is.EqualTo(100));
            _controller.TickCombat(200); Assert.That(_controller.Progress.SharedExperience, Is.EqualTo(110));
            Capture("ExperienceClear.png");
            _controller.TryReturnToMainMenu(); yield return LoadMenu();
            Assert.That(_controller.Progress.SharedExperience, Is.EqualTo(110));
            Assert.That(_controller.Progress.GetStage("stage_04").Cleared, Is.True);
        }

        [UnityTest]
        public IEnumerator ExperienceGameOver_UsesCurrentRoundRatherThanHistoricalHighestRound()
        {
            foreach (var scenario in new[] { (round: 4, expected: 10L), (round: 5, expected: 25L), (round: 8, expected: 25L),
                (round: 9, expected: 50L), (round: 12, expected: 50L) })
            {
                var progress = new PlayerProgress(); progress.RecordRound("stage_01", 12, 12, true);
                Assert.That(new PlayerProgressStore(_savePath).TrySave(progress, out _), Is.True);
                yield return LoadMenu(); _controller.TryStartRun();
                for (int i = 1; i < scenario.round; i++) _controller.AdvanceRound();
                for (int i = 0; i < 3; i++) { Lose(); if (i < 2) _controller.ReturnToPreparation(); }
                Assert.That(_controller.Progress.SharedExperience, Is.EqualTo(scenario.expected), $"Round {scenario.round}");
                Assert.That(new PlayerProgressStore(_savePath).Load(out _).SharedExperience, Is.EqualTo(scenario.expected));
            }
        }

        [UnityTest]
        public IEnumerator LevelUp_InvestsSharedExperienceInPreviewedCharacter_PersistsAndDrivesStageItemStats()
        {
            var initial = new PlayerProgress(); initial.TryAddExperience(1000); initial.RecordRound("stage_01", 4, 12);
            Assert.That(new PlayerProgressStore(_savePath).TrySave(initial, out _), Is.True);
            yield return LoadMenu(); _controller.ShowCharacterSelection(); yield return null;
            Carousel().Move(1); SettleCarousel();
            Click("Character Level Up Button");
            Assert.That(_controller.Progress.GetCharacterLevel("serena"), Is.EqualTo(2));
            Assert.That(_controller.Progress.GetCharacterLevel("allen"), Is.EqualTo(1));
            Assert.That(_controller.SelectedCharacter.CharacterId, Is.EqualTo("allen"), "Investing in a preview must not silently select it.");
            Assert.That(_controller.Progress.SharedExperience, Is.EqualTo(900));
            Assert.That(Text("Main Experience Amount"), Is.EqualTo("900"));
            Assert.That(Text("Character Preview Level"), Is.EqualTo("Lv. 2"));
            Assert.That(Text("Character Preview Stats"), Does.Contain("55").And.Contain("11"));
            Assert.That(_controller.GetComponentsInChildren<Transform>(true).Any(t => t.name is "Character Next Level Stats"
                or "Character Shared Experience" or "Main Shared Experience"), Is.False);
            Assert.That(Text("Character Level Up Button Label"), Is.EqualTo("레벨 업"));
            Assert.That(Text("Character Level Up Cost"), Is.EqualTo("125"));
            Assert.That(Find("Character Level Up Experience Icon").GetComponent<PrototypeExperienceIcon>(), Is.Not.Null);
            Click("Character Level Up Button");
            Assert.That(_controller.Progress.SharedExperience, Is.EqualTo(775));
            Carousel().Move(-1); SettleCarousel(); Click("Character Level Up Button");
            Assert.That(_controller.Progress.SharedExperience, Is.EqualTo(675));
            Assert.That(Text("Main Experience Amount"), Is.EqualTo("675"));
            Capture("CharacterExperienceGrowth.png");
            Click("Character Confirm Button");
            Assert.That(Text("Main Character Health"), Is.EqualTo("체력  57.75")); Assert.That(Text("Main Character Attack"), Is.EqualTo("공격력  11.55"));
            Assert.That(Text("Main Character Level"), Is.EqualTo("Lv. 2"));
            yield return LoadMenu();
            Assert.That(_controller.Progress.SharedExperience, Is.EqualTo(675));
            Assert.That(_controller.Progress.GetCharacterLevel("serena"), Is.EqualTo(3));
            Assert.That(_controller.Progress.GetCharacterLevel("allen"), Is.EqualTo(2));
            Assert.That(_controller.Progress.GetStage("stage_01").HighestRound, Is.EqualTo(4));
            Capture("MainExperienceGrowth.png");
            _controller.TryStartRun();
            Assert.That(_controller.StageEntryStats.MaximumHealth, Is.EqualTo(57.75)); Assert.That(_controller.StageEntryStats.Attack, Is.EqualTo(11.55));
            Assert.That(_controller.TryLevelUpCharacter("allen"), Is.False);
            _controller.TryStartBattle();
            Assert.That(_controller.ActiveCombat.PlayerMaxHealth, Is.EqualTo(57.75));
            Assert.That(_controller.ActiveCombat.PlayerWeapons.Single().Stats.AttackDamage, Is.EqualTo(6.3525).Within(.001));
            Assert.That(_controller.Progress.SharedExperience, Is.EqualTo(675));
        }

        [UnityTest]
        public IEnumerator LevelUp_WhenSaveFails_DoesNotSpendExperienceOrChangeLevel()
        {
            var initial = new PlayerProgress(); initial.TryAddExperience(100);
            Assert.That(new PlayerProgressStore(_savePath).TrySave(initial, out _), Is.True);
            yield return LoadMenu(); _controller.ShowCharacterSelection(); yield return null;
            File.Delete(_savePath); Directory.CreateDirectory(_savePath); // Block the file destination inside this test's unique directory.
            try
            {
                Assert.That(_controller.TryLevelUpCharacter("allen"), Is.False);
                Assert.That(_controller.Progress.SharedExperience, Is.EqualTo(100));
                Assert.That(_controller.Progress.GetCharacterLevel("allen"), Is.EqualTo(1));
                Assert.That(Text("Character Growth Error"), Is.Not.Empty);
            }
            finally { Directory.Delete(_savePath); }
        }

        private void Win()
        {
            Assert.That(_controller.TryStartBattle(), Is.True);
            var weapon = _controller.ActiveCombat.PlayerWeapons.First(); weapon.Stats.AttackDamage = 10000; weapon.Stats.AccuracyPercent = 100;
            _controller.TickCombat(2);
            Assert.That(_controller.Phase, Is.EqualTo(PrototypePhase.Completed));
        }

        private void SeedClearedStages(int count)
        {
            var progress = new PlayerProgress();
            for (int i = 1; i <= count; i++) progress.RecordRound($"stage_{i:00}", 12, 12, true);
            Assert.That(new PlayerProgressStore(_savePath).TrySave(progress, out _), Is.True);
        }
        private void SettleCarousel()
        { for (int i = 0; i < 40; i++) Carousel().TickAnimation(.05f); Canvas.ForceUpdateCanvases(); }
        private Transform Find(string name) => _controller.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
        private string Text(string name) => Find(name).GetComponent<Text>().text;
        private void Click(string name) => Find(name).GetComponent<Button>().onClick.Invoke();
        private void Capture(string name) => PrototypeScreenCapture.CaptureIfRequested(_controller.GetComponentInChildren<Canvas>(), name);
        private static PointerEventData Pointer(Transform target) => new(EventSystem.current)
        { position = RectTransformUtility.WorldToScreenPoint(null, target.GetComponent<RectTransform>().TransformPoint(target.GetComponent<RectTransform>().rect.center)) };
    }
}
