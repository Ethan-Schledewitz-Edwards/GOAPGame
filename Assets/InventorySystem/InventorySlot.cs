using System;
using UnityEngine;
using InventorySystem.Items;

namespace InventorySystem
{
	[Serializable]
	public class InventorySlot
	{
		[Header("Item")]
		public ItemData SlotsItem { get; private set; }

		public int AmountInSlot { get; private set; }

		[Header("Slot Characteristics")]
		public bool HoldsStacks { get; private set; }

		private Action<InventorySlot> OnSlotChanged;

		#region Constructors
		/// <summary>
		/// Creates an empty inventory slot
		/// </summary>
		public InventorySlot(Action<InventorySlot> invSlotChanged)
		{
			OnSlotChanged = invSlotChanged;
			SlotsItem = null;
			AmountInSlot = 0;
		}

		/// <summary>
		/// Creates an inventory slot with item data
		/// </summary>
		public InventorySlot(ItemData itemData, int amount)
		{
			SlotsItem = itemData;
			AmountInSlot = amount;
		}
		#endregion

		#region Methods

		public void AssignSlotData(InventorySlot assignedSlotData)
		{
			if (SlotsItem == assignedSlotData.SlotsItem)
			{
				AddToStack(assignedSlotData.AmountInSlot);
			}
			else
			{
				SlotsItem = assignedSlotData.SlotsItem;
				AmountInSlot = 0;
				AddToStack(assignedSlotData.AmountInSlot);
			}

		}

		public void AddItem(ItemData itemData, int Amount)
		{
			SlotsItem = itemData;
			AmountInSlot = Amount;

			SlotChanged();
		}

		public void AddToStack(int amount)
		{
			AmountInSlot += amount;

			SlotChanged();
		}

		public void ClearSlot()
		{
			SlotsItem = null;
			AmountInSlot = 0;

			SlotChanged();
		}

		public void SetStackAmount(int amount)
		{
			AmountInSlot = amount;

			if (AmountInSlot <= 0)
			{
				ClearSlot();
				return;
			}

			SlotChanged();
		}

		public void RemoveFromStack(int amount)
		{
			AmountInSlot -= amount;

			if (AmountInSlot <= 0)
			{
				ClearSlot();
				return;
			}

			SlotChanged();
		}

		public void DropFromStack(int amount, Vector3 WorldDropPos)
		{
			AmountInSlot -= amount;

			if (AmountInSlot <= 0)
			{
				ClearSlot();
				return;
			}

			SlotChanged();
		}

		public bool IsRoomAvailable(int amount, out int roomRemaining)
		{
			roomRemaining = (SlotsItem == null) ? 0 : SlotsItem.MaxStackSize - AmountInSlot;
			return SlotsItem == null || amount <= roomRemaining;
		}
		#endregion

		private void SlotChanged() => OnSlotChanged?.Invoke(this);
	}
}