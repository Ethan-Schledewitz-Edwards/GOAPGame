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
		/// Creates an inventory with a number of slots
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

		public bool ContainsItem(ItemData itemData, out List<InventorySlot> slots)
		{
			slots = Slots.Where(i => i.SlotsItem == itemData).ToList();
			return slots.Count > 0;
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
	}
}