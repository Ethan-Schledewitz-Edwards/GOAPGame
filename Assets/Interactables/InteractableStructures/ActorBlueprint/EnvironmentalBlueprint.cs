using Construction;
using Entities.Savable;
using InventorySystem;
using System;
using UnityEngine;

namespace Interaction.InteractableStructures.Blueprints
{
    public class EnvironmentalBlueprint : BlueprintIO, IBlueprintObject
	{
		[SerializeField] private GameObject m_prefab;
		[SerializeField] private bool m_isPrefabInstantiated;

		public override event Action<IBlueprintObject> BlueprintCompleted;
		public override event Action<IBlueprintObject> BlueprintCanceled;

		// IBlueprintObject Properties

		public void HandleBlueprintStarted(BlueprintData structureBlueprintData, Vector3 position, Quaternion rotation)
		{
			m_saveableEntity.InitializeSavableEntity();
		}

		public override void HandleBlueprintCompleted()
		{
			Debug.Log($"A blueprint of SettlementBlueprintID:{m_settlementStructureID} was completed in settlement:{SettlementID}.");
			BlueprintCompleted?.Invoke(this);
		}
	}
}
