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
		private Stack<Transform> m_physicalItemObjects = new Stack<Transform>();

		#region Constructors

		public InventorySlot(Action<InventorySlot> invSlotChanged)
		{
			SlotUpdated = invSlotChanged;
			SlotsItem = null;
			AmountInSlot = 0;
		}

		#endregion

		private void AddItemTransforms(Transform parent, Transform[] itemTransforms)
		{
			if (itemTransforms == null || itemTransforms.Length == 0)
				return;

			for (int i = 0; i < itemTransforms.Length; i++)
			{
				Transform itemTransform = itemTransforms[i];

				if (itemTransform == null || m_physicalItemObjects.Contains(itemTransform))
					continue;

				if (itemTransform.TryGetComponent(out IItemObject itemObject))
				{
					itemObject.ItemStored(parent);
				}

				m_physicalItemObjects.Push(itemTransform);
			}
		}

		public void SetSlotsItem(
			ItemData itemData,
			int Amount,
			Transform parent = null,
			Transform[] physicalItemObjects = null)
		{
			SlotsItem = itemData;
			AmountInSlot = Amount;
			AddItemTransforms(parent, physicalItemObjects);
			SlotChanged();
		}

		public void AddToStack(
			int amount,
			Transform parent = null,
			Transform[] physicalItemObjects = null)
		{
			AmountInSlot += amount;
			AddItemTransforms(parent, physicalItemObjects);
			SlotChanged();
		}

		public void ClearSlot()
		{
			SlotsItem = null;
			AmountInSlot = 0;

			foreach (Transform item in m_physicalItemObjects)
			{
				if (item != null)
					GameObject.Destroy(item.gameObject);
			}

			m_physicalItemObjects.Clear();
			SlotChanged();
		}

		public void RemoveFromStack(
			int amountToDrop,
			out Transform[] droppedItems,
			bool dropItems = false,
			Vector3 WorldDropPos = default)
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

						if (itemTransform != null &&
							itemTransform.TryGetComponent(out IItemObject itemObject))
						{
							itemObject.ItemDropped(WorldDropPos);
						}
					}
					else if (itemData != null)
					{
						GameObject spawnedItem =
							GameObject.Instantiate(
								itemData.ItemPrefab,
								WorldDropPos,
								Quaternion.identity);

						itemTransform = spawnedItem.transform;

						if (itemTransform.TryGetComponent(out Rigidbody itemRB))
							itemRB.constraints = RigidbodyConstraints.None;
					}

					droppedItems[i] = itemTransform;
				}
			}
			else
			{
				droppedItems = null;

				if (m_physicalItemObjects.Count > 0)
				{
					for (int i = 0; i < amountToDrop && m_physicalItemObjects.Count > 0; i++)
					{
						Transform itemTransform = m_physicalItemObjects.Pop();

						if (itemTransform != null)
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

		/// <summary>
		/// Removes items from the slot for an inventory-to-inventory transfer
		/// WITHOUT dropping or destroying their physical item objects.
		///
		public bool TryExtractForTransfer(
			int amountToTransfer,
			out ItemData itemData,
			out Transform[] physicalItemObjects)
		{
			itemData = SlotsItem;
			physicalItemObjects = Array.Empty<Transform>();

			if (itemData == null ||
				amountToTransfer <= 0 ||
				AmountInSlot < amountToTransfer)
			{
				return false;
			}

			int physicalCount =
				Mathf.Min(amountToTransfer, m_physicalItemObjects.Count);

			if (physicalCount > 0)
			{
				physicalItemObjects = new Transform[physicalCount];

				for (int i = 0; i < physicalCount; i++)
					physicalItemObjects[i] = m_physicalItemObjects.Pop();
			}

			AmountInSlot -= amountToTransfer;

			if (AmountInSlot <= 0)
				SlotsItem = null;

			SlotChanged();
			return true;
		}

		/// <summary>
		/// Restores a transfer that the destination inventory rejected.
		/// The physical objects were never dropped, so they can simply be put
		/// back into the source slot.
		/// </summary>
		public bool RestoreAfterFailedTransfer(
			ItemData itemData,
			int amount,
			Transform[] physicalItemObjects)
		{
			if (itemData == null || amount <= 0)
				return false;

			if (SlotsItem != null && SlotsItem != itemData)
				return false;

			if (SlotsItem == null)
				SlotsItem = itemData;

			AmountInSlot += amount;

			if (physicalItemObjects != null)
			{
				for (int i = 0; i < physicalItemObjects.Length; i++)
				{
					Transform itemTransform = physicalItemObjects[i];

					if (itemTransform != null &&
						!m_physicalItemObjects.Contains(itemTransform))
					{
						m_physicalItemObjects.Push(itemTransform);
					}
				}
			}

			SlotChanged();
			return true;
		}

		public bool IsRoomAvailable(int roomNeeded, out int roomRemaining)
		{
			roomRemaining =
				(SlotsItem == null)
					? 0
					: SlotsItem.MaxStackSize - AmountInSlot;

			return SlotsItem == null ||
				roomNeeded <= roomRemaining;
		}

		private void SlotChanged() => SlotUpdated?.Invoke(this);
	}
}