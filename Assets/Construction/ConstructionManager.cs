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

		/// <summary>
		/// Creates and places a structure blueprint at the specified world position and rotation within a settlement.
		/// </summary>
		/// <param name="settlementID">The identifier of the settlement to which the structure will be added.</param>
		/// <param name="structureBlueprintID">The identifier of the structure blueprint asset to instantiate.</param>
		/// <param name="worldPosition">The world position where the blueprint will be placed.</param>
		/// <param name="rotation">The rotation to apply to the blueprint upon creation.</param>
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
			IStructure structure = SettlementManager.s_WorldSettlements[blueprintObject.SettlementID].SettlementStructures[blueprintObject.SettlementStructureID];
			SettlementManager.s_WorldSettlements[blueprintObject.SettlementID].RemoveStructure(structure.SettlementStructureID);

			blueprintObject.BlueprintCompleted -= OnBlueprintCompleted;
			blueprintObject.BlueprintCanceled -= OnBlueprintCanceled;

			Destroy(blueprintObject.Object);
		}
	}
}