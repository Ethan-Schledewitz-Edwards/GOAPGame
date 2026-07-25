using BehaviourTrees;
using InventorySystem;
using InventorySystem.Items;
using Terrain.Generation;
using UnityEngine;

public class FindItemEntityOfIDTask : BTNodeBase
{
	private const int c_chunkSearchRadius = 2;

	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		Transform targetItemTransform = FindItemOfID(context);
		if (targetItemTransform != null)
		{
			context.SetData<Transform>(AIContextKeys.c_TargetTransform, targetItemTransform);
			context.SetData<Vector3>(AIContextKeys.c_TargetDestination, targetItemTransform.position);
			context.ClearData(AIContextKeys.c_ItemToFindID);

			return EBTNodeState.STATE_SUCSESS;
		}

		return EBTNodeState.STATE_RUNNING;
	}

	private Transform FindItemOfID(AIContext context)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		Vector3 executorPosition = executorTransform.position;

		int idOfItemToFind = context.GetData<int>(AIContextKeys.c_ItemToFindID);

		Transform candidate = SearchForItem(idOfItemToFind, executorPosition);
		return candidate;
	}

	private Transform SearchForItem(int itemID, Vector3 executorPosition)
	{
		Vector2Int[] neighbourChunkCoordinates
			= ChunkUtility.GetChunkCoordinatesInRadius(executorPosition, c_chunkSearchRadius);

		Transform nearest = null;
		float minDistanceSqr = float.MaxValue;
		foreach (Vector2Int chunkXZ in neighbourChunkCoordinates)
		{
			TerrainChunk terrainChunk = WorldBuilder.GetChunkData(chunkXZ);
			foreach (GameObject entity in terrainChunk.ResidentEntities)
			{
				if(entity == null) 
					continue;	

				if (entity.TryGetComponent(out IItemObject itemObject) &&
					!itemObject.IsItemStored && 
					itemObject.ItemData.ItemID == itemID)
				{
					float distSqr = (entity.transform.position - executorPosition).sqrMagnitude;
					if (distSqr < minDistanceSqr)
					{
						minDistanceSqr = distSqr;
						nearest = entity.transform;
					}
				}
			}
		}

		return nearest;
	}

	protected override void OnFirstEvaluate(AIContext context)
	{
		Debug.Log($"Trying to find an item of ID {context.GetData<int>(AIContextKeys.c_ItemToFindID)}.");
	}

	protected override void OnNodeExited(AIContext context) {}

	protected override void OnNodeReset(AIContext context) {}
}