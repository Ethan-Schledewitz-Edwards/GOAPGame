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
/// Searches for the nearest item with a specific tag then adds its data to
/// the behaviour trees context.
/// </summary>
/// <remarks>
/// This node should be decorated with a <see cref="BTTimeoutNode"/> 
/// and must be followed by a <see cref="ReserveInteractionPositionTask"/> node to reserve the target.
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

		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		if (executorTransform == null || !executorTransform.TryGetComponent(out IInteractor interactor))
			return EBTNodeState.STATE_FAILURE;

		Transform targetItemTransform = FindItemOfTags(executorTransform, interactor, itemTags.ToArray());

		if (targetItemTransform == null)
			return EBTNodeState.STATE_RUNNING;

		// The subsequent ReserveInteractionPositionTask handles reservation and cleanup.
		context.SetData<Transform>(AIContextKeys.c_TargetTransform, targetItemTransform);

		// Clean up temporary context data on success
		foreach (ItemTag tag in itemTags)
		{
			context.ClearData(AIContextKeys.c_ItemTagPrefix + tag.TagID);
		}
		context.ClearData(c_ItemTagsKey);

		return EBTNodeState.STATE_SUCSESS;
	}

	private Transform FindItemOfTags(Transform executorTransform, IInteractor interactor, ItemTag[] itemTags)
	{
		Vector3 executorPosition = executorTransform.position;
		Vector2Int[] neighbourChunkCoordinates = ChunkUtility.GetChunkCoordinatesInRadius(executorPosition, c_chunkSearchRadius);

		Transform nearest = null;
		float minDistanceSqr = float.MaxValue;
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
					!(itemObject.ItemData is ITaggable<ItemTag> taggable))
				{
					continue;
				}

				if (entity.TryGetComponent(out InteractableObjectBase interactable) &&
					!interactable.HasAvailableWork(interactor))
				{
					continue;
				}

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

		return nearest;
	}

	/// <summary>
	/// Extracts item tags from the AI context data set and stores them in the context.
	/// </summary>
	protected override void OnFirstEvaluate(AIContext context)
	{
		List<ItemTag> itemTags = new List<ItemTag>();
		foreach (string key in context.GetDataSet().Keys)
		{
			if (!key.StartsWith(AIContextKeys.c_ItemTagPrefix))
				continue;

			string idString = key.Substring(AIContextKeys.c_ItemTagPrefix.Length);

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