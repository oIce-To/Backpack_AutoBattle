using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    internal sealed class PrototypeItemCompendium
    {
        private const int Columns = 8;
        private const float Pitch = 68f;
        private static readonly StringComparer KoreanNameOrder = StringComparer.Create(CultureInfo.GetCultureInfo("ko-KR"), false);
        private readonly PrototypeMenuPopup _popup;
        private readonly ScrollRect _grid, _details;
        private readonly PrototypeItemTooltip _itemDetails;
        private readonly Dictionary<string, Outline> _selection = new(StringComparer.Ordinal);
        private readonly List<GameObject> _detailRows = new();
        private IReadOnlyList<FusionRecipeData> _recipes;
        private HashSet<string> _purchasable;
        private PlayerProgress _profile;
        private CharacterStatsSnapshot _stats;
        private Outline _selected;

        internal PrototypeItemCompendium(Transform canvas, Action close)
        {
            _popup = new PrototypeMenuPopup(canvas, "Item Compendium Popup", "도감", new Vector2(1100, 610),
                close, PrototypeMenuPopup.CloseStyle.Pixel);
            PrototypeGuildStyle.Apply(_popup.Panel.GetComponent<Image>());
            _popup.Title.color = PrototypeGuildStyle.Cream;
            _grid = CreateScroll("Compendium Grid", new Vector2(-248, -29), new Vector2(562, 478));
            _details = CreateScroll("Compendium Details", new Vector2(267, -29), new Vector2(436, 478));
            _itemDetails = PrototypeItemTooltip.Create(_details.content);
            _itemDetails.name = "Compendium Item Details";
        }

        internal bool IsOpen => _popup.IsOpen;
        internal void Hide() => _popup.Hide();

        internal void Show(PlayerProgress profile, CharacterStatsSnapshot stats,
            IReadOnlyList<ItemData> shopItems, IReadOnlyList<FusionRecipeData> recipes)
        {
            _profile = profile;
            _stats = stats;
            _recipes = recipes.Where(recipe => recipe != null && recipe.ValidationError == null).ToArray();
            var shop = shopItems.Where(item => item != null && !item.ExcludeFromShop && item.Rarity != ItemRarity.Legendary).ToArray();
            _purchasable = new HashSet<string>(shop.Select(item => item.ItemId), StringComparer.Ordinal);
            var items = shop.Concat(_recipes.Select(recipe => recipe.ResultItem)).GroupBy(item => item.ItemId, StringComparer.Ordinal)
                .Select(group => group.First()).OrderBy(item => item.Rarity).ThenBy(item => !_purchasable.Contains(item.ItemId))
                .ThenBy(item => CategoryOrder(item.Category)).ThenBy(item => item.DisplayName, KoreanNameOrder)
                .ThenBy(item => item.ItemId, StringComparer.Ordinal).ToArray();

            foreach (Transform tile in _grid.content) Remove(tile.gameObject);
            _selection.Clear();
            _selected = null;
            for (int i = 0; i < items.Length; i++)
            {
                ItemData item = items[i];
                var tile = ItemTile(_grid.content, item, $"Compendium Item {item.ItemId}", 50);
                PlaceFromTop(tile.rectTransform, (i % Columns - 3.5f) * Pitch, 10 + i / Columns * Pitch, new Vector2(60, 60));
                var outline = tile.gameObject.AddComponent<Outline>();
                outline.effectColor = PrototypeGuildStyle.Cream;
                outline.effectDistance = new Vector2(2, 2);
                outline.enabled = false;
                _selection.Add(item.ItemId, outline);
            }
            _popup.Show();
            _grid.content.sizeDelta = new Vector2(0, Mathf.Max(_grid.viewport.rect.height,
                18 + Mathf.CeilToInt(items.Length / (float)Columns) * Pitch));
            ResetScroll(_grid);
            Select(items.FirstOrDefault());
        }

        private void Select(ItemData item)
        {
            if (_selected != null) _selected.enabled = false;
            _selected = item != null && _selection.TryGetValue(item.ItemId, out var outline) ? outline : null;
            if (_selected != null) _selected.enabled = true;
            foreach (GameObject row in _detailRows) Remove(row);
            _detailRows.Clear();
            _itemDetails.HideAll();

            float top = 13f;
            if (item == null)
            {
                AddDetailLabel("Compendium Empty", "등록된 아이템이 없습니다.", 18, top, 32, alignment: TextAnchor.MiddleCenter);
            }
            else
            {
                bool locked = IsLocked(item);
                if (locked)
                {
                    var silhouette = SelectionUi.Image(_details.content, "Compendium Unknown Silhouette", new Color(.34f, .37f, .40f));
                    _detailRows.Add(silhouette.gameObject);
                    PlaceFromTop(silhouette.rectTransform, 0, top, new Vector2(74, 74));
                    AddArtwork(silhouette.transform, item, true, 66);
                    AddDetailLabel("Compendium Unknown Name", "???", 20, top + 78, 26, alignment: TextAnchor.MiddleCenter);
                    top += 114;
                }
                else
                {
                    string price = _purchasable.Contains(item.ItemId) ? $"\n\n가격 : {item.PurchasePrice}G" : "";
                    _itemDetails.ShowEmbedded(ItemTooltipContent.FromItem(item, _stats), price);
                    var rect = _itemDetails.RectTransform;
                    PlaceFromTop(rect, 0, top, rect.sizeDelta);
                    top += rect.sizeDelta.y + 14;
                }

                AddRecipeGroup("이 아이템을 만드는 조합", _recipes.Where(recipe => recipe.ResultItem.ItemId == item.ItemId), ref top);
                AddRecipeGroup("이 아이템을 재료로 쓰는 조합", _recipes.Where(recipe => recipe.Contains(item)), ref top);
            }

            _details.content.sizeDelta = new Vector2(0, Mathf.Max(_details.viewport.rect.height, top + 12));
            ResetScroll(_details);
        }

        private void AddRecipeGroup(string title, IEnumerable<FusionRecipeData> matching, ref float top)
        {
            var recipes = matching.ToArray();
            if (recipes.Length == 0) return;
            AddDetailLabel("Compendium Recipe Heading", title, 18, top, 28);
            top += 34;
            foreach (var recipe in recipes)
            {
                var nodes = new List<ItemData> { recipe.CenterItem };
                foreach (var ingredient in recipe.Ingredients)
                    nodes.AddRange(Enumerable.Repeat(ingredient.Item, ingredient.Quantity));
                nodes.Add(recipe.ResultItem);
                for (int start = 0; start < nodes.Count; start += 4)
                {
                    int count = Mathf.Min(nodes.Count - start, 4);
                    const float nodePitch = 100;
                    for (int i = 0; i < count; i++)
                    {
                        int index = start + i;
                        ItemData item = nodes[index];
                        float x = (i - (count - 1) * .5f) * nodePitch;
                        var card = ItemTile(_details.content, item, $"Compendium Recipe Item {item.ItemId}", 44, artworkOffsetY: 8);
                        _detailRows.Add(card.gameObject);
                        PlaceFromTop(card.rectTransform, x, top, new Vector2(82, 82));
                        var name = SelectionUi.Label(card.transform, "Recipe Name", IsLocked(item) ? "???" : item.DisplayName,
                            13, new Vector2(0, -26), new Vector2(78, 25));
                        name.resizeTextForBestFit = true;
                        name.resizeTextMinSize = 10;
                        name.resizeTextMaxSize = 13;
                        if (index > 0)
                            AddDetailLabel("Recipe Connector", index == nodes.Count - 1 ? "→" : "+",
                                16, top + 22, 22, x - nodePitch / 2, 18, TextAnchor.MiddleCenter);
                    }
                    top += 88;
                }
                top += 9;
            }
        }

        private Image ItemTile(Transform parent, ItemData item, string objectName, float artworkSize, float artworkOffsetY = 0)
        {
            bool locked = IsLocked(item);
            Image tile = SelectionUi.Image(parent, objectName, locked ? new Color(.34f, .37f, .40f) : SelectionUi.Idle);
            tile.raycastTarget = true;
            var button = tile.gameObject.AddComponent<PrototypeAudioButton>();
            button.targetGraphic = tile;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => Select(item));
            if (!locked) PrototypeGearBadge.RenderBorder(tile.transform, item.Rarity);
            AddArtwork(tile.transform, item, locked, artworkSize, artworkOffsetY);
            return tile;
        }

        private static void AddArtwork(Transform parent, ItemData item, bool locked, float size, float offsetY = 0)
        {
            Vector2 offset = Vector2.up * offsetY;
            if (item.InventorySprite != null)
            {
                var art = SelectionUi.Image(parent, "Compendium Artwork", locked ? Color.black : Color.white);
                art.sprite = item.InventorySprite;
                art.preserveAspect = true;
                SelectionUi.Place(art.rectTransform, offset, Vector2.one * size);
                return;
            }

            var cells = item.CreateShape().BaseCells;
            var dimensions = new Vector2(cells.Max(cell => cell.x) + 1, cells.Max(cell => cell.y) + 1);
            float pitch = size / Mathf.Max(dimensions.x, dimensions.y);
            foreach (var cell in cells)
            {
                var block = SelectionUi.Image(parent, "Compendium Shape Cell", locked ? Color.black : item.DisplayColor);
                var center = new Vector2(cell.x + .5f - dimensions.x / 2, dimensions.y / 2 - cell.y - .5f);
                SelectionUi.Place(block.rectTransform, offset + center * pitch, Vector2.one * (locked ? pitch : pitch - 1));
            }
        }

        private bool IsLocked(ItemData item) => !_purchasable.Contains(item.ItemId) && item.Rarity >= ItemRarity.Epic
            && !_profile.HasCraftedStageItem(item.ItemId);

        private static int CategoryOrder(ItemCategory category) => category switch
        {
            ItemCategory.Weapon => 0,
            ItemCategory.Armor => 1,
            ItemCategory.Accessory or ItemCategory.Pet => 2,
            _ => 3
        };

        private ScrollRect CreateScroll(string name, Vector2 center, Vector2 size)
        {
            var background = SelectionUi.Image(_popup.Panel, name + " Panel", Color.white);
            SelectionUi.Place(background.rectTransform, center, size);
            PrototypeGuildStyle.Apply(background, PrototypeGuildStyle.Surface.Iron);
            var viewport = SelectionUi.Image(background.transform, name + " Viewport", Color.clear);
            BackpackPrototypeController.Stretch(viewport.rectTransform, 5);
            viewport.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = new GameObject(name + " Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport.transform, false);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            content.anchoredPosition = Vector2.zero;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport.rectTransform;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 38;
            var track = SelectionUi.Image(background.transform, name + " Scrollbar", new Color(.08f, .10f, .11f));
            SelectionUi.Place(track.rectTransform, new Vector2(size.x / 2 - 8, 0), new Vector2(7, size.y - 18));
            var handle = SelectionUi.Image(track.transform, "Handle", new Color(.64f, .58f, .48f));
            BackpackPrototypeController.Stretch(handle.rectTransform);
            track.raycastTarget = handle.raycastTarget = true;
            var scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.targetGraphic = handle;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return scroll;
        }

        private void AddDetailLabel(string name, string text, int size, float top, float height, float x = 0, float width = 390,
            TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var label = SelectionUi.Label(_details.content, name, text, size, Vector2.zero, new Vector2(width, height));
            label.color = PrototypeGuildStyle.Cream;
            label.alignment = alignment;
            PlaceFromTop(label.rectTransform, x, top, new Vector2(width, height));
            _detailRows.Add(label.gameObject);
        }

        private static void PlaceFromTop(RectTransform rect, float x, float top, Vector2 size)
        {
            BackpackPrototypeController.SetRect(rect, new Vector2(.5f, 1), new Vector2(x, -top - size.y / 2), size);
        }

        private static void ResetScroll(ScrollRect scroll)
        {
            scroll.StopMovement();
            scroll.content.anchoredPosition = Vector2.zero;
        }

        private static void Remove(GameObject row)
        {
            row.SetActive(false);
            UnityEngine.Object.Destroy(row);
        }
    }
}
