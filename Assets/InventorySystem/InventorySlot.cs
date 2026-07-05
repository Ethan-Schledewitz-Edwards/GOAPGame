using InventorySystem.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InventorySystem
{
	[Serializable]
	public class InventorySlot
	{
		[Header("Item")]
		public ItemData SlotsItem { get; private set; }
		public int AmountInSlot { get; private set; }

		// Events
		public Action<InventorySlot> SlotUpdated;

		// System
		[SerializeField] private Stack<Transform> m_physicalItemObjects = new Stack<Transform>();

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

		public void CloneSlotData(InventorySlot assignedSlotData)
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

		private void AddItemTransforms(Transform parent, Transform[] itemTransforms)
		{
			if (itemTransforms == null || itemTransforms.Length == 0)
				return;

			for (int i = 0; i < itemTransforms.Length; i++) 
			{ 
				Transform transform = itemTransforms[i];

				if (transform == null || m_physicalItemObjects.Contains(transform)) 
					continue;

				if (transform.TryGetComponent(out IItemObject itemObject))
				{
					itemObject.HandleItemStored(parent);
				}

				m_physicalItemObjects.Push(transform);
			}
		}

		public void SetSlotsItem(ItemData itemData, int Amount, Transform parent = null, Transform[] physicalItemObjecs = null)
		{
			SlotsItem = itemData;
			AmountInSlot = Amount;
			AddItemTransforms(parent, physicalItemObjecs);

			SlotChanged();
		}

		public void AddToStack(int amount, Transform parent = null, Transform[] physicalItemObject = null)
		{
			AmountInSlot += amount;
			AddItemTransforms(parent, physicalItemObject);

			SlotChanged();
		}

		/// <summary>
		/// Removes the item and amount from the slot, destroys associated game objects, and invokes the slot changed event.
		/// </summary>
		public void ClearSlot()
		{
			SlotsItem = null;
			AmountInSlot = 0;

			foreach (Transform item in m_physicalItemObjects)
			{
				GameObject.Destroy(item.gameObject);
			}
			m_physicalItemObjects.Clear();

			SlotChanged();
		}

		/// <summary>
		/// Removes the specified number of items from the stack and drops them at the given world position.
		/// </summary>
		/// <param name="amountToDrop">The number of items to remove from the stack.</param>
		/// <param name="WorldDropPos">The world position where the items are dropped.</param>
		public void RemoveFromStack(int amountToDrop, out Transform[] droppedItems, bool dropItems = false, Vector3 WorldDropPos = default)
		{
			ItemData itemData = SlotsItem;
			AmountInSlot -= amountToDrop;

			if (dropItems)
			{
				droppedItems = new Transform[amountToDrop];
				for (int i = 0; i < amountToDrop; i++)
				{
					Transform itemTransform = null;
					if (m_physicalItemObjects.Count > 0)
					{
						itemTransform = m_physicalItemObjects.Pop();
						if (itemTransform.TryGetComponent(out IItemObject itemObject))
						{
							itemObject.HandleItemDropped(WorldDropPos);
						}
					}
					else
					{
						// Create a new prefab if the physical item stack is empty but there is more items in the slot
						GameObject spawnedItem = GameObject.Instantiate(itemData.ItemPrefab, WorldDropPos, Quaternion.identity);
						itemTransform = spawnedItem.transform;

						if (itemTransform.TryGetComponent(out Rigidbody itemRB))
							itemRB.constraints = RigidbodyConstraints.None;
					}

					droppedItems[i] = itemTransform;
				}
			}
			else // Destroy physical items without dropping them
			{
				droppedItems = null;
				if(m_physicalItemObjects.Count > 0)
				{
					for (int i = 0; i < amountToDrop; i++)
					{
						Transform itemTransform = m_physicalItemObjects.Pop();
						GameObject.Destroy(itemTransform.gameObject);
					}
				}
			}

			if (AmountInSlot <= 0)
			{
				ClearSlot();
				return;
			}

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

		public bool IsRoomAvailable(int roomNeeded, out int roomRemaining)
		{
			roomRemaining = (SlotsItem == null) ? 0 : SlotsItem.MaxStackSize - AmountInSlot;
			return SlotsItem == null || roomNeeded <= roomRemaining;
		}

		private void SlotChanged() => SlotUpdated?.Invoke(this);
	}
}