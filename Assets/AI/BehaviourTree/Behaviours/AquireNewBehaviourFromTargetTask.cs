using BehaviourTrees;
using UnityEngine;

/// <summary>
/// A behavior tree node that attempts to update the 
/// executors current behaviour tree to the one defined by their target.
/// </summary>
/// <remarks>
/// This node should always be decorated with a timeout node.
/// </remarks>
public class AquireNewBehaviourFromTargetTask : BTNodeBase
{
	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		if (executorTransform == null)
			return EBTNodeState.STATE_FAILURE;

		IInteractor interactor = executorTransform.GetComponent<IInteractor>();
		if (interactor == null)
			return EBTNodeState.STATE_FAILURE;

		Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);
		if (targetTransform == null)
			return EBTNodeState.STATE_FAILURE;

		InteractableObjectBase iob = targetTransform.GetComponent<InteractableObjectBase>()
								  ?? targetTransform.GetComponentInParent<InteractableObjectBase>();

		if (iob == null)
			return EBTNodeState.STATE_FAILURE;

		interactor.InteractWith(iob, true);
		return EBTNodeState.STATE_SUCSESS;
	}

	protected override void OnFirstEvaluate(AIContext context){}

	protected override void OnNodeExited(AIContext context) {}

	protected override void OnNodeReset(AIContext context) {}
}