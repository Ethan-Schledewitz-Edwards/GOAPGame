using InventorySystem;
using InventorySystem.Items;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Interaction.InteractableStructures.Blueprints
{
	public class BluerprintInventoryComponent : InventoryComponent
	{
		private ItemQuantity[] m_requiredItemsToBuild;

		public event Action BlueprintItemsAchieved;

		private void OnDestroy()
		{
			Inventory.SlotChanged -= HandleInventorySlotUpdated;
		}

		public void InitializeBlueprintInventory(ItemQuantity[] itemsRequiredToBuild)
		{
			m_requiredItemsToBuild = itemsRequiredToBuild;
			int slotsRequired = m_requiredItemsToBuild.Length;

			InitializeInventory(slotsRequired);
			Inventory.SlotChanged += HandleInventorySlotUpdated;
		}

		private void HandleInventorySlotUpdated(InventorySlot _)
		{
			bool inventoryMeetsRequiredItemQuantities = false;
			foreach (ItemQuantity quantity in m_requiredItemsToBuild)
			{
				if (GetItemTypeSatisfied(quantity))
				{
					inventoryMeetsRequiredItemQuantities = true;
					break;
				}
			}

			if (inventoryMeetsRequiredItemQuantities)
				BlueprintItemsAchieved?.Invoke();
		}

		public bool GetItemTypeSatisfied(ItemQuantity quantity)
		{
			ItemData itemData = quantity.itemType;
			int amountNeeded = quantity.amount;
			return Inventory.GetTotalOfItem(itemData.ItemID) >= amountNeeded;
		}

		public override bool TryAddItem(ItemData addedItemData, int amount, Transform[] itemTransforms = null)
		{
			if (addedItemData == null)
			{
				Debug.Log("Tried to add an item without data to a blueprint inventory.");
				return false;
			}

			if (amount <= 0)
			{
				Debug.Log("Tried to add an item with a quantity of zero to a blueprint inventory.");
				return false;
			}

			bool isItemNeeded = false;
			foreach (ItemQuantity itemQuantity in m_requiredItemsToBuild)
			{
				isItemNeeded = itemQuantity.itemType.ItemID == addedItemData.ItemID;

				if (isItemNeeded)
					break;
			}

			if (isItemNeeded)
				return base.TryAddItem(addedItemData, amount, itemTransforms);

			return false;
		}
	}

}