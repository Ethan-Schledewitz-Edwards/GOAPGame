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
		/// Creates a blueprint prefab at the specified world position.
		/// </summary>
		public void CreateBlueprintPreview(
			BlueprintData blueprintData,
			Vector3 worldPosition,
			out IBlueprintObject blueprintObject)
		{
			blueprintObject = null;

			if (blueprintData?.BlueprintPrefab == null)
			{
				Debug.LogError($"[ConstructionManager] Tried to create a blueprint, but BlueprintData or its prefab is null.", this);
				return;
			}

			GameObject prefab = Instantiate(blueprintData.BlueprintPrefab);

			if (!prefab.TryGetComponent<IStructure>(out _) || !prefab.TryGetComponent(out blueprintObject))
			{
				Debug.LogError($"[ConstructionManager] Prefab '{prefab.name}' is missing IStructure or IBlueprintObject.", prefab);
				Destroy(prefab);
				blueprintObject = null;
				return;
			}

			// Position the blueprint at ground level using its renderer bounds if available.
			if (prefab.TryGetComponent(out MeshRenderer meshRenderer))
			{
				worldPosition.y -= meshRenderer.bounds.min.y - worldPosition.y;
			}

			prefab.transform.position = worldPosition;
		}

		/// <summary>
		/// Initializes a placed a blueprint preview and assigns it to a settlement.
		/// </summary>
		public void PlacePreviewBlueprint(
			int settlementID,
			BlueprintData blueprintData,
			Vector3 worldPosition,
			Quaternion rotation,
			IBlueprintObject blueprintObject)
		{
			if (blueprintObject == null)
			{
				Debug.LogError("[ConstructionManager] Tried to place a null blueprint object.", this);
				return;
			}

			if (!SettlementManager.s_WorldSettlements.TryGetValue(settlementID, out var settlement) || settlement == null)
			{
				Debug.LogError($"[ConstructionManager] Settlement with ID {settlementID} does not exist.", this);
				return;
			}

			// Initialize the blueprint.
			blueprintObject.HandleBlueprintPlaced(blueprintData, worldPosition, rotation);

			// Register the structure with the settlement.
			if (blueprintObject.Object.TryGetComponent<IStructure>(out var structure))
			{
				settlement.AddStructure(structure);
			}
			else
			{
				Debug.LogError($"[ConstructionManager] Blueprint object prefab is missing an IStructure component.", blueprintObject.Object);
				return;
			}

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