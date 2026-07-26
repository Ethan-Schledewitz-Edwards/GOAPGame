using UnityEngine;
using GenericIndex;
using InventorySystem.Items;

namespace Construction
{
	[CreateAssetMenu(fileName = "BlueprintData", menuName = "Blueprints/BlueprintData")]
	public class BlueprintData : ScriptableObject, IIndexedAsset
	{
		[field: SerializeField] public int BlueprintDataID { get; private set; }
		[field: SerializeField] public string DisplayName { get; private set; }
		[field: SerializeField, TextArea(5, 5)] public string Description { get; private set; }
		[field: SerializeField] public ItemQuantity[] RequiredItems { get; private set; }
		[field: SerializeField] public FeatureTileData BlueprintFeatureData { get; private set; }

		[Header("Blueprint World Properties")]
		[field: SerializeField] public Mesh BlueprintMesh { get; private set; }
		[field: SerializeField] public float PlacementClearenceRadius { get; private set; } = 0.2f;
		[field: SerializeField] public Vector3 InteractionLocalOffset { get; private set; }

#if UNITY_EDITOR
		public void SetID(int newID)
		{
			BlueprintDataID = newID;
		}
#endif
	}
}
