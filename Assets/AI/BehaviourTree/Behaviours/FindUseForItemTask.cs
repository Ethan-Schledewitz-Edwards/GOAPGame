using BehaviourTrees;
using GenericIndex;
using Interaction.InteractableStructures;
using InventorySystem;
using InventorySystem.Items;
using ObjectTags;
using Settlements;
using System.Linq;
using UnityEngine;
using Factions.Core;

/// <summary>
/// Locates a suitable structure for an item in the nearest settlement. 
/// Evaluates proximity to structures matching the specified blueprint or storage tags 
/// and verifies item compatibility against structure filters.
/// </summary>
/// <remarks>
/// This node should be decorated with a <see cref="BTTimeoutNode"/> 
/// and must be followed by a <see cref="ReserveInteractionPositionTask"/> node to reserve the target.
/// </remarks>
public class FindUseForItemTask : BTNodeBase
{
	private readonly StructureTag m_blueprintTag;
	private readonly StructureTag m_storageTag;

	public FindUseForItemTask(StructureTag blueprintTag, StructureTag storageTag) : base()
	{
		m_blueprintTag = blueprintTag;
		m_storageTag = storageTag;
	}

	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		EFaction executorFaction = context.GetData<EFaction>(AIContextKeys.c_ExecutorFaction);

		Settlement closestSettlement = SettlementManager.GetClosestSettlement(executorTransform.position, executorFaction);
		if (closestSettlement != null)
		{
			if (TryFindStructureOfTag(m_storageTag, executorTransform, closestSettlement, context))
				return EBTNodeState.STATE_SUCSESS;

			if (TryFindStructureOfTag(m_blueprintTag, executorTransform, closestSettlement, context))
				return EBTNodeState.STATE_SUCSESS;

			return EBTNodeState.STATE_RUNNING;
		}

		return EBTNodeState.STATE_FAILURE;
	}

	protected override void OnFirstEvaluate(AIContext context) { }

	protected override void OnNodeExited(AIContext context) { }

	protected override void OnNodeReset(AIContext context) { }

	private bool TryFindStructureOfTag(StructureTag structureTag,
		Transform executorTransform,
		Settlement closestSettlement,
		AIContext context)
	{
		IStructure closestStructure = closestSettlement.FindNearestStructureOfType(executorTransform.position, structureTag);
		Debug.Log($"[FindUseForItemTask] Searching for tag {structureTag.name}. Closest found: {(closestStructure != null ? closestStructure.Object.name : "NONE")}");

		if (closestStructure == null)
			return false;

		GameObject structureObject = closestStructure.Object;

		// Ensure the structure is interactable and can filter items
		if (!structureObject.TryGetComponent(out InteractableObjectBase interactable) ||
			!structureObject.TryGetComponent(out IItemFiltered itemFiltered))
		{
			return false;
		}

		ItemIndex itemIndex = IndexRegistry.GetIndex<ItemData>() as ItemIndex;
		int heldItemID = context.GetData<int>(AIContextKeys.c_HeldItemID);

		if (itemIndex?.GetIndexedAsset(heldItemID) is ITaggable<ItemTag> itemTaggable)
		{
			bool passesFilter = itemTaggable.RuntimeTagSet.Any(tag => itemFiltered.ItemTagFilter.Contains(tag));
			Debug.Log($"[FindUseForItemTask] Item filter match for {structureObject.name}: {passesFilter}");

			if (passesFilter)
			{
				// The subsequent ReserveInteractionPositionTask handles reservation and cleanup.
				context.SetData<Transform>(AIContextKeys.c_TargetTransform, structureObject.transform);
				return true;
			}
		}

		return false;
	}
}
