using UnityEngine;
using GenericIndex;
using InventorySystem.Items;

[CreateAssetMenu(fileName = "BlueprintData", menuName = "Structures/BlueprintData")]
public class BlueprintData : ScriptableObject, IIndexedAsset
{
	[field: SerializeField] public int BlueprintID { get; private set; }
	[field: SerializeField] public string DisplayName { get; private set; }
	[field: SerializeField, TextArea(5,5)] public string Description { get; private set; }
	[field: SerializeField] public Mesh BlueprintMesh { get; private set; }

	[field: SerializeField] public FeatureTileData BlueprintFeatureData { get; private set; }

	[field: SerializeField] public ItemQuantity[] RequiredItems { get; private set; }

	public void SetID(int newID)
	{
		BlueprintID = newID;
	}
}
