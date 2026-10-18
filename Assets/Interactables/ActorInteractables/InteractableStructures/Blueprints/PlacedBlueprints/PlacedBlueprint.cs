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
	public class PlacedBlueprint : BlueprintActorInteractableBase, IBlueprintObject
	{
		protected override int m_minActorsToFunction => 0;

		[Header("Settings & Visuals")]
		[SerializeField] private Material m_blueprintMaterial;
		[SerializeField] private MeshFilter m_meshFilter;
		[SerializeField] private MeshRenderer m_meshRenderer;

		[Header("Components")]
		private BlueprintCancelation m_cancelBlueprint;
		private BoxCollider m_boxCollider;

		private BlueprintData m_blueprintData;

		public event Action<IBlueprintObject> BlueprintCompleted;
		public event Action<IBlueprintObject> BlueprintCanceled;

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
			m_blueprintData = blueprintData;

			m_noddRequestComponent.SetRequiredResources(m_blueprintData.RequiredNodds);

			transform.position = position;
			transform.rotation = rotation;

			Debug.Log($"Starting blueprint: {blueprintData.DisplayName}");
		}

		public void HandlePointAdded(Vector3 position) { }

		public override void HandleBlueprintCompleted()
		{
			Debug.Log($"A blueprint of SettlementBlueprintID:{m_settlementStructureID} " +
				$"was completed in settlement:{SettlementID}.");

			// Create the final structure
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

		public void HandleBlueprintCanceled()
		{
			Debug.Log($"A blueprint of SettlementBlueprintID:{m_settlementStructureID} " +
				$"was canceled in settlement:{SettlementID}.");

			BlueprintCanceled?.Invoke(this);
		}

		public override void LostMinimumActors() { }
	}
}
