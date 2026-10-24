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
	[RequireComponent(typeof(BlueprintCancelation))]
	public class PlacedBlueprint : BlueprintActorInteractableBase, IBlueprintObject
	{
		protected override int m_minActorsToFunction => 0;

		[Header("Settings & Visuals")]
		[SerializeField] private Material m_blueprintMaterial;

		[Header("Components")]
		private BlueprintCancelation m_cancelBlueprint;

		// IBlueprintObject properties
		public event Action<IBlueprintObject> BlueprintCompleted;
		public event Action<IBlueprintObject> BlueprintCanceled;

		// System
		private BlueprintData m_blueprintData;

		protected override void Awake()
		{
			base.Awake();

			m_cancelBlueprint = GetComponent<BlueprintCancelation>();

			if (m_noddDepositComponent != null)
				m_noddDepositComponent.IsTrackingEnabled = false;

			m_cancelBlueprint.CanceledBlueprint += HandleBlueprintCanceled;
		}

		private void OnDestroy()
		{
			m_cancelBlueprint.CanceledBlueprint -= HandleBlueprintCanceled;
		}

		public void HandleBlueprintPlaced(BlueprintData blueprintData, Vector3 position, Quaternion rotation)
		{
			if (blueprintData == null ||  blueprintData.IsLinkBlueprint) 
				return;

			m_blueprintData = blueprintData;

			NoddQuantity[] requiredNodds = null;
			requiredNodds = m_blueprintData.RequiredNodds;

			if (requiredNodds != null) 
			{
				m_noddDepositComponent.SetRequiredResources(requiredNodds);

				// Update runtime object tags
				foreach (NoddQuantity i in requiredNodds)
				{
					RuntimeTagSet.Add(i.TypeRequired);
				}
			}

			transform.position = position;
			transform.rotation = rotation;

			if (m_noddDepositComponent != null)
				m_noddDepositComponent.IsTrackingEnabled = true;

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

		public override void LostMinimumActors() { }
	}
}
