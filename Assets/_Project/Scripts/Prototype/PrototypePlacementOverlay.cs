using System.Collections.Generic;
using System.Linq;
using BackpackAutoBattle.Combat;
using UnityEngine;

namespace BackpackAutoBattle.Prototype
{
    public sealed class PrototypePlacementOverlay
    {
        private readonly RectTransform _root;
        private readonly float _pitch;
        private readonly List<PrototypePlacementDiamond> _markers = new();
        private Component _owner;
        public PrototypePlacementOverlay(RectTransform board, float pitch)
        {
            var obj = new GameObject("Placement Effect Overlay", typeof(RectTransform));
            obj.transform.SetParent(board, false);
            _root = (RectTransform)obj.transform;
            BackpackPrototypeController.Stretch(_root);
            _pitch = pitch;
            Hide();
        }
        public void Show(Component owner, CombatLoadoutSnapshot loadout, CombatItemSnapshot source, bool forceOutline = false)
        {
            _owner = owner;
            var cells = PlacementEffectResolver.GetCells(loadout, source).GroupBy(cell => cell.Cell).ToArray();
            _root.gameObject.SetActive(cells.Length > 0);
            _root.SetAsLastSibling();
            int index = 0;
            foreach (var cell in cells)
            {
                if (index == _markers.Count)
                {
                    var obj = new GameObject("Placement Range Diamond", typeof(RectTransform), typeof(PrototypePlacementDiamond));
                    obj.transform.SetParent(_root, false);
                    _markers.Add(obj.GetComponent<PrototypePlacementDiamond>());
                }
                PrototypePlacementDiamond marker = _markers[index++];
                marker.gameObject.SetActive(true);
                marker.Set(cell.Key, !forceOutline && cell.Any(result => result.ReceivesEffect));
                BackpackPrototypeController.SetRect(marker.rectTransform, Vector2.one * 0.5f,
                    new Vector2((cell.Key.x + 0.5f - loadout.Width / 2f) * _pitch,
                        (loadout.Height / 2f - cell.Key.y - 0.5f) * _pitch), Vector2.one * (_pitch * 0.4f));
            }
            for (; index < _markers.Count; index++) _markers[index].gameObject.SetActive(false);
        }
        public void Hide(Component owner = null)
        {
            if (owner != null && _owner != owner) return;
            _owner = null;
            _root.gameObject.SetActive(false);
        }
    }
}
