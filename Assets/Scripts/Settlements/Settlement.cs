using System;
using System.Collections.Generic;
using UnityEngine;

public class Settlement : MonoBehaviour
{
	[SerializeField] InteractableObjectBase[] m_HousingBuildingsTemp;// Remove this when the player can build storage containers
	[SerializeField] InteractableObjectBase[] m_StorageBuildingsTemp;// Remove this when the player can build storage containers

	public List<InteractableObjectBase> ActorHouses { get; private set; } = new List<InteractableObjectBase>();
	public List<InteractableObjectBase> ItemStorageBuildings { get; private set; } = new List<InteractableObjectBase>();

	public event Action<Vector3> OnSettlementBoundsUpdated;

	private void Awake()
	{
		ActorHouses.AddRange(m_HousingBuildingsTemp);
		ItemStorageBuildings.AddRange(m_StorageBuildingsTemp);
	}

	public void AddActorHouse(InteractableObjectBase actorHouse)
	{
		ActorHouses.Add(actorHouse);
		OnSettlementBoundsUpdated?.Invoke(GetSettlementCenter());
	}

	public void AddStorageBuilding(InteractableObjectBase storageBuilding)
	{
		ItemStorageBuildings.Add(storageBuilding);
		OnSettlementBoundsUpdated?.Invoke(GetSettlementCenter());
	}

	public InteractableObjectBase TryFindResourceStorage()
	{
		//// Find a free item storage building for the correct item ID
		//foreach (ActorInteractableObjectBase i in ItemStorageBuildings)
		//{
		//	if (i != null && i.TryGetComponent(out InventoryComponent inventoryComponent))
		//	{
		//		// Skip containers of the wrong type
		//		if (i.ItemType.ItemID != itemID)
		//			continue;

		//		return i;
		//	}
		//}

		return null;
	}

	public Vector3 GetSettlementCenter()
	{
		Vector3 center = Vector3.zero;
		return center;
	}
}
