using UnityEngine;
using AssetIndex.Core;
using InventorySystem.Items;

/*
	NOTE: IDs are used a lot in this project, and I don't think they're actually necessary
	most of the time. I'm not sure though.

	In the case of BlueprintData (and probably anything else using IIndexedAsset), I'm almost
	certain it would be much simpler to use ScriptableObject.name or ScriptableObject.GetHashCode
	if you want to be performant. This wouldn't require the user to generate IDs and should be
	more source control	friendly.
*/
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
