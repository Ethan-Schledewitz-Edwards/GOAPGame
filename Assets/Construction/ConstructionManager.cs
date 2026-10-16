using Settlements;
using System;
using System.Collections.Generic;
using UnityEngine;
using AssetIndex.Core;

namespace Construction 
{
	public class ConstructionManager : MonoBehaviour
	{
		public static ConstructionManager Instance;

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
		/// Creates and places a blueprint at the specified world position and rotation within a settlement.
		/// </summary>
		public void CreateBlueprint(
			int settlementID,
			BlueprintData blueprintData,
			Vector3 worldPosition,
			Quaternion rotation,
			out IBlueprintObject blueprintObject)
		{

			if (blueprintData == null)
			{
				blueprintObject = null;

				Debug.LogError(
					$"[ConstructionManager] Tried to create a blueprint but BlueprintData was null.",
					this);
				return;
			}

			if (blueprintData.BlueprintPrefab == null)
			{
				blueprintObject = null;

				Debug.LogError(
					$"[ConstructionManager] Tried to create a blueprint but BlueprintData had no prefab.",
					blueprintData);
				return;
			}

			if (!SettlementManager.s_WorldSettlements.TryGetValue(settlementID, out var settlement)
				|| settlement == null)
			{
				blueprintObject = null;

				Debug.LogError(
					$"Failed to create blueprint '{blueprintData}': Settlement with ID {settlementID} does not exist.",
					this);
				return;
			}

			GameObject prefab = Instantiate(blueprintData.BlueprintPrefab);

			IStructure structure = prefab.GetComponent<IStructure>();
			blueprintObject = prefab.GetComponent<IBlueprintObject>();

			if (structure == null || blueprintObject == null)
			{
				Debug.LogError(
					$"Failed to create blueprint '{blueprintData}': Prefab '{prefab.name}' is missing " +
					$"{(structure == null ? nameof(IStructure) : string.Empty)}" +
					$"{(structure == null && blueprintObject == null ? " and " : string.Empty)}" +
					$"{(blueprintObject == null ? nameof(IBlueprintObject) : string.Empty)}.",
					prefab);

				Destroy(prefab);
				return;
			}

			// Position the blueprint at ground level using its renderer bounds.
			Vector3 offsetPosition = worldPosition;
			if (prefab.TryGetComponent(out MeshRenderer meshRenderer))
			{
				float distanceToBottom = meshRenderer.bounds.min.y;
				offsetPosition.y -= distanceToBottom - worldPosition.y;
			}

			// Initialize the blueprint.
			blueprintObject.HandleBlueprintPlaced(
				blueprintData,
				offsetPosition,
				rotation);

			// Register the structure with the settlement.
			settlement.AddStructure(structure);

			// Subscribe to blueprint events.
			blueprintObject.BlueprintCompleted += CleanupBlueprint;
			blueprintObject.BlueprintCanceled += CleanupBlueprint;
		}

		/// <summary>
		/// Removes the specified blueprint object, its associated structure, and unsubscribes related event handlers.
		/// </summary>
		/// <param name="blueprintObject">The blueprint object to remove and clean up.</param>
		private void CleanupBlueprint(IBlueprintObject blueprintObject)
		{
			if (blueprintObject == null)
			{
				Debug.LogError("[ConstructionManager] Tried to cleanup a blueprint that was null.", this);
				return;
			}

			IStructure structure = SettlementManager.s_WorldSettlements[blueprintObject.SettlementID].
				SettlementStructures[blueprintObject.SettlementStructureID];

			SettlementManager.s_WorldSettlements[blueprintObject.SettlementID].
				RemoveStructure(structure.SettlementStructureID);

			blueprintObject.BlueprintCompleted -= CleanupBlueprint;
			blueprintObject.BlueprintCanceled -= CleanupBlueprint;

			Destroy(blueprintObject.Object);
		}
	}
}