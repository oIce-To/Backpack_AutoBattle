using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed class BackpackPrototypeController : MonoBehaviour
    {
        public const int GridSize = 6;
        public const float CellPitch = 74f;
        public const float ItemCellSize = 68f;

        private static readonly Color GridColorA = new(0.16f, 0.19f, 0.24f, 1f);
        private static readonly Color GridColorB = new(0.19f, 0.22f, 0.28f, 1f);
        private static readonly Color ValidPreviewColor = new(0.20f, 0.72f, 0.39f, 0.95f);
        private static readonly Color InvalidPreviewColor = new(0.88f, 0.25f, 0.28f, 0.95f);

        [SerializeField] private ItemData[] _itemCatalog;
        [SerializeField, Min(1)] private int _shopSlotCount = 4;

        private readonly Dictionary<string, BackpackItemView> _itemViews = new();
        private readonly List<PrototypeShopSlotView> _shopSlots = new();
        private readonly List<ItemData> _currentShopItems = new();
        private BackpackGrid _grid;
        private System.Random _shopRandom;
        private RectTransform _gridRoot;
        private Image[,] _gridCells;
        private Text _statusText;
        private BackpackItemView _selectedItem;
        private BackpackItemView _draggingItem;
        private Vector2 _dragOffset;
        private Vector2Int _dragOrigin;
        private ItemRotation _dragRotation;
        private Vector2Int _previewOrigin;

        private float BoardSize => GridSize * CellPitch;

        public IReadOnlyList<ItemData> ItemCatalog => _itemCatalog;

        public IReadOnlyList<ItemData> CurrentShopItems => _currentShopItems;

        private void Awake()
        {
            _grid = new BackpackGrid(GridSize, GridSize);
            _shopRandom = new System.Random(System.Environment.TickCount);
            BuildInterface();
            CreateStartingItems();
            RefreshShop();
            SetStatus("아이템을 선택하세요. 좌클릭 드래그: 이동 / 우클릭 또는 R: 회전");
        }

        private void Update()
        {
            if (_selectedItem != null && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                RotateItem(_selectedItem);
            }
        }

        public void SelectItem(BackpackItemView itemView)
        {
            _selectedItem = itemView;
            string[] adjacentNames = _grid.GetAdjacentItemIds(itemView.InstanceId)
                .Select(id => _itemViews[id].DisplayName)
                .ToArray();
            string adjacency = adjacentNames.Length == 0 ? "없음" : string.Join(", ", adjacentNames);
            SetStatus($"선택: {itemView.DisplayName}  |  인접 아이템: {adjacency}");
        }

        public void BeginDrag(BackpackItemView itemView, PointerEventData eventData)
        {
            SelectItem(itemView);
            _draggingItem = itemView;
            _dragOrigin = itemView.Origin;
            _dragRotation = itemView.Rotation;
            itemView.transform.SetAsLastSibling();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _gridRoot,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 pointerPosition);
            _dragOffset = pointerPosition - itemView.RectTransform.anchoredPosition;
        }

        public void Drag(BackpackItemView itemView, PointerEventData eventData)
        {
            if (_draggingItem != itemView)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _gridRoot,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 pointerPosition);

            Vector2 proposedPosition = pointerPosition - _dragOffset;
            itemView.RectTransform.anchoredPosition = proposedPosition;
            _previewOrigin = LocalPositionToGrid(proposedPosition);
            UpdatePlacementPreview(itemView, _previewOrigin);
        }

        public void EndDrag(BackpackItemView itemView)
        {
            if (_draggingItem != itemView)
            {
                return;
            }

            bool placed = _grid.TryPlace(itemView.InstanceId, itemView.Shape, _previewOrigin, itemView.Rotation);
            if (placed)
            {
                itemView.SetPlacement(_previewOrigin, itemView.Rotation);
                SetStatus($"{itemView.DisplayName} 배치 완료: ({_previewOrigin.x}, {_previewOrigin.y})");
            }
            else
            {
                itemView.SetPlacement(_dragOrigin, _dragRotation);
                SetStatus("놓을 수 없는 위치입니다. 아이템을 원래 자리로 돌려놓았습니다.");
            }

            itemView.ResetTint();
            ResetGridColors();
            _draggingItem = null;
        }

        public void RotateItem(BackpackItemView itemView)
        {
            if (!itemView.Data.CanRotate)
            {
                SetStatus($"{itemView.DisplayName}은(는) 회전할 수 없는 아이템입니다.");
                return;
            }

            ItemRotation nextRotation = NextRotation(itemView.Rotation);

            if (_draggingItem == itemView)
            {
                itemView.SetPreviewRotation(nextRotation);
                _previewOrigin = LocalPositionToGrid(itemView.RectTransform.anchoredPosition);
                UpdatePlacementPreview(itemView, _previewOrigin);
                return;
            }

            if (_grid.TryPlace(itemView.InstanceId, itemView.Shape, itemView.Origin, nextRotation))
            {
                itemView.SetPlacement(itemView.Origin, nextRotation);
                SetStatus($"{itemView.DisplayName} 회전 완료");
                SelectItem(itemView);
                return;
            }

            SetStatus("현재 위치에서는 회전할 공간이 부족합니다.");
            StartCoroutine(FlashInvalid(itemView));
        }

        public void PositionItem(BackpackItemView itemView)
        {
            itemView.RectTransform.anchoredPosition = GridToLocalPosition(itemView.Origin);
        }

        public void RefreshShop()
        {
            _currentShopItems.Clear();

            if (_itemCatalog == null || _itemCatalog.Length == 0)
            {
                SetStatus("상점에 표시할 ItemData가 없습니다.");
                return;
            }

            List<ItemData> candidates = _itemCatalog
                .Where(itemData => itemData != null)
                .ToList();

            for (int index = candidates.Count - 1; index > 0; index--)
            {
                int swapIndex = _shopRandom.Next(index + 1);
                (candidates[index], candidates[swapIndex]) = (candidates[swapIndex], candidates[index]);
            }

            int visibleCount = Mathf.Min(_shopSlots.Count, candidates.Count);
            for (int index = 0; index < _shopSlots.Count; index++)
            {
                bool hasItem = index < visibleCount;
                _shopSlots[index].gameObject.SetActive(hasItem);

                if (!hasItem)
                {
                    continue;
                }

                ItemData itemData = candidates[index];
                _currentShopItems.Add(itemData);
                _shopSlots[index].Bind(itemData);
            }

            SetStatus($"상점 갱신 완료: {_currentShopItems.Count}개의 아이템이 무작위로 진열되었습니다.");
        }

        public void InspectShopItem(ItemData itemData)
        {
            SetStatus($"상점 아이템: {itemData.DisplayName}  |  {itemData.PurchasePrice} G  |  {itemData.Description}");
        }

        public static Text CreateText(
            Transform parent,
            string objectName,
            string content,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment)
        {
            var textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = new Color(0.91f, 0.94f, 0.98f, 1f);
            return text;
        }

        private void BuildInterface()
        {
            var eventSystemObject = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventSystemObject.transform.SetParent(transform, false);

            var canvasObject = new GameObject(
                "Prototype Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            Image background = CreateImage(canvasObject.transform, "Background", new Color(0.055f, 0.07f, 0.10f, 1f));
            Stretch(background.rectTransform);

            Text title = CreateText(
                canvasObject.transform,
                "Title",
                "BACKPACK SHOP & PLACEMENT PROTOTYPE",
                30,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(720f, 52f));

            Text instructions = CreateText(
                canvasObject.transform,
                "Instructions",
                "좌클릭 드래그: 이동    |    우클릭 / R: 회전    |    초록색: 배치 가능    |    빨간색: 배치 불가",
                18,
                FontStyle.Normal,
                TextAnchor.MiddleCenter);
            instructions.color = new Color(0.67f, 0.73f, 0.82f, 1f);
            SetRect(instructions.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(980f, 38f));

            BuildShop(canvasObject.transform);

            Image boardFrame = CreateImage(canvasObject.transform, "Board Frame", new Color(0.08f, 0.10f, 0.14f, 1f));
            SetRect(boardFrame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(170f, -8f), new Vector2(BoardSize + 28f, BoardSize + 28f));
            boardFrame.raycastTarget = false;

            var gridObject = new GameObject("Backpack Grid", typeof(RectTransform));
            gridObject.transform.SetParent(canvasObject.transform, false);
            _gridRoot = (RectTransform)gridObject.transform;
            SetRect(_gridRoot, new Vector2(0.5f, 0.5f), new Vector2(170f, -8f), Vector2.one * BoardSize);

            _gridCells = new Image[GridSize, GridSize];
            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    Image cell = CreateImage(
                        _gridRoot,
                        $"Grid Cell {x},{y}",
                        (x + y) % 2 == 0 ? GridColorA : GridColorB);
                    RectTransform cellRect = cell.rectTransform;
                    cellRect.anchorMin = new Vector2(0.5f, 0.5f);
                    cellRect.anchorMax = new Vector2(0.5f, 0.5f);
                    cellRect.pivot = new Vector2(0.5f, 0.5f);
                    cellRect.sizeDelta = Vector2.one * ItemCellSize;
                    cellRect.anchoredPosition = new Vector2(
                        -BoardSize / 2f + CellPitch / 2f + x * CellPitch,
                        BoardSize / 2f - CellPitch / 2f - y * CellPitch);
                    cell.raycastTarget = false;
                    _gridCells[x, y] = cell;
                }
            }

            Image statusPanel = CreateImage(canvasObject.transform, "Status Panel", new Color(0.10f, 0.13f, 0.18f, 1f));
            SetRect(statusPanel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(1160f, 54f));
            statusPanel.raycastTarget = false;

            _statusText = CreateText(statusPanel.transform, "Status", string.Empty, 18, FontStyle.Normal, TextAnchor.MiddleCenter);
            Stretch(_statusText.rectTransform, 14f);
        }

        private void CreateStartingItems()
        {
            CreateStartingItem("training_blade", "starter_training_blade", new Vector2Int(0, 0));
            CreateStartingItem("wooden_shield", "starter_wooden_shield", new Vector2Int(1, 0));
            CreateStartingItem("swift_charm", "starter_swift_charm", new Vector2Int(3, 3));
        }

        private void CreateStartingItem(string itemId, string instanceId, Vector2Int origin)
        {
            ItemData itemData = _itemCatalog?.FirstOrDefault(data => data != null && data.ItemId == itemId);
            if (itemData == null)
            {
                Debug.LogError($"Starting ItemData was not found: {itemId}");
                return;
            }

            ItemShape shape = itemData.CreateShape();
            if (!_grid.TryPlace(instanceId, shape, origin))
            {
                Debug.LogError($"Could not place prototype item: {instanceId}");
                return;
            }

            var itemObject = new GameObject(itemData.DisplayName, typeof(RectTransform), typeof(BackpackItemView));
            itemObject.transform.SetParent(_gridRoot, false);
            var itemView = itemObject.GetComponent<BackpackItemView>();
            itemView.Initialize(this, instanceId, itemData, origin, ItemRotation.None);
            _itemViews.Add(instanceId, itemView);
            PositionItem(itemView);
        }

        private void BuildShop(Transform canvasTransform)
        {
            Image shopPanel = CreateImage(canvasTransform, "Prototype Shop", new Color(0.08f, 0.10f, 0.14f, 1f));
            SetRect(shopPanel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-405f, -8f), new Vector2(310f, 472f));

            Text shopTitle = CreateText(shopPanel.transform, "Shop Title", "임시 상점", 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(shopTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(280f, 42f));

            Text shopHint = CreateText(shopPanel.transform, "Shop Hint", "카드를 누르면 ItemData 정보를 확인합니다", 13, FontStyle.Normal, TextAnchor.MiddleCenter);
            shopHint.color = new Color(0.60f, 0.67f, 0.76f, 1f);
            SetRect(shopHint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -61f), new Vector2(280f, 26f));

            for (int index = 0; index < _shopSlotCount; index++)
            {
                var slotObject = new GameObject($"Shop Slot {index + 1}", typeof(RectTransform));
                slotObject.transform.SetParent(shopPanel.transform, false);
                var slotRect = (RectTransform)slotObject.transform;
                SetRect(
                    slotRect,
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -106f - index * 76f),
                    new Vector2(278f, 66f));
                var slotView = slotObject.AddComponent<PrototypeShopSlotView>();
                slotView.Initialize(this, index);
                _shopSlots.Add(slotView);
            }

            var rerollObject = new GameObject("Reroll Button", typeof(RectTransform), typeof(Image), typeof(Button));
            rerollObject.transform.SetParent(shopPanel.transform, false);
            var rerollRect = (RectTransform)rerollObject.transform;
            SetRect(rerollRect, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(278f, 44f));
            rerollObject.GetComponent<Image>().color = new Color(0.20f, 0.42f, 0.66f, 1f);
            rerollObject.GetComponent<Button>().onClick.AddListener(RefreshShop);

            Text rerollText = CreateText(rerollObject.transform, "Label", "리롤 (무료 테스트)", 17, FontStyle.Bold, TextAnchor.MiddleCenter);
            rerollText.raycastTarget = false;
            Stretch(rerollText.rectTransform);
        }

        private void UpdatePlacementPreview(BackpackItemView itemView, Vector2Int origin)
        {
            bool valid = _grid.CanPlace(itemView.InstanceId, itemView.Shape, origin, itemView.Rotation);
            Color previewColor = valid ? ValidPreviewColor : InvalidPreviewColor;
            itemView.SetTint(previewColor);
            ResetGridColors();

            foreach (Vector2Int localCell in itemView.Shape.GetCells(itemView.Rotation))
            {
                Vector2Int worldCell = origin + localCell;
                if (_grid.IsInside(worldCell))
                {
                    _gridCells[worldCell.x, worldCell.y].color = previewColor;
                }
            }
        }

        private void ResetGridColors()
        {
            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    _gridCells[x, y].color = (x + y) % 2 == 0 ? GridColorA : GridColorB;
                }
            }
        }

        private Vector2 GridToLocalPosition(Vector2Int origin)
        {
            return new Vector2(
                -BoardSize / 2f + origin.x * CellPitch + (CellPitch - ItemCellSize) / 2f,
                BoardSize / 2f - origin.y * CellPitch - (CellPitch - ItemCellSize) / 2f);
        }

        private Vector2Int LocalPositionToGrid(Vector2 localPosition)
        {
            float inset = (CellPitch - ItemCellSize) / 2f;
            int x = Mathf.RoundToInt((localPosition.x + BoardSize / 2f - inset) / CellPitch);
            int y = Mathf.RoundToInt((BoardSize / 2f - inset - localPosition.y) / CellPitch);
            return new Vector2Int(x, y);
        }

        private static ItemRotation NextRotation(ItemRotation rotation)
        {
            return (ItemRotation)(((int)rotation + 1) % 4);
        }

        private IEnumerator FlashInvalid(BackpackItemView itemView)
        {
            itemView.SetTint(InvalidPreviewColor);
            yield return new WaitForSecondsRealtime(0.22f);
            itemView.ResetTint();
        }

        private void SetStatus(string message)
        {
            _statusText.text = message;
        }

        private static Image CreateImage(Transform parent, string objectName, Color color)
        {
            var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset;
            rect.offsetMax = Vector2.one * -inset;
        }
    }
}
