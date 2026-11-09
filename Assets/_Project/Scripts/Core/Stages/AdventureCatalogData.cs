using System.Collections.Generic;
using BackpackAutoBattle.Items;
using UnityEngine;

namespace BackpackAutoBattle.Stages
{
    [CreateAssetMenu(menuName = "Backpack AutoBattle/Adventure Catalog", fileName = "AdventureCatalog")]
    public sealed class AdventureCatalogData : ScriptableObject
    {
        [SerializeField] private CharacterData[] _characters = System.Array.Empty<CharacterData>();
        [SerializeField] private StageData[] _stages = System.Array.Empty<StageData>();
        [SerializeField] private UniqueGearData[] _uniqueGear = System.Array.Empty<UniqueGearData>();
        [SerializeField] private StageData[] _originMines = System.Array.Empty<StageData>();
        public IReadOnlyList<CharacterData> Characters => _characters;
        public IReadOnlyList<StageData> Stages => _stages;
        public IReadOnlyList<UniqueGearData> UniqueGear => _uniqueGear;
        public IReadOnlyList<StageData> OriginMines => _originMines;
    }
}
