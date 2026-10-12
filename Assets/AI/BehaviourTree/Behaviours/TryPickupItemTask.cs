using AI.AIContext;
using BehaviourTrees;
using Interaction.Core;
using InventorySystem;
using InventorySystem.Items;
using System.Collections.Generic;
using UnityEngine;

public class TryPickupItemTask : BTNodeBase
{
	protected override EBTNodeState OnNodeEvaluated(ref AIContext context, float t)
	{
		Transform executorTransform = context.ExecutorTransform;
		if (executorTransform == null)
			return EBTNodeState.FAILURE;

		if (!executorTransform.TryGetComponent(out IInteractor interactor))
			return EBTNodeState.FAILURE;

		if (!executorTransform.TryGetComponent(out InventoryComponent actorInventory))
			return EBTNodeState.FAILURE;

		Transform targetTransform = context.TargetTransform;
		InteractionPoint assignedPos =  context.AssignedInteractionPosition;

		string itemID = context.ItemToFindID;

		if (targetTransform == null || assignedPos == null)
			return EBTNodeState.FAILURE;

		if (!assignedPos.GetPositionInRange(interactor, executorTransform.position))
			return EBTNodeState.RUNNING;

		if (!targetTransform.TryGetComponent(out ActorInteractableBase interactable))
			return EBTNodeState.FAILURE;

		// ItemIO.TryInteract() moves a standalone item into the actor's inventory.
		if (interactable.TryGetComponent(out IItemObject itemObject) && !itemObject.IsItemStored)
		{
			if (itemObject.ItemData == null || itemObject.ItemData.ID != itemID)
				return EBTNodeState.FAILURE;

			if (!interactable.TryInteract(
					interactor,
					executorTransform.position,
					assignedPos,
					out _))
			{
				return EBTNodeState.FAILURE;
			}

			Debug.Log($"{executorTransform}: Picked up item of ID:{itemID}.");

			// Pickup is complete, release the reservation immediately.
			interactable.StopInteract(interactor, assignedPos);

			context.ItemToFindID = string.Empty;
			context.AssignedInteractionPosition = null;
			return EBTNodeState.SUCSESS;
		}

		// Take items from the inventory
		if (interactable.TryGetComponent(out InventoryComponent storageInventory) &&
			storageInventory.Inventory != null &&
			storageInventory.Inventory.ContainsItem(itemID, out List<InventorySlot> inventorySlots) &&
			inventorySlots.Count > 0)
		{
			if (actorInventory.TryTransferFrom(inventorySlots[0], 1, out int itemsTransferred) && itemsTransferred > 0)
			{
				Debug.Log($"{executorTransform}: Took {itemsTransferred} items " +
					$"of ID:{itemID} from {targetTransform}'s inventory.");

				context.ItemToFindID = string.Empty;
				interactable.StopInteract(interactor, assignedPos);
				context.AssignedInteractionPosition = null;
				return EBTNodeState.SUCSESS;
			}
		}

		interactable.StopInteract(interactor, assignedPos);
		return EBTNodeState.FAILURE;
	}

	protected override void OnFirstEvaluate(ref AIContext context) { }

	protected override void OnNodeExited(ref AIContext context) { }

	protected override void OnNodeReset(ref AIContext context) { }
}
