using BehaviourTrees;
using InventorySystem;
using InventorySystem.Items;
using UnityEngine;

/// <summary>
/// A behavior tree node that attempts to move an item from the executors inventory to their targets inventory.
/// </summary>
/// <remarks>
/// This node should always be decorated with a timeout node.
/// </remarks>
public class DepositHeldItemTask : BTNodeBase
{
	private const string c_doneDepositingKey = "DoneDepositing";
	private const string c_depositCooldownKey = "DepositCooldownTimer";
	private const string c_isInteractingKey = "IsInteracting";
	private const float c_depositCooldown = 0.5f;

	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		if (context.GetData<bool>(c_doneDepositingKey))
		{
			return EBTNodeState.STATE_SUCSESS;
		}

		Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);
		if (targetTransform == null)
			return EBTNodeState.STATE_FAILURE;

		return TryDepositItem(context, t);
	}

	private EBTNodeState TryDepositItem(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);
		InteractionPosition assignedPos = context.GetData<InteractionPosition>(AIContextKeys.c_AssignedInteractionPosition);
		IInteractor interactor = executorTransform.GetComponent<IInteractor>();

		if (executorTransform.TryGetComponent(out InventoryComponent executorInventoryComponent) &&
			targetTransform.TryGetComponent(out InventoryComponent targetInventoryComponent) &&
			targetTransform.TryGetComponent(out InteractableObjectBase iob))
		{
			bool isInteracting = context.GetData<bool>(c_isInteractingKey);

			// 1. Establish the formal interaction if we haven't already
			if (!isInteracting)
			{
				if (!iob.TryInteract(interactor, executorTransform.position, assignedPos, out int interactorValue))
				{
					// Actor was out of range or lost the reservation
					context.ClearData(AIContextKeys.c_AssignedInteractionPosition);
					return EBTNodeState.STATE_FAILURE;
				}

				context.SetData<bool>(c_isInteractingKey, true);
				context.ClearData(AIContextKeys.c_ReservationCleanup); // Consumed the reservation successfully
			}

			// Deposit loop
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
					return FinishAndSucceed(context, iob, interactor, assignedPos);
				}

				if (containerInventory.TryFindRoomForItem(heldItemData, 1, out InventorySlot firstSlot, out int roomAvailable))
				{
					heldItemSlot.RemoveFromStack(1, out Transform[] droppedItems, true);

					// Transfer an item from the held item stack
					if (targetInventoryComponent.TryAddItem(heldItemData, 1, droppedItems))
					{
						Debug.Log($"Transferred item of ID: {heldItemData.ItemID} from {executorTransform.name} to {targetTransform.name}.");
					}

					// Did the executor run out of items or is the container completely full?
					if (heldItemSlot.AmountInSlot <= 0 || !containerInventory.TryFindRoomForItem(heldItemData, 1, out _, out _))
					{
						return FinishAndSucceed(context, iob, interactor, assignedPos);
					}

					return EBTNodeState.STATE_RUNNING;
				}
				else // No room at all
				{
					return FinishAndSucceed(context, iob, interactor, assignedPos);
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

	private EBTNodeState FinishAndSucceed(AIContext context, InteractableObjectBase iob, IInteractor interactor, InteractionPosition assignedPos)
	{
		context.SetData<bool>(c_doneDepositingKey, true);

		iob.StopInteract(interactor, assignedPos);
		context.ClearData(AIContextKeys.c_AssignedInteractionPosition);

		return EBTNodeState.STATE_SUCSESS;
	}

	protected override void OnFirstEvaluate(AIContext context)
	{
		context.SetData<bool>(c_doneDepositingKey, false);
		context.SetData<bool>(c_isInteractingKey, false);
		context.SetData<float>(c_depositCooldownKey, c_depositCooldown);
	}

	protected override void OnNodeExited(AIContext context)
	{
		// If the node is aborted via timeout, release the interaction
		bool isInteracting = context.GetData<bool>(c_isInteractingKey);
		if (isInteracting)
		{
			Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
			Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);
			InteractionPosition assignedPos = context.GetData<InteractionPosition>(AIContextKeys.c_AssignedInteractionPosition);

			if (targetTransform != null && executorTransform != null && assignedPos != null)
			{
				if (targetTransform.TryGetComponent(out InteractableObjectBase iob))
				{
					IInteractor interactor = executorTransform.GetComponent<IInteractor>();
					iob.StopInteract(interactor, assignedPos);
				}
			}
		}

		// Cleanup for reservations that never converted to interactions
		context.GetData<System.Action>(AIContextKeys.c_ReservationCleanup)?.Invoke();
		context.ClearData(AIContextKeys.c_ReservationCleanup);

		context.ClearData(AIContextKeys.c_AssignedInteractionPosition);
		context.ClearData(c_doneDepositingKey);
		context.ClearData(c_depositCooldownKey);
		context.ClearData(c_isInteractingKey);
	}

	protected override void OnNodeReset(AIContext context)
	{
		context.ClearData(c_doneDepositingKey);
		context.ClearData(c_depositCooldownKey);
		context.ClearData(c_isInteractingKey);
	}
}