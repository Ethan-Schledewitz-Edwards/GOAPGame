using AI.AIContext;
using BehaviourTrees;
using Interaction.Core;
using UnityEngine;

/// <summary>
/// A behavior tree node that attempts to perform a standard interaction with a target object without taking a job.
/// </summary>
/// <remarks>
/// This node should always be decorated with a timeout node.
/// </remarks>
public class InteractWithTargetTask : BTNodeBase
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

		ActorInteractableBase iob = targetTransform.GetComponent<ActorInteractableBase>()
								  ?? targetTransform.GetComponentInParent<ActorInteractableBase>();

		if (iob == null)
			return EBTNodeState.FAILURE;

		interactor.UseInteractable(iob);
		return EBTNodeState.SUCSESS;
	}

	protected override void OnFirstEvaluate(ref AIContext context) {}

	protected override void OnNodeExited(ref AIContext context) {}

	protected override void OnNodeReset(ref AIContext context) {}
}