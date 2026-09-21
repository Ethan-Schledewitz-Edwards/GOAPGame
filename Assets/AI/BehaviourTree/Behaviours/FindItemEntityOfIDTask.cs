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
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		if (executorTransform == null)
			return EBTNodeState.STATE_FAILURE;

		if (!executorTransform.TryGetComponent(out IInteractor interactor))
			return EBTNodeState.STATE_FAILURE;

		// Find the closest item or storage structure
		int idOfItemToFind = context.GetData<int>(AIContextKeys.c_ItemToFindID);
		Transform targetItemTransform = SearchForItem
		(
			idOfItemToFind, 
			executorTransform, 
			interactor, 
			context
		);

		if (targetItemTransform == null)
			return EBTNodeState.STATE_FAILURE;

		Vector3 destination = targetItemTransform.position;
		if (targetItemTransform.TryGetComponent(out InteractableObjectBase interactable))
		{
			if (interactable.TryReserveClosestPosition(interactor, executorTransform.position, out InteractionPosition assignedPosition))
			{
				if (assignedPosition.TryGetInteractionPosition(interactor, out Vector3 position))
				{
					destination = position;
					context.SetData<InteractionPosition>(AIContextKeys.c_AssignedInteractionPosition, assignedPosition);

					// Cleanup delegate in case the behavior tree aborts
					System.Action cleanup = () =>
					{
						if (interactable != null && interactor != null && assignedPosition != null)
						{
							interactable.CancelReservation(interactor, assignedPosition);
						}
					};
					context.SetData<System.Action>(AIContextKeys.c_ReservationCleanup, cleanup);
				}
			}
			else
			{
				// Reservation fails
				return EBTNodeState.STATE_FAILURE;
			}
		}

		context.SetData<Transform>(AIContextKeys.c_TargetTransform, targetItemTransform);
		context.SetData<Vector3>(AIContextKeys.c_TargetDestination, destination);

		return EBTNodeState.STATE_SUCSESS;
	}

	private Transform SearchForItem(int itemID, Transform executorTransform, IInteractor interactor, AIContext context)
	{
		Vector3 executorPosition = executorTransform.position;
		Vector2Int[] neighbourChunkCoordinates = ChunkUtility.GetChunkCoordinatesInRadius(executorPosition, c_chunkSearchRadius);

		Transform nearest = null;
		float minDistanceSqr = float.MaxValue;

		// Try to find the nearest item on the ground
		foreach (Vector2Int chunkXZ in neighbourChunkCoordinates)
		{
			TerrainChunk terrainChunk = WorldManager.GetChunkData(chunkXZ);
			if (terrainChunk == null || terrainChunk.ResidentEntities == null)
				continue;

			foreach (GameObject entity in terrainChunk.ResidentEntities)
			{
				if (entity == null)
					continue;

				// Check if the entity is an item
				if (entity.TryGetComponent(out IItemObject itemObject) &&
					!itemObject.IsItemStored &&
					itemObject.ItemData.ItemID == itemID)
				{
					// Check if the item has work
					if (entity.TryGetComponent(out InteractableObjectBase interactable))
					{
						if (!interactable.HasAvailableWork(interactor))
							continue;
					}

					float distSqr = (entity.transform.position - executorPosition).sqrMagnitude;
					if (distSqr < minDistanceSqr)
					{
						minDistanceSqr = distSqr;
						nearest = entity.transform;
					}
				}
			}
		}

		// Try to find the nearest friendly storage structure
		minDistanceSqr = float.MaxValue;
		Transform nearestStorageStructure = FindStorageStructure(itemID, executorTransform, interactor, context);
		if (nearestStorageStructure != null)
		{
			float distSqr = (nearestStorageStructure.position - executorPosition).sqrMagnitude;
			if (distSqr < minDistanceSqr)
			{
				nearest = nearestStorageStructure;
			}
		}

		return nearest;
	}

	/// <summary>
	/// Finds the nearest available storage structure that contains the requested item.
	/// </summary>
	private Transform FindStorageStructure(int itemID, Transform executorTransform, IInteractor interactor, AIContext context)
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
					// Check if the storage has work
					if (!interactable.HasAvailableWork(interactor))
						return null;

					// Validate item filters and inventory counts
					if (structureObject.TryGetComponent(out IItemFiltered itemFiltered))
					{
						ItemIndex itemIndex = IndexRegistry.GetIndex<ItemData>() as ItemIndex;
						if (itemIndex?.GetIndexedAsset(itemID) is ITaggable<ItemTag> itemTaggable)
						{
							bool passesFilter = itemTaggable.RuntimeTagSet.Any(tag => itemFiltered.ItemTagFilter.Contains(tag));
							if (passesFilter)
							{
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