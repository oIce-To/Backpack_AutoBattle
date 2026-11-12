using System;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Progression;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    internal sealed class PrototypeEquipmentScreen
    {
        private const int EquippedMarkerSize = 28;
        private readonly BackpackPrototypeController _controller;
        private readonly RectTransform _root, _content;
        private readonly ScrollRect _scroll;
        private readonly Text _stats, _error, _empty, _inventoryTitle;
        private readonly Text _transferMessage;
        private readonly RectTransform _modal;
        private readonly RectTransform _transferModal;
        private readonly PrototypeItemTooltip _details;
        private readonly Button _equip, _unequip;
        private readonly Button _craft, _enhance, _promote, _showInventory, _showCrafting;
        private readonly Text _enhanceCost;
        private readonly PrototypeEconomyIcon _enhanceIcon;
        private readonly RectTransform _craftMaterials;
        private readonly Text _craftShardCost, _craftBlueprintCost;
        private readonly PrototypePromotionScreen _promotion;
        private readonly PrototypeDismantleScreen _dismantle;
        private readonly Button _dismantleButton, _lockModeButton;
        private readonly Text _managementNotice;
        private readonly RectTransform _detailLock;
        private readonly Dictionary<string, Button> _ownedTiles = new(StringComparer.Ordinal);
        private readonly Dictionary<UniqueGearSlot, Button> _slots = new();
        private readonly Dictionary<UniqueGearSlot, Button> _craftCategories = new();
        private readonly RectTransform _craftTabs;
        private readonly RectTransform _ownedTabs;
        private readonly Button[] _ownedCategories = new Button[4];
        private UniqueGearSlot? _ownedSlot;
        private UniqueGearSlot _craftSlot = UniqueGearSlot.Weapon;
        private string _selectedId;
        private Action _confirmationAction;
        private string _craftDefinitionId;
        private bool _craftMode;
        private bool _lockMode;
        internal bool IsDetailsOpen => _modal.gameObject.activeSelf || (_dismantle?.IsOpen ?? false);

        internal PrototypeEquipmentScreen(Transform canvas, BackpackPrototypeController controller)
        {
            _controller = controller;
            var background = SelectionUi.Image(canvas, "Equipment Screen", new Color(.035f, .04f, .055f));
            background.raycastTarget = true;
            _root = background.rectTransform;
            BackpackPrototypeController.Stretch(_root);
            var panel = SelectionUi.Image(_root, "Equipment Panel", new Color(.07f, .075f, .085f));
            SelectionUi.Place(panel.rectTransform, Vector2.zero, new Vector2(1100, 610));
            SelectionUi.Label(panel.transform, "Equipment Title", "고유 장비", 30, new Vector2(0, 253), new Vector2(1000, 48))
                .color = new Color(.95f, .88f, .72f);

            var loadout = SelectionUi.Image(panel.transform, "Equipped Loadout", new Color(.10f, .12f, .16f));
            SelectionUi.Place(loadout.rectTransform, new Vector2(-313, -3), new Vector2(418, 440));
            _stats = SelectionUi.Label(loadout.transform, "Equipped Character Stats", "", 19, new Vector2(0, 126), new Vector2(380, 166));
            _stats.lineSpacing = 1.15f;
            foreach (UniqueGearSlot slot in Enum.GetValues(typeof(UniqueGearSlot)))
            {
                var button = SelectionUi.Button(loadout.transform, "Equipment Slot " + slot, "",
                    new Vector2(((int)slot - 1) * 128, -38), Vector2.one * 114f, () => SelectSlot(slot));
                _slots.Add(slot, button);
                SelectionUi.Label(loadout.transform, "Equipment Slot Label", SlotLabel(slot), 15,
                    new Vector2(((int)slot - 1) * 128, -114), new Vector2(120, 27));
            }
            _showInventory = SelectionUi.Button(loadout.transform, "Show Owned Equipment", "보유 장비",
                new Vector2(-87, -174), new Vector2(164, 40), () => SwitchCrafting(false));
            _showCrafting = SelectionUi.Button(loadout.transform, "Show Equipment Crafting", "장비 제작",
                new Vector2(87, -174), new Vector2(164, 40), () => SwitchCrafting(true));

            var inventory = SelectionUi.Image(panel.transform, "Shared Gear Inventory", new Color(.10f, .12f, .16f));
            SelectionUi.Place(inventory.rectTransform, new Vector2(222, -3), new Vector2(614, 440));
            _inventoryTitle = SelectionUi.Label(inventory.transform, "Crafting Title", "고유 장비 제작", 22, new Vector2(0, 190), new Vector2(568, 35));
            var categories = new GameObject("Crafting Categories", typeof(RectTransform));
            categories.transform.SetParent(inventory.transform, false); _craftTabs = (RectTransform)categories.transform;
            SelectionUi.Place(_craftTabs, new Vector2(0, 145), new Vector2(572, 36));
            const float categoryGap = 6, categoryWidth = (572 - categoryGap * 2) / 3;
            foreach (UniqueGearSlot slot in Enum.GetValues(typeof(UniqueGearSlot)))
            {
                var button = SelectionUi.Button(_craftTabs, "Crafting Category " + slot, SlotLabel(slot),
                    new Vector2(((int)slot - 1) * (categoryWidth + categoryGap), 0), new Vector2(categoryWidth, 36),
                    () => SwitchCraftCategory(slot));
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                _craftCategories.Add(slot, button);
            }
            var ownedCategories = new GameObject("Owned Equipment Categories", typeof(RectTransform));
            ownedCategories.transform.SetParent(inventory.transform, false); _ownedTabs = (RectTransform)ownedCategories.transform;
            SelectionUi.Place(_ownedTabs, new Vector2(0, 180), new Vector2(572, 36));
            const float ownedWidth = (572 - categoryGap * 3) / 4;
            for (int i = 0; i < _ownedCategories.Length; i++)
            {
                UniqueGearSlot? slot = i == 0 ? null : (UniqueGearSlot)(i - 1);
                _ownedCategories[i] = SelectionUi.Button(_ownedTabs, "Owned Category " + (slot?.ToString() ?? "All"),
                    slot.HasValue ? SlotLabel(slot.Value) : "전체", new Vector2((i - 1.5f) * (ownedWidth + categoryGap), 0),
                    new Vector2(ownedWidth, 36), () => SwitchOwnedCategory(slot));
                _ownedCategories[i].navigation = new Navigation { mode = Navigation.Mode.None };
            }
            var viewport = SelectionUi.Image(inventory.transform, "Equipment Inventory Viewport", new Color(.075f, .09f, .12f));
            SelectionUi.Place(viewport.rectTransform, new Vector2(0, -17), new Vector2(572, 360));
            viewport.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = new GameObject("Equipment Inventory Items", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            _content = (RectTransform)content.transform;
            _content.anchorMin = new Vector2(0, 1);
            _content.anchorMax = Vector2.one;
            _content.pivot = new Vector2(.5f, 1);
            _scroll = viewport.gameObject.AddComponent<ScrollRect>();
            _scroll.viewport = viewport.rectTransform;
            _scroll.content = _content;
            _scroll.horizontal = false;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 32;
            _empty = SelectionUi.Label(viewport.transform, "Empty Equipment Inventory", "보유한 고유 장비가 없습니다.", 18, Vector2.zero, new Vector2(540, 45));
            _dismantleButton = SelectionUi.Button(panel.transform, "Open Gear Dismantle", "분해", new Vector2(75, -260), new Vector2(278, 44), OpenDismantle);
            _dismantleButton.image.color = new Color(.65f, .27f, .26f);
            _lockModeButton = SelectionUi.Button(panel.transform, "Gear Lock Mode", "장비 잠금 모드", new Vector2(369, -260), new Vector2(278, 44),
                () => { _lockMode = !_lockMode; RefreshLockMode(); });
            _managementNotice = SelectionUi.Label(panel.transform, "Equipment Management Notice", "", 14, new Vector2(222, -295), new Vector2(572, 20));
            _managementNotice.color = new Color(1, .45f, .4f);

            var modal = new GameObject("Equipment Details Modal", typeof(RectTransform));
            modal.transform.SetParent(canvas, false);
            _modal = (RectTransform)modal.transform;
            BackpackPrototypeController.Stretch(_modal);
            var backdrop = SelectionUi.Image(_modal, "Equipment Details Backdrop", new Color(0f, 0f, 0f, .72f));
            BackpackPrototypeController.Stretch(backdrop.rectTransform);
            backdrop.raycastTarget = true;
            var dismiss = backdrop.gameObject.AddComponent<Button>();
            dismiss.transition = Selectable.Transition.None;
            dismiss.navigation = new Navigation { mode = Navigation.Mode.None };
            dismiss.onClick.AddListener(CloseDetails);

            _details = PrototypeItemTooltip.Create(_modal);
            _details.name = "Equipment Item Tooltip";
            var detailLock = new GameObject("Equipment Detail Lock", typeof(RectTransform), typeof(PrototypeShopLockIcon));
            detailLock.transform.SetParent(_details.transform, false); _detailLock = (RectTransform)detailLock.transform;
            detailLock.GetComponent<PrototypeShopLockIcon>().color = new Color(1f, .81f, .19f);
            detailLock.GetComponent<PrototypeShopLockIcon>().raycastTarget = false;
            detailLock.SetActive(false);
            _equip = CreateAction("Equip Unique Gear", "장착", new Color(.18f, .52f, .32f), EquipSelectedGear);
            _unequip = CreateAction("Unequip Unique Gear", "장착 해제", new Color(.66f, .23f, .23f),
                () => { if (SelectedGear() is { } item && item.EquippedBy == CharacterId) _controller.TryUnequipUniqueGear(item.Slot); });
            _craft = CreateAction("Craft Unique Gear", "제작", new Color(.18f, .52f, .32f),
                () => _controller.TryCraftUniqueGear(_craftDefinitionId));
            BackpackPrototypeController.SetRect((RectTransform)_craft.transform, Vector2.zero, new Vector2(314, 48), new Vector2(112, 64));
            var materials = new GameObject("Crafting Materials", typeof(RectTransform));
            materials.transform.SetParent(_details.transform, false); _craftMaterials = (RectTransform)materials.transform;
            BackpackPrototypeController.SetRect(_craftMaterials, Vector2.zero, new Vector2(126, 48), new Vector2(216, 64));
            _craftShardCost = CraftingCost("Crafting Shard Cost", EconomyIconKind.OriginShard, -90);
            _craftBlueprintCost = CraftingCost("Crafting Blueprint Cost", EconomyIconKind.Blueprint, 18);
            _enhance = CreateAction("Enhance Unique Gear", "강화", new Color(.35f, .61f, .86f),
                () => { if (SelectedGear() is { } item) _controller.TryEnhanceUniqueGear(item.InstanceId); });
            _promote = CreateAction("Promote Unique Gear", "승급", new Color(.64f, .46f, .82f), OpenPromotion);
            BackpackPrototypeController.SetRect((RectTransform)_enhance.transform, Vector2.zero, new Vector2(194, 48), new Vector2(112, 64));
            BackpackPrototypeController.SetRect((RectTransform)_promote.transform, Vector2.zero, new Vector2(314, 48), new Vector2(112, 64));
            var icon = new GameObject("Enhancement Cost Icon", typeof(RectTransform), typeof(PrototypeEconomyIcon));
            icon.transform.SetParent(_enhance.transform, false);
            _enhanceIcon = icon.GetComponent<PrototypeEconomyIcon>(); _enhanceIcon.raycastTarget = false;
            _enhanceIcon.SetKind(EconomyIconKind.OriginShard);
            SelectionUi.Place(_enhanceIcon.rectTransform, new Vector2(-28, -15), new Vector2(22, 22));
            _enhanceCost = SelectionUi.Label(_enhance.transform, "Enhancement Cost Amount", "", 14,
                new Vector2(16, -15), new Vector2(62, 22));
            _enhanceCost.alignment = TextAnchor.MiddleLeft;
            _error = SelectionUi.Label(_details.transform, "Equipment Error", "", 16, Vector2.zero, new Vector2(390, 42));
            BackpackPrototypeController.SetRect(_error.rectTransform, new Vector2(.5f, 0f), new Vector2(0f, -27f), new Vector2(390, 42));
            _error.color = new Color(1, .45f, .4f);

            var transferBackdrop = SelectionUi.Image(_modal, "Equipment Transfer Modal", new Color(0f, 0f, 0f, .55f));
            transferBackdrop.raycastTarget = true;
            _transferModal = transferBackdrop.rectTransform;
            BackpackPrototypeController.Stretch(_transferModal);
            var transferPanel = SelectionUi.Image(_transferModal, "Equipment Transfer Panel", new Color(.10f, .13f, .19f));
            SelectionUi.Place(transferPanel.rectTransform, Vector2.zero, new Vector2(460, 178));
            _transferMessage = SelectionUi.Label(transferPanel.transform, "Equipment Transfer Message", "", 20,
                new Vector2(0f, 25f), new Vector2(412, 85));
            var confirm = SelectionUi.Button(transferPanel.transform, "Confirm Equipment Transfer", "예",
                new Vector2(-101, -49), new Vector2(180, 41), ConfirmTransfer);
            var cancel = SelectionUi.Button(transferPanel.transform, "Cancel Equipment Transfer", "아니오",
                new Vector2(101, -49), new Vector2(180, 41), CloseTransfer);
            confirm.navigation = cancel.navigation = new Navigation { mode = Navigation.Mode.None };
            _promotion = new PrototypePromotionScreen(_modal, _controller, ClosePromotion);
            _dismantle = new PrototypeDismantleScreen(canvas, _controller, () => _dismantle.Hide());
            Hide();
        }

        private Button CreateAction(string name, string label, Color color, Action action)
        {
            var button = SelectionUi.Button(_details.transform, name, label, Vector2.zero, new Vector2(112, 64),
                () => { if (IsDetailsOpen && !_transferModal.gameObject.activeSelf && !_promotion.IsOpen) action(); });
            BackpackPrototypeController.SetRect((RectTransform)button.transform, Vector2.zero, new Vector2(74f, 48f), new Vector2(112, 64));
            button.GetComponent<Image>().color = color;
            button.gameObject.AddComponent<CanvasGroup>();
            var colors = button.colors; colors.fadeDuration = 0; colors.disabledColor = Color.white; button.colors = colors;
            return button;
        }

        private Text CraftingCost(string name, EconomyIconKind kind, float x)
        {
            var obj = new GameObject(name + " Icon", typeof(RectTransform), typeof(PrototypeEconomyIcon));
            obj.transform.SetParent(_craftMaterials, false);
            var icon = obj.GetComponent<PrototypeEconomyIcon>(); icon.SetKind(kind); icon.raycastTarget = false;
            SelectionUi.Place(icon.rectTransform, new Vector2(x, 0), new Vector2(28, 28));
            var amount = SelectionUi.Label(_craftMaterials, name, "", 17, new Vector2(x + 48, 0), new Vector2(62, 32));
            amount.alignment = TextAnchor.MiddleLeft;
            return amount;
        }

        internal void Show()
        {
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            Render();
        }

        internal void Hide()
        {
            CloseDetails();
            _lockMode = false;
            _root.gameObject.SetActive(false);
        }

        internal void CloseDetails()
        {
            CloseTransfer();
            _promotion.Hide();
            _dismantle?.Hide();
            if (IsDetailsOpen) EventSystem.current?.SetSelectedGameObject(null);
            _selectedId = null;
            _craftDefinitionId = null;
            _modal.gameObject.SetActive(false);
            _details.HideAll();
        }
        internal void DismissTopmost()
        {
            if (_dismantle.IsOpen) _dismantle.Hide();
            else if (_promotion.IsOpen) ClosePromotion();
            else if (_transferModal.gameObject.activeSelf) CloseTransfer();
            else CloseDetails();
        }
        internal void ShowError(string error)
        {
            _error.text = error ?? "";
            if (_promotion.IsOpen) _promotion.ShowError(error);
            if (_dismantle.IsOpen) _dismantle.ShowError(error);
            if (!IsDetailsOpen) _managementNotice.text = error ?? "";
        }
        private string CharacterId => _controller.SelectedCharacter?.CharacterId;
        private UniqueGearInstance SelectedGear() => _controller.Progress.UniqueGear.FirstOrDefault(item => item.InstanceId == _selectedId);

        private void SelectSlot(UniqueGearSlot slot)
        {
            SelectOwnedGear(_controller.Progress.GetEquippedGear(CharacterId, slot)?.InstanceId);
        }

        private void SelectOwnedGear(string instanceId)
        {
            var item = _controller.Progress.UniqueGear.FirstOrDefault(gear => gear.InstanceId == instanceId);
            if (item == null) return;
            if (_lockMode) _controller.TrySetUniqueGearLocked(item.InstanceId, !item.IsLocked);
            else OpenDetails(item);
        }

        private void RefreshLockMode()
        {
            _lockModeButton.image.color = _lockMode ? new Color(.55f, .40f, .12f) : SelectionUi.Idle;
            _lockModeButton.GetComponentInChildren<Text>().text = _lockMode ? "잠금 모드 종료" : "장비 잠금 모드";
            EventSystem.current?.SetSelectedGameObject(null);
        }

        internal void RefreshGearLockState()
        {
            _managementNotice.text = "";
            foreach (var item in _controller.Progress.UniqueGear)
                if (_ownedTiles.TryGetValue(item.InstanceId, out var tile))
                    PrototypeGearBadge.Render(tile.transform, _controller.FindGearDefinition(item.GearId), item, EquippedMarkerSize);
            foreach (var slot in _slots)
            {
                var item = _controller.Progress.GetEquippedGear(CharacterId, slot.Key);
                PrototypeGearBadge.Render(slot.Value.transform, item == null ? null : _controller.FindGearDefinition(item.GearId), item, EquippedMarkerSize);
            }
        }

        private void OpenDismantle()
        {
            if (_craftMode || IsDetailsOpen) return;
            _lockMode = false; RefreshLockMode();
            _dismantle.Show();
        }

        private void Render()
        {
            var character = _controller.SelectedCharacter;
            var progress = _controller.Progress;
            var stats = _controller.GetCharacterStats(character);
            _stats.text = $"{character?.DisplayName}  Lv. {progress.GetCharacterLevel(CharacterId)}\n체력  {stats.MaximumHealth:0.##}"
                + $"\n공격력  {stats.Attack:0.##}";
            if (stats.CriticalDamageBonusPercent > 0)
                _stats.text += $"\n치명타 피해 보너스  +{stats.CriticalDamageBonusPercent:0.##}%";
            if (stats.CriticalChanceBonusPercent > 0)
                _stats.text += $"\n치명타 확률 보너스  +{stats.CriticalChanceBonusPercent:0.##}%p";
            if (stats.ArmorGainBonusPercent > 0)
                _stats.text += $"\n방어도 획득량  +{stats.ArmorGainBonusPercent:0.##}%";
            if (stats.HealingBonusPercent > 0)
                _stats.text += $"\n치유 효율  +{stats.HealingBonusPercent:0.##}%";
            _inventoryTitle.gameObject.SetActive(_craftMode);
            _managementNotice.text = "";
            _dismantleButton.gameObject.SetActive(!_craftMode);
            _lockModeButton.gameObject.SetActive(!_craftMode);
            RefreshLockMode();
            _craftTabs.gameObject.SetActive(_craftMode);
            _ownedTabs.gameObject.SetActive(!_craftMode);
            for (int i = 0; i < _ownedCategories.Length; i++)
                _ownedCategories[i].image.color = i == (_ownedSlot.HasValue ? (int)_ownedSlot.Value + 1 : 0) ? SelectionUi.Selected : SelectionUi.Idle;
            SelectionUi.Place(_scroll.viewport, _craftMode ? new Vector2(0, -42) : new Vector2(0, -24),
                new Vector2(572, _craftMode ? 310 : 352));
            _showInventory.GetComponent<Image>().color = _craftMode ? SelectionUi.Idle : SelectionUi.Selected;
            _showCrafting.GetComponent<Image>().color = _craftMode ? SelectionUi.Selected : SelectionUi.Idle;
            foreach (var pair in _slots)
            {
                var (slot, button) = (pair.Key, pair.Value);
                var item = progress.GetEquippedGear(CharacterId, slot);
                var definition = item == null ? null : _controller.FindGearDefinition(item.GearId);
                PrototypeGearBadge.Render(button.transform, definition, item, equippedMarkerSize: EquippedMarkerSize);
                button.interactable = definition != null;
                button.GetComponentInChildren<Text>().text = definition == null ? "비어 있음" : "";
            }

            float scrollPosition = _content.childCount == 0 ? 1f : _scroll.verticalNormalizedPosition;
            _ownedTiles.Clear();
            foreach (Transform child in _content) { child.gameObject.SetActive(false); UnityEngine.Object.Destroy(child.gameObject); }
            if (_craftMode) { RenderCrafting(); return; }
            var items = progress.UniqueGear.Where(item => _controller.FindGearDefinition(item.GearId) != null
                && (!_ownedSlot.HasValue || item.Slot == _ownedSlot.Value)).ToArray();
            _empty.text = _ownedSlot.HasValue ? "해당 종류의 보유 장비가 없습니다." : "보유한 고유 장비가 없습니다.";
            _empty.gameObject.SetActive(items.Length == 0);
            const int columns = 4;
            const float tileSize = 128f, stride = 140f;
            _content.sizeDelta = new Vector2(0, Mathf.Max(352, Mathf.CeilToInt(items.Length / (float)columns) * stride + 8));
            for (int index = 0; index < items.Length; index++)
            {
                var item = items[index];
                var definition = _controller.FindGearDefinition(item.GearId);
                var button = SelectionUi.Button(_content, "Inventory Gear " + item.InstanceId, "", Vector2.zero,
                    Vector2.one * tileSize, () => SelectOwnedGear(item.InstanceId));
                _ownedTiles.Add(item.InstanceId, button);
                BackpackPrototypeController.SetRect((RectTransform)button.transform, new Vector2(.5f, 1),
                    new Vector2((index % columns - 1.5f) * stride, -72f - index / columns * stride), Vector2.one * tileSize);
                PrototypeGearBadge.Render(button.transform, definition, item, equippedMarkerSize: EquippedMarkerSize);
            }
            _scroll.verticalNormalizedPosition = scrollPosition;
        }

        private void SwitchCrafting(bool crafting)
        {
            _craftMode = crafting;
            if (crafting) _lockMode = false;
            _scroll.verticalNormalizedPosition = 1f;
            Render();
        }

        private void SwitchOwnedCategory(UniqueGearSlot? slot)
        {
            if (_craftMode || _ownedSlot == slot) return;
            _ownedSlot = slot;
            Render();
            _scroll.verticalNormalizedPosition = 1;
        }

        private void RenderCrafting()
        {
            var definitions = _controller.AvailableGear.Where(definition => definition.Slot == _craftSlot).ToArray();
            foreach (var category in _craftCategories)
                category.Value.image.color = category.Key == _craftSlot ? SelectionUi.Selected : SelectionUi.Idle;
            _empty.text = "제작 가능한 장비가 없습니다.";
            _empty.gameObject.SetActive(definitions.Length == 0);
            _content.sizeDelta = new Vector2(0, Mathf.Max(310, Mathf.CeilToInt(definitions.Length / 2f) * 178f + 8));
            for (int index = 0; index < definitions.Length; index++)
            {
                var definition = definitions[index];
                var button = SelectionUi.Button(_content, "Crafting Choice " + definition.GearId, "", Vector2.zero,
                    new Vector2(254, 164), () => OpenCraftDetails(definition));
                BackpackPrototypeController.SetRect((RectTransform)button.transform, new Vector2(.5f, 1),
                    new Vector2(index % 2 == 0 ? -141 : 141, -89 - index / 2 * 178f), new Vector2(254, 164));
                Graphic icon;
                if (definition.Icon != null)
                {
                    var sprite = SelectionUi.Image(button.transform, "Crafting Item Icon", Color.white);
                    sprite.sprite = definition.Icon;
                    sprite.preserveAspect = true;
                    sprite.rectTransform.localScale = Vector3.one * definition.IconScale;
                    icon = sprite;
                }
                else
                {
                    var glyph = new GameObject("Crafting Item Icon", typeof(RectTransform), typeof(PrototypeGearGlyph))
                        .GetComponent<PrototypeGearGlyph>();
                    glyph.transform.SetParent(button.transform, false);
                    glyph.raycastTarget = false;
                    glyph.Configure(definition.Slot, definition.Item.Tags, definition.Item.DisplayColor);
                    icon = glyph;
                }
                SelectionUi.Place(icon.rectTransform, new Vector2(0, 25), new Vector2(90, 90));
                SelectionUi.Label(button.transform, "Crafting Item Name", $"{definition.Item.DisplayName}\n설계도 {CompactNumber.Format(_controller.Progress.GetBlueprints(definition.GearId))} / 10",
                    17, new Vector2(0, -53), new Vector2(242, 48));
            }
        }

        private void SwitchCraftCategory(UniqueGearSlot slot)
        {
            if (!_craftMode || _craftSlot == slot) return;
            _craftSlot = slot;
            _scroll.verticalNormalizedPosition = 1;
            Render();
        }

        internal void ShowGearDetails(string instanceId)
            => OpenDetails(_controller.Progress.UniqueGear.FirstOrDefault(item => item.InstanceId == instanceId));

        internal void ShowCraftDetails(string gearId) => OpenCraftDetails(_controller.FindGearDefinition(gearId));

        private void OpenCraftDetails(UniqueGearData definition)
        {
            if (definition == null || IsDetailsOpen) return;
            _selectedId = null; _craftDefinitionId = definition.GearId;
            _equip.gameObject.SetActive(false); _unequip.gameObject.SetActive(false);
            _enhance.gameObject.SetActive(false); _promote.gameObject.SetActive(false);
            _craft.gameObject.SetActive(true);
            _craftMaterials.gameObject.SetActive(true);
            _craft.interactable = _controller.Progress.OriginShards >= PlayerProgress.GearCraftShardCost
                && _controller.Progress.GetBlueprints(definition.GearId) >= PlayerProgress.GearCraftBlueprintCost;
            _craft.GetComponent<CanvasGroup>().alpha = _craft.interactable ? 1 : .35f;
            _craftShardCost.text = CompactNumber.Format(PlayerProgress.GearCraftShardCost);
            _craftBlueprintCost.text = CompactNumber.Format(PlayerProgress.GearCraftBlueprintCost);
            Color enough = Color.white, missing = new Color(1, .45f, .4f);
            _craftShardCost.color = _controller.Progress.OriginShards >= PlayerProgress.GearCraftShardCost ? enough : missing;
            _craftBlueprintCost.color = _controller.Progress.GetBlueprints(definition.GearId) >= PlayerProgress.GearCraftBlueprintCost ? enough : missing;
            var stats = _controller.GetCharacterStats(_controller.SelectedCharacter);
            string notes = BonusNotes(definition, ItemRarity.Common, 0);
            ShowDetails(ItemTooltipContent.FromItem(definition.Item, stats, ItemRarity.Common, definition.EffectFor(ItemRarity.Common)), " +0", notes);
        }

        private void ShowDetails(ItemTooltipContent content, string rank, string notes)
        {
            ShowError("");
            _detailLock.gameObject.SetActive(false);
            _modal.gameObject.SetActive(true); _modal.SetAsLastSibling();
            _details.ShowPinned(content, rank, notes, 88f);
            EventSystem.current?.SetSelectedGameObject(null);
        }

        private void OpenDetails(UniqueGearInstance item)
        {
            if (item == null || IsDetailsOpen) return;
            var definition = _controller.FindGearDefinition(item.GearId);
            if (definition == null) return;
            _selectedId = item.InstanceId;
            _craftDefinitionId = null;
            _craft.gameObject.SetActive(false);
            _craftMaterials.gameObject.SetActive(false);
            _enhance.gameObject.SetActive(true);
            _promote.gameObject.SetActive(true);
            long cost = PlayerProgress.EnhancementCost(item);
            _enhance.interactable = cost > 0 && _controller.Progress.OriginShards >= cost;
            _promote.interactable = item.Rarity == ItemRarity.Common && item.Enhancement == UniqueGearData.MaxEnhancement(item.Rarity);
            _enhance.GetComponent<CanvasGroup>().alpha = _enhance.interactable ? 1 : .35f;
            _promote.GetComponent<CanvasGroup>().alpha = _promote.interactable ? 1 : .35f;
            var enhanceLabel = _enhance.transform.Find("Enhance Unique Gear Label").GetComponent<Text>();
            SelectionUi.Place(enhanceLabel.rectTransform, new Vector2(0, 14), new Vector2(100, 24));
            _enhanceIcon.gameObject.SetActive(cost > 0);
            _enhanceCost.text = cost > 0 ? CompactNumber.Format(cost) : "최대 강화";
            _enhanceCost.alignment = cost > 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            SelectionUi.Place(_enhanceCost.rectTransform, cost > 0 ? new Vector2(16, -15) : new Vector2(0, -15),
                new Vector2(cost > 0 ? 62 : 108, 22));
            bool equippedHere = item.EquippedBy == CharacterId;
            _equip.gameObject.SetActive(!equippedHere);
            _unequip.gameObject.SetActive(equippedHere);
            _equip.interactable = CharacterId != null;
            var owner = FindOwner(item);
            var stats = _controller.GetCharacterStats(owner ?? _controller.SelectedCharacter);
            string notes = BonusNotes(definition, item.Rarity, item.Enhancement);
            var content = ItemTooltipContent.FromItem(definition.Item, stats, item.Rarity, definition.EffectFor(item.Rarity));
            ShowDetails(content, $" +{item.Enhancement}", notes);
            if (item.IsLocked)
            {
                var name = _details.transform.Find("Tooltip Name").GetComponent<Text>();
                name.resizeTextForBestFit = true; name.resizeTextMinSize = 16; name.resizeTextMaxSize = 25;
                name.rectTransform.sizeDelta = new Vector2(214, 36);
                _detailLock.anchorMin = _detailLock.anchorMax = new Vector2(0, 1);
                _detailLock.sizeDelta = Vector2.one * 25;
                _detailLock.anchoredPosition = new Vector2(18 + Mathf.Min(name.preferredWidth, 214) + 18.5f, -26);
                _detailLock.gameObject.SetActive(true);
            }
        }

        private static string BonusNotes(UniqueGearData definition, ItemRarity rarity, int enhancement)
        {
            double value = definition.BonusPercentFor(rarity, enhancement);
            string bonus = string.Join("\n", new[]
            {
                definition.BonusHealth ? $"장착한 캐릭터의 체력 +{value:0.##}%" : null,
                definition.BonusAttack ? $"장착한 캐릭터의 공격력 +{value:0.##}%" : null,
                definition.BonusCriticalChance ? $"장착한 캐릭터의 치명타 확률 +{value:0.##}%p" : null,
                definition.BonusCriticalDamage ? $"장착한 캐릭터의 치명타 피해 +{value:0.##}%" : null,
                definition.BonusArmorGain ? $"장착한 캐릭터의 방어도 획득량 +{value:0.##}%" : null,
                definition.BonusHealing ? $"장착한 캐릭터의 치유 효율 +{value:0.##}%" : null
            }.Where(line => line != null));
            return bonus.Length == 0 ? "" : "\n\n" + bonus;
        }

        private void EquipSelectedGear()
        {
            var item = SelectedGear();
            if (item == null || CharacterId == null || item.EquippedBy == CharacterId) return;
            if (string.IsNullOrEmpty(item.EquippedBy))
            {
                _controller.TryEquipUniqueGear(item.Slot, item.InstanceId);
                return;
            }

            string name = FindOwner(item)?.DisplayName?.Trim();
            if (string.IsNullOrEmpty(name)) name = "다른 캐릭터";
            char last = name[^1];
            string subject = last >= '가' && last <= '힣' && (last - '가') % 28 != 0 ? "이" : "가";
            _transferMessage.text = $"해당 아이템은 현재 {name}{subject} 장착하고 있습니다.\n장착 해제하시겠습니까?";
            string expectedOwner = item.EquippedBy;
            _confirmationAction = () => _controller.TryEquipUniqueGear(item.Slot, item.InstanceId, expectedOwner);
            OpenConfirmation();
        }

        private void OpenPromotion()
        {
            if (SelectedGear() is not { } item) return;
            if (!_promote.interactable) return;
            _promotion.Show(item);
            _details.GetComponent<CanvasGroup>().interactable = false;
        }

        private void ClosePromotion()
        {
            _promotion.Hide();
            _details.GetComponent<CanvasGroup>().interactable = true;
            EventSystem.current?.SetSelectedGameObject(null);
        }

        private void OpenConfirmation()
        {
            _transferModal.gameObject.SetActive(true);
            _transferModal.SetAsLastSibling();
            _details.GetComponent<CanvasGroup>().interactable = false;
            EventSystem.current?.SetSelectedGameObject(null);
        }

        private void ConfirmTransfer()
        {
            if (!_transferModal.gameObject.activeSelf) return;
            var action = _confirmationAction;
            CloseTransfer();
            action?.Invoke();
        }

        private void CloseTransfer()
        {
            if (_transferModal.gameObject.activeSelf) EventSystem.current?.SetSelectedGameObject(null);
            _confirmationAction = null;
            _transferModal.gameObject.SetActive(false);
            _details.GetComponent<CanvasGroup>().interactable = true;
        }

        private CharacterData FindOwner(UniqueGearInstance item)
            => _controller.AvailableCharacters.FirstOrDefault(character => character.CharacterId == item.EquippedBy);

        internal static string SlotLabel(UniqueGearSlot slot) => slot switch
        {
            UniqueGearSlot.Weapon => "무기",
            UniqueGearSlot.Armor => "방어구",
            _ => "장신구 / 펫"
        };
    }
}
