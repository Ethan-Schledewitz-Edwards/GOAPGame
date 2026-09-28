using UnityEngine;
using AssetIndex.Core;
using InventorySystem.Items;

namespace Construction
{
	[CreateAssetMenu(fileName = "BlueprintData", menuName = "Blueprints/BlueprintData")]
	public class BlueprintData : ScriptableObject, IIndexedAsset
	{
		[SerializeField] private string m_id;
		public string ID => m_id;
		[field: SerializeField] public string DisplayName { get; private set; }
		[field: SerializeField, TextArea(5, 5)] public string Description { get; private set; }
		[field: SerializeField] public ItemQuantity[] RequiredItems { get; private set; }
		[field: SerializeField] public GameObject PrefabSpawnedOnCompletion { get; private set; }

		[Header("Blueprint World Properties")]
		[field: SerializeField] public Mesh BlueprintMesh { get; private set; }
		[field: SerializeField] public float PlacementClearenceRadius { get; private set; } = 0.2f;
		[field: SerializeField] public InteractionPositionConfig[] InteractionPositions { get; private set; }

#if UNITY_EDITOR
		public void SetID(string id) => m_id = id;
#endif
	}
}
