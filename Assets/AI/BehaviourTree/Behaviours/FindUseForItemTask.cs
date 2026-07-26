using BehaviourTrees;
using GenericIndex;
using Interaction.InteractableStructures;
using InventorySystem;
using InventorySystem.Items;
using ObjectTags;
using Settlements;
using System.Linq;
using UnityEditor.Graphs;
using UnityEngine;

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

		Settlement closestSettlement = SettlementManager.GetClosestSettlement(executorTransform.position, true, true);
		if (closestSettlement != null)
		{
			if(TryFindStructureOfTag(m_storageTag, executorTransform, closestSettlement, context))
				return EBTNodeState.STATE_SUCSESS;

			if (TryFindStructureOfTag(m_blueprintTag, executorTransform, closestSettlement, context))
				return EBTNodeState.STATE_SUCSESS;

			return EBTNodeState.STATE_RUNNING;
		}

		return EBTNodeState.STATE_FAILURE;
	}

	protected override void OnFirstEvaluate(AIContext context)
	{
		Debug.Log("Trying to find a resource deposit.");
	}

	protected override void OnNodeExited(AIContext context) {}

	protected override void OnNodeReset(AIContext context) {}

	private bool TryFindStructureOfTag(StructureTag structureTag, 
		Transform executorTransform, 
		Settlement closestSettlement, 
		AIContext context)
	{
		IStructure closestStructure = closestSettlement.FindNearestStructureOfType(executorTransform.position, structureTag);
		if (closestStructure != null && 
			closestStructure.Object is GameObject closestStructureObject)
		{
			if (closestStructureObject.TryGetComponent(out InteractableObjectBase interactable))
			{
				if(interactable.TryGetComponent(out IItemFiltered itemFiltered))
				{
					ItemIndex itemIndex = IndexRegistry.GetIndex<ItemData>() as ItemIndex;
					int heldItemID = context.GetData<int>(AIContextKeys.c_HeldItemID);

					if (itemIndex?.GetIndexedAsset(heldItemID) is ITaggable<ItemTag> itemTaggable)
					{
						// Check if the structures tags include the held items tags
						bool passesFilter = itemTaggable.RuntimeTagSet.Any(tag => itemFiltered.ItemTagFilter.Contains(tag));
						if (passesFilter)
						{
							context.SetData<Transform>(AIContextKeys.c_TargetTransform, closestStructureObject.transform);
							context.SetData<Vector3>(AIContextKeys.c_TargetDestination, interactable.GetInteractionPositon());
							return true;
						}
					}
				}
			}
		}
		return false;
	}
}
