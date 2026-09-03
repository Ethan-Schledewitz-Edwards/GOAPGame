using BehaviourTrees;
using GenericIndex;
using InventorySystem;
using InventorySystem.Items;
using ObjectTags;
using Settlements;
using System.Linq;
using UnityEngine;
using WorldManagement.Core;
using Factions.Core;

/// <summary>
/// A behavior tree node that searches for the nearest item with a specific ID.
/// </summary>
/// <remarks>
/// This node should always be decorated with a timeout node.
/// </remarks>
public class FindItemEntityOfIDTask : BTNodeBase
{
	private const int c_chunkSearchRadius = 2;
	private readonly StructureTag m_storageTag;

	public FindItemEntityOfIDTask(StructureTag storageTag) : base()
	{
		m_storageTag = storageTag;
	}

	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		// Find the closest item
		Transform targetItemTransform = FindItemOfID(context);

		if (targetItemTransform == null)
			return EBTNodeState.STATE_FAILURE;

		// Get the items interaction offset
		Vector3 destination = targetItemTransform.position;
		if (targetItemTransform != null && targetItemTransform.TryGetComponent(out InteractableObjectBase interactableObjectBase))
		{
			destination = interactableObjectBase.GetInteractionPositon();
		}

		if (targetItemTransform != null)
		{
			context.SetData<Transform>(AIContextKeys.c_TargetTransform, targetItemTransform);
			context.SetData<Vector3>(AIContextKeys.c_TargetDestination, destination);

			return EBTNodeState.STATE_SUCSESS;
		}

		return EBTNodeState.STATE_RUNNING;
	}

	private Transform FindItemOfID(AIContext context)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);

		int idOfItemToFind = context.GetData<int>(AIContextKeys.c_ItemToFindID);

		Transform candidate = SearchForItem(idOfItemToFind, executorTransform, context);
		
		return candidate;
	}

	private Transform SearchForItem(int itemID, Transform executorTransform, AIContext context)
	{
		Vector3 executorPosition = executorTransform.position;

		Vector2Int[] neighbourChunkCoordinates
			= ChunkUtility.GetChunkCoordinatesInRadius(executorPosition, c_chunkSearchRadius);

		// Track the nearest structure
		Transform nearest = null;
		float minDistanceSqr = float.MaxValue;
		float distSqr = 0;

		// Try to find the nearest item on the ground
		foreach (Vector2Int chunkXZ in neighbourChunkCoordinates)
		{
			TerrainChunk terrainChunk = WorldManager.GetChunkData(chunkXZ);
			foreach (GameObject entity in terrainChunk.ResidentEntities)
			{
				if (entity == null)
					continue;

				if (entity.TryGetComponent(out IItemObject itemObject) &&
					!itemObject.IsItemStored &&
					itemObject.ItemData.ItemID == itemID)
				{
					distSqr = (entity.transform.position - executorPosition).sqrMagnitude;
					if (distSqr < minDistanceSqr)
					{
						minDistanceSqr = distSqr;
						nearest = entity.transform;
					}
				}
			}
		}

		// Try to find the nearest friendly storage structure
		Transform nearestStorageStructure = FindStorageStructure(itemID, executorTransform, context);
		if(nearestStorageStructure != null)
		{
			Vector3 structurePosition = nearestStorageStructure.position;
			distSqr = (structurePosition - executorPosition).sqrMagnitude;
			if (distSqr < minDistanceSqr)
			{
				minDistanceSqr = distSqr;
				nearest = nearestStorageStructure;
			}
		}

		return nearest;
	}

	/// <summary>
	/// Finds the nearest storage structure that matches the specified item's tag.
	/// </summary>
	/// <param name="itemID">The item identifier used to filter storage structures by their tags.</param>
	/// <param name="executorTransform">The transform representing the executor's position and orientation.</param>
	/// <param name="context">The AI context for the search operation.</param>
	/// <returns>The transform of the nearest matching storage structure, or null if none is found.</returns>
	private Transform FindStorageStructure(int itemID, Transform executorTransform, AIContext context)
	{
		EFaction executorFaction = context.GetData<EFaction>(AIContextKeys.c_ExecutorFaction);

		Settlement closestFactionSettlement = SettlementManager.GetClosestSettlement(executorTransform.position, executorFaction);
		if (closestFactionSettlement != null)
		{
			IStructure closestStructure = closestFactionSettlement.FindNearestStructureOfType(executorTransform.position, m_storageTag);
			if (closestStructure != null)
			{
				GameObject structureObject = closestStructure.Object;
				if (structureObject.TryGetComponent(out InteractableObjectBase interactable))
				{
					if (interactable.TryGetComponent(out IItemFiltered itemFiltered))
					{
						ItemIndex itemIndex = IndexRegistry.GetIndex<ItemData>() as ItemIndex;
						if (itemIndex?.GetIndexedAsset(itemID) is ITaggable<ItemTag> itemTaggable)
						{
							// Check if the structures tags include the held items tags
							bool passesFilter = itemTaggable.RuntimeTagSet.Any(tag => itemFiltered.ItemTagFilter.Contains(tag));
							if (passesFilter)
							{
								// Check if the storage unit contains the desired item ID
								if (structureObject.TryGetComponent(out InventoryComponent inventory))
								{
									if (inventory.Inventory.GetTotalOfItem(itemID) > 0)
										return structureObject.transform;
								}
							}
						}
					}
				}
			}
		}

		return null;
	}

	protected override void OnFirstEvaluate(AIContext context)
	{
		Debug.Log($"Trying to find an item of ID {context.GetData<int>(AIContextKeys.c_ItemToFindID)}.");
	}

	protected override void OnNodeExited(AIContext context) { }

	protected override void OnNodeReset(AIContext context) { }
}