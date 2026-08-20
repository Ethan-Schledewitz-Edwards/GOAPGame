using Construction;
using Entities.Savable;
using InventorySystem;
using System;
using UnityEngine;

namespace Interaction.InteractableStructures.Blueprints
{
    public class EnvironmentalBlueprint : BlueprintIO, IBlueprintObject
	{
		public override event Action<IBlueprintObject> BlueprintCompleted;
		public override event Action<IBlueprintObject> BlueprintCanceled;

		// IBlueprintObject Properties
		public int BlueprintDataID => m_blueprintDataID;

		public void HandleBlueprintStarted(BlueprintData structureBlueprintData, Vector3 position, Quaternion rotation)
		{
			throw new NotImplementedException();
		}

		public override void HandleBlueprintCanceled()
		{
			Debug.Log($"A blueprint of SettlementBlueprintID:{m_settlementStructureID} was canceld in settlement:{SettlementID}.");
			BlueprintCanceled?.Invoke(this);

			foreach (InventorySlot slot in m_inventoryComponent.Slots)
			{
				slot.RemoveFromStack(slot.AmountInSlot, out var _, true, transform.position);
			}

			Destroy(gameObject);
		}

		public override void HandleBlueprintCompleted()
		{
			m_saveableEntity.InitializeSavableEntity();

			Debug.Log($"A blueprint of SettlementBlueprintID:{m_settlementStructureID} was completed in settlement:{SettlementID}.");
			BlueprintCompleted?.Invoke(this);
		}
	}
}
