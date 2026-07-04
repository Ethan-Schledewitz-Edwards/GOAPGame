using BehaviourTrees;
using UnityEngine;

public class InteractWithTargetTask : BTNodeBase
{
	protected override EBTNodeState OnUpdate(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		IInteractor interactor = executorTransform.GetComponent<IInteractor>();
		Vector3 executorPosition = executorTransform.position;

		Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);
		Vector3 targetPosition = context.GetData<Vector3>("TargetPosition");

		// Try to interact with the target
		if (targetTransform != null &&
			targetTransform.TryGetComponent(out InteractableObjectBase iob))
		{
			if (iob.TryInteract(interactor))
				return EBTNodeState.STATE_SUCSESS;
			else return EBTNodeState.STATE_FAILURE;
		}

		return EBTNodeState.STATE_RUNNING;
	}

	protected override void OnFirstEvaluate(AIContext context){}
}
