using System;
using System.Collections.Generic;
using UnityEngine;

public class ConstructionManager : MonoBehaviour
{
	public static ConstructionManager Instance;

	[SerializeField] private BlueprintIndex m_blueprintIndex;
	[SerializeField] private Material m_blueprintMaterial;

	public event Action<BlueprintData> NewDevelopmentAttempted;

	private void Awake()
	{
		if(Instance == null)
			Instance = this;
		else Destroy(this);
	}

	public void HandleBlueprintButton(BlueprintData blueprintData)
	{
		NewDevelopmentAttempted?.Invoke(blueprintData);
	}

	public void CreateStructureBlueprint(int settlementID, BlueprintData blueprintData, Vector3 position, Quaternion rotation)
	{
		if (blueprintData == null || blueprintData.BlueprintFeatureData?.Prefab == null)
		{
			Debug.LogError("StructureData or Prefab is missing!");
			return;
		}

		GameObject prefab = blueprintData.BlueprintFeatureData.Prefab;
		if (!prefab.TryGetComponent(out InteractableObjectBase interactableObject))
		{
			Debug.LogError($"Prefab {prefab.name} is missing InteractableObjectBase!", prefab);
			return;
		}

		// Create the blueprint object
		GameObject blueprintObject = new GameObject(blueprintData.DisplayName);
		BlueprintIO blueprintIO = blueprintObject.AddComponent<BlueprintIO>();

		// Setup the local interaction offset of the structure being blueprinted
		Transform interactionTransform = interactableObject.GetInteractionOffsetTransform();
		Vector3 localPos = interactionTransform != null ? interactionTransform.localPosition : Vector3.zero;

		GameObject offsetChild = new GameObject("InteractionOffset");
		offsetChild.transform.SetParent(blueprintObject.transform);
		offsetChild.transform.localPosition = localPos;
		blueprintIO.SetInteractionOffsetTransform(offsetChild.transform, localPos);

		Mesh mesh = blueprintData.BlueprintMesh;
		if (mesh != null)
		{
			MeshFilter meshFilter = blueprintObject.AddComponent<MeshFilter>();
			meshFilter.mesh = mesh;

			MeshRenderer meshRenderer = blueprintObject.AddComponent<MeshRenderer>();
			meshRenderer.material = m_blueprintMaterial;

			// Move the mesh out of the ground based on bounds
			Bounds bounds = mesh.bounds;
			blueprintObject.transform.position = position + new Vector3(0, bounds.extents.y, 0);

			BoxCollider bpBoxCol = blueprintObject.AddComponent<BoxCollider>();
			bpBoxCol.isTrigger = true;
			bpBoxCol.size = bounds.size;
		}
		else
			blueprintObject.transform.position = position;

		blueprintObject.transform.rotation = rotation;

		// Add to settlement
		SettlementManager.s_WorldSettlements[settlementID].AddBlueprint(blueprintIO);

		// Init the blueprint
		blueprintIO.InitializeBlueprint(blueprintData.BlueprintID, 
			settlementID, 
			blueprintData.RequiredItems, 
			position, 
			rotation);

		blueprintIO.BlueprintCompleted += OnBlueprintCompleted;
	}

	public void OnBlueprintCompleted(BlueprintIO blueprintIO)
	{
		if (blueprintIO == null)
			return;

		Vector3 blueprintIOPosition = blueprintIO.Position;
		Quaternion blueprintIORotation = blueprintIO.Rotation;
		blueprintIO.BlueprintCompleted -= OnBlueprintCompleted;

		int settlementID = blueprintIO.SettlementID;
		SettlementManager.s_WorldSettlements[settlementID].RemoveBlueprint(blueprintIO);
		Destroy(blueprintIO.gameObject);

		GameObject prefab = m_blueprintIndex.Blueprints[blueprintIO.BlueprintID].BlueprintFeatureData.Prefab;
		Instantiate(prefab, blueprintIOPosition, blueprintIORotation);
	}

	public void CancleBlueprint(BlueprintIO blueprintIO)
	{
		blueprintIO.CancleBlueprint();
	}
}
