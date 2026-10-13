using AI.AIContext;
using BehaviourTrees;
using Interaction.Core;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A behavior tree node that attempts to parrent the interactor to the followPoint of a carryable object.
/// </summary>
/// <remarks>
/// This node should always be decorated with a timeout node.
/// </remarks>
public class CarryObjectTask : BTNodeBase
{
	protected override EBTNodeState OnNodeEvaluated(ref AIContext context, float t)
	{
		Transform executorTransform = context.ExecutorTransform;
		if (executorTransform == null)
			return EBTNodeState.FAILURE;

		IInteractor interactor = executorTransform.GetComponent<IInteractor>();
		if (interactor == null)
			return EBTNodeState.FAILURE;

		Transform targetTransform = context.TargetTransform;
		if (targetTransform == null)
			return EBTNodeState.FAILURE;

		// Snap into calculated formation position
		InteractionPoint reservedPoint = context.ReservedInteractionPoint;
		if (reservedPoint.TryGetInteractionPosition(interactor, out Vector3 formationWorldPos))
		{
			interactor.BeginCarrying(reservedPoint.transform, reservedPoint.transform.InverseTransformPoint(formationWorldPos));
		}

		return EBTNodeState.RUNNING;
	}

	protected override void OnFirstEvaluate(ref AIContext context) { }

	protected override void OnNodeExited(ref AIContext context) { }

	protected override void OnNodeReset(ref AIContext context)
	{
		OnNodeExited(ref context);
	}
}