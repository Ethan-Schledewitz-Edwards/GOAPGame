using Construction;
using AssetIndex.Core;
using NoddTracking.Core;
using ObjectTags;
using Settlements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Interaction.Core;

namespace Interaction.ActorInteractables.Structures
{
	[RequireComponent(typeof(BoxCollider), typeof(BlueprintCancelation))]
	public class PlacedBlueprint : BlueprintActorInteractableBase, IMultiPointBlueprintObject
	{
		protected override int m_minActorsToFunction => 0;

		[Header("Settings & Visuals")]
		[SerializeField] private Material m_blueprintMaterial;
		[SerializeField] private MeshFilter m_meshFilter;
		[SerializeField] private MeshRenderer m_meshRenderer;

		[Header("Components")]
		private BlueprintCancelation m_cancelBlueprint;
		private BoxCollider m_boxCollider;

		// IBlueprintObject properties
		public bool IsMultiPointBlueprint { get; set; }

		public event Action<IBlueprintObject> BlueprintCompleted;
		public event Action<IBlueprintObject> BlueprintCanceled;

		// System
		private BlueprintData m_blueprintData;
		private List<Vector3> m_placementPoints = new List<Vector3>();

		protected override void Awake()
		{
			base.Awake();

			m_cancelBlueprint = GetComponent<BlueprintCancelation>();
			m_boxCollider = GetComponent<BoxCollider>();

			m_cancelBlueprint.CanceledBlueprint += HandleBlueprintCanceled;
		}

		private void OnDestroy()
		{
			m_cancelBlueprint.CanceledBlueprint -= HandleBlueprintCanceled;
		}

		public void HandleBlueprintPlaced(BlueprintData blueprintData, Vector3 position, Quaternion rotation)
		{
			if(blueprintData == null) 
				return;

			m_blueprintData = blueprintData;

			m_noddRequestComponent.SetRequiredResources(m_blueprintData.RequiredNodds);

			transform.position = position;
			transform.rotation = rotation;

			Debug.Log($"Starting blueprint: {blueprintData.DisplayName}");
		}

		public void AddPoint(Vector3 position)
		{
			m_placementPoints.Add(position);
		}

		public IReadOnlyList<Vector3> GetPlacementPositions() => m_placementPoints;

		public override void HandleBlueprintCompleted()
		{
			Debug.Log($"A blueprint of SettlementBlueprintID:{m_settlementStructureID} " +
				$"was completed in settlement:{SettlementID}.");

			if (m_blueprintData == null)
				return;

			if (m_blueprintData.PrefabSpawnedOnCompletion != null)
			{
				GameObject prefab = m_blueprintData.PrefabSpawnedOnCompletion;
				GameObject spawnedStructureObj = Instantiate(prefab, transform.position, transform.rotation);

				// Initiate multi point structures on the final built object if needed
				if (m_placementPoints.Count > 1 &&
					spawnedStructureObj != null &&
					spawnedStructureObj.TryGetComponent(out MultiPointStructure multiPointStructure))
				{
					Debug.Log("WTF");

					float spacing = m_blueprintData.MultiPointSpacing;
					multiPointStructure.GenerateMultiPointStructureLinks(m_placementPoints, spacing);
				}

				if (spawnedStructureObj.TryGetComponent(out IStructure builtStructure))
				{
					SettlementManager.s_WorldSettlements[SettlementID].AddStructure(builtStructure);
				}
				else
				{
					Debug.LogWarning($"Prefab {prefab.name} is missing an IStructure component!");
				}

				BlueprintCompleted?.Invoke(this);
			}
			else
			{
				Debug.LogWarning($"Tried to build a structure without an assigned completion prefab.", m_blueprintData);
			}
		}

		public void HandleBlueprintCanceled()
		{
			Debug.Log($"A blueprint of SettlementBlueprintID:{m_settlementStructureID} " +
				$"was canceled in settlement:{SettlementID}.");

			BlueprintCanceled?.Invoke(this);
		}

		/// <summary>
		/// Generates and places mesh segments internally between multiple points along a path using specified spacing.
		/// </summary>
		public void GenerateMultiPointBlueprintLinks()
		{
			if (m_blueprintData == null || 
				m_blueprintData.PlacementMesh == null || 
				m_placementPoints.Count < 2)
				return;

			float spacing = m_blueprintData.MultiPointSpacing;
			Mesh blueprintMesh = m_blueprintData.PlacementMesh;

			for (int i = 0; i < m_placementPoints.Count - 1; i++)
			{
				Vector3 start = m_placementPoints[i];
				Vector3 end = m_placementPoints[i + 1];

				Vector3 direction = end - start;
				float totalDistance = direction.magnitude;
				if (totalDistance <= 0.001f)
					continue;
				direction.Normalize();

				int segmentCount = Mathf.FloorToInt(totalDistance / spacing);
				Quaternion pathRotation = Quaternion.LookRotation(direction, Vector3.up);

				for (int j = 0; j <= segmentCount; j++)
				{
					if (i == 0 && j == 0)
						continue;

					Vector3 spawnPosition = start + (direction * (j * spacing));

					GameObject segmentObject = new GameObject($"Segment_{i}_{j}");
					segmentObject.transform.SetParent(transform, true);
					segmentObject.transform.position = spawnPosition;
					segmentObject.transform.rotation = pathRotation;

					MeshFilter filter = segmentObject.AddComponent<MeshFilter>();
					MeshRenderer renderer = segmentObject.AddComponent<MeshRenderer>();

					filter.mesh = blueprintMesh;
					renderer.sharedMaterial = m_blueprintMaterial;
				}
			}
		}

		public override void LostMinimumActors() { }
	}
}
