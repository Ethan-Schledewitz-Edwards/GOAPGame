using BehaviourTrees;
using UnityEngine;

public class MoveToInteractionPositionTask : BTNodeBase
{
	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		if (executorTransform == null ||
			!executorTransform.TryGetComponent(out AIPathing pathing))
		{
			return EBTNodeState.STATE_FAILURE;
		}

		// Recalculate the destination every  so the actor always
		// moves toward the currently assigned interaction position.
		if (!TryGetDynamicDestination(context, executorTransform, out Vector3 targetDestination))
		{
			Debug.Log($"{executorTransform} could not resolve its interaction destination.", executorTransform);
			return EBTNodeState.STATE_FAILURE;
		}

		// Keep the context synchronized with the dynamically calculated position.
		context.SetData<Vector3>(AIContextKeys.c_TargetDestination, targetDestination);

		float arrivalDistance = Mathf.Max(pathing.StoppingDistance, 0.05f);

		// Proximity to the destination is close enough
		if (pathing.IsWithinDistance(targetDestination, arrivalDistance))
		{
			pathing.ClearDestination();

			Debug.Log($"{executorTransform} reached their target destination.", executorTransform);
			return EBTNodeState.STATE_SUCSESS;
		}

		bool destinationChanged =
			(pathing.CurrentDestination - targetDestination).sqrMagnitude >= 0.01f;

		pathing.SetDestination(targetDestination);

		if (destinationChanged)
			return EBTNodeState.STATE_RUNNING;

		if (pathing.IsCalculatingPath())
			return EBTNodeState.STATE_RUNNING;

		// At this point the path has had time to resolve.
		if (pathing.IsDestinationInvalid())
		{
			Debug.Log($"{executorTransform} has an invalid path to {targetDestination}.", executorTransform);
			return EBTNodeState.STATE_FAILURE;
		}

		return EBTNodeState.STATE_RUNNING;
	}

	private bool TryGetDynamicDestination(AIContext context, Transform executorTransform, out Vector3 destination)
	{
		destination = Vector3.zero;

		InteractionPosition assignedPosition = context.GetData<InteractionPosition>(AIContextKeys.c_AssignedInteractionPosition);

		if (assignedPosition != null)
		{
			IInteractor interactor = executorTransform.GetComponent<IInteractor>();
			if (interactor == null)
				return false;

			if (!assignedPosition.TryGetInteractionPosition(interactor, out destination))
				return false;

			return true;
		}

		// Fall back to the static destination stored in context.
		destination = context.GetData<Vector3>(AIContextKeys.c_TargetDestination);

		return true;
	}

	protected override void OnFirstEvaluate(AIContext context) { }

	protected override void OnNodeExited(AIContext context) { }

	protected override void OnNodeReset(AIContext context) { }
}
