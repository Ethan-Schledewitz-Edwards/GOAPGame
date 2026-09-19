using BehaviourTrees;
using InventorySystem;
using InventorySystem.Items;
using UnityEngine;

/// <summary>
/// Moves items from the executor's inventory to the target inventory.
///
/// The actor must already have the target's interaction position reserved.
/// This node converts that reservation into an interaction and then
/// transfers any held items.
/// </summary>
public class DepositHeldItemTask : BTNodeBase
{
	private const string c_isDepositingKey = "IsDepositing";
	private const string c_depositCooldownKey = "DepositCooldownTimer";
	private const float c_depositCooldown = 0.5f;

	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);
		if (targetTransform == null)
			return EBTNodeState.STATE_FAILURE;

		return TryDepositItem(context, t);
	}

	private EBTNodeState TryDepositItem(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);
		InteractionPosition assignedPos =context.GetData<InteractionPosition>(AIContextKeys.c_AssignedInteractionPosition);

		if (executorTransform == null ||
			targetTransform == null ||
			assignedPos == null)
		{
			return EBTNodeState.STATE_FAILURE;
		}

		if (!executorTransform.TryGetComponent(out InventoryComponent executorInventoryComponent))
			return EBTNodeState.STATE_FAILURE;

		if (!targetTransform.TryGetComponent(out InventoryComponent targetInventoryComponent) ||
			targetInventoryComponent.Inventory == null)
			return EBTNodeState.STATE_FAILURE;

		if (!targetTransform.TryGetComponent(out InteractableObjectBase interactable))
			return EBTNodeState.STATE_FAILURE;

		IInteractor interactor = executorTransform.GetComponent<IInteractor>();

		if (interactor == null)
			return EBTNodeState.STATE_FAILURE;

		// InteractionPosition is authoritative for the final interaction range.
		if (!assignedPos.GetPositionInRange(interactor, executorTransform.position))
		{
			return EBTNodeState.STATE_RUNNING;
		}

		bool isInteracting = context.GetData<bool>(c_isDepositingKey);
		if (!isInteracting)
		{
			if (!interactable.TryInteract(interactor, executorTransform.position, assignedPos, out _))
			{
				// Do not release the reservation just because this particular
				// frame did not successfully convert it to an interaction.
				return EBTNodeState.STATE_RUNNING;
			}

			context.SetData<bool>(c_isDepositingKey, true);
			context.ClearData(AIContextKeys.c_ReservationCleanup);
		}

		float currentCooldown = context.GetData<float>(c_depositCooldownKey) - t;
		if (currentCooldown > 0f)
		{
			context.SetData<float>(c_depositCooldownKey, currentCooldown);
			return EBTNodeState.STATE_RUNNING;
		}

		context.SetData<float>(c_depositCooldownKey, c_depositCooldown);

		Inventory executorInventory = executorInventoryComponent.Inventory;
		Inventory containerInventory = targetInventoryComponent.Inventory;

		if (executorInventory == null ||
			executorInventory.Slots == null ||
			executorInventory.Slots.Count == 0)
		{
			return FinishAndSucceed(
				context,
				interactable,
				interactor,
				assignedPos);
		}

		InventorySlot heldItemSlot =
			executorInventory.Slots[0];

		ItemData heldItemData =
			heldItemSlot.SlotsItem;

		if (heldItemData == null ||
			heldItemSlot.AmountInSlot <= 0)
		{
			return FinishAndSucceed(
				context,
				interactable,
				interactor,
				assignedPos);
		}

		if (!containerInventory.TryFindRoomForItem(
				heldItemData,
				1,
				out _,
				out _))
		{
			return FinishAndSucceed(
				context,
				interactable,
				interactor,
				assignedPos);
		}

		// TryTransferFrom now performs a transactional inventory transfer.
		// It does not call ItemDropped() on the physical object before the
		// destination inventory accepts it.
		if (!targetInventoryComponent.TryTransferFrom(
				heldItemSlot,
				1,
				out int itemsTransferred) ||
			itemsTransferred <= 0)
		{
			Debug.LogWarning(
				$"Deposit transfer failed for item ID: {heldItemData.ItemID} " +
				$"from {executorTransform.name} to {targetTransform.name}. " +
				$"The source item remains in the actor inventory.",
				executorTransform);

			return EBTNodeState.STATE_RUNNING;
		}

		Debug.Log(
			$"Transferred item of ID: {heldItemData.ItemID} " +
			$"from {executorTransform.name} to {targetTransform.name}.",
			executorTransform);

		// Continue until the actor has no more items or the destination
		// cannot accept another item.
		if (heldItemSlot.AmountInSlot <= 0 ||
			!containerInventory.TryFindRoomForItem(
				heldItemData,
				1,
				out _,
				out _))
		{
			return FinishAndSucceed(
				context,
				interactable,
				interactor,
				assignedPos);
		}

		return EBTNodeState.STATE_RUNNING;
	}

	private EBTNodeState FinishAndSucceed(
		AIContext context,
		InteractableObjectBase interactable,
		IInteractor interactor,
		InteractionPosition assignedPos)
	{
		interactable.StopInteract(
			interactor,
			assignedPos);

		context.ClearData(AIContextKeys.c_AssignedInteractionPosition);
		context.ClearData(c_isDepositingKey);

		return EBTNodeState.STATE_SUCSESS;
	}

	protected override void OnFirstEvaluate(AIContext context)
	{
		context.SetData<bool>(c_isDepositingKey, false);
		context.SetData<float>(c_depositCooldownKey, 0f);
	}

	protected override void OnNodeExited(AIContext context)
	{
		bool isInteracting = context.GetData<bool>(c_isDepositingKey);
		if (isInteracting)
		{
			Transform executorTransform = context.GetData<Transform>( AIContextKeys.c_ExecutorTransform);
			Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);
			InteractionPosition assignedPos = context.GetData<InteractionPosition>( AIContextKeys.c_AssignedInteractionPosition);

			if (targetTransform != null &&
				executorTransform != null &&
				assignedPos != null &&
				targetTransform.TryGetComponent(out InteractableObjectBase interactable))
			{
				IInteractor interactor = executorTransform.GetComponent<IInteractor>();

				if (interactor != null)
					interactable.StopInteract(interactor, assignedPos);
			}
		}

		System.Action cleanup = context.GetData<System.Action>(AIContextKeys.c_ReservationCleanup);

		cleanup?.Invoke();
		context.ClearData(AIContextKeys.c_ReservationCleanup);
		context.ClearData(AIContextKeys.c_AssignedInteractionPosition);
		context.ClearData(c_depositCooldownKey);
		context.ClearData(c_isDepositingKey);
	}

	protected override void OnNodeReset(AIContext context)
	{
		context.ClearData(c_depositCooldownKey);
		context.ClearData(c_isDepositingKey);
	}
}