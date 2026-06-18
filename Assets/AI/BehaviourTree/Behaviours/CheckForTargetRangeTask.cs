using BehaviourTrees;
using UnityEngine;

public class CheckForTargetRangeTask : BTNodeBase
{
	protected override EBTNodeState OnUpdate(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>("ExecutorTransform");
		Transform targetTransform = context.GetData<Transform>("TargetTransform");
		float interactionRange = context.GetData<float>("InteractionDist");
		int interactionLayer = context.GetData<int>("InteractionLayer");

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
				Debug.Log("AT TARGET: " + executorTransform.name);
				return EBTNodeState.STATE_SUCSESS;
			}
		}

		return EBTNodeState.STATE_FAILURE;
	}

	protected override void OnFirstEvaluate(AIContext context) { }
}
