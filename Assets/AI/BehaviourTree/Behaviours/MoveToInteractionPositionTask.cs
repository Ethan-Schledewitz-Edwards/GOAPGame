using AI.AIContext;
using BehaviourTrees;
using Interaction.Core;
using UnityEngine;

public class MoveToInteractionPositionTask : BTNodeBase
{
	protected override EBTNodeState OnNodeEvaluated(ref AIContext context, float t)
	{
		Transform executorTransform = context.ExecutorTransform;
		if (executorTransform == null ||
			!executorTransform.TryGetComponent(out AIPathing pathing))
		{
			return EBTNodeState.FAILURE;
		}

		// Recalculate the destination 
		if (!TryGetDynamicDestination(ref context, executorTransform, out Vector3 targetDestination))
		{
			Debug.Log($"{executorTransform} could not resolve its interaction destination.", executorTransform);
			return EBTNodeState.FAILURE;
		}

		// Keep the context synchronized with the dynamically calculated position.
		context.TargetDestination = targetDestination;

		float arrivalDistance = Mathf.Max(pathing.StoppingDistance, 0.05f);

		// Proximity to the destination is close enough.
		if (pathing.IsWithinDistance(targetDestination, arrivalDistance))
		{
			pathing.ClearDestination();

			Debug.Log($"{executorTransform} reached their target destination.", executorTransform);
			return EBTNodeState.SUCSESS;
		}

		bool destinationChanged =
			(pathing.CurrentDestination - targetDestination).sqrMagnitude >= 0.01f;

		pathing.SetDestination(targetDestination);

		if (destinationChanged)
			return EBTNodeState.RUNNING;

		if (pathing.IsCalculatingPath())
			return EBTNodeState.RUNNING;

		// At this point the path has had time to resolve.
		if (pathing.IsDestinationInvalid())
		{
			Debug.Log($"{executorTransform} has an invalid path to {targetDestination}.", executorTransform);
			return EBTNodeState.FAILURE;
		}

		return EBTNodeState.RUNNING;
	}

	private bool TryGetDynamicDestination(ref AIContext context, Transform executorTransform, out Vector3 destination)
	{
		destination = Vector3.zero;

		InteractionPoint assignedPosition = context.AssignedInteractionPosition;
		if (assignedPosition != null)
		{
			IInteractor interactor = executorTransform.GetComponent<IInteractor>();
			if (interactor == null)
				return false;

			if (!assignedPosition.TryGetInteractionPosition(interactor, out destination))
				return false;

			return true;
		}

		// Fallback to the static destination stored in context.
		destination = context.TargetDestination;

		return true;
	}

	protected override void OnFirstEvaluate(ref AIContext context) { }

	protected override void OnNodeExited(ref AIContext context) { }

	protected override void OnNodeReset(ref AIContext context) { }
}
