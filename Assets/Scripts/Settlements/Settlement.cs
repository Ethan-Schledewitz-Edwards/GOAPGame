using InventorySystem;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Settlement
{
	public event Action<Vector3> OnSettlementBoundsUpdated;

	public int SettlementID { get; private set; }
	public bool IsSettlementFriendly { get; private set; }
	public bool IsSettlementBuildable { get; private set; }

	public List<InteractableObjectBase> ActorHouses { get; private set; } = new List<InteractableObjectBase>();
	public List<InteractableObjectBase> Blueprints { get; private set; } = new List<InteractableObjectBase>();
	public List<InteractableObjectBase> ItemStorageBuildings { get; private set; } = new List<InteractableObjectBase>();

	public Settlement(int settlementID, bool isSettlementFriendly, bool isSettlementBuildable)
	{
		SettlementID = settlementID;
		IsSettlementFriendly = isSettlementFriendly;
		IsSettlementBuildable = isSettlementBuildable;
		Debug.Log($"New settlement of ID:{SettlementID} was created");
	}

	#region ManagementMethods

	public void AddActorHouse(InteractableObjectBase actorHouse)
	{
		if (actorHouse != null && !ActorHouses.Contains(actorHouse))
		{
			ActorHouses.Add(actorHouse);
			OnSettlementBoundsUpdated?.Invoke(GetSettlementCenter());
		}
	}

	public void RemoveActorHouse(InteractableObjectBase actorHouse)
	{
		if (actorHouse != null && ActorHouses.Contains(actorHouse))
		{
			ActorHouses.Remove(actorHouse);
			OnSettlementBoundsUpdated?.Invoke(GetSettlementCenter());
		}
	}

	public void AddBlueprint(InteractableObjectBase blueprint)
	{
		if (blueprint != null && !Blueprints.Contains(blueprint))
		{
			Blueprints.Add(blueprint);
			OnSettlementBoundsUpdated?.Invoke(GetSettlementCenter());
		}
	}

	public void RemoveBlueprint(InteractableObjectBase blueprint)
	{
		if (blueprint != null && Blueprints.Contains(blueprint))
		{
			Blueprints.Remove(blueprint);
			OnSettlementBoundsUpdated?.Invoke(GetSettlementCenter());
		}
	}

	public void AddStorageBuilding(InteractableObjectBase storageBuilding)
	{
		if (storageBuilding != null && !ItemStorageBuildings.Contains(storageBuilding))
		{
			ItemStorageBuildings.Add(storageBuilding);
			OnSettlementBoundsUpdated?.Invoke(GetSettlementCenter());
		}
	}

	public void RemoveStorageBuilding(InteractableObjectBase storageBuilding)
	{
		if (storageBuilding != null && ItemStorageBuildings.Contains(storageBuilding))
		{
			ItemStorageBuildings.Remove(storageBuilding);
			OnSettlementBoundsUpdated?.Invoke(GetSettlementCenter());
		}
	}
	#endregion

	public InteractableObjectBase FindActorHouse(Vector3 position)
	{
		InteractableObjectBase closest = null;
		float minDistance = float.MaxValue;
		foreach (var house in ActorHouses)
		{
			if (house == null) 
				continue;
			float dist = Vector3.Distance(house.transform.position, position);
			if (dist < minDistance)
			{
				minDistance = dist;
				closest = house;
			}
		}
		return closest;
	}

	public InteractableObjectBase FindBlueprint(Vector3 position)
	{
		InteractableObjectBase closest = null;
		float minDistance = float.MaxValue;
		foreach (var blueprint in Blueprints)
		{
			if (blueprint == null) 
				continue;
			float dist = Vector3.Distance(blueprint.transform.position, position);
			if (dist < minDistance)
			{
				minDistance = dist;
				closest = blueprint;
			}
		}
		return closest;
	}

	public InteractableObjectBase FindItemStorage(Vector3 position)
	{
		InteractableObjectBase closest = null;
		float minDistance = float.MaxValue;
		foreach (var storage in ItemStorageBuildings)
		{
			if (storage == null) 
				continue;
			float dist = Vector3.Distance(storage.transform.position, position);
			if (dist < minDistance)
			{
				minDistance = dist;
				closest = storage;
			}
		}
		return closest;
	}

	public Vector3 GetSettlementCenter()
	{
		List<InteractableObjectBase> allStructures = new List<InteractableObjectBase>();
		allStructures.AddRange(ActorHouses);
		allStructures.AddRange(Blueprints);
		allStructures.AddRange(ItemStorageBuildings);

		if (allStructures.Count == 0)
			return Vector3.zero;

		Vector3 totalPosition = Vector3.zero;
		int validCount = 0;

		foreach (var structure in allStructures)
		{
			if (structure != null)
			{
				totalPosition += structure.transform.position;
				validCount++;
			}
		}

		return validCount > 0 ? totalPosition / validCount : Vector3.zero;
	}
}
