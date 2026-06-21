using System;
using UnityEngine;
using InventorySystem.Items;
using System.Collections.Generic;
using UnityEditor;

namespace InventorySystem
{
	[Serializable]
	public class InventorySlot
	{
		[Header("Item")]
		public ItemData SlotsItem { get; private set; }
		public Stack<Transform> PhysicalItemObjects { get; private set; } = new Stack<Transform>(); // Optional

		public int AmountInSlot { get; private set; }

		[Header("Slot Characteristics")]
		public bool HoldsStacks { get; private set; }

		private Action<InventorySlot> OnSlotChanged;
		private Action<Transform> OnDroppedPhysicalItem;

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

		public void SetSlotsItem(ItemData itemData, int Amount, Transform physicalItemObject = null)
		{
			SlotsItem = itemData;
			AmountInSlot = Amount;
			TryAddPhysicalItem(physicalItemObject);

			SlotChanged();
		}

		public void AddToStack(int amount, Transform physicalItemObject = null)
		{
			AmountInSlot += amount;
			TryAddPhysicalItem(physicalItemObject);

			SlotChanged();
		}

		public void ClearSlot()
		{
			SlotsItem = null;
			AmountInSlot = 0;

			foreach (Transform item in PhysicalItemObjects)
			{
				GameObject.Destroy(item.gameObject);
			}
			PhysicalItemObjects.Clear();

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
			for (int i = 0; i < amount; i++)
			{
				Transform itemTransform = PhysicalItemObjects.Pop();
				if (itemTransform.TryGetComponent(out Rigidbody itemRB))
				{
					itemTransform.gameObject.SetActive(true);
					itemRB.constraints = RigidbodyConstraints.None;
					itemTransform.position = WorldDropPos;
				}

				OnDroppedPhysicalItem?.Invoke(itemTransform);
			}

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

		private void TryAddPhysicalItem(Transform itemTransform)
		{
			if(itemTransform == null || PhysicalItemObjects.Contains(itemTransform))
				return;

			if(itemTransform.TryGetComponent(out Rigidbody itemRB))
			{
				itemTransform.gameObject.SetActive(false);
				itemRB.constraints = RigidbodyConstraints.FreezeAll;
			}

			PhysicalItemObjects.Push(itemTransform);
		}

		private void SlotChanged() => OnSlotChanged?.Invoke(this);
	}
}