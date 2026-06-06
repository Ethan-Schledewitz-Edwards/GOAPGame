using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using InventorySystem.Items;

namespace InventorySystem
{
	[Serializable]
	public class Inventory
	{
		[Header("Slots")]
		public List<InventorySlot> Slots { get; private set; }

		// The size of the inventory list
		public int Size => Slots.Count;

		public event Action<InventorySlot> OnSlotChanged;

		/// <summary>
		/// Creates an inventory with a number of slots
		/// </summary>
		public Inventory(int size)
		{
			// Create a local method for a new inventory instance
			void SlotChangedAction(InventorySlot slot)
			{
				OnSlotChanged?.Invoke(slot);
			}

			// Make list
			Slots = new List<InventorySlot>(size);

			// Fill list
			for (int i = 0; i < size; i++)
			{
				Slots.Add(new InventorySlot(SlotChangedAction));
			}
		}

		/// <summary>
		/// Returns the first found slot containing an item of the desired type
		/// </summary>
		public bool ContainsItem(ItemData itemData, out InventorySlot slot)
		{
			slot = null;
			foreach (InventorySlot i in Slots)
			{
				if (i.SlotsItem == itemData)
				{
					slot = i;
				}
			}
			return slot == null ? false : true;
		}

		/// <summary>
		/// Checks if the inventory contains an item of a certain type.
		/// </summary>
		/// <param name="slotsContainingItem">a list of slots containing that item type</param>
		public bool ContainsItem(ItemData itemData, out List<InventorySlot> slotsContainingItem)
		{
			slotsContainingItem = Slots.Where(i => i.SlotsItem == itemData).ToList();
			return Slots == null ? false : true;
		}

		/// <summary>
		/// Returns the total count of an item in this Inventory
		/// </summary>
		public int GetTotalOfItem(ItemData itemData)
		{
			int count = 0;
			foreach (InventorySlot i in Slots)
			{
				if (i.SlotsItem != itemData)
					continue;
				count += i.AmountInSlot;
			}

			return count;
		}

		public bool TryGetEmptySlot(out InventorySlot emptySlot)
		{
			emptySlot = Slots.FirstOrDefault(i => !i.SlotsItem);
			return emptySlot == null ? false : true;
		}

		/// <summary>
		/// Attempts to add an item to this inventory
		/// </summary>
		/// <param name="addedItem">The identifier value of the item</param>
		/// <param name="amount">The amount to be added</param>
		/// <returns>True if the item was added to this inventory</returns>
		public bool TryAddItem(ItemData addedItemData, int amount)
		{
			// Check if any slots contain an item of the same type
			if (ContainsItem(addedItemData, out List<InventorySlot> slotsWithItems))
			{
				foreach (var slot in slotsWithItems)
				{
					if (slot.IsRoomAvailable(amount, out _))
					{
						slot.AddToStack(amount);
						return true;
					}
				}
			}

			// Check for first empty slot
			if (TryGetEmptySlot(out InventorySlot emptySlot))
			{
				emptySlot.AddItem(addedItemData, amount);
				return true;
			}

			return false;
		}
	}
}