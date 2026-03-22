using System.Collections.Generic;
using UnityEngine;

public class SettlementManager : MonoBehaviour
{
	[SerializeField] ItemStorage_AIO[] m_StorageBuildingsTemp;// Remove this when the player can build storage containers

	private List<ItemStorage_AIO> m_itemStorageBuildings = new List<ItemStorage_AIO>();

	private void Awake()
	{
		m_itemStorageBuildings.AddRange(m_StorageBuildingsTemp);
	}

	ItemStorage_AIO TryFindResourceStorage(int itemID)
	{
		// Find a free item storage building for the correct item ID
		foreach (ItemStorage_AIO i in m_itemStorageBuildings)
		{
			if(i != null)
			{
				// Skip containers of the wrong type
				if (i.ItemType.ItemID != itemID)
					continue;

				return i;
			}
		}

		return null;
	}
}
