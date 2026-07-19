using Settlements;
using System;
using System.Collections.Generic;
using UnityEngine;
using GenericIndex;

namespace Construction 
{
	public class ConstructionManager : MonoBehaviour
	{
		public static ConstructionManager Instance;

		public BlueprintDataIndex BlueprintIndex => m_blueprintIndex;
		[SerializeField] private BlueprintDataIndex m_blueprintIndex;

		[SerializeField] private Material m_blueprintMaterial;
		[SerializeField] private GameObject m_blueprintPrefab;

		// Events
		public event Action<StructureBlueprintData> NewDevelopmentAttempted;

		private void Awake()
		{
			if (Instance == null)
				Instance = this;
			else Destroy(this);
		}

		public void HandleBlueprintButton(StructureBlueprintData blueprintData)
		{
			NewDevelopmentAttempted?.Invoke(blueprintData);
		}

		public void CreateStructureBlueprint(int settlementID, int structureBlueprintID, Vector3 worldPosition, Quaternion rotation)
		{
			if (m_blueprintPrefab == null)
			{
				Debug.LogError("StructureData or Prefab is missing!");
				return;
			}

			StructureBlueprintData blueprintData = IndexRegistry.GetAsset<StructureBlueprintData>(structureBlueprintID);
			//StructureBlueprintData blueprintData = m_blueprintIndex.GetIndexedAsset(structureBlueprintID);

			GameObject prefab = Instantiate(m_blueprintPrefab);
			IStructure structure = prefab.GetComponent<IStructure>();
			IBlueprintObject blueprintObject = prefab.GetComponent<IBlueprintObject>();

			// Add to settlement
			SettlementManager.s_WorldSettlements[settlementID].AddStructure(structure, out int structureID);
			structure.SettlementStructureID = structureID;

			// Offset the blueprint out of the ground
			Bounds bounds = blueprintData.BlueprintMesh.bounds;
			Vector3 offsetPosition = worldPosition + new Vector3(0, bounds.extents.y, 0);

			// Init the blueprint
			blueprintObject.HandleBlueprintStarted
				(
					settlementID,
					structureID,
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

			IStructure blueprintStructure = SettlementManager.s_WorldSettlements[blueprintObject.SettlementID].SettlementStructures[blueprintObject.SettlementStructureID];
			int settlementID = blueprintObject.SettlementID;
			Vector3 blueprintIOPosition = blueprintObject.BlueprintObject.transform.position;
			Quaternion blueprintIORotation = blueprintObject.BlueprintObject.transform.rotation;

			CleanupBlueprint(blueprintObject);

			// Create the final structure
			GameObject prefab = m_blueprintIndex.StructureBlueprintData[blueprintObject.StructureBlueprintID].BlueprintFeatureData.Prefab;
			Instantiate(prefab, blueprintIOPosition, blueprintIORotation);

			// Add the structure to the settlement
			IStructure builtStructure = prefab.GetComponent<IStructure>();
			SettlementManager.s_WorldSettlements[settlementID].AddStructure(builtStructure, out int structureID);
			builtStructure.SettlementStructureID = structureID;

			Debug.Log($"Added structure of StructureID:{structureID} to Settlement of SettlementID:{settlementID}");
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

			Debug.Log($"Removed blueprint of StructureID:{blueprintObject.SettlementStructureID} from Settlement of SettlementID:{blueprintObject.SettlementID}");

			blueprintObject.BlueprintCompleted -= OnBlueprintCompleted;
			blueprintObject.BlueprintCanceled -= OnBlueprintCanceled;

			Destroy(blueprintObject.BlueprintObject);
		}
	}

}