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

		public Action<InventorySlot> SlotUpdated;
		public Action<Transform> DroppedPhysicalItem;

		#region Constructors
		/// <summary>
		/// Creates an empty inventory slot
		/// </summary>
		public InventorySlot(Action<InventorySlot> invSlotChanged)
		{
			SlotUpdated = invSlotChanged;
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

		private void TryAddPhysicalItem(Transform parent, Transform itemTransform)
		{
			if (itemTransform == null || PhysicalItemObjects.Contains(itemTransform))
				return;

			if (itemTransform.TryGetComponent(out IItemObject itemObject))
			{
				itemObject.HandleItemStored(parent);
			}

			PhysicalItemObjects.Push(itemTransform);
		}

		public void SetSlotsItem(ItemData itemData, int Amount, Transform parent = null, Transform physicalItemObject = null)
		{
			SlotsItem = itemData;
			AmountInSlot = Amount;
			TryAddPhysicalItem(parent, physicalItemObject);

			SlotChanged();
		}

		public void AddToStack(int amount, Transform parent = null, Transform physicalItemObject = null)
		{
			AmountInSlot += amount;
			TryAddPhysicalItem(parent, physicalItemObject);

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

		/// <summary>
		/// Removes the specified amount from the slot and clears the slot if the resulting amount is zero or less.
		/// </summary>
		/// <param name="amount">The amount to subtract from the slot.</param>
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

		/// <summary>
		/// Removes the specified number of items from the stack and drops them at the given world position.
		/// </summary>
		/// <param name="amount">The number of items to remove from the stack.</param>
		/// <param name="WorldDropPos">The world position where the items are dropped.</param>
		public void RemoveFromStack(int amount, Vector3 WorldDropPos)
		{
			ItemData itemData = SlotsItem;
			AmountInSlot -= amount;

			for (int i = 0; i < amount; i++)
			{
				Transform itemTransform = null;
				if (PhysicalItemObjects.Count > 0)
				{
					itemTransform = PhysicalItemObjects.Pop();
					if (itemTransform.TryGetComponent(out Rigidbody itemRB))
					{
						itemRB.constraints = RigidbodyConstraints.None;
						itemTransform.position = WorldDropPos;
						itemTransform.parent = null;
						itemTransform.gameObject.SetActive(true);
					}
				}
				else
				{
					GameObject spawnedItem = GameObject.Instantiate(itemData.ItemPrefab, WorldDropPos, Quaternion.identity);
					itemTransform = spawnedItem.transform;

					if (itemTransform.TryGetComponent(out Rigidbody itemRB))
						itemRB.constraints = RigidbodyConstraints.None;
				}

				DroppedPhysicalItem?.Invoke(itemTransform);
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

		private void SlotChanged() => SlotUpdated?.Invoke(this);
	}
}