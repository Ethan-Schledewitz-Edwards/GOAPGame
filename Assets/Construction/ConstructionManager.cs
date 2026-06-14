using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ConstructionManager : MonoBehaviour
{
	public static ConstructionManager Instance;

	[SerializeField] private StructureIndex m_structureIndex;
	[SerializeField] private Material m_blueprintMaterial;

	public event Action<StructureData> NewDevelopmentAttempted;

	private void Awake()
	{
		if(Instance == null)
			Instance = this;
		else Destroy(this);
	}

	public void HandleBlueprintButton(StructureData structureData)
	{
		NewDevelopmentAttempted?.Invoke(structureData);
	}

	public void PlaceStructureBlueprint(StructureData structureData, Vector3 position)
	{
		if (structureData == null || structureData.StructureFeatureData?.Prefab == null)
		{
			Debug.LogError("StructureData or Prefab is missing!");
			return;
		}

		GameObject prefab = structureData.StructureFeatureData.Prefab;
		if (!prefab.TryGetComponent(out InteractableObjectBase interactableObject))
		{
			Debug.LogError($"Prefab {prefab.name} is missing InteractableObjectBase!", prefab);
			return;
		}

		GameObject blueprintObject = new GameObject(structureData.DisplayName);
		ActorBlueprintIO blueprintIO = blueprintObject.AddComponent<ActorBlueprintIO>();


		// Setup the local interaction offset of the structure being blueprinted
		Transform interactionTransform = interactableObject.GetInteractionOffsetTransform();
		Vector3 localPos = interactionTransform != null ? interactionTransform.localPosition : Vector3.zero;

		GameObject offsetChild = new GameObject("InteractionOffset");
		offsetChild.transform.SetParent(blueprintObject.transform);
		offsetChild.transform.localPosition = localPos;
		blueprintIO.SetInteractionOffsetTransform(offsetChild.transform, localPos);

		Mesh mesh = structureData.StructureBlueprintMesh;
		if (mesh != null)
		{
			MeshFilter meshFilter = blueprintObject.AddComponent<MeshFilter>();
			meshFilter.mesh = mesh;

			MeshRenderer meshRenderer = blueprintObject.AddComponent<MeshRenderer>();
			meshRenderer.material = m_blueprintMaterial;

			// Move the mesh out of the ground based on bounds
			Bounds bounds = mesh.bounds;
			blueprintObject.transform.position = position + new Vector3(0, bounds.extents.y, 0);
		}
		else
		{
			blueprintObject.transform.position = position;
		}
	}
}
