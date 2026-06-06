using UnityEngine;
using InventorySystem.Items;

[CreateAssetMenu(fileName = "StructureData", menuName = "StructureData/StructureData")]
public class StructureData : ScriptableObject
{
    FeatureTileData structureFeatureData;

	ItemQuantity[] requiredItems;
}
