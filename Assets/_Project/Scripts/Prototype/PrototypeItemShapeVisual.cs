using System.Collections.Generic;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    /// <summary>The same multi-cell silhouette is used in the bag, drag preview and freeform storage.</summary>
    public static class PrototypeItemShapeVisual
    {
        public static List<Image> Rebuild(RectTransform root, ItemData item, ItemRotation rotation)
        {
            bool wasLocked = root.Find("Fusion Lock") is Transform badge && badge.gameObject.activeSelf;
            foreach (Transform child in root)
            {
                child.name += " (retired)";
                child.gameObject.SetActive(false);
                Object.Destroy(child.gameObject);
            }
            var images = new List<Image>();
            int width = 0;
            int height = 0;
            foreach (Vector2Int cell in item.CreateShape().GetCells(rotation))
            {
                width = Mathf.Max(width, cell.x + 1);
                height = Mathf.Max(height, cell.y + 1);
                var obj = new GameObject($"Cell {cell.x},{cell.y}", typeof(RectTransform), typeof(Image));
                obj.transform.SetParent(root, false);
                RectTransform rect = (RectTransform)obj.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(cell.x, -cell.y) * BackpackPrototypeController.CellPitch;
                rect.sizeDelta = Vector2.one * BackpackPrototypeController.ItemCellSize;
                Image image = obj.GetComponent<Image>();
                image.color = item.DisplayColor;
                images.Add(image);
            }
            root.sizeDelta = new Vector2(width, height) * BackpackPrototypeController.CellPitch;
            Text label = BackpackPrototypeController.CreateText(root, "Label", item.DisplayName, 14,
                FontStyle.Bold, TextAnchor.MiddleCenter);
            label.raycastTarget = false;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 9;
            label.resizeTextMaxSize = 14;
            BackpackPrototypeController.Stretch(label.rectTransform);
            if (wasLocked) SetFusionLocked(root, true);
            return images;
        }

        public static void SetFusionLocked(RectTransform root, bool locked)
        {
            Transform badge = root.Find("Fusion Lock");
            if (badge == null)
            {
                if (!locked) return;
                var obj = new GameObject("Fusion Lock", typeof(RectTransform), typeof(PrototypeShopLockIcon));
                obj.transform.SetParent(root, false);
                var icon = obj.GetComponent<PrototypeShopLockIcon>();
                icon.raycastTarget = false;
                icon.color = new Color(1f, .86f, .25f);
                badge = obj.transform;
            }
            badge.gameObject.SetActive(locked);
            BackpackPrototypeController.SetRect((RectTransform)badge, Vector2.one * .5f, Vector2.zero, new Vector2(16, 20));
            // Keep the lock on top of the item art, with the existing name drawn over it.
            // Position and visibility do not depend on whether the temporary name label exists.
            Transform name = root.Find("Label");
            if (name != null && badge.GetSiblingIndex() > name.GetSiblingIndex())
                badge.SetSiblingIndex(name.GetSiblingIndex());
        }
    }
}
