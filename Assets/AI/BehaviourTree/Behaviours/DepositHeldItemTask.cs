using AI.AIContext;
using BehaviourTrees;
using Interaction.Core;
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
	private struct DepositData
	{
		public Transform ExecutorTransform;
		public Transform TargetTransform;
		public InteractionPoint ReservedPosition;
		public InventoryComponent ExecutorInventory;
		public InventoryComponent TargetInventory;
		public ActorInteractableBase Interactable;
		public IInteractor Interactor;
	}

	protected override void OnFirstEvaluate(ref AIContext context)
	{
		context.DepositInteractionStarted = false;
	}

	protected override EBTNodeState OnNodeEvaluated(ref AIContext context, float t)
	{
		if (!TryGetDepositData(context, out DepositData data))
			return EBTNodeState.FAILURE;

		// Distance check
		if (!data.ReservedPosition.GetPositionInRange(data.Interactor, data.ExecutorTransform.position))
			return EBTNodeState.RUNNING;

		InventorySlot heldSlot = data.ExecutorInventory.Inventory.Slots[0];
		ItemData heldItemData = heldSlot.SlotsItem;
		int heldAmount = heldSlot.AmountInSlot;

		// Check if stack is already deposited
		if (heldItemData == null || heldAmount <= 0)
			return EBTNodeState.SUCSESS;

		// Begin interaction (convert reservation)
		bool interactionStarted = context.DepositInteractionStarted;
		if (!interactionStarted)
		{
			if (!data.Interactable.TryInteract(data.Interactor, 
				data.ExecutorTransform.position, 
				data.ReservedPosition))
			{
				Debug.LogError($"[DepositHeldItemTask] Failed to begin interaction " +
					$"with '{data.TargetTransform.name}'.", data.ExecutorTransform);
				return EBTNodeState.FAILURE;
			}

			context.DepositInteractionStarted = true;
		}

		// Validate space and execute transfer
		Inventory targetInventory = data.TargetInventory.Inventory;
		if (!targetInventory.TryFindRoomForItem(heldItemData, heldAmount, out _, out _))
		{
			Debug.LogError($"[DepositHeldItemTask] '{data.TargetTransform.name}' lacks " +
				$"capacity for {heldAmount}x {heldItemData.ID}.", data.ExecutorTransform);
			return EBTNodeState.FAILURE;
		}

		if (!data.TargetInventory.TryTransferFrom(heldSlot, heldAmount, out int transferredAmount) || transferredAmount != heldAmount)
		{
			Debug.LogError($"[DepositHeldItemTask] Transfer failure or partial " +
				$"transfer with '{data.TargetTransform.name}'.", data.ExecutorTransform);
			return EBTNodeState.FAILURE;
		}

		return EBTNodeState.SUCSESS;
	}

	protected override void OnNodeExited(ref AIContext context)
	{
		Transform executorTransform = context.ExecutorTransform;
		Transform targetTransform = context.TargetTransform;
		InteractionPoint reservedPoint = context.ReservedInteractionPoint;
		bool interactionStarted = context.DepositInteractionStarted;

		if (executorTransform != null && targetTransform != null && reservedPoint != null)
		{
			if (executorTransform.TryGetComponent(out IInteractor interactor) &&
				targetTransform.TryGetComponent(out ActorInteractableBase interactable))
			{
				if (interactionStarted)
				{
					Debug.Log("[DepositHeldItemTask] Stopping interaction.");
					interactable.StopInteract(interactor, reservedPoint);
				}
				else
				{
					Debug.Log("[DepositHeldItemTask] Cancelling reservation.");
					interactable.CancelReservation(interactor, reservedPoint);
				}
			}
		}
		else
		{
			Debug.LogWarning("[DepositHeldItemTask] OnNodeExited called but context data was already missing/cleared!");
		}

		// Context cleanup
		context.ReservationCleanup = null;
		context.ReservedInteractionPoint = null;
		context.DepositInteractionStarted = false;
	}

	protected override void OnNodeReset(ref AIContext context)
	{
		context.DepositInteractionStarted = false;
	}

	#region Helper Methods

	private bool TryGetDepositData(AIContext context, out DepositData data)
	{
		data = default;

		data.ExecutorTransform = context.ExecutorTransform;
		data.TargetTransform = context.TargetTransform;
		data.ReservedPosition = context.ReservedInteractionPoint;

		if (data.ExecutorTransform == null || data.TargetTransform == null || data.ReservedPosition == null)
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