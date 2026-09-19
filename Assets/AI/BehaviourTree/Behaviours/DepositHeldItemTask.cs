using BehaviourTrees;
using InventorySystem;
using InventorySystem.Items;
using UnityEngine;

/// <summary>
/// Moves items from the executor's inventory to the target inventory.
/// The actor must already have the target's interaction position reserved.
/// This node converts that reservation into an interaction and then
/// transfers any held items.
/// </summary>
public class DepositHeldItemTask : BTNodeBase
{
	private const string c_interactionStartedKey = "DepositInteractionStarted";

	protected override void OnFirstEvaluate(AIContext context)
	{
		context.SetData<bool>(c_interactionStartedKey, false);
	}

	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);
		InteractionPosition assignedPos = context.GetData<InteractionPosition>( AIContextKeys.c_AssignedInteractionPosition);

		if (executorTransform == null ||
			targetTransform == null ||
			assignedPos == null)
		{
			Debug.LogError("[DepositHeldItemTask] Missing executor, target, or " +
				"assigned interaction position.",
				executorTransform);

			return EBTNodeState.STATE_FAILURE;
		}

		if (!executorTransform.TryGetComponent(out InventoryComponent executorInventoryComponent) ||
			executorInventoryComponent.Inventory == null)
		{
			Debug.LogError("[DepositHeldItemTask] Executor has no valid inventory.",
				executorTransform);

			return EBTNodeState.STATE_FAILURE;
		}

		if (!targetTransform.TryGetComponent(out InventoryComponent targetInventoryComponent) ||
			targetInventoryComponent.Inventory == null)
		{
			Debug.LogError($"[DepositHeldItemTask] Target '{targetTransform.name}' " +
				"has no valid InventoryComponent.",
				targetTransform);

			return EBTNodeState.STATE_FAILURE;
		}

		if (!targetTransform.TryGetComponent(out InteractableObjectBase interactable))
		{
			Debug.LogError($"[DepositHeldItemTask] Target '{targetTransform.name}' " +
				"has no InteractableObjectBase.",
				targetTransform);

			return EBTNodeState.STATE_FAILURE;
		}

		IInteractor interactor = executorTransform.GetComponent<IInteractor>();
		if (interactor == null)
		{
			Debug.LogError("[DepositHeldItemTask] Executor has no IInteractor.",
				executorTransform);

			return EBTNodeState.STATE_FAILURE;
		}

		// The actor must actually be at the assigned interaction position.
		if (!assignedPos.GetPositionInRange(interactor,executorTransform.position))
			return EBTNodeState.STATE_RUNNING;

		InventorySlot heldItemSlot = executorInventoryComponent.Inventory.Slots[0];
		ItemData heldItemData = heldItemSlot.SlotsItem;
		int heldAmount = heldItemSlot.AmountInSlot;

		// Empty slot means the entire stack has already been deposited.
		if (heldItemData == null || heldAmount <= 0)
		{
			Debug.Log("[DepositHeldItemTask] Entire held stack deposited.",
				executorTransform);

			return EBTNodeState.STATE_SUCSESS;
		}

		// Convert the reservation into an active interaction exactly once.
		bool interactionStarted = context.GetData<bool>(c_interactionStartedKey);
		if (!interactionStarted)
		{
			if (!interactable.TryBeginInteraction(interactor, executorTransform.position, assignedPos, out _))
			{
				Debug.LogError($"[DepositHeldItemTask] Failed to interact with " +
					$"'{targetTransform.name}' while in range.",
					executorTransform);

				return EBTNodeState.STATE_FAILURE;
			}

			context.SetData<bool>(c_interactionStartedKey, true);

			// The reservation has now become an active interaction.
			context.ClearData(AIContextKeys.c_ReservationCleanup);
		}

		Inventory targetInventory = targetInventoryComponent.Inventory;

		// Find room for the whole stack
		if (!targetInventory.TryFindRoomForItem(heldItemData, heldAmount, out _, out _))
		{
			Debug.LogError($"[DepositHeldItemTask] Target '{targetTransform.name}' " +
				$"cannot accept the entire stack of {heldAmount} " +
				$"item(s) ID {heldItemData.ItemID}.",
				executorTransform);

			return EBTNodeState.STATE_FAILURE;
		}

		// Transfer the entire stack in one operation.
		if (!targetInventoryComponent.TryTransferFrom(heldItemSlot, heldAmount, out int transferredAmount))
		{
			Debug.LogError($"[DepositHeldItemTask] Failed to transfer the full stack " +
				$"of {heldAmount} item(s) ID {heldItemData.ItemID} " +
				$"from '{executorTransform.name}' to " +
				$"'{targetTransform.name}'.",
				executorTransform);

			return EBTNodeState.STATE_FAILURE;
		}

		// The transfer must have been complete.
		if (transferredAmount != heldAmount ||
			heldItemSlot.AmountInSlot > 0)
		{
			Debug.LogError($"[DepositHeldItemTask] Partial deposit detected. " +
				$"Expected {heldAmount}, transferred {transferredAmount}, " +
				$"remaining {heldItemSlot.AmountInSlot}.",
				executorTransform);

			return EBTNodeState.STATE_FAILURE;
		}

		Debug.Log(
			$"[DepositHeldItemTask] Deposited entire stack of " +
			$"{transferredAmount} item(s) ID {heldItemData.ItemID} " +
			$"into '{targetTransform.name}'.",
			executorTransform);

		return EBTNodeState.STATE_SUCSESS;
	}

	protected override void OnNodeExited(AIContext context)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);
		InteractionPosition assignedPos = context.GetData<InteractionPosition>(AIContextKeys.c_AssignedInteractionPosition);

		if (executorTransform != null &&
			targetTransform != null &&
			assignedPos != null &&
			targetTransform.TryGetComponent(out InteractableObjectBase interactable))
		{
			IInteractor interactor = executorTransform.GetComponent<IInteractor>();

			if (interactor != null)
				interactable.StopInteract(interactor, assignedPos);
		}

		System.Action cleanup = context.GetData<System.Action>(AIContextKeys.c_ReservationCleanup);
		cleanup?.Invoke();

		context.ClearData(AIContextKeys.c_ReservationCleanup);
		context.ClearData(AIContextKeys.c_AssignedInteractionPosition);
		context.ClearData(c_interactionStartedKey);
	}

	protected override void OnNodeReset(AIContext context)
	{
		context.ClearData(
			c_interactionStartedKey);
	}
}