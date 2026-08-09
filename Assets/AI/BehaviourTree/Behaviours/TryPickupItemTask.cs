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

		int itemID = context.GetData<int>(AIContextKeys.c_ItemToFindID);
		if (actorInventory != null)
		{
			// Try to interact with the target
			Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);
			if (targetTransform != null &&
				targetTransform.TryGetComponent(out InteractableObjectBase iob))
			{
				// Pickup the item
				if (iob.TryGetComponent(out IItemObject itemObject) &&
					!itemObject.IsItemStored &&
					itemObject.ItemData.ItemID == itemID)
				{
					// Pickup the item 
					actorInventory.TryAddItem(itemObject.ItemData, 
						itemObject.StackSize, 
						new Transform[] { targetTransform });
					Debug.Log($"{executorTransform}: Picked up item of ID:{itemID}.");

					context.ClearData(AIContextKeys.c_ItemToFindID);
					return EBTNodeState.STATE_SUCSESS;
				}

				// Take the item from a storage unit
				if (iob.TryGetComponent(out InventoryComponent storageInventory) &&
					storageInventory.Inventory.ContainsItem(itemID, out List<InventorySlot> inventorySlots))
				{
					actorInventory.TryTransferFrom(inventorySlots[0], 1, out int itemsTransfered);
					Debug.Log($"{executorTransform}: Took {itemsTransfered} items of ID:{itemID} from {targetTransform}'s inventory.");

					context.ClearData(AIContextKeys.c_ItemToFindID);
					return EBTNodeState.STATE_SUCSESS;
				}
			}
		}
		else
		{
			return EBTNodeState.STATE_FAILURE;
		}

		return EBTNodeState.STATE_RUNNING;
	}

	protected override void OnFirstEvaluate(AIContext context) { }

	protected override void OnNodeExited(AIContext context) { }

	protected override void OnNodeReset(AIContext context) { }
}
