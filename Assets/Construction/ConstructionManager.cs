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
			string blueprintKey,
			Vector3 worldPosition,
			Quaternion rotation)
		{
			BlueprintData blueprintData = IndexRegistry.GetAsset<BlueprintData>(blueprintKey);

			if (blueprintData == null)
			{
				Debug.LogError(
					$"Failed to create blueprint: No BlueprintData found for key '{blueprintKey}'.",
					this);
				return;
			}

			if (blueprintData.BlueprintPrefab == null)
			{
				Debug.LogError(
					$"Failed to create blueprint '{blueprintKey}': BlueprintPrefab is null.",
					blueprintData);
				return;
			}

			if (!SettlementManager.s_WorldSettlements.TryGetValue(settlementID, out var settlement)
				|| settlement == null)
			{
				Debug.LogError(
					$"Failed to create blueprint '{blueprintKey}': Settlement with ID {settlementID} does not exist.",
					this);
				return;
			}

			GameObject prefab = Instantiate(blueprintData.BlueprintPrefab);

			IStructure structure = prefab.GetComponent<IStructure>();
			IBlueprintObject blueprintObject = prefab.GetComponent<IBlueprintObject>();

			if (structure == null || blueprintObject == null)
			{
				Debug.LogError(
					$"Failed to create blueprint '{blueprintKey}': Prefab '{prefab.name}' is missing " +
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