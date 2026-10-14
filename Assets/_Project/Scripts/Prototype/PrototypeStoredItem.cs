using BackpackAutoBattle.Items;
using BackpackAutoBattle.Inventory;
using UnityEngine;

namespace BackpackAutoBattle.Prototype
{
    public sealed class PrototypeStoredItem
    {
        public PrototypeStoredItem(string instanceId, ItemData itemData, Vector2 storagePosition, ItemRotation rotation = ItemRotation.None)
        {
            InstanceId = instanceId;
            ItemData = itemData;
            StoragePosition = storagePosition;
            Rotation = rotation;
        }

        public string InstanceId { get; }

        public ItemData ItemData { get; }

        public Vector2 StoragePosition { get; set; }
        public ItemRotation Rotation { get; set; }
    }
}
