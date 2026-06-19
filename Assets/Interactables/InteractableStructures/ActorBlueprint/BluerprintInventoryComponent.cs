using InventorySystem;
using InventorySystem.Items;
using System;
using UnityEngine;

public class BluerprintInventoryComponent : InventoryComponent
{
	ItemQuantity[] requiredItemsToBuild;

	public event Action BlueprintItemsAchieved;

	private void OnDestroy()
	{
		Inventory.SlotChanged -= HandleInventorySlotUpdated;
	}

	public void InitializeBlueprintInventory(ItemQuantity[] itemsRequiredToBuild)
	{
		requiredItemsToBuild = itemsRequiredToBuild;
		int slotsRequired = requiredItemsToBuild.Length;

		InitializeInventory(slotsRequired);
		Inventory.SlotChanged += HandleInventorySlotUpdated;
	}

	private void HandleInventorySlotUpdated(InventorySlot _)
	{
		bool inventoryMeetsRequiredItemQuantities = true;
		foreach (ItemQuantity quantity in requiredItemsToBuild)
		{
			ItemData itemData = quantity.itemType;
			int amountNeeded = quantity.amount;

			if (Inventory.GetTotalOfItem(itemData) != amountNeeded)
			{
				inventoryMeetsRequiredItemQuantities = false;
				break;
			}
		}

		if (inventoryMeetsRequiredItemQuantities)
			BlueprintItemsAchieved?.Invoke();
	}

	public override bool TryAddItem(ItemData addedItemData, int amount, Transform itemTransform = null)
	{
		return base.TryAddItem(addedItemData, amount);
	}
}
