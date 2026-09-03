using BehaviourTrees;
using GenericIndex;
using InventorySystem;
using InventorySystem.Items;
using ObjectTags;
using System.Collections.Generic;
using System.Linq;
using WorldManagement.Core;
using UnityEngine;

/// <summary>
/// A behavior tree node that searches for the nearest item with a specific tag.
/// </summary>
/// <remarks>
/// This node should always be decorated with a timeout node.
/// </remarks>
public class FindItemEntityOfTagTask : BTNodeBase
{
	private const int c_chunkSearchRadius = 2;

	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		HashSet<int> itemTagIDs = context.GetDataSet().Keys
			.Where(key => key.StartsWith(AIContextKeys.c_ItemTagPrefix))
			.Select(key => int.TryParse(key.Substring(AIContextKeys.c_ItemTagPrefix.Length), out int id) ? id : (int?)null)
			.OfType<int>()
			.ToHashSet();

		ItemTagIndex index = IndexRegistry.GetIndex<ItemTag>() as ItemTagIndex;
		ItemTag[] itemTags = index.GetAllIndexedAssets()
			.Where(asset => itemTagIDs.Contains(asset.TagID))
			.ToArray();

		Transform targetItemTransform = FindItemOfTags(context, itemTags);
		if (targetItemTransform != null)
		{
			context.SetData<Transform>(AIContextKeys.c_TargetTransform, targetItemTransform);
			context.SetData<Vector3>(AIContextKeys.c_TargetDestination, targetItemTransform.position);

			// Clear each matching tag
			foreach (int id in itemTagIDs)
				context.ClearData(AIContextKeys.c_ItemTagPrefix + id);

			return EBTNodeState.STATE_SUCSESS;
		}

		return EBTNodeState.STATE_RUNNING;
	}

	private Transform FindItemOfTags(AIContext context, ItemTag[] itemTags)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		Vector3 executorPosition = executorTransform.position;

		Vector2Int[] neighbourChunkCoordinates
			= ChunkUtility.GetChunkCoordinatesInRadius(executorPosition, c_chunkSearchRadius);

		Transform nearest = null;
		float minDistanceSqr = float.MaxValue;
		foreach (Vector2Int chunkXZ in neighbourChunkCoordinates)
		{
			TerrainChunk terrainChunk = WorldManager.GetChunkData(chunkXZ);
			foreach (GameObject entity in terrainChunk.ResidentEntities)
			{
				if (entity == null)
					continue;

				// Check if the entity is an item
				if (entity.TryGetComponent(out IItemObject itemObject) &&
					!itemObject.IsItemStored &&
					itemObject.ItemData is ITaggable<ItemTag> taggable
					)

				{
					// Check if the items tags match the actors current search filters
					if (itemTags.Any(tag => taggable.HasTag(tag)))
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
		}

		return nearest;
	}

	protected override void OnFirstEvaluate(AIContext context) { }

	protected override void OnNodeExited(AIContext context) { }

	protected override void OnNodeReset(AIContext context) { }
}
