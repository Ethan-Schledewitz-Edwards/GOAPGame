using BehaviourTrees;
using UnityEngine;

public class CheckForDestinationRangeTask : BTNodeBase
{
	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		float interactionDistanceSqrt = context.GetData<float>(AIContextKeys.c_InteractionDistanceSqrt);
		Vector3 targetDestination = context.GetData<Vector3>(AIContextKeys.c_TargetDestination);

		Vector3 delta = targetDestination - executorTransform.position;
		delta.y = 0f;
		if (delta.sqrMagnitude <= interactionDistanceSqrt)
			return EBTNodeState.STATE_SUCSESS;

		return EBTNodeState.STATE_RUNNING;
	}

	protected override void OnFirstEvaluate(AIContext context) 
	{
		Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);
		Debug.Log($"Checking if the target: {targetTransform.name} is in range.");
	}

	protected override void OnNodeExited(AIContext context) { }

	protected override void OnNodeReset(AIContext context) { }
}
