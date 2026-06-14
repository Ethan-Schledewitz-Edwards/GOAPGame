using UnityEngine;
using GenericIndex;
using InventorySystem.Items;

[CreateAssetMenu(fileName = "StructureData", menuName = "Structures/StructureData")]
public class StructureData : ScriptableObject, IIndexedAsset
{
	[field: SerializeField] public int ID { get; private set; }
	[field: SerializeField] public string DisplayName { get; private set; }
	[field: SerializeField, TextArea(5,5)] public string Description { get; private set; }
	[field: SerializeField] public Mesh StructureBlueprintMesh { get; private set; }

	[field: SerializeField] public FeatureTileData StructureFeatureData { get; private set; }

	[field: SerializeField] public ItemQuantity[] RequiredItems { get; private set; }

	public void SetID(int newID)
	{
		ID = newID;
	}
}
