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

		IInteractor interactor = executorTransform.GetComponent<IInteractor>();
		if (interactor == null)
			return false;

		IStructure closestStructure = closestSettlement.FindNearestStructureOfType(executorTransform.position, structureTag);
		Debug.Log($"[FindUseForItemTask] Searching for tag {structureTag.name}. Closest found: {(closestStructure != null ? closestStructure.Object.name : "NONE")}");

		if (closestStructure != null)
		{
			GameObject structureObject = closestStructure.Object;
			if (structureObject.TryGetComponent(out InteractableObjectBase interactable))
			{
				if (interactable.TryGetComponent(out IItemFiltered itemFiltered))
				{
					ItemIndex itemIndex = IndexRegistry.GetIndex<ItemData>() as ItemIndex;
					int heldItemID = context.GetData<int>(AIContextKeys.c_HeldItemID);

					if (itemIndex?.GetIndexedAsset(heldItemID) is ITaggable<ItemTag> itemTaggable)
					{
						bool passesFilter = itemTaggable.RuntimeTagSet.Any(tag => itemFiltered.ItemTagFilter.Contains(tag));
						Debug.Log($"[FindUseForItemTask] Item filter match for {structureObject.name}: {passesFilter}");

						if (passesFilter)
						{
							// Only proceed if we successfully reserve a slot at the storage building
							if (interactable.TryReserveClosestPosition(interactor, executorTransform.position, out InteractionPosition assignedPosition))
							{
								if (assignedPosition != null && assignedPosition.TryGetInteractionPosition(interactor, out Vector3 validDestination))
								{
									context.SetData<Transform>(AIContextKeys.c_TargetTransform, structureObject.transform);
									context.SetData<Vector3>(AIContextKeys.c_TargetDestination, validDestination);
									context.SetData<InteractionPosition>(AIContextKeys.c_AssignedInteractionPosition, assignedPosition);

									// Cleanup delegate in case the behavior tree aborts
									System.Action cleanup = () =>
									{
										if (interactable != null && interactor != null && assignedPosition != null)
										{
											interactable.CancelReservation(interactor, assignedPosition);
										}
									};
									context.SetData<System.Action>(AIContextKeys.c_ReservationCleanup, cleanup);

									return true;
								}
							}
						}
					}
				}
			}
		}
		return false;
	}
}
