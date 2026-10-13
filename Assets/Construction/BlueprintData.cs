using UnityEngine;
using AssetIndex.Core;
using InventorySystem.Items;
using Interaction.Core;

namespace Construction
{
	[CreateAssetMenu(fileName = "BlueprintData", menuName = "Blueprints/BlueprintData")]
	public class BlueprintData : ScriptableObject, IIndexedAsset
	{
		public string ID => name;
		[field: SerializeField] public string DisplayName { get; private set; }
		[field: SerializeField, TextArea(5, 5)] public string Description { get; private set; }
		[field: SerializeField] public ItemQuantity[] RequiredItems { get; private set; }
		[field: SerializeField] public Mesh PlacementMesh { get; private set; }
		[field: SerializeField] public float PlacementClearenceRadius { get; private set; } = 0.2f;
		[field: SerializeField] public GameObject BlueprintPrefab { get; private set; }
		[field: SerializeField] public GameObject PrefabSpawnedOnCompletion { get; private set; }
	}
}
