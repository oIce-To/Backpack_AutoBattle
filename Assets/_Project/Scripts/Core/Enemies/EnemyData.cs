using UnityEngine;

namespace BackpackAutoBattle.Enemies
{
    public enum EnemyTrait
    {
        None, VenomImmunity, StoneArmor, Dryad, BurnImmunity, Pharaoh, PenitentPriest,
        FalseIdol, Yeti, NamelessKnight, FreezeImmunity, WeaponMaster, LegionCommander
    }

    /// <summary>Shared species identity. Combat numbers belong to encounters, not this asset.</summary>
    [CreateAssetMenu(menuName = "Backpack AutoBattle/Enemy Type", fileName = "EnemyType_")]
    public sealed class EnemyData : ScriptableObject
    {
        [SerializeField] private string _enemyId;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _illustration;
        [SerializeField] private bool _illustrationFacesLeft;
        [SerializeField] private EnemyTrait _trait;
        [Tooltip("Boss icon crop in normalized sprite coordinates, measured from the bottom left.")]
        [SerializeField] private Rect _portraitCrop = new(0f, 0f, 1f, 1f);
        public string EnemyId => _enemyId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Illustration => _illustration;
        public bool IllustrationFacesLeft => _illustrationFacesLeft;
        public EnemyTrait Trait => _trait;
        public Rect PortraitCrop => _portraitCrop;
    }
}
