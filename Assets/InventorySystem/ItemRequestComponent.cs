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

		private Dictionary<InventorySlot, string> m_trackedSlots = new Dictionary<InventorySlot, string>();
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

		public string RequestItem(InventorySlot slotToTrack)
		{
			if (m_isAchieved) 
				return "";

			foreach (ItemQuantity quantity in m_requiredItems)
			{
				string itemKey = quantity.itemType.ID;
				int amountFulfilled = m_trackedInventory.GetTotalOfItem(itemKey);

				if (amountFulfilled < quantity.amount)
				{
					int itemAmountCurrentlyRequested = 0;
					foreach (var trackedSlot in m_trackedSlots)
					{
						if(itemKey == trackedSlot.Value)
							itemAmountCurrentlyRequested += trackedSlot.Key.AmountInSlot;
					}

					if ((amountFulfilled + itemAmountCurrentlyRequested) < quantity.amount)
					{
						// Track the slot
						if (!m_trackedSlots.ContainsKey(slotToTrack))
						{
							m_trackedSlots.Add(slotToTrack, itemKey);
							slotToTrack.SlotUpdated += OnTrackedSlotUpdated;
						}

						return itemKey;
					}
				}
			}

			return "";
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

			if (m_trackedSlots[trackedSlot] != trackedSlot.SlotsItem.ID)
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
				string itemKey = quantity.itemType.ID;
				int amountFulfilled = m_trackedInventory.GetTotalOfItem(itemKey);

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
