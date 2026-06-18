using BehaviourTrees;
using UnityEngine;
using UnityEngine.AI;

public class MoveToTargetDataTask : BTNodeBase
{
	private const float c_targetingRangeSqrt = 0.3f * 0.3f;

	protected override EBTNodeState OnUpdate(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>("ExecutorTransform");
		Vector3 targetPos = context.GetData<Vector3>("TargetPosition");

		Transform targetTransform = context.GetData<Transform>("TargetTransform");
		if (targetTransform == null)
			return EBTNodeState.STATE_FAILURE;

		if (executorTransform.TryGetComponent(out AIPathing pathing))
		{
			Vector3 offset = targetPos - executorTransform.position;

			if (offset.sqrMagnitude <= c_targetingRangeSqrt)
				return EBTNodeState.STATE_SUCSESS;

			// We are still walking.
			return EBTNodeState.STATE_RUNNING;
		}

		return EBTNodeState.STATE_FAILURE;
	}

	protected override void OnFirstEvaluate(AIContext context)
	{
		Transform executorTransform = context.GetData<Transform>("ExecutorTransform");
		Vector3 targetPos = context.GetData<Vector3>("TargetPosition");

		if (executorTransform.TryGetComponent(out AIPathing pathing))
		{
			pathing.SetDestination(targetPos);
		}
	}
}
