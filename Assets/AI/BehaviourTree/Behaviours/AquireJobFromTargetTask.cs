using BehaviourTrees;
using UnityEngine;

public class AquireJobFromTargetTask : BTNodeBase
{
	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		IInteractor interactor = executorTransform.GetComponent<IInteractor>();
		Vector3 executorPosition = executorTransform.position;

		// Try to interact with the target
		Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);
		if (targetTransform != null &&
			targetTransform.TryGetComponent(out InteractableObjectBase iob))
		{
			if(iob.TryInteract(interactor, true))
				return EBTNodeState.STATE_SUCSESS;
		}

		return EBTNodeState.STATE_RUNNING;
	}

	protected override void OnFirstEvaluate(AIContext context){}

	protected override void OnNodeExited(AIContext context) {}

	protected override void OnNodeReset(AIContext context) {}
}