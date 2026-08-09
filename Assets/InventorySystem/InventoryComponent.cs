using InventorySystem.Items;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InventorySystem
{
	[System.Serializable]
	public class InventoryComponent : MonoBehaviour
	{
		public Inventory Inventory { get; private set; }
		[SerializeField] private int m_inventorySize = 1;

		public List<InventorySlot> Slots => Inventory.Slots;

		protected virtual void Awake()
		{
			if (Inventory == null)
				InitializeInventory(m_inventorySize);
		}

		public void InitializeInventory(int inventorySize)
		{
			Inventory = new Inventory(inventorySize);
		}

		public virtual bool TryAddItem(ItemData addedItemData, int amount, Transform[] itemTransforms = null)
		{
			if (addedItemData.MaxStackSize > 1 && Inventory.ContainsItem(addedItemData.ItemID, out var slots))
			{
				foreach (var slot in slots.Where(s => s.IsRoomAvailable(amount, out _)))
				{
					slot.AddToStack(amount, transform, itemTransforms);
					return true;
				}
			}

			if (Inventory.TryGetEmptySlot(out InventorySlot emptySlot))
			{
				emptySlot.SetSlotsItem(addedItemData, amount, transform, itemTransforms);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Transfers an item stack from a source inventory slot into this inventory.
		/// </summary>
		public bool TryTransferFrom(InventorySlot sourceSlot, int amountToTransfer, out int transferredAmount)
		{
			transferredAmount = 0;

			// Check if source slot has anything to transfer
			if (sourceSlot == null ||
				sourceSlot.SlotsItem == null ||
				sourceSlot.AmountInSlot <= 0 ||
				amountToTransfer <= 0)
			{
				return false;
			}

			ItemData itemToTransfer = sourceSlot.SlotsItem;
			int availableToTake = Mathf.Min(amountToTransfer, sourceSlot.AmountInSlot);

			// Check if this inventory has room for at least 1 item
			if (!Inventory.TryFindRoomForItem(itemToTransfer, 1, out _, out _))
			{
				return false;
			}

			// Extract items from the source without destroying them
			sourceSlot.RemoveFromStack(availableToTake, out Transform[] itemsToTransfer, dropItems: true);

			// Add items into this inventory
			if (TryAddItem(itemToTransfer, availableToTake, itemsToTransfer))
			{
				transferredAmount = availableToTake;
				return true;
			}

			return false;
		}
	}
}