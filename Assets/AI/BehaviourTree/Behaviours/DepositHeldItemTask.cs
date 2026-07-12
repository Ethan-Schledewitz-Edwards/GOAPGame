using BehaviourTrees;
using InventorySystem;
using InventorySystem.Items;
using NUnit.Framework.Interfaces;
using UnityEditor.Graphs;
using UnityEngine;

public class DepositHeldItemTask : BTNodeBase
{
	private const string c_doneKey = "DoneDepositing";
	private const string c_depositCooldownKey = "CooldownTimer";
	private const float c_depositCooldown = 0.5f;

	private const string c_stackSizeKey = "SizeOfItemStackToTransfer";
	private const string c_containerRoomKey = "ContainerRoom";

	protected override EBTNodeState OnUpdate(AIContext context, float t)
	{
		if (context.GetData<bool>(c_doneKey))
		{
			return EBTNodeState.STATE_SUCSESS;
		}

		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);

		if (targetTransform == null)
			return EBTNodeState.STATE_FAILURE;

		EBTNodeState depositResult = TryDepositItem(context, t);

		switch (depositResult)
		{
			case EBTNodeState.STATE_RUNNING:
				return EBTNodeState.STATE_RUNNING;

			case EBTNodeState.STATE_SUCSESS:

				// Try to interact with the target
				if(targetTransform.TryGetComponent(out InteractableObjectBase interactableObjectBase))
					interactableObjectBase.TryInteract(executorTransform.GetComponent<IInteractor>(), true);
				return EBTNodeState.STATE_SUCSESS;

			case EBTNodeState.STATE_FAILURE:
				return EBTNodeState.STATE_FAILURE;

			default:
				return EBTNodeState.STATE_RUNNING;
		}
	}

	private EBTNodeState TryDepositItem(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);

		if (executorTransform.TryGetComponent(out InventoryComponent executorInventoryComponent) &&
			targetTransform.TryGetComponent(out InventoryComponent targetInventoryComponent))
		{
			float currentCooldown = context.GetData<float>(c_depositCooldownKey) - t;
			if (currentCooldown <= 0f)
			{
				context.SetData<float>(c_depositCooldownKey, c_depositCooldown); // Reset timer

				Inventory executorInventory = executorInventoryComponent.Inventory;
				Inventory containerInventory = targetInventoryComponent.Inventory;

				InventorySlot heldItemSlot = executorInventory.Slots[0];
				ItemData heldItemData = heldItemSlot.SlotsItem;

				if (heldItemData == null || heldItemSlot.AmountInSlot <= 0)
				{
					// Executor has nothing to deposit
					context.SetData<bool>(c_doneKey, true);
					return EBTNodeState.STATE_SUCSESS;
				}

				if (containerInventory.TryFindRoomForItem(heldItemData, 1, out InventorySlot firstSlot, out int roomAvailable))
				{
					heldItemSlot.RemoveFromStack(1, out Transform[] droppedItems, true);

					Debug.Log("Dropped Items: " + droppedItems.Length);

					// Transfer an item from the held item stack
					if (targetInventoryComponent.TryAddItem(heldItemData, 1, droppedItems))
					{
						Debug.Log($"Transferred item of ID: {heldItemData.ItemID} from " +
							$"{executorTransform.name}'s heldItemData to {targetTransform.name}'s inventory.");
					}

					// Did the executor run out of items?
					if (heldItemSlot.AmountInSlot <= 0)
					{
						context.SetData<bool>(c_doneKey, true);
						return EBTNodeState.STATE_SUCSESS;
					}

					// Is the container completely full of this item?
					if (!containerInventory.TryFindRoomForItem(heldItemData, 1, out _, out _))
					{
						context.SetData<bool>(c_doneKey, true);
						return EBTNodeState.STATE_SUCSESS;
					}

					return EBTNodeState.STATE_RUNNING;
				}
				else // No room at all
				{
					context.SetData<bool>(c_doneKey, true);
					return EBTNodeState.STATE_SUCSESS;
				}
			}
			else
			{
				context.SetData<float>(c_depositCooldownKey, currentCooldown);
			}

			return EBTNodeState.STATE_RUNNING;
		}

		return EBTNodeState.STATE_FAILURE;
	}

	protected override void OnFirstEvaluate(AIContext context)
	{
		Debug.Log("Begin item deposit attempt");
		context.SetData<bool>(c_doneKey, false);
	}
}