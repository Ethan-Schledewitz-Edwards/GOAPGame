using BehaviourTrees;
using InventorySystem;
using InventorySystem.Items;
using System.Collections.Generic;
using UnityEngine;

public class TryPickupItemTask : BTNodeBase
{
	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		Transform executorTransform =
			context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);

		if (executorTransform == null)
			return EBTNodeState.STATE_FAILURE;

		if (!executorTransform.TryGetComponent(out IInteractor interactor))
			return EBTNodeState.STATE_FAILURE;

		if (!executorTransform.TryGetComponent(out InventoryComponent actorInventory))
			return EBTNodeState.STATE_FAILURE;

		Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);
		InteractionPosition assignedPos = context.GetData<InteractionPosition>(AIContextKeys.c_AssignedInteractionPosition);
		int itemID = context.GetData<int>(AIContextKeys.c_ItemToFindID);

		if (targetTransform == null || assignedPos == null)
			return EBTNodeState.STATE_FAILURE;

		if (!assignedPos.GetPositionInRange(interactor, executorTransform.position))
			return EBTNodeState.STATE_RUNNING;

		if (!targetTransform.TryGetComponent(out InteractableObjectBase interactable))
			return EBTNodeState.STATE_FAILURE;

		// ItemIO.TryInteract() moves a standalone item into the actor's inventory.
		if (interactable.TryGetComponent(out IItemObject itemObject) && !itemObject.IsItemStored)
		{
			if (itemObject.ItemData == null || itemObject.ItemData.ItemID != itemID)
				return EBTNodeState.STATE_FAILURE;

			if (!interactable.TryInteract(
					interactor,
					executorTransform.position,
					assignedPos,
					out _))
			{
				return EBTNodeState.STATE_FAILURE;
			}

			Debug.Log($"{executorTransform}: Picked up item of ID:{itemID}.");

			// Pickup is complete, release the reservation immediately.
			interactable.StopInteract(interactor, assignedPos);

			context.ClearData(AIContextKeys.c_ItemToFindID);
			context.ClearData(AIContextKeys.c_AssignedInteractionPosition);
			return EBTNodeState.STATE_SUCSESS;
		}

		// Take items from the inventory
		if (interactable.TryGetComponent(out InventoryComponent storageInventory) &&
			storageInventory.Inventory != null &&
			storageInventory.Inventory.ContainsItem(itemID, out List<InventorySlot> inventorySlots) &&
			inventorySlots.Count > 0)
		{
			if (actorInventory.TryTransferFrom(inventorySlots[0], 1, out int itemsTransferred) && itemsTransferred > 0)
			{
				Debug.Log($"{executorTransform}: Took {itemsTransferred} items of ID:{itemID} from {targetTransform}'s inventory.");

				context.ClearData(AIContextKeys.c_ItemToFindID);
				interactable.StopInteract(interactor, assignedPos);
				context.ClearData(AIContextKeys.c_AssignedInteractionPosition);
				return EBTNodeState.STATE_SUCSESS;
			}
		}

		interactable.StopInteract(interactor, assignedPos);
		return EBTNodeState.STATE_FAILURE;
	}

	protected override void OnFirstEvaluate(AIContext context) { }

	protected override void OnNodeExited(AIContext context) { }

	protected override void OnNodeReset(AIContext context) { }
}
