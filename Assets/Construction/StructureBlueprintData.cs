using UnityEngine;
using GenericIndex;
using InventorySystem.Items;

namespace Construction
{
	[CreateAssetMenu(fileName = "StructureBlueprint", menuName = "Blueprints/StructureBlueprint")]
	public class StructureBlueprintData : ScriptableObject, IIndexedAsset
	{
		[field: SerializeField] public int StructureBlueprintID { get; private set; }
		[field: SerializeField] public string DisplayName { get; private set; }
		[field: SerializeField, TextArea(5, 5)] public string Description { get; private set; }
		[field: SerializeField] public ItemQuantity[] RequiredItems { get; private set; }
		[field: SerializeField] public FeatureTileData BlueprintFeatureData { get; private set; }
		[field: SerializeField] public Mesh BlueprintMesh { get; private set; }
		[field: SerializeField] public float PlacementClearenceRadius { get; private set; } = 2.0f;
		[field: SerializeField] public Vector3 InteractionLocalOffset { get; private set; }

#if UNITY_EDITOR
		public void SetID(int newID)
		{
			StructureBlueprintID = newID;
		}
#endif
	}
}
