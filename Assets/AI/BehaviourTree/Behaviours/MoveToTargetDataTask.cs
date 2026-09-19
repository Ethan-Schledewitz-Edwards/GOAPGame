using BehaviourTrees;
using UnityEngine;

public class MoveToTargetDataTask : BTNodeBase
{
	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		Vector3 targetDestination = context.GetData<Vector3>(AIContextKeys.c_TargetDestination);

		if (executorTransform == null || !executorTransform.TryGetComponent(out AIPathing pathing))
			return EBTNodeState.STATE_FAILURE;

		float arrivalDistance = Mathf.Max(pathing.StoppingDistance, 0.05f);
		if (pathing.IsWithinDistance(targetDestination, arrivalDistance))
		{
			return EBTNodeState.STATE_SUCSESS;
		}

		// If navigation has definitively failed, don't keep the behaviour tree
		if (pathing.IsDestinationInvalid())
			return EBTNodeState.STATE_FAILURE;

		pathing.SetDestination(targetDestination);
		return EBTNodeState.STATE_RUNNING;
	}

	protected override void OnFirstEvaluate(AIContext context)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		Vector3 targetDestination = context.GetData<Vector3>(AIContextKeys.c_TargetDestination);

		if (executorTransform != null &&
			executorTransform.TryGetComponent(out AIPathing pathing))
		{
			pathing.SetDestination(targetDestination);
		}
	}

	protected override void OnNodeExited(AIContext context) { }

	protected override void OnNodeReset(AIContext context) { }
}
