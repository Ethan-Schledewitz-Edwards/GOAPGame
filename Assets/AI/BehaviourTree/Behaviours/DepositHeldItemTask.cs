using BehaviourTrees;
using InventorySystem;
using InventorySystem.Items;
using UnityEngine;

public class DepositHeldItemTask : BTNodeBase
{
	private const string c_doneDepositingKey = "DoneDepositing";
	private const string c_depositCooldownKey = "DepositCooldownTimer";
	private const float c_depositCooldown = 0.5f;

	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		if (context.GetData<bool>(c_doneDepositingKey))
		{
			return EBTNodeState.STATE_SUCSESS;
		}

		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);

		if (targetTransform == null)
			return EBTNodeState.STATE_FAILURE;

		return TryDepositItem(context, t);
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
					context.SetData<bool>(c_doneDepositingKey, true);
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
						context.SetData<bool>(c_doneDepositingKey, true);
						return EBTNodeState.STATE_SUCSESS;
					}

					// Is the container completely full of this item?
					if (!containerInventory.TryFindRoomForItem(heldItemData, 1, out _, out _))
					{
						context.SetData<bool>(c_doneDepositingKey, true);
						return EBTNodeState.STATE_SUCSESS;
					}

					return EBTNodeState.STATE_RUNNING;
				}
				else // No room at all
				{
					context.SetData<bool>(c_doneDepositingKey, true);
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
		context.SetData<bool>(c_doneDepositingKey, false);
		context.SetData<float>(c_depositCooldownKey, c_depositCooldown); // Reset timer
	}

	protected override void OnNodeExited(AIContext context) 
	{
		context.ClearData(c_doneDepositingKey);
		context.ClearData(c_depositCooldownKey);
	}

	protected override void OnNodeReset(AIContext context)
	{
		context.ClearData(c_doneDepositingKey);
		context.ClearData(c_depositCooldownKey);
	}
}