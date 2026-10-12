using AI.AIContext;
using BehaviourTrees;
using UnityEngine;

public class CheckForDestinationRangeTask : BTNodeBase
{
	protected override EBTNodeState OnNodeEvaluated(ref AIContext context, float t)
	{
		Transform executorTransform = context.ExecutorTransform;
		if(executorTransform == null)
			return EBTNodeState.FAILURE;

		float stoppingDistance = context.StoppingDistance;
		Vector3 targetDestination = context.TargetDestination;

		Vector3 delta = targetDestination - executorTransform.position;
		delta.y = 0f;
		if (delta.sqrMagnitude <= stoppingDistance *stoppingDistance)
			return EBTNodeState.SUCSESS;

		return EBTNodeState.RUNNING;
	}

	protected override void OnFirstEvaluate(ref AIContext context) 
	{
		Debug.Log($"Checking if the target: {context.TargetTransform} is in range.");
	}

	protected override void OnNodeExited(ref AIContext context) { }

	protected override void OnNodeReset(ref AIContext context) { }
}
