using BehaviourTrees;
using UnityEngine;

public class CheckForTargetRangeTask : BTNodeBase
{
	protected override EBTNodeState OnUpdate(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		float interactionRange = context.GetData<float>(AIContextKeys.c_InteractionDistance);
		int interactionLayer = context.GetData<int>(AIContextKeys.c_InteractionLayer);

		Transform targetTransform = context.GetData<Transform>("TargetTransform");
		if (targetTransform == null)
		{
			return EBTNodeState.STATE_FAILURE;
		}

		Collider[] hitColliders = Physics.OverlapSphere(
			executorTransform.position,
			interactionRange,
			interactionLayer,
			QueryTriggerInteraction.Collide);

		foreach (Collider i in hitColliders)
		{
			if (i.transform == targetTransform)
			{
				return EBTNodeState.STATE_SUCSESS;
			}
		}

		return EBTNodeState.STATE_RUNNING;
	}

	protected override void OnFirstEvaluate(AIContext context) 
	{
		Transform targetTransform = context.GetData<Transform>("TargetTransform");
		Debug.Log($"Checking if the target: {targetTransform} is in range.");
	}
}
