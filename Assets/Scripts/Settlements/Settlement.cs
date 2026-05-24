using System;
using System.Collections.Generic;
using UnityEngine;

public class Settlement : MonoBehaviour
{
	[SerializeField] ActorHouseAIO[] m_HousingBuildingsTemp;// Remove this when the player can build storage containers
	[SerializeField] ItemStorageAIO[] m_StorageBuildingsTemp;// Remove this when the player can build storage containers

	public List<ActorHouseAIO> ActorHouses { get; private set; } = new List<ActorHouseAIO>();
	public List<ItemStorageAIO> ItemStorageBuildings { get; private set; } = new List<ItemStorageAIO>();

	public event Action<Vector3> OnSettlementBoundsUpdated;

	private void Awake()
	{
		ActorHouses.AddRange(m_HousingBuildingsTemp);
		ItemStorageBuildings.AddRange(m_StorageBuildingsTemp);
	}

	public void AddActorHouse(ActorHouseAIO actorHouse)
	{
		ActorHouses.Add(actorHouse);
		OnSettlementBoundsUpdated?.Invoke(GetSettlementCenter());
	}

	public void AddStorageBuilding(ItemStorageAIO storageBuilding)
	{
		ItemStorageBuildings.Add(storageBuilding);
		OnSettlementBoundsUpdated?.Invoke(GetSettlementCenter());
	}

	public ActorHouseAIO TryAssignActorHouse()
	{
		foreach (ActorHouseAIO i in ActorHouses)
		{
			if (i.ActorsAssigned >= i.MaxCapacity)
				continue;

			return (i);
		}

		return null;
	}

	public ItemStorageAIO TryFindResourceStorage(int itemID)
	{
		// Find a free item storage building for the correct item ID
		foreach (ItemStorageAIO i in ItemStorageBuildings)
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

	public Vector3 GetSettlementCenter()
	{
		Vector3 center = Vector3.zero;
		return center;
	}
}
