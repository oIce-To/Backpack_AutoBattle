using System.Collections;
using System.Linq;
using BackpackAutoBattle.Combat;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using BackpackAutoBattle.Shop;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class PrototypeItemFeedbackTests
    {
        [UnityTest]
        public IEnumerator StatIcons_HavePointedSwordAndEightEqualStarTips()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            var panel = new GameObject("Icon QA Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            panel.GetComponent<Image>().color = new Color(0.07f, 0.095f, 0.14f);
            BackpackPrototypeController.SetRect((RectTransform)panel.transform, Vector2.one * 0.5f,
                Vector2.zero, new Vector2(900f, 280f));
            string[] captions = { "공격력", "사용 주기", "명중률", "치명타 확률", "치명타 피해" };
            var icons = new PrototypeStatIcon[5];
            for (int index = 0; index < icons.Length; index++)
            {
                float x = (index - 2) * 160f;
                icons[index] = CreateSampleIcon(panel.transform, (WeaponStatIcon)index, x, 48f, 96f);
                CreateSampleIcon(panel.transform, (WeaponStatIcon)index, x, -40f, 31f);
                Text caption = BackpackPrototypeController.CreateText(panel.transform, captions[index], captions[index],
                    18, FontStyle.Normal, TextAnchor.MiddleCenter);
                BackpackPrototypeController.SetRect(caption.rectTransform, Vector2.one * 0.5f,
                    new Vector2(x, -86f), new Vector2(150f, 30f));
            }
            Canvas.ForceUpdateCanvases();
            Vector2[] sword = GlyphPoints(icons[0]);
            Assert.That(sword.Any(point => Vector2.Distance(point, Vector2.one * (12f / Mathf.Sqrt(2f))) < 0.01f), Is.True);
            Assert.That(sword.Any(point => Vector2.Distance(point, Vector2.one * (-10.7f / Mathf.Sqrt(2f))) < 0.01f), Is.True);
            Vector2[] star = GlyphPoints(icons[3]);
            Vector2[] tips = star.Where(point => point.magnitude > 10f).ToArray();
            Assert.That(tips, Has.Length.EqualTo(8));
            for (int index = 0; index < 8; index++)
            {
                float angle = index * Mathf.PI / 4f;
                Vector2 expected = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 10.8f;
                Assert.That(tips.Any(point => Vector2.Distance(point, expected) < 0.01f), Is.True);
            }
            foreach (PrototypeStatIcon icon in icons)
                Assert.That(GlyphPoints(icon).All(point => point.magnitude < 12.6f), Is.True, icon.name);
            RawImage original = icons[4].GetComponentInChildren<RawImage>();
            Assert.That(original, Is.Not.Null);
            Assert.That(original.raycastTarget, Is.False);
            Assert.That(original.texture, Is.SameAs(Resources.Load<Texture2D>("UI/CriticalDamageSource")));
            Assert.That(original.material.shader.name, Is.EqualTo("BackpackAutoBattle/UI/White Alpha Icon"));
            Assert.That(original.material.shader.isSupported, Is.True);
            Assert.That(original.color, Is.EqualTo(icons[4].color));
            var iconCorners = new Vector3[4];
            original.rectTransform.GetWorldCorners(iconCorners);
            Rect frameRect = icons[4].rectTransform.rect;
            float frameScale = Mathf.Min(frameRect.width, frameRect.height) / 32f;
            foreach (Vector3 corner in iconCorners)
                Assert.That(((Vector2)icons[4].transform.InverseTransformPoint(corner) - frameRect.center).magnitude / frameScale,
                    Is.LessThan(12.3f), "The original image must fit inside the ring's 12.6-unit inner radius.");
            var source = (Texture2D)original.texture;
            Assert.That(source.width, Is.EqualTo(72));
            Assert.That(source.height, Is.EqualTo(72));
            Assert.That(source.mipmapCount, Is.EqualTo(1));
            var decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.That(decoded.LoadImage(System.IO.File.ReadAllBytes(System.IO.Path.Combine(Application.dataPath,
                "_Project/Resources/UI/CriticalDamageSource.png"))), Is.True);
            Color32[] expectedPixels = decoded.GetPixels32();
            Color32[] imported = source.GetPixels32();
            Assert.That(imported.Select(pixel => pixel.a), Is.EqualTo(expectedPixels.Select(pixel => pixel.a)));
            Object.Destroy(decoded);
            PrototypeScreenCapture.CaptureIfRequested(canvas, "StatIcons.png");
            icons[4].SetKind(WeaponStatIcon.Attack);
            Assert.That(original.gameObject.activeSelf, Is.False);
            icons[4].SetKind(WeaponStatIcon.CriticalDamage);
            Assert.That(original.gameObject.activeSelf, Is.True);
            Assert.That(icons[4].GetComponentsInChildren<RawImage>(), Has.Length.EqualTo(1));
            Object.Destroy(panel);
        }

        private static PrototypeStatIcon CreateSampleIcon(Transform parent, WeaponStatIcon kind, float x, float y, float size)
        {
            var obj = new GameObject($"{kind} Sample", typeof(RectTransform), typeof(PrototypeStatIcon));
            obj.transform.SetParent(parent, false);
            PrototypeStatIcon icon = obj.GetComponent<PrototypeStatIcon>();
            icon.SetKind(kind);
            icon.color = new Color(0.90f, 0.94f, 1f);
            icon.raycastTarget = false;
            BackpackPrototypeController.SetRect(icon.rectTransform, Vector2.one * 0.5f,
                new Vector2(x, y), Vector2.one * size);
            return icon;
        }

        private static Vector2[] GlyphPoints(PrototypeStatIcon icon)
        {
            Mesh mesh = icon.canvasRenderer.GetMesh();
            Assert.That(mesh, Is.Not.Null);
            Vector3[] vertices = mesh.vertices;
            Color32[] colors = mesh.colors32;
            Color32 ink = icon.color;
            Rect rect = icon.GetPixelAdjustedRect();
            float scale = Mathf.Min(rect.width, rect.height) / 32f;
            return vertices.Where((vertex, index) => colors[index].Equals(ink))
                .Select(vertex => ((Vector2)vertex - rect.center) / scale).ToArray();
        }

        [UnityTest]
        public IEnumerator CycleColor_FillsWholeItemBottomToTopAndResetsAfterUse()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            controller.BattleController.AutoTick = false;
            Assert.That(controller.TryStartBattle(), Is.True);
            PrototypeBattleItemView club = controller.GetComponentsInChildren<PrototypeBattleItemView>()
                .Single(view => view.Side == CombatSide.Enemy);
            CombatWeapon weapon = controller.ActiveCombat.EnemyWeapons.Single();
            RectTransform upper = club.transform.Find("Shape Cell 0,0/Cycle Color").GetComponent<RectTransform>();
            RectTransform lower = club.transform.Find("Shape Cell 0,1/Cycle Color").GetComponent<RectTransform>();
            Image muted = club.transform.Find("Shape Cell 0,0").GetComponent<Image>();
            Assert.That(muted.color, Is.Not.EqualTo(club.Snapshot.DisplayColor));
            Assert.That(club.CycleProgress, Is.Zero);
            Assert.That(upper.anchorMax.y, Is.Zero);
            Assert.That(lower.anchorMax.y, Is.Zero);
            Assert.That(club.GetComponentsInChildren<Transform>().Any(child => child.name == "Cooldown"), Is.False);

            controller.TickCombat(0.5f);
            Assert.That(club.CycleProgress, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(lower.anchorMax.y, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(upper.anchorMax.y, Is.Zero);
            controller.TickCombat(0.5f);
            Assert.That(lower.anchorMax.y, Is.EqualTo(1f));
            Assert.That(upper.anchorMax.y, Is.Zero);
            controller.TickCombat(0.5f);
            Assert.That(lower.anchorMax.y, Is.EqualTo(1f));
            Assert.That(upper.anchorMax.y, Is.GreaterThan(0f).And.LessThan(1f));
            club.RenderCooldown(weapon, weapon.NextAttackAt);
            Assert.That(upper.anchorMax.y, Is.EqualTo(1f));
            Assert.That(lower.anchorMax.y, Is.EqualTo(1f));

            Color mutedColor = muted.color;
            controller.TickCombat(0.5f);
            Assert.That(controller.ActiveCombat.PlayerHealth, Is.EqualTo(35d));
            Assert.That(club.CycleProgress, Is.Zero);
            Assert.That(upper.anchorMax.y, Is.Zero);
            Assert.That(lower.anchorMax.y, Is.Zero);
            yield return new WaitForSeconds(0.2f);
            Assert.That(muted.color, Is.EqualTo(mutedColor));
            PrototypeBattleItemView shield = controller.GetComponentsInChildren<PrototypeBattleItemView>()
                .Single(view => view.Snapshot.ItemId == "wooden_shield");
            Assert.That(shield.GetComponentsInChildren<Transform>().Any(child => child.name == "Cycle Color"), Is.False);
            Assert.That(shield.transform.Find("Shape Cell 0,0").GetComponent<Image>().color, Is.EqualTo(shield.Snapshot.DisplayColor));
        }

        [UnityTest]
        public IEnumerator ShopBackpackAndStorage_HoverWithoutClicksAndHideDuringDrag()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            PrototypeItemTooltip tooltip = controller.ItemTooltip;
            var pointer = new PointerEventData(Object.FindFirstObjectByType<EventSystem>());
            var swordData = controller.ItemCatalog.Single(item => item.ItemId == "training_blade");
            var slot = Object.FindFirstObjectByType<PrototypeShopSlotView>();
            slot.Bind(new ShopOffer(swordData, false));
            pointer.position = ScreenPoint(slot.transform);
            slot.OnPointerEnter(pointer);
            Assert.That(tooltip.IsVisible, Is.True);
            Assert.That(TooltipText(tooltip, "Tooltip Name").text, Is.EqualTo(swordData.DisplayName));
            Assert.That(TooltipText(tooltip, "Tooltip Rarity").text, Is.EqualTo("일반"));
            Assert.That(TooltipText(tooltip, "Tooltip Context").gameObject.activeSelf, Is.False);
            Assert.That(TooltipText(tooltip, "Attack Value").text, Is.EqualTo("5"));
            Assert.That(TooltipText(tooltip, "Cooldown Value").text, Is.EqualTo("2.0s"));
            Assert.That(TooltipText(tooltip, "Accuracy Value").text, Is.EqualTo("90%"));
            Assert.That(TooltipText(tooltip, "Critical Chance Value").text, Is.EqualTo("10%"));
            Assert.That(TooltipText(tooltip, "Critical Damage Value").text, Is.EqualTo("200%"));
            Assert.That(TooltipText(tooltip, "Tooltip Description").text, Is.EqualTo(swordData.Description));
            Assert.That(tooltip.GetComponentsInChildren<PrototypeStatIcon>(), Has.Length.EqualTo(5));
            Assert.That(TooltipText(tooltip, "Attack Caption").text, Is.EqualTo("공격력"));
            Assert.That(TooltipText(tooltip, "Critical Damage Caption").text, Is.EqualTo("치명타 피해"));
            Canvas.ForceUpdateCanvases();
            foreach (PrototypeStatIcon icon in tooltip.GetComponentsInChildren<PrototypeStatIcon>())
            {
                Mesh mesh = icon.canvasRenderer.GetMesh();
                Assert.That(mesh, Is.Not.Null, icon.name);
                Assert.That(mesh.vertexCount, Is.GreaterThan(0), icon.name);
            }
            Assert.That(tooltip.GetComponentsInChildren<Graphic>().All(graphic => !graphic.raycastTarget), Is.True);

            RectTransform canvasRect = (RectTransform)canvas.transform;
            pointer.position = RectTransformUtility.WorldToScreenPoint(null,
                canvasRect.TransformPoint(new Vector3(canvasRect.rect.xMax - 1f, canvasRect.rect.yMin + 1f)));
            slot.OnPointerMove(pointer);
            Assert.That(tooltip.RectTransform.anchoredPosition.x + tooltip.RectTransform.sizeDelta.x,
                Is.LessThanOrEqualTo(canvasRect.rect.width - 7f));
            Assert.That(tooltip.RectTransform.anchoredPosition.y - tooltip.RectTransform.sizeDelta.y, Is.GreaterThanOrEqualTo(7f));

            BackpackItemView sword = Object.FindObjectsByType<BackpackItemView>(FindObjectsSortMode.None)
                .Single(view => view.InstanceId == "starter_training_blade");
            pointer.position = ScreenPoint(sword.transform);
            sword.OnPointerEnter(pointer);
            slot.OnPointerExit(pointer); // A stale exit from the previous item must not hide the new tooltip.
            Assert.That(tooltip.Owner, Is.SameAs(sword));
            PrototypeScreenCapture.CaptureIfRequested(canvas, "ShopTooltip.png");
            sword.OnPointerExit(pointer);
            Assert.That(tooltip.IsVisible, Is.False);

            slot.Bind(new ShopOffer(controller.ItemCatalog.Single(item => item.ItemId == "healing_potion"), false));
            pointer.button = PointerEventData.InputButton.Left;
            pointer.position = ScreenPoint(slot.transform);
            slot.OnPointerEnter(pointer);
            slot.OnBeginDrag(pointer);
            Assert.That(tooltip.IsVisible, Is.False);
            pointer.position = ScreenPoint(controller.GetComponentsInChildren<Transform>().Single(child => child.name == "Freeform Storage"));
            slot.OnDrag(pointer);
            slot.OnEndDrag(pointer);
            yield return null;
            PrototypeStorageItemView stored = Object.FindFirstObjectByType<PrototypeStorageItemView>();
            pointer.position = ScreenPoint(stored.transform);
            stored.OnPointerEnter(pointer);
            Assert.That(tooltip.IsVisible, Is.True);
            Assert.That(TooltipText(tooltip, "Tooltip Name").text, Is.EqualTo("회복 물약"));
            Assert.That(tooltip.GetComponentsInChildren<PrototypeStatIcon>(), Is.Empty);
            stored.OnBeginDrag(pointer);
            Assert.That(tooltip.IsVisible, Is.False);
            stored.OnEndDrag(pointer);
            yield return null;
            stored.OnPointerEnter(pointer);
            Assert.That(tooltip.IsVisible, Is.True);
            Assert.That(controller.TryStartBattle(), Is.True);
            Assert.That(tooltip.IsVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator CombatHover_ShowsLiveEffectiveStatsAndHidesOnScreenReturn()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            controller.BattleController.AutoTick = false;
            controller.TryStartBattle();
            PrototypeItemTooltip tooltip = controller.ItemTooltip;
            PrototypeBattleItemView club = controller.GetComponentsInChildren<PrototypeBattleItemView>()
                .Single(view => view.Side == CombatSide.Enemy);
            var pointer = new PointerEventData(Object.FindFirstObjectByType<EventSystem>()) { position = ScreenPoint(club.transform) };
            club.OnPointerEnter(pointer);
            Assert.That(tooltip.IsVisible, Is.True);
            Assert.That(TooltipText(tooltip, "Tooltip Name").text, Is.EqualTo("고블린 몽둥이"));
            Assert.That(TooltipText(tooltip, "Tooltip Context").text, Is.EqualTo("전투 중 실제 스탯"));
            Assert.That(TooltipText(tooltip, "Accuracy Value").text, Is.EqualTo("100%"));
            controller.TickCombat(1f);
            PrototypeScreenCapture.CaptureIfRequested(Object.FindFirstObjectByType<Canvas>(), "BattleTooltip.png");

            CombatWeapon enemyWeapon = controller.ActiveCombat.EnemyWeapons.Single();
            enemyWeapon.Stats.AttackDamage = 12f;
            enemyWeapon.Stats.CriticalChancePercent = 25f;
            tooltip.Refresh();
            Assert.That(TooltipText(tooltip, "Attack Value").text, Is.EqualTo("12"));
            Assert.That(TooltipText(tooltip, "Critical Chance Value").text, Is.EqualTo("25%"));
            Assert.That(club.Snapshot.BaseWeaponStats.AttackDamage, Is.EqualTo(5f));
            Assert.That(controller.EnemyData.Items[0].Item.BaseWeaponStats.AttackDamage, Is.EqualTo(5f));
            club.OnPointerExit(pointer);
            Assert.That(tooltip.IsVisible, Is.False);

            PrototypeBattleItemView sword = controller.GetComponentsInChildren<PrototypeBattleItemView>()
                .Single(view => view.Snapshot.ItemId == "training_blade");
            pointer.position = ScreenPoint(sword.transform);
            sword.OnPointerEnter(pointer);
            CombatWeapon playerWeapon = controller.ActiveCombat.PlayerWeapons.Single();
            playerWeapon.Stats.AttackDamage = 100f;
            playerWeapon.Stats.AccuracyPercent = 100f;
            playerWeapon.Stats.CriticalChancePercent = 0f;
            controller.TickCombat(1f);
            Assert.That(controller.Phase, Is.EqualTo(PrototypePhase.Victory));
            Assert.That(controller.AdvanceRound(), Is.True);
            Assert.That(tooltip.IsVisible, Is.False);
        }

        private static Text TooltipText(PrototypeItemTooltip tooltip, string name)
            => tooltip.GetComponentsInChildren<Text>(true).Single(text => text.name == name);
        private static Vector2 ScreenPoint(Transform transform) => RectTransformUtility.WorldToScreenPoint(null, transform.position);
    }
}
