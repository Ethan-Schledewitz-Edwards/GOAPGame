using BehaviourTrees;
using UnityEngine;

public class CheckForDestinationRangeTask : BTNodeBase
{
	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		float interactionRange = context.GetData<float>(AIContextKeys.c_InteractionDistance);

		Vector3 targetDestination = context.GetData<Vector3>(AIContextKeys.c_TargetDestination);

		// Check if the destination is within square range
		float distSqrt = (targetDestination - executorTransform.position).sqrMagnitude;
		if (distSqrt <= interactionRange * interactionRange)
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
