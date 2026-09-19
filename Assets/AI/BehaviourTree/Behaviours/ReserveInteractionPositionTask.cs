using BehaviourTrees;
using UnityEngine;

/// <summary>
/// Reserves the interaction position belonging to the target in the BT
/// context and makes that reserved position the movement destination.
/// </summary>
public class ReserveInteractionPositionTask : BTNodeBase
{
	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);

		if (executorTransform == null || targetTransform == null)
			return EBTNodeState.STATE_FAILURE;

		IInteractor interactor = executorTransform.GetComponent<IInteractor>();
		if (interactor == null)
			return EBTNodeState.STATE_FAILURE;

		if (!targetTransform.TryGetComponent(out InteractableObjectBase interactable))
			return EBTNodeState.STATE_FAILURE;

		// The context may still contain the interaction position from the
		// Only reuse it if it actually belongs to the current target.
		// Otherwise, reserve a fresh position on this target.
		InteractionPosition assignedPosition = context.GetData<InteractionPosition>(AIContextKeys.c_AssignedInteractionPosition);

		if (assignedPosition != null &&
			interactable.HasInteractionPosition(assignedPosition))
		{
			if (!assignedPosition.TryGetInteractionPosition(interactor, out Vector3 existingDestination))
				return EBTNodeState.STATE_FAILURE;

			context.SetData<Vector3>(AIContextKeys.c_TargetDestination, existingDestination);
			return EBTNodeState.STATE_SUCSESS;
		}

		if (!interactable.TryReserveClosestPosition(
				interactor,
				executorTransform.position,
				out assignedPosition))
		{
			return EBTNodeState.STATE_FAILURE;
		}

		if (!assignedPosition.TryGetInteractionPosition(
				interactor,
				out Vector3 validDestination))
		{
			assignedPosition.ReleaseReservation(interactor);
			return EBTNodeState.STATE_FAILURE;
		}

		context.SetData<InteractionPosition>(AIContextKeys.c_AssignedInteractionPosition, assignedPosition);
		context.SetData<Vector3>(AIContextKeys.c_TargetDestination, validDestination);

		return EBTNodeState.STATE_SUCSESS;
	}

	protected override void OnFirstEvaluate(AIContext context) { }

	protected override void OnNodeExited(AIContext context) { }

	protected override void OnNodeReset(AIContext context) { }
}
