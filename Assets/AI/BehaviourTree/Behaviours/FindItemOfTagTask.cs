using BehaviourTrees;
using AssetIndex.Core;
using InventorySystem;
using InventorySystem.Items;
using ObjectTags;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// NOTE: I'm putting this here because I don't know where else to.
// This is being written halfway through reading all the code for context.
// I'm noticing a lot of IDs and string tags in this project. I don't think
// these are actually needed 99% of the time. For example, here

/// <summary>
/// Searches for the nearest item with a specific tag then adds its data to
/// the behaviour trees context.
/// </summary>
/// <remarks>
/// This node should be decorated with a <see cref="BTTimeoutNode"/> 
/// and must be followed by a <see cref="ReserveInteractionPositionTask"/> node to reserve the target.
/// </remarks>
public class FindItemOfTagTask : BTNodeBase
{
	private const float c_searchRadius = 40f;
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
			context.ClearData(AIContextKeys.c_ItemTagFilterPrefix + tag.ID);
		}
		context.ClearData(c_ItemTagsKey);

		return EBTNodeState.STATE_SUCSESS;
	}

	private Transform FindItemOfTags(Transform executorTransform,
		IInteractor interactor,
		ItemTag[] itemTags)
	{
		Vector3 executorPosition = executorTransform.position;

		Transform nearest = null;
		float minDistanceSqr = float.MaxValue;

		Collider[] hitColliders = Physics.OverlapSphere(executorPosition, c_searchRadius);
		for (int i = 0; i < hitColliders.Length; i++)
		{
			Collider col = hitColliders[i];
			if (col == null) 
				continue;

			GameObject entity = col.gameObject;

			if (!entity.TryGetComponent(out IItemObject itemObject) ||
				itemObject.IsItemStored ||
				!(itemObject.ItemData is ITaggable<ItemTag> taggable))
			{
				continue;
			}

			if (entity.TryGetComponent(out ActorInteractableBase interactable) &&
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
			if(!key.StartsWith(AIContextKeys.c_ItemTagFilterPrefix))
			continue;

			// Extract the string remaining after the prefix
			string tagKey = key.Substring(AIContextKeys.c_ItemTagFilterPrefix.Length);

			ItemTag tag = IndexRegistry.GetAsset<ItemTag>(tagKey);
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