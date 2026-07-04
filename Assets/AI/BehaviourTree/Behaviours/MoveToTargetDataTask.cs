using BehaviourTrees;
using UnityEngine;
using UnityEngine.AI;

public class MoveToTargetDataTask : BTNodeBase
{

	protected override EBTNodeState OnUpdate(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		Vector3 targetPos = context.GetData<Vector3>("TargetPosition");

		Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);
		if (targetTransform == null)
			return EBTNodeState.STATE_FAILURE;

		if (executorTransform.TryGetComponent(out AIPathing pathing))
		{
			bool hasArrived = pathing.PathDistRemaining() < pathing.StoppingDistance;

			if (hasArrived && !pathing.IsMoving)
				return EBTNodeState.STATE_SUCSESS;

			// We are still walking.
			return EBTNodeState.STATE_RUNNING;
		}

		return EBTNodeState.STATE_FAILURE;
	}

	protected override void OnFirstEvaluate(AIContext context)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		Vector3 targetPos = context.GetData<Vector3>("TargetPosition");

		if (executorTransform.TryGetComponent(out AIPathing pathing))
		{
			pathing.SetDestination(targetPos);
		}
	}
}
