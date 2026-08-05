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

		private void Awake()
		{
			if (Instance == null)
				Instance = this;
			else Destroy(this);
		}

		public void HandleBlueprintButton(BlueprintData blueprintData)
		{
			NewDevelopmentAttempted?.Invoke(blueprintData);
		}

		public void CreateBlueprint(int settlementID, int structureBlueprintID, Vector3 worldPosition, Quaternion rotation)
		{
			if (m_blueprintPrefab == null)
			{
				Debug.LogError("StructureData or Prefab is missing!");
				return;
			}

			BlueprintData blueprintData = IndexRegistry.GetAsset<BlueprintData>(structureBlueprintID);

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
			blueprintObject.HandleBlueprintStarted
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

			int settlementID = blueprintObject.SettlementID;
			Vector3 blueprintIOPosition = blueprintObject.Object.transform.position;
			Quaternion blueprintIORotation = blueprintObject.Object.transform.rotation;

			CleanupBlueprint(blueprintObject);

			// Create the final structure
			BlueprintData blueprintData = IndexRegistry.GetAsset<BlueprintData>(blueprintObject.BlueprintDataID);
			GameObject prefab = blueprintData.BlueprintFeatureData.Prefab;
			GameObject spawnedStructureObj = Instantiate(prefab, blueprintIOPosition, blueprintIORotation);

			if (spawnedStructureObj.TryGetComponent(out IStructure builtStructure))
			{
				SettlementManager.s_WorldSettlements[settlementID].AddStructure(builtStructure);
			}
			else
			{
				Debug.LogError($"Prefab {prefab.name} is missing an IStructure component!");
			}

			//// Stop the structure entity from allowing movement
			//if (spawnedStructureObj.TryGetComponent(out Entity entity))
			//{
			//	entity.EnableDynamicPositionUpdates(false);
			//}

			if (spawnedStructureObj.TryGetComponent(out SaveableEntity saveableEntity))
			{
				saveableEntity.InitializeSavableEntity();
			}
		}

		public void OnBlueprintCanceled(IBlueprintObject blueprintIO)
		{
			if (blueprintIO == null)
				return;

			CleanupBlueprint(blueprintIO);
		}

		private void CleanupBlueprint(IBlueprintObject blueprintObject)
		{
			IStructure structure = SettlementManager.s_WorldSettlements[blueprintObject.SettlementID].SettlementStructures[blueprintObject.SettlementStructureID];
			SettlementManager.s_WorldSettlements[blueprintObject.SettlementID].RemoveStructure(structure.SettlementStructureID);

			blueprintObject.BlueprintCompleted -= OnBlueprintCompleted;
			blueprintObject.BlueprintCanceled -= OnBlueprintCanceled;

			Destroy(blueprintObject.Object);
		}
	}
}