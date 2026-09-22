using BehaviourTrees;
using UnityEngine;

/// <summary>
/// Attempts to reserve an interaction position on the target object stored in the behavior tree context 
/// and registers the destination for movement.
/// </summary>
/// <remarks>
/// Reuses an existing valid position if it matches the current target. Otherwise, reserves the closest 
/// available position on the interactable object and registers a cleanup delegate for safe abort handling.
/// </remarks>
public class ReserveInteractionPositionTask : BTNodeBase
{
	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);

		if (executorTransform == null || targetTransform == null)
		{
			Debug.Log($"[ReserveInteractionPositionTask] Failed: Missing executorTransform " +
				$"({executorTransform}) or targetTransform ({targetTransform}).", executorTransform);
			return EBTNodeState.STATE_FAILURE;
		}

		IInteractor interactor = executorTransform.GetComponent<IInteractor>();
		if (interactor == null)
		{
			Debug.Log("[ReserveInteractionPositionTask] Failed: Executor is missing the " +
				"IInteractor component.", executorTransform);
			return EBTNodeState.STATE_FAILURE;
		}

		if (!targetTransform.TryGetComponent(out InteractableObjectBase interactable))
		{
			Debug.Log($"[ReserveInteractionPositionTask] Failed: Target object " +
				$"'{targetTransform.name}' lacks an InteractableObjectBase component.", targetTransform);
			return EBTNodeState.STATE_FAILURE;
		}

		// Reuse an existing valid position if it already belongs to the current target. 
		// Otherwise, attempt to reserve a fresh position.
		InteractionPosition assignedPosition = context.GetData<InteractionPosition>(AIContextKeys.c_AssignedInteractionPosition);
		if (assignedPosition != null && interactable.HasInteractionPosition(assignedPosition))
		{
			if (!assignedPosition.TryGetInteractionPosition(interactor, out Vector3 existingDestination))
			{
				Debug.Log("[ReserveInteractionPositionTask] Failed: Could not retrieve a " +
					"destination from the existing assigned position.", targetTransform);
				return EBTNodeState.STATE_FAILURE;
			}

			Debug.Log("[ReserveInteractionPositionTask] Sucseeded: Retrieved an " +
					"existing interaction position from the behaviour tree.", targetTransform);
			context.SetData<Vector3>(AIContextKeys.c_TargetDestination, existingDestination);
			return EBTNodeState.STATE_SUCSESS;
		}

		if (!interactable.TryReserveClosestPosition(interactor, executorTransform.position, out assignedPosition))
		{
			Debug.Log($"[ReserveInteractionPositionTask] Failed: Could not reserve closest " +
				$"position on interactable object '{targetTransform.name}'.", targetTransform);
			return EBTNodeState.STATE_FAILURE;
		}

		if (!assignedPosition.TryGetInteractionPosition(interactor, out Vector3 validDestination))
		{
			Debug.Log("[ReserveInteractionPositionTask] Failed: Could not retrieve a valid " +
				"destination from the newly reserved position.", targetTransform);
			assignedPosition.ReleaseReservation(interactor);
			return EBTNodeState.STATE_FAILURE;
		}

		context.SetData<InteractionPosition>(AIContextKeys.c_AssignedInteractionPosition, assignedPosition);

		// Register a cleanup delegate to cancel the reservation if the behavior tree aborts.
		System.Action cleanup = () =>
		{
			if (interactable != null && interactor != null && assignedPosition != null)
			{
				interactable.CancelReservation(interactor, assignedPosition);
			}
		};
		context.SetData<System.Action>(AIContextKeys.c_ReservationCleanup, cleanup);
		context.SetData<Vector3>(AIContextKeys.c_TargetDestination, validDestination);

		Debug.Log("[ReserveInteractionPositionTask] Sucseeded: Found a new " +
			"interaction position and a valid movement destination.", targetTransform);
		return EBTNodeState.STATE_SUCSESS;
	}

	protected override void OnFirstEvaluate(AIContext context) { }

	protected override void OnNodeExited(AIContext context) { }

	protected override void OnNodeReset(AIContext context) { }
}