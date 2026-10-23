using UnityEngine;
using AssetIndex.Core;
using NoddTracking.Core;
using Interaction.Core;
using System;
using ObjectTags;

namespace Construction
{
	[CreateAssetMenu(fileName = "BlueprintData", menuName = "Blueprints/BlueprintData")]
	public class BlueprintData : ScriptableObject, IIndexedAsset
	{
		public string ID => name;

		[field: SerializeField] public string DisplayName { get; private set; }
		[field: SerializeField, TextArea(5, 5)] public string Description { get; private set; }

		[Header("Completion Requirments")]
		[field: SerializeField] public NoddQuantity[] RequiredNodds { get; private set; }

		[Header("Placement Properties")]
		[field: SerializeField] public Mesh PlacementMesh { get; private set; }
		[field: SerializeField] public float PlacementClearenceRadius { get; private set; } = 0.2f;
		[field: SerializeField] public float MaxPlacementAngle { get; private set; } = 15f;

		[Header("Link Properties")]
		[field: SerializeField] public bool IsLinkBlueprint { get; private set; } // Blueprints that are created between two points
		[field: SerializeField] public float SegmentSpacing { get; private set; } = 0.5f;
		[field: SerializeField] public ResourceTypeTag SegmentResourceType { get; private set; }
		[field: SerializeField] public float ResourceCostPerSegment { get; private set; } = 0.25f;

		[Header("Blueprint Prefabs")]
		[field: SerializeField] public GameObject BlueprintPrefab { get; private set; }
		[field: SerializeField] public GameObject PrefabSpawnedOnCompletion { get; private set; }
	}
}
