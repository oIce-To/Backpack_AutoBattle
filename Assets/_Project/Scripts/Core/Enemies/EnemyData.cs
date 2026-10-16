using UnityEngine;

namespace BackpackAutoBattle.Enemies
{
    /// <summary>Shared species identity. Combat numbers belong to encounters, not this asset.</summary>
    [CreateAssetMenu(menuName = "Backpack AutoBattle/Enemy Type", fileName = "EnemyType_")]
    public sealed class EnemyData : ScriptableObject
    {
        [SerializeField] private string _enemyId;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _illustration;
        public string EnemyId => _enemyId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Illustration => _illustration;
    }
}
