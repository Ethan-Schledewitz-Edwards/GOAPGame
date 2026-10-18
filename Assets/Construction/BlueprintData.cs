using UnityEngine;
using AssetIndex.Core;
using InventorySystem.Items;
using Interaction.Core;
using System;
using ObjectTags;

namespace Construction
{
	[Serializable]
	public struct NoddRequirement
	{
		public ResourceTypeTag TypeRequired;
		public int AmountRequired;
	}

	[CreateAssetMenu(fileName = "BlueprintData", menuName = "Blueprints/BlueprintData")]
	public class BlueprintData : ScriptableObject, IIndexedAsset
	{
		public string ID => name;

		[field: SerializeField] public string DisplayName { get; private set; }
		[field: SerializeField, TextArea(5, 5)] public string Description { get; private set; }

		[Header("Completion Requirments")]
		[field: SerializeField] public NoddRequirement[] RequiredItems { get; private set; }

		[Header("Placement Properties")]
		[field: SerializeField] public Mesh PlacementMesh { get; private set; }
		[field: SerializeField] public float PlacementClearenceRadius { get; private set; } = 0.2f;
		[field: SerializeField] public float MaxPlacementAngle { get; private set; } = 15f;
		[field: SerializeField] public bool IsMultiPointBlueprint { get; private set; }
		[field: SerializeField] public int MaxPoints { get; private set; }
		[field: SerializeField] public float MultiPointSpacing { get; private set; } = 0.5f;

		[Header("Blueprint Prefabs")]
		[field: SerializeField] public GameObject BlueprintPrefab { get; private set; }
		[field: SerializeField] public GameObject PrefabSpawnedOnCompletion { get; private set; }
	}
}
