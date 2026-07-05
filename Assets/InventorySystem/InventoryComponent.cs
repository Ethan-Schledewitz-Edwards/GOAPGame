using InventorySystem.Items;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphs;
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
	}
}