using Settlements;
using System;
using System.Collections.Generic;
using UnityEngine;
using GenericIndex;
using Entities.Savable;
using Entities.Core;

namespace Construction 
{
	public class ConstructionManager : MonoBehaviour
	{
		public static ConstructionManager Instance;

		[SerializeField] private Material m_blueprintMaterial;
		[SerializeField] private GameObject m_blueprintPrefab;

		// Events
		public event Action<BlueprintData> NewDevelopmentAttempted;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticData()
		{
			Instance = null;
		}

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}

		public void HandleBlueprintButton(BlueprintData blueprintData)
		{
			NewDevelopmentAttempted?.Invoke(blueprintData);
		}

		/// <summary>
		/// Creates and places a structure blueprint at the specified 
		/// world position and rotation within a settlement.
		/// </summary>
		public void CreateBlueprint(int settlementID, 
			string blueprintKey, 
			Vector3 worldPosition, 
			Quaternion rotation)
		{
			if (m_blueprintPrefab == null)
			{
				Debug.LogError("StructureData or Prefab is missing!");
				return;
			}

			BlueprintData blueprintData = 
				IndexRegistry.GetAsset<BlueprintData>(blueprintKey);

			GameObject prefab = Instantiate(m_blueprintPrefab);
			IStructure structure = prefab.GetComponent<IStructure>();
			IBlueprintObject blueprintObject = prefab.GetComponent<IBlueprintObject>();

			// Add to settlement
			SettlementManager.s_WorldSettlements[settlementID].AddStructure(structure);

			// Place the blueprint on the ground
			Bounds blueprintBounds = blueprintData.BlueprintMesh.bounds;
			float distanceToBottom = (blueprintBounds.center.y - blueprintBounds.extents.y);
			Vector3 offsetPosition = worldPosition + Vector3.up * distanceToBottom;

			// Init the blueprint
			blueprintObject.HandleBlueprintPlaced
				(
					blueprintData,
					offsetPosition,
					rotation
				);

			blueprintObject.BlueprintCompleted += OnBlueprintCompleted;
			blueprintObject.BlueprintCanceled += OnBlueprintCanceled;
		}

		public void OnBlueprintCompleted(IBlueprintObject blueprintObject)
		{
			if (blueprintObject == null)
				return;

			CleanupBlueprint(blueprintObject);
		}

		public void OnBlueprintCanceled(IBlueprintObject blueprintIO)
		{
			if (blueprintIO == null)
				return;

			CleanupBlueprint(blueprintIO);
		}

		private void CleanupBlueprint(IBlueprintObject blueprintObject)
		{
			IStructure structure = SettlementManager.s_WorldSettlements[blueprintObject.SettlementID].
				SettlementStructures[blueprintObject.SettlementStructureID];

			SettlementManager.s_WorldSettlements[blueprintObject.SettlementID].
				RemoveStructure(structure.SettlementStructureID);

			blueprintObject.BlueprintCompleted -= OnBlueprintCompleted;
			blueprintObject.BlueprintCanceled -= OnBlueprintCanceled;

			Destroy(blueprintObject.Object);
		}
	}
}