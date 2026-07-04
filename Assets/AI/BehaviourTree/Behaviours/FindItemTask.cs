using BehaviourTrees;
using InventorySystem;
using InventorySystem.Items;
using UnityEngine;
using Terrain.Generation;

public class FindItemTask : BTNodeBase
{
	private const int c_searchRadius = 2;
	private const string c_itemSearchContextKey = "ItemIDToFind";

	protected override EBTNodeState OnUpdate(AIContext context, float t)
	{
		Transform targetItemTransform = FindItemOfID(context);
		if (targetItemTransform != null)
		{
			context.SetData<Transform>(AIContextKeys.c_TargetTransform, targetItemTransform);
			context.SetData<Vector3>("TargetPosition", targetItemTransform.position);
			context.ClearData(c_itemSearchContextKey);
			return EBTNodeState.STATE_SUCSESS;
		}

		return EBTNodeState.STATE_RUNNING;
	}

	protected override void OnFirstEvaluate(AIContext context)
	{
		Debug.Log($"Trying to find an item of ID {context.GetData<int>(c_itemSearchContextKey)}.");
	}

	private Transform FindItemOfID(AIContext context)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		Vector3 executorPosition = executorTransform.position;
		int idOfItemToFind = context.GetData<int>(c_itemSearchContextKey);

		Transform candidate = SearchForItem(idOfItemToFind, executorPosition);
		return candidate;
	}

	private Transform SearchForItem(int itemID, Vector3 executorPosition)
	{
		Vector2Int[] neighbourChunkCoordinates
			= ChunkUtility.GetChunkCoordinatesInRadius(executorPosition, c_searchRadius);

		Transform nearest = null;
		float minDistanceSqr = float.MaxValue;
		foreach (Vector2Int chunkXZ in neighbourChunkCoordinates)
		{
			TerrainChunk terrainChunk = WorldBuilder.GetChunkData(chunkXZ);
			foreach (GameObject entity in terrainChunk.ResidentEntities)
			{
				if (entity.TryGetComponent(out IItemObject itemObject) && 
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
}

