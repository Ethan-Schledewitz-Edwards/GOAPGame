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

		public virtual bool TryAddItem(
			ItemData addedItemData,
			int amount,
			Transform[] itemTransforms = null)
		{
			if (addedItemData == null ||
				Inventory == null ||
				amount <= 0)
			{
				return false;
			}

			if (addedItemData.MaxStackSize > 1 &&
				Inventory.ContainsItem(
					addedItemData.ItemID,
					out var slots))
			{
				foreach (var slot in slots.Where(
					s => s.IsRoomAvailable(amount, out _)))
				{
					slot.AddToStack(
						amount,
						transform,
						itemTransforms);

					return true;
				}
			}

			if (Inventory.TryGetEmptySlot(
					out InventorySlot emptySlot))
			{
				emptySlot.SetSlotsItem(
					addedItemData,
					amount,
					transform,
					itemTransforms);

				return true;
			}

			return false;
		}

		/// <summary>
		/// Transfers an item stack from a source inventory slot into this
		/// inventory without exposing the physical item objects to the world.
		/// </summary>
		/// <remarks>
		/// The source is only committed after the destination has accepted the
		/// transfer. If the destination rejects it, the source slot is restored.
		/// </remarks>
		public bool TryTransferFrom(
			InventorySlot sourceSlot,
			int amountToTransfer,
			out int transferredAmount)
		{
			transferredAmount = 0;

			if (Inventory == null ||
				sourceSlot == null ||
				sourceSlot.SlotsItem == null ||
				sourceSlot.AmountInSlot <= 0 ||
				amountToTransfer <= 0)
			{
				return false;
			}

			ItemData itemToTransfer = sourceSlot.SlotsItem;

			int transferAmount =
				Mathf.Min(
					amountToTransfer,
					sourceSlot.AmountInSlot);

			// Require the destination to have room for the exact requested
			// transfer. The caller can retry later if it does not.
			if (!Inventory.TryFindRoomForItem(
					itemToTransfer,
					transferAmount,
					out _,
					out _))
			{
				return false;
			}

			if (!sourceSlot.TryExtractForTransfer(
					transferAmount,
					out ItemData extractedItemData,
					out Transform[] physicalItemObjects))
			{
				return false;
			}

			if (TryAddItem(
					extractedItemData,
					transferAmount,
					physicalItemObjects))
			{
				transferredAmount = transferAmount;
				return true;
			}

			// Destination rejected the transfer. Restore the exact source
			// state without dropping or duplicating physical objects.
			sourceSlot.RestoreAfterFailedTransfer(
				extractedItemData,
				transferAmount,
				physicalItemObjects);

			return false;
		}
	}
}