using System.Collections.Generic;
using UnityEngine;

public class Settlement : MonoBehaviour
{
	[SerializeField] ItemStorageAIO[] m_StorageBuildingsTemp;// Remove this when the player can build storage containers
	[SerializeField] ActorHouseAIO[] m_HousingBuildingsTemp;// Remove this when the player can build storage containers

	private List<ItemStorageAIO> m_itemStorageBuildings = new List<ItemStorageAIO>();
	private List<ActorHouseAIO> m_actorHouses = new List<ActorHouseAIO>();

	private void Awake()
	{
		m_itemStorageBuildings.AddRange(m_StorageBuildingsTemp);
		m_actorHouses.AddRange(m_HousingBuildingsTemp);
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

	public ActorInteractableObjectBase FindClosestBuildingOfType(int itemID)
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

	public ActorHouseAIO TryFindActorHouse(int houseID)
	{
		for (int i = 0; i < m_actorHouses.Count; ++i)
		{
			if (i == houseID)
			{
				return m_actorHouses[i];
			}
		}

		return null;
	}
}
