using System.Linq;
using BackpackAutoBattle.Progression;
using BackpackAutoBattle.Shop;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed partial class BackpackPrototypeController
    {
        private enum TutorialStep
        {
            LockShop, UnlockShop, BuySword, BuyShield, BuyWhetstone, LockFusion, UnlockFusion,
            StartFirstBattle, PlaceSteel, StartFinalBattle
        }

        private static readonly string[] TutorialShopIds = { "wooden_sword", "wooden_shield", "whetstone", "healing_potion" };
        private static readonly Vector2Int TutorialSwordCell = new(1, 1);
        private static readonly Vector2Int TutorialShieldCell = new(3, 1);
        private static readonly Vector2Int TutorialWhetstoneCell = new(2, 2);
        private bool _tutorialActive;
        private TutorialStep _tutorialStep;
        private RectTransform _tutorialGuide, _tutorialSource, _tutorialDestination;
        private Text _tutorialGuideTitle, _tutorialGuideMessage, _tutorialNameError;
        private PrototypeMenuPopup _tutorialNamePopup;
        private InputField _tutorialNameInput;
        private Button _rerollButton;
        private bool TutorialCanStartBattle => _tutorialStep is TutorialStep.StartFirstBattle or TutorialStep.StartFinalBattle;

        private void OpenApplicationEntry()
        {
            EnsureProgressLoaded();
            switch (_progress.Onboarding)
            {
                case OnboardingState.TutorialRequired when _progressStore.CanWrite:
                    StartTutorial();
                    break;
                case OnboardingState.NameRequired:
                    _tutorialActive = true;
                    _shopScreenRoot.gameObject.SetActive(false);
                    _lifeHearts.gameObject.SetActive(false);
                    _stageSettingsButton.gameObject.SetActive(false);
                    ShowTutorialName();
                    break;
                default:
                    ShowMainMenu();
                    break;
            }
        }

        private void StartTutorial()
        {
            _tutorialActive = true;
            _trackProgress = false;
            _tutorialStep = TutorialStep.LockShop;
            ClearRunContent();
            _run = new PrototypeRunState(15, "tutorial", playerName: _characterData?.DisplayName ?? "플레이어",
                stageName: "튜토리얼");
            InitializeRunContent();
        }

        private void PopulateTutorialShop()
        {
            _currentShopItems.Clear();
            for (int i = 0; i < _shopSlots.Count; i++)
            {
                var item = _run.Round == 1 && i < TutorialShopIds.Length
                    ? _itemCatalog.First(entry => entry != null && entry.ItemId == TutorialShopIds[i]) : null;
                _shopSlots[i].Bind(item != null ? new ShopOffer(item, false) : null);
                _shopSlots[i].gameObject.SetActive(item != null);
                if (item != null) _currentShopItems.Add(item);
            }
            UpdateEconomyText();
        }

        private string TutorialPurchaseId => _tutorialStep switch
        {
            TutorialStep.BuySword => TutorialShopIds[0],
            TutorialStep.BuyShield => TutorialShopIds[1],
            TutorialStep.BuyWhetstone => TutorialShopIds[2],
            _ => null
        };

        private bool CanToggleTutorialShopLock(PrototypeShopSlotView slot)
            => (_tutorialStep is TutorialStep.LockShop or TutorialStep.UnlockShop) && slot.ItemData.ItemId == TutorialShopIds[3];

        private bool CanToggleTutorialFusionLock(string id)
            => (_tutorialStep is TutorialStep.LockFusion or TutorialStep.UnlockFusion)
                && _itemViews.TryGetValue(id, out var weapon) && weapon.Data.ItemId == TutorialShopIds[0];

        private bool IsTutorialPlacement(BackpackItemView item, Vector2Int origin)
        {
            if (!_tutorialActive) return true;
            return _tutorialStep switch
            {
                TutorialStep.BuySword or TutorialStep.PlaceSteel => origin == TutorialSwordCell,
                TutorialStep.BuyShield => origin == TutorialShieldCell && ((int)item.Rotation & 1) != 0,
                TutorialStep.BuyWhetstone => origin == TutorialWhetstoneCell,
                _ => false
            };
        }

        private void AdvanceTutorialAction()
        {
            _tutorialStep++;
            RenderEncounterPreview();
            RenderTutorialGuide();
        }

        private void ShowTutorialBattleResult()
        {
            if (Phase == PrototypePhase.Completed)
            {
                var candidate = PlayerProgress.FromData(_progress.Export());
                if (candidate.FinishTutorial() && _progressStore.TrySave(candidate, out _saveNotice)) _progress = candidate;
            }
            _battleController.ShowTutorialResult(Phase, Phase switch
            {
                PrototypePhase.Victory => "상점으로 · 합성하기",
                PrototypePhase.Completed => "유저명 입력",
                PrototypePhase.GameOver => "다시 시작",
                _ => "상점으로 · 재도전"
            });
            RefreshCombatInterface();
        }

        private void ShowTutorialName()
        {
            if (_tutorialNamePopup.IsOpen || IsSettingsOpen) return;
            _itemTooltip.HideAll();
            _tutorialNameError.text = _saveNotice ?? "";
            _tutorialNamePopup.Show();
            _tutorialNameInput.Select();
            _tutorialNameInput.ActivateInputField();
        }

        private void ConfirmTutorialName()
        {
            if (!_tutorialNamePopup.IsOpen) return;
            var candidate = PlayerProgress.FromData(_progress.Export());
            if (Phase == PrototypePhase.Completed) candidate.FinishTutorial();
            if (!candidate.TrySetInitialUserName(_tutorialNameInput.text))
            {
                _tutorialNameError.text = "한글, 영문, 숫자만 사용해 2~12자로 입력해주세요.";
                return;
            }
            if (!_progressStore.TrySave(candidate, out _saveNotice))
            {
                _tutorialNameError.text = _saveNotice;
                return;
            }
            _progress = candidate;
            _tutorialNameInput.DeactivateInputField();
            _tutorialNamePopup.Hide();
            ClearRunContent();
            _tutorialActive = false;
            _run = CreateRunState();
            ShowMainMenu();
        }

        private void BuildTutorialInterface(Transform canvas)
        {
            var panel = SelectionUi.Image(_shopScreenRoot, "Tutorial Guide", Color.white);
            _tutorialGuide = panel.rectTransform;
            SelectionUi.Place(_tutorialGuide, new Vector2(500, 50), new Vector2(230, 290));
            PrototypePixelSkin.Apply(panel, PrototypePixelSkin.Surface.Panel);
            SelectionUi.Label(_tutorialGuide, "Tutorial Caption", "튜토리얼", 19, new Vector2(0, 101), new Vector2(188, 27))
                .color = PrototypePixelSkin.Muted;
            _tutorialGuideTitle = SelectionUi.Label(_tutorialGuide, "Tutorial Step Title", "", 23,
                new Vector2(0, 50), new Vector2(198, 50));
            _tutorialGuideMessage = SelectionUi.Label(_tutorialGuide, "Tutorial Step Message", "", 18,
                new Vector2(0, -48), new Vector2(190, 136));
            _tutorialGuideMessage.fontStyle = FontStyle.Normal;
            _tutorialGuideMessage.lineSpacing = 1.2f;
            _tutorialGuide.gameObject.SetActive(false);
            _tutorialSource = MakeTutorialHighlight("Tutorial Source");
            _tutorialDestination = MakeTutorialHighlight("Tutorial Destination");

            _tutorialNamePopup = new PrototypeMenuPopup(canvas, "Tutorial Name Popup", "유저명 입력",
                new Vector2(478, 286), () => { }, PrototypeMenuPopup.CloseStyle.None);
            RectTransform body = _tutorialNamePopup.Panel;
            PrototypeGuildStyle.Apply(body.GetComponent<Image>());
            _tutorialNamePopup.Title.color = PrototypeGuildStyle.Cream;
            SelectionUi.Label(body, "Tutorial Name Rules", "한글, 영문, 숫자 2~12자", 17,
                new Vector2(0, 51), new Vector2(390, 24));
            var field = SelectionUi.Image(body, "Tutorial Name Input", new Color(.11f, .15f, .18f));
            field.raycastTarget = true;
            SelectionUi.Place(field.rectTransform, new Vector2(0, 0), new Vector2(326, 46));
            PrototypeGuildStyle.Apply(field, PrototypeGuildStyle.Surface.Iron);
            var value = SelectionUi.Label(field.transform, "Tutorial Name Value", "", 23, Vector2.zero, new Vector2(300, 34));
            value.alignment = TextAnchor.MiddleLeft;
            value.supportRichText = false;
            _tutorialNameInput = field.gameObject.AddComponent<InputField>();
            _tutorialNameInput.textComponent = value;
            _tutorialNameInput.targetGraphic = field;
            _tutorialNameInput.lineType = InputField.LineType.SingleLine;
            _tutorialNameInput.onValueChanged.AddListener(_ => _tutorialNameError.text = "");
            _tutorialNameError = SelectionUi.Label(body, "Tutorial Name Error", "", 14, new Vector2(0, -43), new Vector2(420, 28));
            _tutorialNameError.color = new Color(1f, .55f, .48f);
            var submit = SelectionUi.GuildButton(body, "Tutorial Name Confirm", "확정", new Vector2(0, -91),
                new Vector2(190, 46), ConfirmTutorialName);
            PrototypeGuildStyle.Apply(submit, true);
            _tutorialNamePopup.Panel.gameObject.AddComponent<PrototypeNameImeInput>();
        }

        private RectTransform MakeTutorialHighlight(string name)
        {
            var root = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(_shopScreenRoot, false);
            Color gold = new(1f, .78f, .29f);
            for (int side = 0; side < 4; side++)
            {
                RectTransform edge = SelectionUi.Image(root, "Edge", gold).rectTransform;
                bool horizontal = side < 2;
                float position = side % 2;
                edge.anchorMin = horizontal ? new Vector2(0, position) : new Vector2(position, 0);
                edge.anchorMax = horizontal ? new Vector2(1, position) : new Vector2(position, 1);
                edge.anchoredPosition = Vector2.zero;
                edge.sizeDelta = horizontal ? new Vector2(0, 3) : new Vector2(3, 0);
            }
            root.gameObject.SetActive(false);
            return root;
        }

        private void AimTutorialHighlight(RectTransform frame, RectTransform target)
        {
            if (target == null) return;
            Vector2 position = _shopScreenRoot.InverseTransformPoint(target.TransformPoint(target.rect.center));
            Vector2 size = _shopScreenRoot.InverseTransformVector(target.TransformVector(target.rect.size));
            SelectionUi.Place(frame, position, size);
            frame.gameObject.SetActive(true);
            frame.SetAsLastSibling();
        }

        private void RenderTutorialGuide()
        {
            _tutorialGuide.gameObject.SetActive(_tutorialActive);
            _tutorialSource.gameObject.SetActive(false);
            _tutorialDestination.gameObject.SetActive(false);
            _rerollButton.interactable = !_tutorialActive;
            _rarityOddsText.gameObject.SetActive(!_tutorialActive);
            if (!_tutorialActive || !_shopScreenRoot.gameObject.activeSelf) return;
            (_tutorialGuideTitle.text, _tutorialGuideMessage.text) = _tutorialStep switch
            {
                TutorialStep.LockShop => ("상점 상품 잠그기", "회복 물약을 우클릭해 보세요. 잠긴 상품은 상점을 새로고침해도 남아 있어요."),
                TutorialStep.UnlockShop => ("상점 상품 잠금 해제", "잠긴 회복 물약을 다시 우클릭해 잠금을 풀어 보세요."),
                TutorialStep.BuySword => ("목검 구매", "상점의 목검을 가방의 노란 칸으로 드래그해 구매하세요."),
                TutorialStep.BuyShield => ("나무 방패 회전", "나무 방패를 끌면서 R 키로 회전한 뒤, 가로로 표시된 노란 칸에 놓으세요."),
                TutorialStep.BuyWhetstone => ("합성 재료 배치", "숫돌을 목검 옆의 노란 칸에 놓으세요. 두 재료가 맞닿으면 합성이 준비돼요."),
                TutorialStep.LockFusion => ("합성 잠그기", "Shift 키를 누른 채 목검을 우클릭하세요. 잠긴 아이템은 합성에 쓰이지 않아요."),
                TutorialStep.UnlockFusion => ("합성 잠금 해제", "Shift + 우클릭으로 목검의 잠금을 푸세요. 강철 검 합성 표시가 다시 나타나요."),
                TutorialStep.StartFirstBattle => ("첫 전투 시작", "가방 속 무기는 시간이 차면 자동으로 공격해요. 승리하고 상점으로 돌아오면 강철 검이 만들어져요."),
                TutorialStep.PlaceSteel when IsFusionPresenting => ("강철 검 합성", "합성된 아이템은 자유 보관함에 들어가요."),
                TutorialStep.PlaceSteel => ("강철 검 배치", "자유 보관함의 강철 검을 가방의 노란 칸으로 옮기세요."),
                _ => ("마지막 전투", "강철 검으로 전투를 시작하세요. 승리하면 전투 통계를 확인할 수 있어요.")
            };

            if (_tutorialStep is TutorialStep.LockShop or TutorialStep.UnlockShop)
                AimTutorialHighlight(_tutorialSource, (RectTransform)_shopSlots[3].transform);
            else if (TutorialPurchaseId != null)
                AimTutorialHighlight(_tutorialSource, (RectTransform)_shopSlots.First(slot => slot.ItemData?.ItemId == TutorialPurchaseId).transform);
            else if (_tutorialStep is TutorialStep.LockFusion or TutorialStep.UnlockFusion)
                AimTutorialHighlight(_tutorialSource, _itemViews.Values.First(view => view.Data.ItemId == TutorialShopIds[0]).RectTransform);
            else if (_tutorialStep == TutorialStep.PlaceSteel && !IsFusionPresenting)
                AimTutorialHighlight(_tutorialSource, _storageViews.Values.FirstOrDefault(view => view.StoredItem.ItemData.ItemId == "steel_sword")
                    ?.transform as RectTransform);

            var (cell, columns, rows) = _tutorialStep switch
            {
                TutorialStep.BuySword or TutorialStep.PlaceSteel => (TutorialSwordCell, 1, 3),
                TutorialStep.BuyShield => (TutorialShieldCell, 2, 1),
                TutorialStep.BuyWhetstone => (TutorialWhetstoneCell, 1, 1),
                _ => (Vector2Int.zero, 0, 0)
            };
            if (columns == 0 || IsFusionPresenting) return;
            Vector2 size = new(columns * CellPitch - (CellPitch - ItemCellSize), rows * CellPitch - (CellPitch - ItemCellSize));
            Vector2 center = _gridRoot.anchoredPosition + GridToLocalPosition(cell) + new Vector2(size.x, -size.y) / 2f;
            SelectionUi.Place(_tutorialDestination, center, size);
            _tutorialDestination.gameObject.SetActive(true);
            _tutorialDestination.SetAsLastSibling();
        }
    }
}
