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

	public void CreateStructureBlueprint(int settlementID, int structureBlueprintID, Vector3 position, Quaternion rotation)
	{
		if (m_blueprintPrefab == null)
		{
			Debug.LogError("StructureData or Prefab is missing!");
			return;
		}

		StructureBlueprintData blueprintData = m_blueprintIndex.Assets[structureBlueprintID];

		GameObject prefab = Instantiate(m_blueprintPrefab);
		IBlueprintObject blueprintObject = prefab.GetComponent<IBlueprintObject>();

		// Add to settlement
		SettlementManager.s_WorldSettlements[settlementID].AddBlueprint(prefab, out int settlementBlueprintID);

		// Init the blueprint
		blueprintObject.HandleBlueprintStarted
			(
				settlementID, 
				settlementBlueprintID, 
				blueprintData,
				position,
				rotation
			);

		blueprintObject.BlueprintCompleted += OnBlueprintCompleted;
	}

	public void OnBlueprintCompleted(IBlueprintObject blueprintIO)
	{
		if (blueprintIO == null)
			return;

		GameObject blueprintObject = SettlementManager.s_WorldSettlements[blueprintIO.SettlementID].Blueprints[blueprintIO.SettlementBlueprintID];

		Vector3 blueprintIOPosition = blueprintObject.transform.position;
		Quaternion blueprintIORotation = blueprintObject.transform.rotation;
		blueprintIO.BlueprintCompleted -= OnBlueprintCompleted;

		int settlementID = blueprintIO.SettlementID;
		SettlementManager.s_WorldSettlements[settlementID].RemoveBlueprint(blueprintObject);
		Destroy(blueprintObject);

		GameObject prefab = m_blueprintIndex.StructureBlueprintData[blueprintIO.StructureBlueprintID].BlueprintFeatureData.Prefab;
		Instantiate(prefab, blueprintIOPosition, blueprintIORotation);
	}
}
