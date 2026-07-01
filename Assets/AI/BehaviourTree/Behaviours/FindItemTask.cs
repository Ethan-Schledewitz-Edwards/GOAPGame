using BehaviourTrees;
using InventorySystem;
using InventorySystem.Items;
using UnityEngine;
using Terrain.Generation;

public class FindItemTask : BTNodeBase
{
	private const int c_searchRadius = 2;

	protected override EBTNodeState OnUpdate(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>("ExecutorTransform");
		Vector3 executorPos = executorTransform.position;

		Settlement closestSettlement = SettlementManager.GetClosestSettlement(executorPos, true, true);
		if (closestSettlement != null)
		{
			InteractableObjectBase closestBlueprint = closestSettlement.FindBlueprint(executorPos);
			if (closestBlueprint != null)
			{
				context.SetData<Transform>("TargetTransform", closestBlueprint.transform);
				context.SetData<Vector3>("TargetPosition", closestBlueprint.GetInteractionPositon());

				return EBTNodeState.STATE_SUCSESS;
			}

			InteractableObjectBase closestStorage = closestSettlement.FindItemStorage(executorPos);
			if (closestStorage != null)
			{
				context.SetData<Transform>("TargetTransform", closestStorage.transform);
				context.SetData<Vector3>("TargetPosition", closestStorage.GetInteractionPositon());

				return EBTNodeState.STATE_SUCSESS;
			}
		}

		return EBTNodeState.STATE_RUNNING;
	}

	protected override void OnFirstEvaluate(AIContext context)
	{
		Debug.Log("Trying to find a resource deposit.");
	}

	private Transform FindTargetItem(AIContext context)
	{
		Transform executorTransform = context.GetData<Transform>("ExecutorTransform");
		Vector3 executorPosition = executorTransform.position;
		ItemQuantity[] requiredItems = context.GetData<ItemQuantity[]>("m_targetItems");

		Transform globalNearest = null;
		float minDistanceSqr = float.MaxValue;

		foreach (ItemQuantity requiredItem in requiredItems)
		{
			Transform candidate = SearchForItem(requiredItem.itemType, executorPosition);
			if (candidate != null)
			{
				float distSqr = (candidate.position - executorPosition).sqrMagnitude;
				if (distSqr < minDistanceSqr)
				{
					minDistanceSqr = distSqr;
					globalNearest = candidate;
				}
			}
		}

		return globalNearest;
	}

	private Transform SearchForItem(ItemData itemData, Vector3 executorPosition)
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
				if (entity.TryGetComponent(out IItemObject itemObject))
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

		return null;
	}
}

