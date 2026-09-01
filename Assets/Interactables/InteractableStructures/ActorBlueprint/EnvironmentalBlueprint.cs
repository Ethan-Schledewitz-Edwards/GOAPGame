using Construction;
using Entities.Savable;
using InventorySystem;
using InventorySystem.Items;
using System;
using UnityEngine;

namespace Interaction.InteractableStructures.Blueprints
{
    public class EnvironmentalBlueprint : BlueprintIO, IBlueprintObject
	{
		[SerializeField] private ItemQuantity[] m_requiredItems;

		[SerializeField] private GameObject m_prefab;
		[SerializeField] private bool m_isPrefabInstantiated;

		public event Action<IBlueprintObject> BlueprintCompleted;
		public event Action<IBlueprintObject> BlueprintCanceled;

		// IBlueprintObject Properties

		protected override void Awake()
		{
			base.Awake();

			m_itemRequestComponent.SetRequiredItems(m_inventoryComponent.Inventory, m_requiredItems);

			m_saveableEntity.InitializeSavableEntity();
		}

		public override void HandleBlueprintCompleted()
		{
			Debug.Log($"A blueprint of SettlementBlueprintID:{m_settlementStructureID} was completed in settlement:{SettlementID}.");
			BlueprintCompleted?.Invoke(this);
		}

		public void HandleBlueprintCanceled()
		{
			Debug.Log($"A blueprint of SettlementBlueprintID:{m_settlementStructureID} was canceled in settlement:{SettlementID}.");

			foreach (InventorySlot slot in m_inventoryComponent.Slots)
			{
				slot.RemoveFromStack(slot.AmountInSlot, out var _, true, transform.position);
			}

			BlueprintCanceled?.Invoke(this);
		}

		public void HandleBlueprintPlaced(BlueprintData structureBlueprintData, Vector3 position, Quaternion rotation) { }
	}
}
