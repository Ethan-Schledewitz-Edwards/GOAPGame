using BehaviourTrees;
using AssetIndex.Core;
using InventorySystem;
using InventorySystem.Items;
using ObjectTags;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AI.AIContext;
using Interaction.Core;

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

	protected override EBTNodeState OnNodeEvaluated(ref AIContext context, float t)
	{
		if (context.CachedResolvedItemTags == null || context.CachedResolvedItemTags.Length == 0)
			return EBTNodeState.FAILURE;

		Transform executorTransform = context.ExecutorTransform;
		if (executorTransform == null || !executorTransform.TryGetComponent(out IInteractor interactor))
			return EBTNodeState.FAILURE;

		Transform targetItemTransform = FindItemOfTags(executorTransform, interactor, context.CachedResolvedItemTags);

		if (targetItemTransform == null)
			return EBTNodeState.RUNNING;

		// The subsequent ReserveInteractionPositionTask handles reservation and cleanup.
		context.TargetTransform = targetItemTransform;

		// Clean up data on success
		context.ItemTagFilters = null;
		context.CachedResolvedItemTags = null;

		return EBTNodeState.SUCSESS;
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
				!interactable.HasWorkForInteractor(interactor))
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
	protected override void OnFirstEvaluate(ref AIContext context)
	{
		if (context.ItemTagFilters == null || context.ItemTagFilters.Length == 0)
		{
			context.CachedResolvedItemTags = null;
			return;
		}

		List<ItemTag> resolvedTags = new List<ItemTag>();
		foreach (string tagKey in context.ItemTagFilters)
		{
			if (string.IsNullOrEmpty(tagKey))
				continue;

			ItemTag tag = IndexRegistry.GetAsset<ItemTag>(tagKey);
			if (tag != null)
				resolvedTags.Add(tag);
		}

		context.CachedResolvedItemTags = resolvedTags.ToArray();
	}

	protected override void OnNodeExited(ref AIContext context)
	{
		context.CachedResolvedItemTags = null;
	}

	protected override void OnNodeReset(ref AIContext context)
	{
		context.CachedResolvedItemTags = null;
	}
}