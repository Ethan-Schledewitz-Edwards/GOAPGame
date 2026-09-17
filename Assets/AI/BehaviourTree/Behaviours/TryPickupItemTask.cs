using BehaviourTrees;
using InventorySystem;
using InventorySystem.Items;
using System.Collections.Generic;
using UnityEngine;

public class TryPickupItemTask : BTNodeBase
{
	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		IInteractor interactor = executorTransform.GetComponent<IInteractor>();
		InventoryComponent actorInventory = executorTransform.GetComponent<InventoryComponent>();
		Vector3 executorPosition = executorTransform.position;

		InteractionPosition assignedPos = context.GetData<InteractionPosition>(AIContextKeys.c_AssignedInteractionPosition);
		int itemID = context.GetData<int>(AIContextKeys.c_ItemToFindID);

		if (actorInventory != null)
		{
			Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);
			if (targetTransform != null && targetTransform.TryGetComponent(out InteractableObjectBase iob))
			{
				// Convert the reservation into an active interaction
				if (iob.TryInteract(interactor, executorPosition, assignedPos, out int interactorValue))
				{
					// Clear the abort delegate since we successfully consumed the reservation
					context.ClearData(AIContextKeys.c_ReservationCleanup);

					// Pickup the standalone item
					if (iob.TryGetComponent(out IItemObject itemObject) &&
						!itemObject.IsItemStored &&
						itemObject.ItemData.ItemID == itemID)
					{
						actorInventory.TryAddItem(itemObject.ItemData, itemObject.StackSize, new Transform[] { targetTransform });
						Debug.Log($"{executorTransform}: Picked up item of ID:{itemID}.");

						context.ClearData(AIContextKeys.c_ItemToFindID);
						iob.StopInteract(interactor, assignedPos);
						context.ClearData(AIContextKeys.c_AssignedInteractionPosition);

						return EBTNodeState.STATE_SUCSESS;
					}

					// Take the item from a storage unit
					if (iob.TryGetComponent(out InventoryComponent storageInventory) &&
						storageInventory.Inventory.ContainsItem(itemID, out List<InventorySlot> inventorySlots))
					{
						actorInventory.TryTransferFrom(inventorySlots[0], 1, out int itemsTransfered);
						Debug.Log($"{executorTransform}: Took {itemsTransfered} items of ID:{itemID} from {targetTransform}'s inventory.");

						context.ClearData(AIContextKeys.c_ItemToFindID);
						iob.StopInteract(interactor, assignedPos);
						context.ClearData(AIContextKeys.c_AssignedInteractionPosition);

						return EBTNodeState.STATE_SUCSESS;
					}

					iob.StopInteract(interactor, assignedPos);
					context.ClearData(AIContextKeys.c_AssignedInteractionPosition);
				}
			}
		}

		return EBTNodeState.STATE_FAILURE;
	}

	protected override void OnFirstEvaluate(AIContext context) { }

	protected override void OnNodeExited(AIContext context) { }

	protected override void OnNodeReset(AIContext context) { }
}
