using Settlements;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ConstructionManager : MonoBehaviour
{
	public static ConstructionManager Instance;

	[SerializeField] private BlueprintDataIndex m_blueprintIndex;
	[SerializeField] private Material m_blueprintMaterial;

	[SerializeField] private GameObject m_blueprintPrefab;

	// Events
	public event Action<StructureBlueprintData> NewDevelopmentAttempted;

	private void Awake()
	{
		if(Instance == null)
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

		StructureBlueprintData blueprintData = m_blueprintIndex.Assets[structureBlueprintID];

		GameObject prefab = Instantiate(m_blueprintPrefab);
		IStructure structure = prefab.GetComponent<IStructure>();
		IBlueprintObject blueprintObject = prefab.GetComponent<IBlueprintObject>();

		// Add to settlement
		SettlementManager.s_WorldSettlements[settlementID].AddStructure(structure, out int structureID);

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

		IStructure blueprintStructure = SettlementManager.s_WorldSettlements[blueprintObject.SettlementID].SettlementStructures[blueprintObject.StructureID];
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
		builtStructure.StructureID = structureID;
	}

	public void OnBlueprintCanceled(IBlueprintObject blueprintIO)
	{
		if (blueprintIO == null)
			return;

		CleanupBlueprint(blueprintIO);
	}

	private void CleanupBlueprint(IBlueprintObject blueprintObject)
	{
		IStructure structure = SettlementManager.s_WorldSettlements[blueprintObject.SettlementID].SettlementStructures[blueprintObject.StructureID];
		SettlementManager.s_WorldSettlements[blueprintObject.SettlementID].RemoveStructure(structure);

		blueprintObject.BlueprintCompleted -= OnBlueprintCompleted;
		blueprintObject.BlueprintCompleted -= OnBlueprintCanceled;

		Destroy(blueprintObject.BlueprintObject);
	}
}
