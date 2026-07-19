using InventorySystem;
using InventorySystem.Items;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Interaction.InteractableStructures
{
	public class ItemRequestComponent : MonoBehaviour
	{
		public event Action ItemsAchieved;

		private Inventory m_trackedInventory;
		private ItemQuantity[] m_requiredItems;

		private Dictionary<InventorySlot, int> m_trackedSlots = new Dictionary<InventorySlot, int>();
		private bool m_isAchieved = false;

		private void OnDestroy()
		{
			if (m_trackedInventory != null)
				m_trackedInventory.SlotChanged -= OnTrackedInventoryUpdated;

			foreach (var i in m_trackedSlots)
			{
				if (i.Key != null)
					i.Key.SlotUpdated -= OnTrackedSlotUpdated;
			}
		}

		public void SetRequiredItems(Inventory trackedInventory, ItemQuantity[] requiredItems)
		{
			m_trackedInventory = trackedInventory;
			m_trackedInventory.SlotChanged += OnTrackedInventoryUpdated;

			m_requiredItems = requiredItems;
			m_isAchieved = false;
		}

		public int RequestItem(InventorySlot slotToTrack)
		{
			if (m_isAchieved) 
				return -1;

			foreach (ItemQuantity quantity in m_requiredItems)
			{
				int itemID = quantity.itemType.ItemID;
				int amountFulfilled = m_trackedInventory.GetTotalOfItem(itemID);

				if (amountFulfilled < quantity.amount)
				{
					int itemAmountCurrentlyRequested = 0;
					foreach (var trackedSlot in m_trackedSlots)
					{
						if(itemID == trackedSlot.Value)
							itemAmountCurrentlyRequested += trackedSlot.Key.AmountInSlot;
					}

					if ((amountFulfilled + itemAmountCurrentlyRequested) < quantity.amount)
					{
						// Track the slot
						if (!m_trackedSlots.ContainsKey(slotToTrack))
						{
							m_trackedSlots.Add(slotToTrack, itemID);
							slotToTrack.SlotUpdated += OnTrackedSlotUpdated;
						}

						return itemID;
					}
				}
			}

			return -1;
		}

		private void OnTrackedSlotUpdated(InventorySlot trackedSlot)
		{
			if (!m_trackedSlots.ContainsKey(trackedSlot))
				return;

			if(trackedSlot.SlotsItem == null)
			{
				UnsubsrcribeFromSlot(trackedSlot);
				return;
			}

			if (m_trackedSlots[trackedSlot] != trackedSlot.SlotsItem.ItemID)
				UnsubsrcribeFromSlot(trackedSlot);

		}

		private void UnsubsrcribeFromSlot(InventorySlot slot) 
		{
			m_trackedSlots.Remove(slot);
			slot.SlotUpdated -= OnTrackedSlotUpdated;
		}

		private void OnTrackedInventoryUpdated(InventorySlot _)
		{
			if (m_isAchieved) 
				return;

			foreach (ItemQuantity quantity in m_requiredItems)
			{
				int itemID = quantity.itemType.ItemID;
				int amountFulfilled = m_trackedInventory.GetTotalOfItem(itemID);

				if (amountFulfilled < quantity.amount)
					return;
			}

			if (m_trackedInventory != null)
				m_trackedInventory.SlotChanged -= OnTrackedInventoryUpdated;

			foreach (var i in m_trackedSlots)
			{
				if (i.Key != null)
					i.Key.SlotUpdated -= OnTrackedSlotUpdated;
			}

			ItemsAchieved?.Invoke();
		}
	}
}
