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
        private readonly Text _stats, _error, _empty;
        private readonly Text _transferMessage;
        private readonly RectTransform _modal;
        private readonly RectTransform _transferModal;
        private readonly PrototypeItemTooltip _details;
        private readonly Button _equip, _unequip;
        private readonly Dictionary<UniqueGearSlot, Button> _slots = new();
        private string _selectedId;
        private string _transferOwner;
        internal bool IsDetailsOpen => _modal.gameObject.activeSelf;

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
            _stats = SelectionUi.Label(loadout.transform, "Equipped Character Stats", "", 22, new Vector2(0, 130), new Vector2(380, 142));
            _stats.lineSpacing = 1.35f;
            foreach (UniqueGearSlot slot in Enum.GetValues(typeof(UniqueGearSlot)))
            {
                var button = SelectionUi.Button(loadout.transform, "Equipment Slot " + slot, "",
                    new Vector2(((int)slot - 1) * 128, -38), Vector2.one * 114f, () => SelectSlot(slot));
                _slots.Add(slot, button);
                SelectionUi.Label(loadout.transform, "Equipment Slot Label", SlotLabel(slot), 15,
                    new Vector2(((int)slot - 1) * 128, -114), new Vector2(120, 27));
            }

            var inventory = SelectionUi.Image(panel.transform, "Shared Gear Inventory", new Color(.10f, .12f, .16f));
            SelectionUi.Place(inventory.rectTransform, new Vector2(222, -3), new Vector2(614, 440));
            SelectionUi.Label(inventory.transform, "Inventory Title", "공용 장비 보관함", 22, new Vector2(0, 190), new Vector2(568, 35));
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
            _equip = CreateAction("Equip Unique Gear", "장착", new Color(.18f, .52f, .32f), EquipSelectedGear);
            _unequip = CreateAction("Unequip Unique Gear", "장착 해제", new Color(.66f, .23f, .23f),
                () => { if (SelectedGear() is { } item && item.EquippedBy == CharacterId) _controller.TryUnequipUniqueGear(item.Slot); });
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
            Hide();
        }

        private Button CreateAction(string name, string label, Color color, Action action)
        {
            var button = SelectionUi.Button(_details.transform, name, label, Vector2.zero, new Vector2(112, 38),
                () => { if (IsDetailsOpen && !_transferModal.gameObject.activeSelf) action(); });
            BackpackPrototypeController.SetRect((RectTransform)button.transform, Vector2.zero, new Vector2(74f, 35f), new Vector2(112, 38));
            button.GetComponent<Image>().color = color;
            return button;
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
            _root.gameObject.SetActive(false);
        }

        internal void CloseDetails()
        {
            CloseTransfer();
            if (IsDetailsOpen) EventSystem.current?.SetSelectedGameObject(null);
            _selectedId = null;
            _modal.gameObject.SetActive(false);
            _details.HideAll();
        }
        internal void DismissTopmost()
        {
            if (_transferModal.gameObject.activeSelf) CloseTransfer();
            else CloseDetails();
        }
        internal void ShowError(string error) => _error.text = error ?? "";
        private string CharacterId => _controller.SelectedCharacter?.CharacterId;
        private UniqueGearInstance SelectedGear() => _controller.Progress.UniqueGear.FirstOrDefault(item => item.InstanceId == _selectedId);

        private void SelectSlot(UniqueGearSlot slot)
        {
            OpenDetails(_controller.Progress.GetEquippedGear(CharacterId, slot));
        }

        private void Render()
        {
            var character = _controller.SelectedCharacter;
            var progress = _controller.Progress;
            var stats = _controller.GetCharacterStats(character);
            _stats.text = $"{character?.DisplayName}  Lv. {progress.GetCharacterLevel(CharacterId)}\n체력  {stats.MaximumHealth:0.##}"
                + $"\n공격력  {stats.Attack:0.##}\n치명타 피해 보너스  +{stats.CriticalDamageBonusPercent:0.##}%";
            foreach (var pair in _slots)
            {
                var (slot, button) = (pair.Key, pair.Value);
                var item = progress.GetEquippedGear(CharacterId, slot);
                var definition = item == null ? null : _controller.FindGearDefinition(item.GearId);
                PrototypeGearBadge.Render(button.transform, definition, item, equippedMarkerSize: EquippedMarkerSize);
                button.interactable = definition != null;
                button.GetComponentInChildren<Text>().text = definition == null ? "비어 있음" : "";
            }

            float scrollPosition = _scroll.verticalNormalizedPosition;
            foreach (Transform child in _content) { child.gameObject.SetActive(false); UnityEngine.Object.Destroy(child.gameObject); }
            var items = progress.UniqueGear.Where(item => _controller.FindGearDefinition(item.GearId) != null).ToArray();
            _empty.gameObject.SetActive(items.Length == 0);
            const int columns = 4;
            const float tileSize = 128f, stride = 140f;
            _content.sizeDelta = new Vector2(0, Mathf.Max(360, Mathf.CeilToInt(items.Length / (float)columns) * stride + 8));
            for (int index = 0; index < items.Length; index++)
            {
                var item = items[index];
                var definition = _controller.FindGearDefinition(item.GearId);
                var button = SelectionUi.Button(_content, "Inventory Gear " + item.InstanceId, "", Vector2.zero,
                    Vector2.one * tileSize, () => OpenDetails(item));
                BackpackPrototypeController.SetRect((RectTransform)button.transform, new Vector2(.5f, 1),
                    new Vector2((index % columns - 1.5f) * stride, -72f - index / columns * stride), Vector2.one * tileSize);
                PrototypeGearBadge.Render(button.transform, definition, item, equippedMarkerSize: EquippedMarkerSize);
            }
            _scroll.verticalNormalizedPosition = scrollPosition;
        }

        private void OpenDetails(UniqueGearInstance item)
        {
            if (item == null || IsDetailsOpen) return;
            var definition = _controller.FindGearDefinition(item.GearId);
            if (definition == null) return;
            _selectedId = item.InstanceId;
            bool equippedHere = item.EquippedBy == CharacterId;
            bool slotOccupied = _controller.Progress.GetEquippedGear(CharacterId, item.Slot) != null;
            _equip.gameObject.SetActive(!equippedHere);
            _unequip.gameObject.SetActive(equippedHere);
            _equip.interactable = CharacterId != null;
            var owner = FindOwner(item);
            var stats = _controller.GetCharacterStats(owner ?? _controller.SelectedCharacter);
            string targets = string.Join(" · ", new[] { definition.BonusHealth ? "체력" : null,
                definition.BonusAttack ? "공격력" : null, definition.BonusCriticalDamage ? "치명타 피해" : null }
                .Where(label => label != null));
            string bonus = targets.Length == 0 ? "" : $"\n{targets} +{UniqueGearData.BonusPercent(item.Rarity, item.Enhancement):0.##}%";
            string availability = !equippedHere && slotOccupied ? " · 장착 시 기존 장비 교체" : "";
            string notes = $"\n\n{SlotLabel(item.Slot)} · {OwnerLabel(item)}{availability}{bonus}";
            var content = ItemTooltipContent.FromItem(definition.Item, stats, item.Rarity, definition.EffectFor(item.Rarity));
            ShowError("");
            _modal.gameObject.SetActive(true);
            _modal.SetAsLastSibling();
            _details.ShowPinned(content, $" +{item.Enhancement}", notes, 60f);
            EventSystem.current?.SetSelectedGameObject(null);
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
            _transferOwner = item.EquippedBy;
            _transferModal.gameObject.SetActive(true);
            _transferModal.SetAsLastSibling();
            _details.GetComponent<CanvasGroup>().interactable = false;
            EventSystem.current?.SetSelectedGameObject(null);
        }

        private void ConfirmTransfer()
        {
            if (!_transferModal.gameObject.activeSelf || SelectedGear() is not { } item) return;
            string expectedOwner = _transferOwner;
            CloseTransfer();
            _controller.TryEquipUniqueGear(item.Slot, item.InstanceId, expectedOwner);
        }

        private void CloseTransfer()
        {
            if (_transferModal.gameObject.activeSelf) EventSystem.current?.SetSelectedGameObject(null);
            _transferOwner = null;
            _transferModal.gameObject.SetActive(false);
            _details.GetComponent<CanvasGroup>().interactable = true;
        }

        private CharacterData FindOwner(UniqueGearInstance item)
            => _controller.AvailableCharacters.FirstOrDefault(character => character.CharacterId == item.EquippedBy);

        private string OwnerLabel(UniqueGearInstance item) => string.IsNullOrEmpty(item.EquippedBy) ? "미장착"
            : (FindOwner(item)?.DisplayName ?? "다른 캐릭터") + " 장착";

        internal static string SlotLabel(UniqueGearSlot slot) => slot switch
        {
            UniqueGearSlot.Weapon => "무기",
            UniqueGearSlot.Armor => "방어구",
            _ => "장신구 / 펫"
        };
    }
}
