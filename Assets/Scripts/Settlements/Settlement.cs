using System.Collections.Generic;
using UnityEngine;

public class Settlement : MonoBehaviour
{
	[SerializeField] ItemStorageAIO[] m_StorageBuildingsTemp;// Remove this when the player can build storage containers

	private List<ItemStorageAIO> m_itemStorageBuildings = new List<ItemStorageAIO>();

	private void Awake()
	{
		m_itemStorageBuildings.AddRange(m_StorageBuildingsTemp);
	}

	public ItemStorageAIO TryFindResourceStorage(int itemID)
	{
		// Find a free item storage building for the correct item ID
		foreach (ItemStorageAIO i in m_itemStorageBuildings)
		{
			if (i != null)
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
