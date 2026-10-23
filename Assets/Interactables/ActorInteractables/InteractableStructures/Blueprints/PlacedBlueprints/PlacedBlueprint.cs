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
	public class PlacedBlueprint : BlueprintActorInteractableBase, ILinkBlueprintObject
	{
		protected override int m_minActorsToFunction => 0;

		[Header("Settings & Visuals")]
		[SerializeField] private Material m_blueprintMaterial;
		[SerializeField] private Transform m_resourceTrackingPosition;

		[Header("Components")]
		private BlueprintCancelation m_cancelBlueprint;
		private BoxCollider m_boxCollider;
		private NoddDepositComponent m_depositComponent;

		// IBlueprintObject properties

		public event Action<IBlueprintObject> BlueprintCompleted;
		public event Action<IBlueprintObject> BlueprintCanceled;

		// ILinkBlueprintObject properties
		public Vector3 LinkStartPosition => m_linkStartPosition;
		private Vector3 m_linkStartPosition;

		public Vector3 LinkEndPosition => m_linkEndPosition;
		private Vector3 m_linkEndPosition;

		// System
		private BlueprintData m_blueprintData;

		protected override void Awake()
		{
			base.Awake();

			m_cancelBlueprint = GetComponent<BlueprintCancelation>();
			m_boxCollider = GetComponent<BoxCollider>();

			m_depositComponent = GetComponent<NoddDepositComponent>();
			if (m_depositComponent != null)
				m_depositComponent.IsTrackingEnabled = false;

			m_cancelBlueprint.CanceledBlueprint += HandleBlueprintCanceled;
		}

		private void OnDestroy()
		{
			m_cancelBlueprint.CanceledBlueprint -= HandleBlueprintCanceled;
		}

		public void HandleBlueprintPlaced(BlueprintData blueprintData, Vector3 position, Quaternion _)
		{
			if(blueprintData == null) 
				return;

			m_blueprintData = blueprintData;


			NoddQuantity[] requiredNodds = null;
			if (!m_blueprintData.IsLinkBlueprint)
			{
				requiredNodds = m_blueprintData.RequiredNodds;
			}
			else // Calculate the resource cost per segment
			{
				if(m_blueprintData.SegmentResourceType == null)
				{
					Debug.LogWarning("Tried to place a blueprint that is marked as a linked but no " +
						"resource type reference was defined in the blueprint data.", m_blueprintData);

					return;
				}

				GenerateLink(out int segmentCount);
				if (segmentCount == 0)
					Destroy(gameObject);

				int resourceCost = Mathf.RoundToInt(segmentCount * m_blueprintData.ResourceCostPerSegment);

				NoddQuantity noddQuantity = new NoddQuantity();
				noddQuantity.AmountRequired = resourceCost;
				noddQuantity.TypeRequired = m_blueprintData.SegmentResourceType;

				requiredNodds = new NoddQuantity[1];
				requiredNodds[0] = noddQuantity;

				// Move the resource tracker UI to the middle of the link
				// The middle is calculated when generating the link.
				if (m_resourceTrackingPosition != null)
				{
					Vector3 linkMiddlePosition = (m_linkStartPosition + m_linkEndPosition) / 2;
					Vector3 localResourceTrackingPosition = linkMiddlePosition + (Vector3.up * 1.5f);
					m_resourceTrackingPosition.position = localResourceTrackingPosition;
				}
			}

			if (requiredNodds != null) 
			{
				m_noddRequestComponent.SetRequiredResources(requiredNodds);

				// Update runtime object tags
				foreach (NoddQuantity i in requiredNodds)
				{
					RuntimeTagSet.Add(i.TypeRequired);
				}
			}

			transform.position = position;

			if (m_depositComponent != null)
				m_depositComponent.IsTrackingEnabled = true;

			Debug.Log($"Starting blueprint: {blueprintData.DisplayName}");
		}

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

		public void SetLinkStartPosition(Vector3 position)
		{
			m_linkStartPosition = position;
		}

		public void SetLinkEndPosition(Vector3 position)
		{
			m_linkEndPosition = position;
		}

		/// <summary>
		/// Generates and places mesh segments internally between multiple points along a path using specified spacing.
		/// </summary>
		private void GenerateLink(out int segmentCount)
		{
			segmentCount = 0;

			if (m_blueprintData == null || 
				m_blueprintData.PlacementMesh == null)
				return;

			float spacing = m_blueprintData.SegmentSpacing;
			Mesh blueprintMesh = m_blueprintData.PlacementMesh;

			Vector3 direction = m_linkEndPosition - m_linkStartPosition;
			float totalDistance = direction.magnitude;

			direction.Normalize();

			Quaternion pathRotation = Quaternion.LookRotation(direction, Vector3.up);

			segmentCount = Mathf.FloorToInt(totalDistance / spacing);

			List<MeshFilter> meshFilters = new List<MeshFilter>();
			for (int j = 0; j <= segmentCount; j++)
			{
				if (j == 0)
					continue;

				Vector3 spawnPosition = m_linkStartPosition + (direction * (j * spacing));

				GameObject segmentObject = new GameObject($"Segment_{j}");
				segmentObject.transform.SetParent(transform, true);
				segmentObject.transform.position = spawnPosition;
				segmentObject.transform.rotation = pathRotation;

				MeshFilter filter = segmentObject.AddComponent<MeshFilter>();
				meshFilters.Add(filter);

				MeshRenderer renderer = segmentObject.AddComponent<MeshRenderer>();

				filter.mesh = blueprintMesh;
				renderer.sharedMaterial = m_blueprintMaterial;
			}
		}

		public override void LostMinimumActors() { }
	}
}
