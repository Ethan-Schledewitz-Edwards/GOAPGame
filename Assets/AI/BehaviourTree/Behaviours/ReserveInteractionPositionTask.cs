using AI.AIContext;
using BehaviourTrees;
using Interaction.Core;
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
	protected override EBTNodeState OnNodeEvaluated(ref AIContext context, float t)
	{
		Transform executorTransform = context.ExecutorTransform;
		if(executorTransform == null )
			return EBTNodeState.FAILURE;

		Transform targetTransform = context.TargetTransform;
		if (targetTransform == null)
			return EBTNodeState.FAILURE;

		IInteractor interactor = executorTransform.GetComponent<IInteractor>();
		if (interactor == null)
		{
			Debug.Log("[ReserveInteractionPositionTask] Failed: Executor is missing the " +
				"IInteractor component.", executorTransform);
			return EBTNodeState.FAILURE;
		}

		if (!targetTransform.TryGetComponent(out ActorInteractableBase interactable))
		{
			Debug.Log($"[ReserveInteractionPositionTask] Failed: Target object " +
				$"'{targetTransform.name}' lacks an InteractableObjectBase component.", targetTransform);
			return EBTNodeState.FAILURE;
		}

		// Reuse an existing valid position if it already belongs to the current target. 
		// Otherwise, attempt to reserve a fresh position.
		InteractionPoint assignedPosition = context.ReservedInteractionPoint;
		if (assignedPosition != null && interactable.OwnsInteractionPoint(assignedPosition))
		{
			if (!assignedPosition.TryGetInteractionPosition(interactor, out Vector3 existingDestination))
			{
				Debug.Log("[ReserveInteractionPositionTask] Failed: Could not retrieve a " +
					"destination from the existing assigned position.", targetTransform);
				return EBTNodeState.FAILURE;
			}

			Debug.Log("[ReserveInteractionPositionTask] Sucseeded: Retrieved an " +
					"existing interaction position from the behaviour tree.", targetTransform);
			context.TargetDestination = existingDestination;
			return EBTNodeState.SUCSESS;
		}

		if (!interactable.TryReserveClosestPosition(interactor, executorTransform.position, out assignedPosition))
		{
			Debug.Log($"[ReserveInteractionPositionTask] Failed: Could not reserve closest " +
				$"position on interactable object '{targetTransform.name}'.", targetTransform);
			return EBTNodeState.FAILURE;
		}

		if (!assignedPosition.TryGetInteractionPosition(interactor, out Vector3 validDestination))
		{
			Debug.Log("[ReserveInteractionPositionTask] Failed: Could not retrieve a valid " +
				"destination from the newly reserved position.", targetTransform);
			assignedPosition.ReleaseReservation(interactor);
			return EBTNodeState.FAILURE;
		}

		context.ReservedInteractionPoint = assignedPosition;

		// Register a cleanup delegate to cancel the reservation if the behavior tree aborts.
		System.Action cleanup = () =>
		{
			if (interactable != null && interactor != null && assignedPosition != null)
			{
				interactable.CancelReservation(interactor, assignedPosition);
			}
		};
		context.ReservationCleanup = cleanup;
		context.TargetDestination = validDestination;

		Debug.Log("[ReserveInteractionPositionTask] Sucseeded: Found a new " +
			"interaction position and a valid movement destination.", targetTransform);
		return EBTNodeState.SUCSESS;
	}

	protected override void OnFirstEvaluate(ref AIContext context) { }

	protected override void OnNodeExited(ref AIContext context) { }

	protected override void OnNodeReset(ref AIContext context) { }
}