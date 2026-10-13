using BackpackAutoBattle.Items;
using UnityEngine;

namespace BackpackAutoBattle.Prototype
{
    public sealed class PrototypeStoredItem
    {
        public PrototypeStoredItem(string instanceId, ItemData itemData, Vector2 storagePosition)
        {
            InstanceId = instanceId;
            ItemData = itemData;
            StoragePosition = storagePosition;
        }

        public string InstanceId { get; }

        public ItemData ItemData { get; }

        public Vector2 StoragePosition { get; set; }
    }
}
