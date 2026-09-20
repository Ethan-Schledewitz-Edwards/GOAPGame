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

	private const string c_ItemTagsKey = "FindItemEntityOfTagTask_ItemTags";

	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		List<ItemTag> itemTags = context.GetData<List<ItemTag>>(c_ItemTagsKey);

		if (itemTags == null || itemTags.Count == 0)
			return EBTNodeState.STATE_FAILURE;

		Transform targetItemTransform = FindItemOfTags(context, itemTags.ToArray());
		if (targetItemTransform == null)
			return EBTNodeState.STATE_RUNNING;

		context.SetData<Transform>(AIContextKeys.c_TargetTransform, targetItemTransform);
		context.SetData<Vector3>(AIContextKeys.c_TargetDestination, targetItemTransform.position);

		foreach (ItemTag tag in itemTags)
		{
			context.ClearData(AIContextKeys.c_ItemTagPrefix + tag.TagID);
		}
		context.ClearData(c_ItemTagsKey);

		return EBTNodeState.STATE_SUCSESS;
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
					itemObject.ItemData is ITaggable<ItemTag> taggable)

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

	protected override void OnFirstEvaluate(AIContext context) 
	{
		List<ItemTag> itemTags = new List<ItemTag>();

		foreach (string key in context.GetDataSet().Keys)
		{
			if (!key.StartsWith(AIContextKeys.c_ItemTagPrefix))
				continue;

			string idString = key.Substring(
				AIContextKeys.c_ItemTagPrefix.Length);

			if (!int.TryParse(idString, out int tagID))
				continue;

			ItemTag tag = IndexRegistry.GetAsset<ItemTag>(tagID);
			if (tag != null)
				itemTags.Add(tag);
		}

		context.SetData<List<ItemTag>>(c_ItemTagsKey, itemTags);
	}

	protected override void OnNodeExited(AIContext context) 
	{
		context.ClearData(c_ItemTagsKey);
	}

	protected override void OnNodeReset(AIContext context) 
	{
		context.ClearData(c_ItemTagsKey);
	}
}
