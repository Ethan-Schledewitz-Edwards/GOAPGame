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
		public int Size => Slots.Count;

		public event Action<InventorySlot> SlotChanged;

		/// <summary>
		/// Creates an inventory with a number of slots.
		/// </summary>
		public Inventory(int size)
		{
			// Create a local method for a new inventory instance
			void SlotChangedAction(InventorySlot slot)
			{
				SlotChanged?.Invoke(slot);
			}

			Slots = new List<InventorySlot>(size);
			for (int i = 0; i < size; i++)
			{
				Slots.Add(new InventorySlot(SlotChangedAction));
			}
		}

		/// <summary>
		/// Determines whether the inventory contains any slots with the specified item ID and retrieves the corresponding
		/// slots.
		/// </summary>
		public bool ContainsItem(int itemID, out List<InventorySlot> slots)
		{
			slots = Slots.Where(i => i.SlotsItem != null && i.SlotsItem.ItemID == itemID).ToList();
			return slots.Count > 0;
		}

		/// <summary>
		/// Returns the total count of an item type in this Inventory
		/// </summary>
		public int GetTotalOfItem(int itemID)
		{
			int count = 0;
			foreach (InventorySlot i in Slots)
			{
				if (i.SlotsItem.ItemID != itemID)
					continue;
				count += i.AmountInSlot;
			}

			return count;
		}

		/// <summary>
		/// Attempts to locate an inventory slot that contains the specified item and has sufficient available room.
		/// </summary>
		public bool TryFindRoomForItem(ItemData item, int roomNeeded, out InventorySlot targetSlot, out int roomAvailable)
		{
			targetSlot = null;
			roomAvailable = 0;

			// Try to find a partially filled stack of the same item
			if (ContainsItem(item.ItemID, out var validSlots))
			{
				foreach (InventorySlot slot in validSlots)
				{
					roomAvailable = slot.SlotsItem.MaxStackSize - slot.AmountInSlot;
					if (roomNeeded <= roomAvailable)
					{
						targetSlot = slot;
						return true;
					}
				}
			}

			// If no partial stacks have room, try to find an empty slot
			if (TryGetEmptySlot(out targetSlot))
			{
				roomAvailable = item.MaxStackSize;
				return roomNeeded <= roomAvailable;
			}

			return false;
		}

		/// <summary>
		/// Returns the first empty slot in the inventory.
		/// </summary>
		public bool TryGetEmptySlot(out InventorySlot emptySlot)
		{
			emptySlot = Slots.FirstOrDefault(i => i.SlotsItem == null);
			return emptySlot == null ? false : true;
		}
	}
}