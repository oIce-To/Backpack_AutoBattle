using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Shop;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed partial class BackpackPrototypeController : MonoBehaviour
    {
        public const int GridSize = 6;
        public const float CellPitch = 54f;
        public const float ItemCellSize = 48f;

        private static readonly Color GridColorA = new(0.16f, 0.19f, 0.24f, 1f);
        private static readonly Color GridColorB = new(0.19f, 0.22f, 0.28f, 1f);
        private static readonly Color ValidPreviewColor = new(0.20f, 0.72f, 0.39f, 0.95f);
        private static readonly Color InvalidPreviewColor = new(0.88f, 0.25f, 0.28f, 0.95f);

        [Header("Content")]
        [SerializeField] private ItemData[] _itemCatalog;
        [SerializeField] private ShopRarityTableData _shopRarityTable;

        [Header("Shop Rules")]
        [SerializeField, Min(1)] private int _shopSlotCount = 4;
        [SerializeField, Min(0)] private int _startingGold = 20;
        [SerializeField, Min(0)] private int _rerollCost = 1;
        [SerializeField, Min(0)] private int _roundIncome = 5;
        [SerializeField, Range(0f, 1f)] private float _saleChance = 0.10f;

        private readonly Dictionary<string, BackpackItemView> _itemViews = new();
        private readonly Dictionary<string, PrototypeStorageItemView> _storageViews = new();
        private readonly List<PrototypeShopSlotView> _shopSlots = new();
        private readonly List<ItemData> _currentShopItems = new();
        private readonly List<PrototypeStoredItem> _storedItems = new();
        private BackpackGrid _grid;
        private System.Random _shopRandom;
        private RectTransform _gridRoot;
        private RectTransform _storageRoot;
        private RectTransform _sellArea;
        private Image _sellPreview;
        private Text _sellPreviewText;
        private Image[,] _gridCells;
        private Text _goldText;
        private Text _roundText;
        private Text _rarityOddsText;
        private Text _rerollText;
        private Text _nextRoundText;
        private BackpackItemView _selectedItem;
        private BackpackItemView _draggingItem;
        private PrototypeStorageItemView _draggingStorageView;
        private DragSource _dragSource;
        private PrototypeShopSlotView _draggingShopSlot;
        private ShopOffer _draggingShopOffer;
        private Vector2 _dragOffset;
        private Vector2Int _dragOrigin;
        private ItemRotation _dragRotation;
        private Vector2Int _previewOrigin;

        private float BoardSize => GridSize * CellPitch;

        public IReadOnlyList<ItemData> ItemCatalog => _itemCatalog;
        public IReadOnlyList<ItemData> CurrentShopItems => _currentShopItems;
        public ShopRarityTableData ShopRarityTable => _shopRarityTable;
        public int Gold => _run.Gold;
        public int CurrentRound => _run?.Round ?? 1;
        public string CurrentStageId => _run?.StageId ?? _stageData?.StageId;
        public int StoredItemCount => _storedItems.Count;

        private void Awake()
        {
            _grid = new BackpackGrid(GridSize, GridSize);
            _shopRandom = new System.Random(System.Environment.TickCount);
            _run = CreateRunState();
            BuildInterface();
            InitializeRunContent();
        }

        private void Update()
        {
            // A slow screen-switch frame must not skip most of the visible fusion sequence.
            if (AutoTickFusionPresentation) TickFusionPresentation(Mathf.Min(Time.unscaledDeltaTime, .05f));
            if (CanEditLoadout && _selectedItem != null && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                RotateItem(_selectedItem);
            }
        }

        public void SelectItem(BackpackItemView itemView)
        {
            if (!CanEditLoadout) return;
            _selectedItem = itemView;
        }

        public void BeginDrag(BackpackItemView itemView, PointerEventData eventData)
        {
            if (!CanEditLoadout || _draggingItem != null)
            {
                return;
            }

            SelectItem(itemView);
            _itemTooltip.HideAll();
            _draggingItem = itemView;
            _dragSource = DragSource.Backpack;
            _dragOrigin = itemView.Origin;
            _dragRotation = itemView.Rotation;
            _gridRoot.SetAsLastSibling(); // Dragged shapes must stay above the enlarged storage panel.
            itemView.transform.SetAsLastSibling();
            SetDragOffset(itemView, eventData);
            _previewOrigin = itemView.Origin;
            _dragRangeInsideGrid = IsPointerInside(_gridRoot, eventData);
            ClearPlacementOverlay();
            RefreshDragPlacementOverlay();
            UpdateSellPreview(eventData);
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
            _dragRangeInsideGrid = IsPointerInside(_gridRoot, eventData);
            UpdatePlacementPreview(itemView, _previewOrigin);
            UpdateSellPreview(eventData);
        }

        public void EndDrag(BackpackItemView itemView, PointerEventData eventData)
        {
            if (_draggingItem != itemView)
            {
                return;
            }

            if (_dragSource != DragSource.Shop && IsPointerInside(_sellArea, eventData))
            {
                SellDraggedItem(itemView);
                FinishDrag();
                return;
            }

            if (IsPointerInside(_storageRoot, eventData))
            {
                StoreDraggedItem(itemView, eventData);
                FinishDrag();
                return;
            }

            bool canAfford = _dragSource != DragSource.Shop || CanPurchaseDraggedOffer();
            bool placed = canAfford && IsPointerInside(_gridRoot, eventData) && _grid.TryPlace(
                itemView.InstanceId,
                itemView.Shape,
                _previewOrigin,
                itemView.Rotation);

            if (placed)
            {
                itemView.SetPlacement(_previewOrigin, itemView.Rotation);
                CompleteGridPlacement(itemView);
            }
            else
            {
                CancelGridPlacement(itemView, canAfford);
            }

            FinishDrag();
        }

        public void BeginShopDrag(PrototypeShopSlotView slotView, PointerEventData eventData)
        {
            if (!CanEditLoadout || !slotView.CanPurchase || _draggingItem != null)
            {
                return;
            }

            ItemData itemData = slotView.ItemData;
            _itemTooltip.HideAll();
            string instanceId = CreatePurchasedInstanceId(itemData);
            BackpackItemView itemView = CreateDragProxy(instanceId, itemData, eventData);
            _draggingItem = itemView;
            _selectedItem = itemView;
            _dragSource = DragSource.Shop;
            _draggingShopSlot = slotView;
            _draggingShopOffer = slotView.Offer;
            _dragRotation = ItemRotation.None;
            SetDragOffsetFromPointer();
            Drag(itemView, eventData);
            SetStatus($"{itemData.DisplayName}: 가방 또는 자유 보관함에 놓으면 {_draggingShopOffer.PurchasePrice} G로 구매합니다.");
        }

        public void DragShopItem(PrototypeShopSlotView slotView, PointerEventData eventData)
        {
            if (_dragSource == DragSource.Shop && slotView == _draggingShopSlot)
            {
                Drag(_draggingItem, eventData);
            }
        }

        public void EndShopDrag(PrototypeShopSlotView slotView, PointerEventData eventData)
        {
            if (_dragSource == DragSource.Shop
                && slotView == _draggingShopSlot
                && _draggingItem != null)
            {
                EndDrag(_draggingItem, eventData);
            }
        }

        public void BeginStorageDrag(PrototypeStorageItemView storageView, PointerEventData eventData)
        {
            if (!CanEditLoadout || storageView.StoredItem == null || _draggingItem != null)
            {
                return;
            }

            PrototypeStoredItem storedItem = storageView.StoredItem;
            _itemTooltip.HideAll();
            BackpackItemView itemView = CreateDragProxy(storedItem.InstanceId, storedItem.ItemData, eventData, storedItem.Rotation);
            _itemViews[storedItem.InstanceId] = itemView;
            _draggingItem = itemView;
            _selectedItem = itemView;
            _draggingStorageView = storageView;
            _dragSource = DragSource.Storage;
            _dragRotation = storedItem.Rotation;
            storageView.SetDragging(true);
            SetDragOffsetFromPointer();
            Drag(itemView, eventData);
        }

        public void DragStorageItem(PrototypeStorageItemView storageView, PointerEventData eventData)
        {
            if (_dragSource == DragSource.Storage && storageView == _draggingStorageView)
            {
                Drag(_draggingItem, eventData);
            }
        }

        public void EndStorageDrag(PrototypeStorageItemView storageView, PointerEventData eventData)
        {
            if (_dragSource == DragSource.Storage
                && storageView == _draggingStorageView
                && _draggingItem != null)
            {
                EndDrag(_draggingItem, eventData);
            }
        }

        public void RotateItem(BackpackItemView itemView)
        {
            if (!CanEditLoadout) return;
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
                RefreshPlacementHover();
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

        public bool TryRerollShop()
        {
            if (!CanEditLoadout || _draggingItem != null)
            {
                return false;
            }

            if (_run.Gold < _rerollCost)
            {
                SetStatus($"새로고침에 필요한 골드가 부족합니다. 필요: {_rerollCost} G");
                return false;
            }

            _run.Gold -= _rerollCost;
            PopulateShop();
            UpdateEconomyText();
            SetStatus($"상점을 새로고침했습니다. (-{_rerollCost} G)");
            return true;
        }

        public bool TryToggleShopLock(PrototypeShopSlotView slot)
        {
            if (!CanEditLoadout || _draggingItem != null || slot == null || !_shopSlots.Contains(slot)
                || !slot.CanPurchase || !slot.Offer.TryToggleLock()) return false;
            slot.RefreshLockVisual();
            return true;
        }

        public bool AdvanceRound()
        {
            if (_draggingItem != null || (_run.Phase != PrototypePhase.Victory
                && !(_enableDebugRoundAdvance && _run.Phase == PrototypePhase.Preparation)))
            {
                return false;
            }

            if (_run.Round >= StageRoundCount)
            {
                SetStatus("마지막 12라운드입니다.");
                return false;
            }

            // Resolve first so missing/invalid content cannot consume income or advance progress.
            if (!TryResolveEnemy(_run.Round + 1, out var nextEnemy, out string error))
            {
                SetStatus(error);
                _enemyConfigurationError = error;
                if (CanEditLoadout)
                {
                    _preparedEnemy = null;
                    RenderEncounterPreview();
                    if (_debugNextRoundButton != null) _debugNextRoundButton.interactable = false;
                }
                else _battleController.ShowConfigurationError(error);
                return false;
            }
            bool processFusions = _run.Phase == PrototypePhase.Victory;
            _run.Round++;
            _preparedEnemy = nextEnemy;
            _enemyConfigurationError = null;
            _run.Gold += _roundIncome;
            _run.Phase = PrototypePhase.Preparation;
            _battleController.EndBattle();
            _selectedItem = null;
            PopulateShop();
            UpdateEconomyText();
            if (processFusions) BeginFusionPresentation();
            RefreshCombatInterface();
            SetStatus($"{_run.Round}라운드 시작. 라운드 수입 +{_roundIncome} G");
            return true;
        }

        public void RefreshShopForTesting()
        {
            if (CanEditLoadout && _draggingItem == null)
            {
                PopulateShop();
            }
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
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.transform.SetParent(transform, false);

            var canvasObject = new GameObject(
                "Prototype Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            _itemTooltip = PrototypeItemTooltip.Create(canvasObject.transform);

            var shopScreen = new GameObject("Shop Screen", typeof(RectTransform));
            shopScreen.transform.SetParent(canvasObject.transform, false);
            _shopScreenRoot = (RectTransform)shopScreen.transform;
            Stretch(_shopScreenRoot);

            Image background = CreateImage(_shopScreenRoot, "Background", new Color(0.055f, 0.07f, 0.10f, 1f));
            Stretch(background.rectTransform);

            Text title = CreateText(_shopScreenRoot, "Title", "BACKPACK AUTO BATTLE PROTOTYPE", 28, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(720f, 46f));
            _livesText = CreateText(_shopScreenRoot, "Run Lives", "", 18, FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(_livesText.rectTransform, new Vector2(1f, 1f), new Vector2(-110f, -36f), new Vector2(170f, 30f));

            Text instructions = CreateText(
                _shopScreenRoot,
                "Instructions",
                "드래그: 구매/배치/판매  |  가방 우클릭/R: 회전  |  가방/보관함 Shift+우클릭: 합성 잠금",
                16,
                FontStyle.Normal,
                TextAnchor.MiddleCenter);
            instructions.color = new Color(0.67f, 0.73f, 0.82f, 1f);
            SetRect(instructions.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(980f, 30f));

            BuildShop(_shopScreenRoot);

            const float boardX = 140f;
            const float boardY = 94f;
            Image boardFrame = CreateImage(_shopScreenRoot, "Board Frame", new Color(0.08f, 0.10f, 0.14f, 1f));
            SetRect(boardFrame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(boardX, boardY), new Vector2(BoardSize + 28f, BoardSize + 28f));
            boardFrame.raycastTarget = false;

            var gridObject = new GameObject("Backpack Grid", typeof(RectTransform));
            gridObject.transform.SetParent(_shopScreenRoot, false);
            _gridRoot = (RectTransform)gridObject.transform;
            _gridInputGroup = gridObject.AddComponent<CanvasGroup>();
            SetRect(_gridRoot, new Vector2(0.5f, 0.5f), new Vector2(boardX, boardY), Vector2.one * BoardSize);

            _gridCells = new Image[GridSize, GridSize];
            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    Image cell = CreateImage(_gridRoot, $"Grid Cell {x},{y}", (x + y) % 2 == 0 ? GridColorA : GridColorB);
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

            BuildStorage(_shopScreenRoot);
            _preparationPanel = new PrototypePreparationPanel(this, _shopScreenRoot);
            InitializeBattleScreen(canvasObject.transform);
            _runResultScreen = new PrototypeRunResultScreen(canvasObject.transform, () => RequestRunRestart());

        }

        private void BuildShop(Transform canvasTransform)
        {
            Image shopPanel = CreateImage(canvasTransform, "Prototype Shop", new Color(0.08f, 0.10f, 0.14f, 1f));
            SetRect(shopPanel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-405f, 12f), new Vector2(310f, 510f));
            _sellArea = shopPanel.rectTransform;
            _shopInputGroup = shopPanel.gameObject.AddComponent<CanvasGroup>();

            Text shopTitle = CreateText(shopPanel.transform, "Shop Title", "상점", 23, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(shopTitle.rectTransform, new Vector2(0f, 1f), new Vector2(20f, -28f), new Vector2(90f, 38f));
            shopTitle.rectTransform.pivot = new Vector2(0f, 0.5f);

            _goldText = CreateText(shopPanel.transform, "Gold", string.Empty, 20, FontStyle.Bold, TextAnchor.MiddleRight);
            _goldText.color = new Color(1f, 0.80f, 0.24f, 1f);
            SetRect(_goldText.rectTransform, new Vector2(1f, 1f), new Vector2(-70f, -28f), new Vector2(120f, 38f));

            _roundText = CreateText(shopPanel.transform, "Round", string.Empty, 16, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(_roundText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(278f, 26f));

            _rarityOddsText = CreateText(shopPanel.transform, "Rarity Odds", string.Empty, 11, FontStyle.Normal, TextAnchor.MiddleCenter);
            _rarityOddsText.color = new Color(0.66f, 0.73f, 0.82f, 1f);
            SetRect(_rarityOddsText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(290f, 22f));

            for (int index = 0; index < _shopSlotCount; index++)
            {
                var slotObject = new GameObject($"Shop Slot {index + 1}", typeof(RectTransform));
                slotObject.transform.SetParent(shopPanel.transform, false);
                SetRect(
                    (RectTransform)slotObject.transform,
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -128f - index * 70f),
                    new Vector2(278f, 62f));
                PrototypeShopSlotView slotView = slotObject.AddComponent<PrototypeShopSlotView>();
                slotView.Initialize(this, index);
                _shopSlots.Add(slotView);
            }

            Button rerollButton = CreateButton(shopPanel.transform, "Reroll Button", new Vector2(-71f, 54f), new Vector2(136f, 40f), new Color(0.20f, 0.42f, 0.66f, 1f));
            rerollButton.onClick.AddListener(() => TryRerollShop());
            _rerollText = CreateText(rerollButton.transform, "Label", string.Empty, 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            _rerollText.raycastTarget = false;
            Stretch(_rerollText.rectTransform);

            Button nextRoundButton = CreateButton(shopPanel.transform, "Next Round Button", new Vector2(71f, 54f), new Vector2(136f, 40f), new Color(0.32f, 0.52f, 0.30f, 1f));
            _debugNextRoundButton = nextRoundButton;
            nextRoundButton.gameObject.SetActive(_enableDebugRoundAdvance);
            nextRoundButton.onClick.AddListener(() => AdvanceRound());
            _nextRoundText = CreateText(nextRoundButton.transform, "Label", string.Empty, 14, FontStyle.Bold, TextAnchor.MiddleCenter);
            _nextRoundText.raycastTarget = false;
            Stretch(_nextRoundText.rectTransform);

            Text shopHint = CreateText(shopPanel.transform, "Shop Hint", "상점 상품 우클릭: 잠금 / 해제", 12, FontStyle.Normal, TextAnchor.MiddleCenter);
            shopHint.color = new Color(0.56f, 0.63f, 0.73f, 1f);
            SetRect(shopHint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 19f), new Vector2(278f, 20f));

            _sellPreview = CreateImage(shopPanel.transform, "Sell Preview", new Color(0.16f, 0.23f, 0.32f, 0.96f));
            Stretch(_sellPreview.rectTransform);
            _sellPreview.raycastTarget = false;
            _sellPreviewText = CreateText(_sellPreview.transform, "Sell Price", string.Empty, 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            _sellPreviewText.raycastTarget = false;
            Stretch(_sellPreviewText.rectTransform, 16f);
            _sellPreview.gameObject.SetActive(false);
        }

        private void BuildStorage(Transform canvasTransform)
        {
            Image storagePanel = CreateImage(canvasTransform, "Freeform Storage", new Color(0.08f, 0.10f, 0.14f, 1f));
            SetRect(storagePanel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(75f, -192f), new Vector2(610f, 208f));
            _storageRoot = storagePanel.rectTransform;
            _storageInputGroup = storagePanel.gameObject.AddComponent<CanvasGroup>();

            Text storageLabel = CreateText(storagePanel.transform, "Storage Label", "자유 보관함 · 개수 제한 없음", 13, FontStyle.Bold, TextAnchor.UpperLeft);
            storageLabel.color = new Color(0.58f, 0.66f, 0.77f, 1f);
            storageLabel.raycastTarget = false;
            SetRect(storageLabel.rectTransform, new Vector2(0f, 1f), new Vector2(10f, -18f), new Vector2(580f, 20f));
            storageLabel.rectTransform.pivot = new Vector2(0f, 0.5f);
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

            if (!_grid.TryPlace(instanceId, itemData.CreateShape(), origin))
            {
                Debug.LogError($"Could not place prototype item: {instanceId}");
                return;
            }

            BackpackItemView itemView = CreateItemView(instanceId, itemData, origin, ItemRotation.None);
            _itemViews.Add(instanceId, itemView);
            PositionItem(itemView);
        }

        private BackpackItemView CreateItemView(string instanceId, ItemData itemData, Vector2Int origin, ItemRotation rotation)
        {
            var itemObject = new GameObject(itemData.DisplayName, typeof(RectTransform), typeof(BackpackItemView));
            itemObject.transform.SetParent(_gridRoot, false);
            BackpackItemView itemView = itemObject.GetComponent<BackpackItemView>();
            itemView.Initialize(this, instanceId, itemData, origin, rotation);
            return itemView;
        }

        private BackpackItemView CreateDragProxy(string instanceId, ItemData itemData, PointerEventData eventData, ItemRotation rotation = ItemRotation.None)
        {
            _gridRoot.SetAsLastSibling();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _gridRoot,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 pointerPosition);
            Vector2 initialPosition = pointerPosition + new Vector2(-ItemCellSize / 2f, ItemCellSize / 2f);
            BackpackItemView itemView = CreateItemView(instanceId, itemData, LocalPositionToGrid(initialPosition), rotation);
            itemView.RectTransform.anchoredPosition = initialPosition;
            return itemView;
        }

        private void PopulateShop()
        {
            _currentShopItems.Clear();
            List<ItemData> candidates = _itemCatalog?
                .Where(item => item != null && !item.ExcludeFromShop && item.Rarity != ItemRarity.Legendary)
                .ToList() ?? new List<ItemData>();

            RoundShopRarityWeights weights = _shopRarityTable?.GetWeights(_run.Round) ?? new RoundShopRarityWeights();
            foreach (PrototypeShopSlotView slot in _shopSlots)
            {
                // Preserve the actual shelf offer, including its sale roll and captured price.
                // A purchased locked offer is cleared on purchase and must be replaced normally.
                if (slot.CanPurchase && slot.Offer.IsLocked)
                {
                    slot.gameObject.SetActive(true);
                    _currentShopItems.Add(slot.ItemData);
                    continue;
                }
                bool hasItem = candidates.Count > 0;
                slot.gameObject.SetActive(hasItem);
                if (!hasItem) { slot.Bind(null); continue; }
                ItemData selected = SelectWeightedItem(candidates, weights);
                _currentShopItems.Add(selected);
                slot.Bind(new ShopOffer(selected, _shopRandom.NextDouble() < _saleChance));
            }
            if (candidates.Count == 0) SetStatus("상점에 표시할 ItemData가 없습니다. 잠긴 상품만 유지합니다.");
            UpdateEconomyText();
        }

        private ItemData SelectWeightedItem(IReadOnlyList<ItemData> candidates, RoundShopRarityWeights weights)
        {
            ItemRarity[] shopRarities =
            {
                ItemRarity.Common,
                ItemRarity.Uncommon,
                ItemRarity.Rare,
                ItemRarity.Epic
            };

            float total = 0f;
            foreach (ItemRarity rarity in shopRarities)
            {
                if (candidates.Any(item => item.Rarity == rarity))
                {
                    total += Mathf.Max(0f, weights.GetWeight(rarity));
                }
            }

            if (total <= 0f)
            {
                return candidates[_shopRandom.Next(candidates.Count)];
            }

            double roll = _shopRandom.NextDouble() * total;
            foreach (ItemRarity rarity in shopRarities)
            {
                List<ItemData> rarityItems = candidates.Where(item => item.Rarity == rarity).ToList();
                if (rarityItems.Count == 0)
                {
                    continue;
                }

                roll -= Mathf.Max(0f, weights.GetWeight(rarity));
                if (roll <= 0d)
                {
                    return rarityItems[_shopRandom.Next(rarityItems.Count)];
                }
            }

            return candidates[_shopRandom.Next(candidates.Count)];
        }

        private void StoreDraggedItem(BackpackItemView itemView, PointerEventData eventData)
        {
            if (_dragSource == DragSource.Shop && !CommitPurchase())
            {
                DestroyDragProxy(itemView);
                SetStatus($"구매할 수 없습니다. 필요: {_draggingShopOffer.PurchasePrice} G");
                return;
            }

            Vector2 position = GetClampedStoragePosition(itemView);
            string displayName = itemView.DisplayName;
            if (_dragSource == DragSource.Backpack)
            {
                _grid.Remove(itemView.InstanceId);
                _itemViews.Remove(itemView.InstanceId);
                PrototypeStoredItem storedItem = new(itemView.InstanceId, itemView.Data, position, itemView.Rotation);
                _storedItems.Add(storedItem);
                CreateStorageView(storedItem);
                Destroy(itemView.gameObject);
            }
            else if (_dragSource == DragSource.Storage)
            {
                PrototypeStoredItem storedItem = _draggingStorageView.StoredItem;
                storedItem.StoragePosition = position;
                storedItem.Rotation = itemView.Rotation;
                _draggingStorageView.RefreshShape();
                _draggingStorageView.SetPosition(position);
                _itemViews.Remove(itemView.InstanceId);
                Destroy(itemView.gameObject);
            }
            else
            {
                PrototypeStoredItem storedItem = new(itemView.InstanceId, itemView.Data, position, itemView.Rotation);
                _storedItems.Add(storedItem);
                CreateStorageView(storedItem);
                Destroy(itemView.gameObject);
            }

            SetStatus($"{displayName}을(를) 자유 보관함에 놓았습니다.");
        }

        private void CompleteGridPlacement(BackpackItemView itemView)
        {
            if (_dragSource == DragSource.Shop)
            {
                if (!CommitPurchase())
                {
                    _grid.Remove(itemView.InstanceId);
                    DestroyDragProxy(itemView);
                    return;
                }

                _itemViews[itemView.InstanceId] = itemView;
                SetStatus($"{itemView.DisplayName} 구매 및 가방 배치 완료.");
            }
            else if (_dragSource == DragSource.Storage)
            {
                RemoveStoredItem(_draggingStorageView.StoredItem.InstanceId);
                SetStatus($"{itemView.DisplayName}을(를) 보관함에서 가방으로 옮겼습니다.");
            }
            else
            {
                SetStatus($"{itemView.DisplayName} 배치 완료: ({_previewOrigin.x}, {_previewOrigin.y})");
            }
        }

        private void CancelGridPlacement(BackpackItemView itemView, bool canAfford)
        {
            if (_dragSource == DragSource.Backpack)
            {
                itemView.SetPlacement(_dragOrigin, _dragRotation);
                SetStatus("놓을 수 없는 위치입니다. 아이템을 원래 자리로 돌려놓았습니다.");
                return;
            }

            DestroyDragProxy(itemView);
            SetStatus(canAfford
                ? "가방이나 자유 보관함 안에 놓아야 구매할 수 있습니다."
                : $"구매할 수 없습니다. 필요: {_draggingShopOffer.PurchasePrice} G");
        }

        private void SellDraggedItem(BackpackItemView itemView)
        {
            _fusionLocks.Remove(itemView.InstanceId);
            string displayName = itemView.DisplayName;
            int salePrice = ShopPriceRules.HalfRoundedUp(itemView.Data.PurchasePrice);
            if (_dragSource == DragSource.Backpack)
            {
                _grid.Remove(itemView.InstanceId);
                _itemViews.Remove(itemView.InstanceId);
                Destroy(itemView.gameObject);
            }
            else
            {
                RemoveStoredItem(itemView.InstanceId);
                _itemViews.Remove(itemView.InstanceId);
                Destroy(itemView.gameObject);
            }

            _run.Gold += salePrice;
            UpdateEconomyText();
            SetStatus($"{displayName} 판매 완료 (+{salePrice} G)");
        }

        private bool CommitPurchase()
        {
            if (!CanPurchaseDraggedOffer() || !_draggingShopOffer.TryMarkPurchased())
            {
                return false;
            }

            _run.Gold -= _draggingShopOffer.PurchasePrice;
            _currentShopItems.Remove(_draggingShopOffer.ItemData);
            _draggingShopSlot.ShowPurchased();
            UpdateEconomyText();
            return true;
        }

        private bool CanPurchaseDraggedOffer()
        {
            return _draggingShopSlot != null
                   && _draggingShopOffer != null
                   && _draggingShopSlot.Offer == _draggingShopOffer
                   && !_draggingShopOffer.IsPurchased
                   && _run.Gold >= _draggingShopOffer.PurchasePrice;
        }

        private void CreateStorageView(PrototypeStoredItem storedItem)
        {
            var viewObject = new GameObject(storedItem.ItemData.DisplayName, typeof(RectTransform), typeof(PrototypeStorageItemView));
            viewObject.transform.SetParent(_storageRoot, false);
            PrototypeStorageItemView storageView = viewObject.GetComponent<PrototypeStorageItemView>();
            storageView.Initialize(this, storedItem);
            _storageViews[storedItem.InstanceId] = storageView;
        }

        private void RemoveStoredItem(string instanceId)
        {
            _storedItems.RemoveAll(item => item.InstanceId == instanceId);
            if (_storageViews.Remove(instanceId, out PrototypeStorageItemView storageView) && storageView != null)
            {
                Destroy(storageView.gameObject);
            }
        }

        private string CreatePurchasedInstanceId(ItemData itemData)
        {
            _run.PurchasedItemSequence++;
            return $"purchased_{itemData.ItemId}_{_run.PurchasedItemSequence}";
        }

        private void DestroyDragProxy(BackpackItemView itemView)
        {
            _itemViews.Remove(itemView.InstanceId);
            Destroy(itemView.gameObject);
        }

        private void FinishDrag()
        {
            ClearPlacementOverlay();
            if (_draggingItem != null)
            {
                _draggingItem.ResetTint();
            }

            if (_draggingStorageView != null)
            {
                _draggingStorageView.SetDragging(false);
            }

            ResetGridColors();
            _draggingItem = null;
            _draggingStorageView = null;
            _draggingShopSlot = null;
            _draggingShopOffer = null;
            _dragSource = DragSource.None;
            _sellPreview.gameObject.SetActive(false);
        }

        private void UpdateSellPreview(PointerEventData eventData)
        {
            bool canSell = _draggingItem != null
                           && (_dragSource == DragSource.Backpack || _dragSource == DragSource.Storage);
            _sellPreview.gameObject.SetActive(canSell);
            if (!canSell)
            {
                return;
            }

            bool hoveringShop = IsPointerInside(_sellArea, eventData);
            _sellPreview.color = hoveringShop
                ? new Color(0.15f, 0.40f, 0.24f, 0.96f)
                : new Color(0.16f, 0.23f, 0.32f, 0.96f);
            int salePrice = ShopPriceRules.HalfRoundedUp(_draggingItem.Data.PurchasePrice);
            _sellPreviewText.text = $"판매\n\n{_draggingItem.DisplayName}\n<color=#FFD65C>+{salePrice} G</color>\n\n<size=17>{(hoveringShop ? "놓으면 판매됩니다" : "상점 안에 놓아 판매하세요")}</size>";
        }

        private void SetDragOffset(BackpackItemView itemView, PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _gridRoot,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 pointerPosition);
            _dragOffset = pointerPosition - itemView.RectTransform.anchoredPosition;
        }

        private void SetDragOffsetFromPointer()
        {
            _dragOffset = new Vector2(ItemCellSize / 2f, -ItemCellSize / 2f);
        }

        private bool IsPointerInside(RectTransform target, PointerEventData eventData)
        {
            return target != null
                   && RectTransformUtility.RectangleContainsScreenPoint(target, eventData.position, eventData.pressEventCamera);
        }

        private Vector2 GetClampedStoragePosition(BackpackItemView itemView)
        {
            // Keep the dragged shape's center at its current location; never snap to a grid.
            Vector2 localPosition = _storageRoot.InverseTransformPoint(
                itemView.RectTransform.TransformPoint(itemView.RectTransform.rect.center));
            Rect rect = _storageRoot.rect;
            Vector2 halfSize = itemView.RectTransform.sizeDelta * 0.5f;
            return new Vector2(
                ClampStorageAxis(localPosition.x, rect.xMin + halfSize.x + 8f, rect.xMax - halfSize.x - 8f),
                ClampStorageAxis(localPosition.y, rect.yMin + halfSize.y + 8f, rect.yMax - halfSize.y - 32f));
        }

        private static float ClampStorageAxis(float value, float minimum, float maximum)
            => minimum <= maximum ? Mathf.Clamp(value, minimum, maximum) : (minimum + maximum) * 0.5f;

        private void UpdatePlacementPreview(BackpackItemView itemView, Vector2Int origin)
        {
            RefreshDragPlacementOverlay();
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

        private void UpdateEconomyText()
        {
            if (_livesText != null)
            {
                _livesText.text = $"라이프 {_run.Lives} / {_run.StartingLives}";
                _livesText.color = _run.Lives <= 1 ? new Color(1f, .4f, .4f) : new Color(.91f, .94f, .98f);
            }
            if (_goldText != null)
            {
                _goldText.text = $"{_run.Gold} G";
            }

            if (_roundText != null)
            {
                _roundText.text = $"ROUND {_run.Round} / {StageRoundCount}";
            }

            if (_rerollText != null)
            {
                _rerollText.text = $"새로고침 {_rerollCost}G";
            }

            if (_nextRoundText != null)
            {
                _nextRoundText.text = _run.Round < StageRoundCount
                    ? $"테스트 다음 +{_roundIncome}G"
                    : "마지막 라운드";
            }

            if (_rarityOddsText != null)
            {
                RoundShopRarityWeights weights = _shopRarityTable?.GetWeights(_run.Round) ?? new RoundShopRarityWeights();
                _rarityOddsText.text = $"일 {weights.Common:0}% · 고 {weights.Uncommon:0}% · 희 {weights.Rare:0}% · 영 {weights.Epic:0}%";
            }
        }

        private void ResetGridColors()
        {
            if (_gridCells == null)
            {
                return;
            }

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
            // Diagnostics remain available in the Console without an on-screen status strip.
            Debug.Log(message, this);
        }

        private static Button CreateButton(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            SetRect((RectTransform)buttonObject.transform, new Vector2(0.5f, 0f), position, size);
            buttonObject.GetComponent<Image>().color = color;
            return buttonObject.GetComponent<Button>();
        }

        private static Image CreateImage(Transform parent, string objectName, Color color)
        {
            var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        public static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset;
            rect.offsetMax = Vector2.one * -inset;
        }

        private enum DragSource
        {
            None,
            Backpack,
            Shop,
            Storage
        }
    }
}
