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
/// Searches for the nearest item with a specific ID then adds its data to
/// the behaviour trees context.
/// </summary>
/// <remarks>
/// This node should be decorated with a <see cref="BTTimeoutNode"/> 
/// and must be followed by a <see cref="ReserveInteractionPositionTask"/> node to reserve the target.
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
		
		if (executorTransform == null || 
			!executorTransform.TryGetComponent(out IInteractor interactor))
			return EBTNodeState.STATE_FAILURE;

		int idOfItemToFind = context.GetData<int>(AIContextKeys.c_ItemToFindID);
		Transform targetItemTransform = SearchForItem(idOfItemToFind, executorTransform, interactor, context);

		if (targetItemTransform == null)
			return EBTNodeState.STATE_FAILURE;

		// The subsequent ReserveInteractionPositionTask handles reservation and cleanup.
		context.SetData<Transform>(AIContextKeys.c_TargetTransform, targetItemTransform);
		
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
			if (terrainChunk?.ResidentEntities == null)
				continue;

			foreach (GameObject entity in terrainChunk.ResidentEntities)
			{
				if (entity == null)
					continue;

				if (!entity.TryGetComponent(out IItemObject itemObject) || 
				    itemObject.IsItemStored || 
				    itemObject.ItemData.ItemID != itemID)
				{
					continue;
				}

				if (entity.TryGetComponent(out InteractableObjectBase interactable) && 
				    !interactable.HasAvailableWork(interactor))
				{
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

		// Try to find a friendly storage structure. (Only use it if it is closer than a ground item)
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
		
		if (closestFactionSettlement == null)
			return null;

		IStructure closestStructure = closestFactionSettlement.
			FindNearestStructureOfType(executorTransform.position, m_storageTag);

		if (closestStructure == null)
			return null;

		GameObject structureObject = closestStructure.Object;

		if (!structureObject.TryGetComponent(out InteractableObjectBase interactable) || 
		    !interactable.HasAvailableWork(interactor))
		{
			return null;
		}

		if (!structureObject.TryGetComponent(out IItemFiltered itemFiltered) || 
		    !structureObject.TryGetComponent(out InventoryComponent inventory))
		{
			return null;
		}

		ItemIndex itemIndex = IndexRegistry.GetIndex<ItemData>() as ItemIndex;
		if (itemIndex?.GetIndexedAsset(itemID) is ITaggable<ItemTag> itemTaggable)
		{
			bool passesFilter = itemTaggable.RuntimeTagSet.Any(tag => itemFiltered.ItemTagFilter.Contains(tag));
			if (passesFilter && inventory.Inventory.GetTotalOfItem(itemID) > 0)
			{
				return structureObject.transform;
			}
		}

		return null;
	}

	protected override void OnFirstEvaluate(AIContext context)
	{
		Debug.Log($"[FindItemEntityOfIDTask] Trying to find an item of ID {context.GetData<int>(AIContextKeys.c_ItemToFindID)}.");
	}

	protected override void OnNodeExited(AIContext context) { }

	protected override void OnNodeReset(AIContext context) { }
}