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
            Assert.That(badge.Find("Gear Rarity Border").GetComponentsInChildren<Image>()
                .All(edge => edge.color == ItemRarity.Common.GetDisplayColor()), Is.True);
            var bowIcon = (RectTransform)Find("Inventory Gear " + bow.InstanceId).Find("Gear Badge/Gear Sprite");
            Assert.That(bowIcon.offsetMin, Is.EqualTo(new Vector2(7, 7)));
            Assert.That(bowIcon.offsetMax, Is.EqualTo(new Vector2(-7, -7)));
            Assert.That(Find("Equipment Details Modal").gameObject.activeSelf, Is.False);
            Click("Equipment Slot Weapon"); yield return null;
            var tooltip = Find("Equipment Item Tooltip");
            Assert.That(tooltip.Find("Tooltip Name").GetComponent<Text>().text, Is.EqualTo("기사의 보검"));
            Assert.That(tooltip.GetComponentInChildren<TextMeshProUGUI>().text, Does.Contain("5%"));
            Assert.That(tooltip.GetComponentInChildren<TextMeshProUGUI>().text, Does.Contain("장착한 캐릭터의 체력 +5%\n장착한 캐릭터의 공격력 +5%")
                .And.Not.Contain("강화 비용").And.Not.Contain("승급 재료").And.Not.Contain("알렌 장착"));
            Assert.That(Find("Enhance Unique Gear").GetComponent<CanvasGroup>().alpha, Is.LessThan(.5f));
            Assert.That(Text("Enhancement Cost Amount"), Is.EqualTo("500"));
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
        public IEnumerator OriginGear_CraftsEnhancesAndPromotesThroughEquipmentUi_WithPersistentCosts()
        {
            var initial = new PlayerProgress();
            Assert.That(initial.TryClaimReward("seed", new StageReward(0, 100000, Enumerable.Repeat("knight_sword", 20)),
                false, DateTimeOffset.UtcNow), Is.True);
            Assert.That(new PlayerProgressStore(_savePath).TrySave(initial, out _), Is.True);
            yield return LoadMenu();
            Click("Main Tab Equipment");
            Click("Show Equipment Crafting"); yield return null;
            Click("Crafting Choice knight_sword"); yield return null;
            Assert.That(Find("Craft Unique Gear").GetComponent<Button>().interactable, Is.True);
            Assert.That(Text("Crafting Shard Cost"), Is.EqualTo("500"));
            Assert.That(Text("Crafting Blueprint Cost"), Is.EqualTo("10"));
            Assert.That(Find("Equipment Item Tooltip").GetComponentInChildren<TextMeshProUGUI>().text,
                Does.Not.Contain("제작 비용").And.Not.Contain("설계도"));
            Assert.That(((RectTransform)Find("Craft Unique Gear")).anchoredPosition.x, Is.GreaterThan(250));
            Capture("OriginGearCrafting.png");
            Click("Craft Unique Gear"); yield return null;
            var center = _controller.Progress.UniqueGear.Single(gear => gear.GearId == "knight_sword" && gear.EquippedBy == "");
            Assert.That(_controller.Progress.OriginShards, Is.EqualTo(99500));
            Assert.That(_controller.Progress.GetBlueprints("knight_sword"), Is.EqualTo(10));
            Assert.That(Find("Craft Unique Gear").gameObject.activeInHierarchy, Is.True);
            Assert.That(Find("Enhance Unique Gear").gameObject.activeSelf, Is.False);
            Click("Equipment Details Backdrop");
            Click("Show Owned Equipment"); yield return null;
            Click("Inventory Gear " + center.InstanceId);
            for (int i = 0; i < 3; i++) { Click("Enhance Unique Gear"); yield return null; }
            Assert.That(_controller.Progress.UniqueGear.Single(gear => gear.InstanceId == center.InstanceId).Enhancement, Is.EqualTo(3));
            Assert.That(Find("Enhance Unique Gear").GetComponent<Button>().interactable, Is.False);
            Assert.That(Text("Enhancement Cost Amount"), Is.EqualTo("최대 강화"));
            Assert.That(_controller.Progress.OriginShards, Is.EqualTo(97300));
            Capture("OriginGearCommonMax.png");
            Assert.That(Find("Promote Unique Gear").GetComponent<Button>().interactable, Is.True);
            Click("Promote Unique Gear"); yield return null;
            Assert.That(Text("Promotion Material Count"), Is.EqualTo("재료 0 / 2"));
            Assert.That(Find("Confirm Promotion Materials").GetComponent<Button>().interactable, Is.False);
            Assert.That(Find("Promotion Inventory").GetComponentsInChildren<Button>().All(button => !button.interactable), Is.True);
            Capture("OriginGearPromotionNoMaterials.png");
            Click("Cancel Promotion Materials");
            Click("Equipment Details Backdrop");
            Click("Show Equipment Crafting"); yield return null;
            Click("Crafting Choice knight_sword");
            Click("Craft Unique Gear"); yield return null;
            Assert.That(_controller.Progress.GetBlueprints("knight_sword"), Is.Zero);
            Click("Equipment Details Backdrop");
            Click("Crafting Choice knight_sword");
            Assert.That(Find("Craft Unique Gear").GetComponent<Button>().interactable, Is.False);
            Assert.That(Find("Craft Unique Gear").GetComponent<CanvasGroup>().alpha, Is.LessThan(.5f));
            Capture("OriginGearCraftingInsufficient.png");
            Click("Equipment Details Backdrop");
            Assert.That(_controller.TryUnequipUniqueGear(UniqueGearSlot.Weapon), Is.True);
            Click("Show Owned Equipment"); yield return null;
            Click("Inventory Gear " + center.InstanceId);
            Assert.That(Find("Promote Unique Gear").GetComponent<Button>().interactable, Is.True);
            var materials = _controller.Progress.PromotionMaterials(center.InstanceId).Take(2).Select(gear => gear.InstanceId).ToArray();
            File.Delete(_savePath); Directory.CreateDirectory(_savePath);
            try
            {
                Assert.That(_controller.TryPromoteUniqueGear(center.InstanceId, materials), Is.False);
                Assert.That(_controller.Progress.UniqueGear.Count, Is.EqualTo(4));
                Assert.That(_controller.Progress.UniqueGear.Single(gear => gear.InstanceId == center.InstanceId).Rarity, Is.EqualTo(ItemRarity.Common));
            }
            finally { Directory.Delete(_savePath); }
            Click("Promote Unique Gear");
            yield return null;
            Assert.That(Find("Confirm Promotion Materials").GetComponent<Button>().interactable, Is.False);
            var candidates = Find("Promotion Inventory").GetComponentsInChildren<Button>().ToArray();
            Assert.That(candidates.Take(2).All(button => button.interactable), Is.True);
            Assert.That(candidates.Skip(2).All(button => !button.interactable), Is.True);
            foreach (string id in materials) Click("Promotion Candidate " + id);
            Assert.That(Text("Promotion Material Count"), Is.EqualTo("재료 2 / 2"));
            Click("Promotion Selected Material 0");
            Assert.That(Find("Confirm Promotion Materials").GetComponent<Button>().interactable, Is.False);
            Click("Cancel Promotion Materials");
            Assert.That(_controller.Progress.UniqueGear.Count, Is.EqualTo(4));
            Click("Promote Unique Gear");
            yield return null;
            foreach (string id in materials) Click("Promotion Candidate " + id);
            Capture("OriginGearPromotionConfirmation.png");
            Click("Confirm Promotion Materials"); yield return null;
            Assert.That(_controller.Progress.UniqueGear.Count, Is.EqualTo(2));
            var promoted = _controller.Progress.UniqueGear.Single(gear => gear.InstanceId == center.InstanceId);
            Assert.That(promoted.Rarity, Is.EqualTo(ItemRarity.Uncommon));
            Assert.That(promoted.Enhancement, Is.Zero);
            Assert.That(Find("Equipment Slot Weapon").Find("Gear Badge").gameObject.activeSelf, Is.False);
            Capture("OriginGearUncommon.png");
            Assert.That(Text("Enhancement Cost Amount"), Is.EqualTo("2K"));
            Click("Enhance Unique Gear"); yield return null;
            Assert.That(_controller.Progress.OriginShards, Is.EqualTo(94800));
            Click("Equip Unique Gear"); yield return null;
            Assert.That(_controller.GetCharacterStats(_controller.SelectedCharacter).Attack, Is.EqualTo(10.7).Within(.0001));
            yield return LoadMenu();
            Assert.That(_controller.Progress.UniqueGear.Single(gear => gear.InstanceId == center.InstanceId).Enhancement, Is.EqualTo(1));
            Assert.That(_controller.Progress.OriginShards, Is.EqualTo(94800));
        }

        [UnityTest]
        public IEnumerator CraftingCategories_FilterSlotsAndRetainCraftPopup_ForConsecutiveCrafts()
        {
            var initial = new PlayerProgress();
            initial.TryClaimReward("repeat-craft", new StageReward(0, 1600, Enumerable.Repeat("knight_sword", 20)), false, DateTimeOffset.UtcNow);
            Assert.That(new PlayerProgressStore(_savePath).TrySave(initial, out _), Is.True);
            yield return LoadMenu();
            Assert.That(Text("Main Experience Amount"), Is.EqualTo("0"));
            Assert.That(Text("Main Origin Shard Amount"), Is.EqualTo("1.6K"));
            var experience = (RectTransform)Find("Main Experience Wallet");
            var shards = (RectTransform)Find("Main Origin Shard Wallet");
            Assert.That(experience.rect.width, Is.EqualTo(144));
            Assert.That(shards.rect.size, Is.EqualTo(experience.rect.size));
            Click("Main Tab Equipment"); Click("Show Equipment Crafting"); yield return null;
            var viewport = (RectTransform)Find("Equipment Inventory Viewport");
            var categories = (RectTransform)Find("Crafting Categories");
            Assert.That(categories.rect.width, Is.EqualTo(viewport.rect.width));
            var left = (RectTransform)Find("Crafting Category Weapon");
            var right = (RectTransform)Find("Crafting Category AccessoryOrPet");
            Assert.That(left.anchoredPosition.x - left.rect.width / 2, Is.EqualTo(-categories.rect.width / 2).Within(.001f));
            Assert.That(right.anchoredPosition.x + right.rect.width / 2, Is.EqualTo(categories.rect.width / 2).Within(.001f));
            Assert.That(viewport.anchoredPosition.y + viewport.rect.height / 2,
                Is.LessThan(categories.anchoredPosition.y - categories.rect.height / 2));
            var choices = Find("Equipment Inventory Items");
            Assert.That(choices.GetComponentsInChildren<Button>().Length, Is.EqualTo(2));
            Assert.That(choices.GetComponentsInChildren<Transform>().Any(t => t.name == "Gear Rarity Border"), Is.False);
            Capture("CraftingWeaponCategory.png");
            foreach (var category in new[] { "Armor", "AccessoryOrPet" })
            {
                Click("Crafting Category " + category); yield return null;
                Assert.That(choices.GetComponentsInChildren<Button>(), Is.Empty);
                Assert.That(Find("Empty Equipment Inventory").gameObject.activeInHierarchy, Is.True);
            }
            Capture("CraftingEmptyAccessoryCategory.png");
            Click("Crafting Category Weapon"); yield return null;
            Click("Crafting Choice knight_sword");
            var popup = Find("Equipment Item Tooltip");
            for (int i = 0; i < 2; i++)
            {
                Click("Craft Unique Gear"); yield return null;
                Assert.That(popup.gameObject.activeInHierarchy, Is.True);
                Assert.That(Find("Craft Unique Gear").gameObject.activeInHierarchy, Is.True);
                Assert.That(Find("Enhance Unique Gear").gameObject.activeSelf, Is.False);
                Assert.That(popup.Find("Tooltip Name").GetComponent<Text>().text, Is.EqualTo("기사의 보검"));
                Assert.That(((RectTransform)Find("Main Experience Wallet")).rect.size,
                    Is.EqualTo(((RectTransform)Find("Main Origin Shard Wallet")).rect.size));
            }
            Assert.That(_controller.Progress.OriginShards, Is.EqualTo(600));
            Assert.That(_controller.Progress.GetBlueprints("knight_sword"), Is.Zero);
            Assert.That(_controller.Progress.UniqueGear.Count(gear => gear.GearId == "knight_sword" && gear.EquippedBy == ""), Is.EqualTo(2));
            Assert.That(Find("Craft Unique Gear").GetComponent<Button>().interactable, Is.False);
            Assert.That(_controller.TryCraftUniqueGear("knight_sword"), Is.False);
            Assert.That(_controller.Progress.OriginShards, Is.EqualTo(600));
            Capture("CraftingPopupAfterRepeatedCrafts.png");
            Click("Equipment Details Backdrop");
            Assert.That(Find("Crafting Categories").gameObject.activeInHierarchy, Is.True);
            Click("Show Owned Equipment"); yield return null;
            Assert.That(Find("Crafting Categories").gameObject.activeSelf, Is.False);
            Assert.That(choices.GetComponentsInChildren<Button>().Length, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator CurrencyWallets_ShowCompactAmountsAndExactHoverDetails_AndCostsAndRewardsUseTheSameUnits()
        {
            var initial = new PlayerProgress();
            initial.TryClaimReward("compact-seed", new StageReward(1234567, 9876543210), false, DateTimeOffset.UtcNow);
            initial.RecordRound("origin_mine_03", 12, 12, cleared: true);
            Assert.That(new PlayerProgressStore(_savePath).TrySave(initial, out _), Is.True);
            yield return LoadMenu();
            Assert.That(Text("Main Experience Amount"), Is.EqualTo("1.2M"));
            Assert.That(Text("Main Origin Shard Amount"), Is.EqualTo("9.9B"));
            var experience = (RectTransform)Find("Main Experience Wallet");
            var shards = (RectTransform)Find("Main Origin Shard Wallet");
            Assert.That(experience.sizeDelta, Is.EqualTo(shards.sizeDelta));
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(Pointer(experience), hits);
            Assert.That(hits.First().gameObject, Is.EqualTo(experience.gameObject));
            var hover = experience.GetComponent<PrototypeCurrencyHover>();
            hover.OnPointerEnter(Pointer(experience));
            var experienceTooltip = Find("경험치 Currency Tooltip");
            Assert.That(experienceTooltip.Find("Currency Tooltip Name").GetComponent<Text>().text, Is.EqualTo("경험치"));
            Assert.That(experienceTooltip.Find("Currency Tooltip Amount").GetComponent<Text>().text, Is.EqualTo("1,234,567"));
            Capture("ExperienceExactBalanceTooltip.png");
            hover.OnPointerExit(Pointer(experience));
            shards.GetComponent<PrototypeCurrencyHover>().OnPointerEnter(Pointer(shards));
            var shardTooltip = Find("기원의 파편 Currency Tooltip");
            Assert.That(shardTooltip.Find("Currency Tooltip Name").GetComponent<Text>().text, Is.EqualTo("기원의 파편"));
            Assert.That(shardTooltip.Find("Currency Tooltip Amount").GetComponent<Text>().text, Is.EqualTo("9,876,543,210"));
            Capture("ShardExactBalanceTooltip.png");
            shards.GetComponent<PrototypeCurrencyHover>().OnPointerExit(Pointer(shards));
            Assert.That(_controller.Progress.SharedExperience, Is.EqualTo(1234567));
            Assert.That(_controller.Progress.OriginShards, Is.EqualTo(9876543210));
            Click("Main Tab Characters"); yield return null;
            for (int i = 0; i < 11; i++) Click("Character Level Up Button");
            Assert.That(Text("Character Level Up Cost"), Is.EqualTo("1.2K"));
            Capture("CompactCharacterLevelCost.png");
            Click("Main Tab Stages"); yield return null;
            Click("Mine Stage Category"); yield return null;
            Click("Stage Choice origin_mine_04");
            Assert.That(Find("Stage Reward Origin Shards").GetComponentInChildren<Text>().text, Is.EqualTo("1K"));
            Assert.That(((RectTransform)Find("Stage Confirm Button")).sizeDelta.y,
                Is.EqualTo(((RectTransform)Find("Sweep Mine Stage")).sizeDelta.y));
            Assert.That(((RectTransform)Find("Stage Confirm Button")).anchoredPosition.y,
                Is.EqualTo(((RectTransform)Find("Sweep Mine Stage")).anchoredPosition.y));
            Capture("CompactStageRewards.png");
        }

        [UnityTest]
        public IEnumerator OriginMine_FailureAndAbandonKeepTickets_ClearAndSweepPayExactlyOnce_AndProgressIsIndependent()
        {
            yield return LoadMenu();
            Assert.That(Text("Main Mine Tickets"), Is.EqualTo("5 / 5"));
            Assert.That(_controller.IsStageUnlocked("origin_mine_01"), Is.True);
            Assert.That(_controller.IsStageUnlocked("origin_mine_02"), Is.False);
            _controller.ShowStageSelection();
            Click("Mine Stage Category"); yield return null;
            var categoryHits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(Pointer(Find("Main Tab Home")), categoryHits);
            Assert.That(categoryHits.First().gameObject.name, Is.EqualTo("Main Tab Home"));
            Assert.That(Text("Stage Preview Name"), Does.Contain("기원의 광산 1"));
            Assert.That(Find("Stage Reward Origin Shards").GetComponentInChildren<Text>().text, Is.EqualTo("500"));
            Assert.That(Find("Stage Reward Blueprints").GetComponentInChildren<Text>().text, Is.EqualTo("5"));
            Assert.That(Find("Sweep Mine Stage").GetComponent<RectTransform>().rect.height, Is.GreaterThan(46));
            Assert.That(Text("Sweep Ticket Cost"), Is.EqualTo("1"));
            Assert.That(Find("Sweep Mine Stage").GetComponent<Button>().interactable, Is.False);
            Capture("OriginMineSelection.png");
            Click("Stage Confirm Button");
            Assert.That(_controller.TryStartRun(), Is.True);
            Assert.That(_controller.Progress.MineTickets, Is.EqualTo(5));
            Lose();
            Assert.That(_controller.Progress.MineTickets, Is.EqualTo(5));
            Assert.That(_controller.Progress.OriginShards, Is.Zero);
            _controller.ReturnToPreparation();
            Click("Abandon Stage Button"); Click("Confirm Abandon Stage");
            Assert.That(_controller.Progress.MineTickets, Is.EqualTo(5));
            Assert.That(_controller.TryStartRun(), Is.True);
            for (int i = 0; i < 11; i++) _controller.AdvanceRound();
            Win();
            Assert.That(_controller.Progress.MineTickets, Is.EqualTo(4));
            Assert.That(_controller.Progress.OriginShards, Is.EqualTo(500));
            Assert.That(_controller.Progress.SharedExperience, Is.Zero);
            Assert.That(_controller.Progress.GetBlueprints("knight_sword") + _controller.Progress.GetBlueprints("silver_longbow"), Is.EqualTo(5));
            Assert.That(_controller.IsStageUnlocked("origin_mine_02"), Is.True);
            Assert.That(_controller.IsStageUnlocked("stage_02"), Is.False);
            Capture("OriginMineClearedRewards.png");
            _controller.TickCombat(200);
            Assert.That(_controller.Progress.OriginShards, Is.EqualTo(500));
            Assert.That(_controller.TryReturnToMainMenu(), Is.True);
            _controller.ShowStageSelection(); yield return null;
            Assert.That(Find("Sweep Mine Stage").GetComponent<Button>().interactable, Is.True);
            Click("Sweep Mine Stage"); yield return null;
            Assert.That(Text("Reward Popup Contents"), Does.Contain("500").And.Contain("설계도"));
            Assert.That(_controller.Progress.MineTickets, Is.EqualTo(3));
            Assert.That(_controller.Progress.OriginShards, Is.EqualTo(1000));
            Capture("OriginMineSweepRewards.png");
            Assert.That(_controller.TrySweepStage("origin_mine_01"), Is.False, "A second click while the reward popup is open cannot spend another ticket.");
            Click("Reward Popup Close");
            for (int i = 0; i < 3; i++)
            {
                Assert.That(_controller.TrySweepStage("origin_mine_01"), Is.True);
                Click("Reward Popup Close");
            }
            Assert.That(_controller.Progress.MineTickets, Is.Zero);
            Assert.That(_controller.TrySweepStage("origin_mine_01"), Is.False);
            _controller.ShowMainMenuTab(MainMenuTab.Home);
            Assert.That(_controller.TryStartRun(), Is.False);
            Assert.That(Text("Main Mine Tickets"), Is.EqualTo("0 / 5"));
            Assert.That(Text("Main Mine Ticket Timer"), Does.StartWith("다음 충전"));
            Capture("OriginMineTicketsDepleted.png");
            yield return LoadMenu();
            Assert.That(_controller.Progress.MineTickets, Is.Zero);
            Assert.That(_controller.Progress.OriginShards, Is.EqualTo(2500));
#if UNITY_EDITOR
            Assert.That(_controller.RefillMineTicketsForTesting(out _), Is.True);
            Assert.That(_controller.Progress.MineTickets, Is.EqualTo(5));
            Assert.That(Text("Main Mine Tickets"), Is.EqualTo("5 / 5"));
#endif
        }

        [UnityTest]
        public IEnumerator OriginMine_SaveFailureKeepsTicketAndRewardTogether_AndReturnRetriesSettlement()
        {
            yield return LoadMenu();
            _controller.TrySelectStage("origin_mine_01"); _controller.TryStartRun();
            for (int i = 0; i < 11; i++) _controller.AdvanceRound();
            File.Delete(_savePath); Directory.CreateDirectory(_savePath);
            try
            {
                Win();
                Assert.That(_controller.Progress.MineTickets, Is.EqualTo(5));
                Assert.That(_controller.Progress.OriginShards, Is.Zero);
                Assert.That(_controller.TryReturnToMainMenu(), Is.False);
            }
            finally { Directory.Delete(_savePath); }
            Assert.That(_controller.TryReturnToMainMenu(), Is.True);
            Assert.That(_controller.Progress.MineTickets, Is.EqualTo(4));
            Assert.That(_controller.Progress.OriginShards, Is.EqualTo(500));
            yield return LoadMenu();
            Assert.That(_controller.Progress.OriginShards, Is.EqualTo(500));
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
            Assert.That(_controller.Progress.OriginShards, Is.EqualTo(10));
            Assert.That(_controller.RunResult.ExperienceReward, Is.EqualTo(10));
            Assert.That(Text("Run Result Experience"), Does.Contain("+10"));
            _controller.TickCombat(200); Assert.That(_controller.Progress.SharedExperience, Is.EqualTo(10));
            Capture("ExperienceGameOver.png");
            _controller.TryReturnToMainMenu(); _controller.TryStartRun();
            for (int i = 0; i < 11; i++) _controller.AdvanceRound();
            Win(); Assert.That(_controller.Progress.SharedExperience, Is.EqualTo(110));
            Assert.That(_controller.Progress.OriginShards, Is.EqualTo(110));
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
                Assert.That(_controller.Progress.OriginShards, Is.EqualTo(scenario.expected), $"Round {scenario.round} shards");
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
