using BehaviourTrees;
using InventorySystem;
using InventorySystem.Items;
using UnityEngine;

/// <summary>
/// Moves items from the executor's inventory to the target inventory.
/// Converts an existing reservation into an active interaction during deposit,
/// and handles proper cleanup on exit.
/// </summary>
public class DepositHeldItemTask : BTNodeBase
{
	private const string c_InteractionStartedKey = "DepositInteractionStarted";

	private struct DepositData
	{
		public Transform ExecutorTransform;
		public Transform TargetTransform;
		public InteractionPosition AssignedPos;
		public InventoryComponent ExecutorInventory;
		public InventoryComponent TargetInventory;
		public InteractableObjectBase Interactable;
		public IInteractor Interactor;
	}

	protected override void OnFirstEvaluate(AIContext context)
	{
		context.SetData(c_InteractionStartedKey, false);
	}

	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		if (!TryGetDepositData(context, out DepositData data))
			return EBTNodeState.STATE_FAILURE;

		// Distance check
		if (!data.AssignedPos.GetPositionInRange(data.Interactor, data.ExecutorTransform.position))
			return EBTNodeState.STATE_RUNNING;

		InventorySlot heldSlot = data.ExecutorInventory.Inventory.Slots[0];
		ItemData heldItemData = heldSlot.SlotsItem;
		int heldAmount = heldSlot.AmountInSlot;

		// Check if stack is already deposited
		if (heldItemData == null || heldAmount <= 0)
			return EBTNodeState.STATE_SUCSESS;

		// Begin interaction (convert reservation)
		bool interactionStarted = context.GetData<bool>(c_InteractionStartedKey);
		if (!interactionStarted)
		{
			if (!data.Interactable.TryBeginInteraction(data.Interactor, 
				data.ExecutorTransform.position, 
				data.AssignedPos, 
				out _))
			{
				Debug.LogError($"[DepositHeldItemTask] Failed to begin interaction with '{data.TargetTransform.name}'.", data.ExecutorTransform);
				return EBTNodeState.STATE_FAILURE;
			}

			context.SetData(c_InteractionStartedKey, true);
		}

		// Validate space and execute transfer
		Inventory targetInventory = data.TargetInventory.Inventory;
		if (!targetInventory.TryFindRoomForItem(heldItemData, heldAmount, out _, out _))
		{
			Debug.LogError($"[DepositHeldItemTask] '{data.TargetTransform.name}' lacks capacity for {heldAmount}x {heldItemData.ItemID}.", data.ExecutorTransform);
			return EBTNodeState.STATE_FAILURE;
		}

		if (!data.TargetInventory.TryTransferFrom(heldSlot, heldAmount, out int transferredAmount) || transferredAmount != heldAmount)
		{
			Debug.LogError($"[DepositHeldItemTask] Transfer failure or partial transfer with '{data.TargetTransform.name}'.", data.ExecutorTransform);
			return EBTNodeState.STATE_FAILURE;
		}

		return EBTNodeState.STATE_SUCSESS;
	}

	protected override void OnNodeExited(AIContext context)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);
		InteractionPosition assignedPos = context.GetData<InteractionPosition>(AIContextKeys.c_AssignedInteractionPosition);
		bool interactionStarted = context.GetData<bool>(c_InteractionStartedKey);

		if (executorTransform != null && targetTransform != null && assignedPos != null)
		{
			if (executorTransform.TryGetComponent(out IInteractor interactor) &&
				targetTransform.TryGetComponent(out InteractableObjectBase interactable))
			{
				if (interactionStarted)
				{
					Debug.Log("[DepositHeldItemTask] Stopping interaction.");
					interactable.StopInteract(interactor, assignedPos);
				}
				else
				{
					Debug.Log("[DepositHeldItemTask] Cancelling reservation.");
					interactable.CancelReservation(interactor, assignedPos);
				}
			}
		}
		else
		{
			Debug.LogWarning("[DepositHeldItemTask] OnNodeExited called but context data was already missing/cleared!");
		}

		// Context cleanup
		context.ClearData(AIContextKeys.c_ReservationCleanup);
		context.ClearData(AIContextKeys.c_AssignedInteractionPosition);
		context.ClearData(c_InteractionStartedKey);
	}

	protected override void OnNodeReset(AIContext context)
	{
		context.ClearData(c_InteractionStartedKey);
	}

	#region Helper Methods

	private bool TryGetDepositData(AIContext context, out DepositData data)
	{
		data = default;

		data.ExecutorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		data.TargetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);
		data.AssignedPos = context.GetData<InteractionPosition>(AIContextKeys.c_AssignedInteractionPosition);

		if (data.ExecutorTransform == null || data.TargetTransform == null || data.AssignedPos == null)
			return false;

		if (!data.ExecutorTransform.TryGetComponent(out data.ExecutorInventory) || data.ExecutorInventory.Inventory == null)
			return false;

		if (!data.TargetTransform.TryGetComponent(out data.TargetInventory) || data.TargetInventory.Inventory == null)
			return false;

		if (!data.TargetTransform.TryGetComponent(out data.Interactable))
			return false;

		if (!data.ExecutorTransform.TryGetComponent(out data.Interactor))
			return false;

		return true;
	}

	#endregion
}