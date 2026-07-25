using BehaviourTrees;
using UnityEngine;
using UnityEngine.AI;

public class MoveToTargetDataTask : BTNodeBase
{
	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		Vector3 targetDestination = context.GetData<Vector3>(AIContextKeys.c_TargetDestination);

		if (executorTransform.TryGetComponent(out AIPathing pathing))
		{
			bool hasArrived = pathing.PathDistRemaining() < pathing.StoppingDistance;

			if (hasArrived && !pathing.IsMoving)
				return EBTNodeState.STATE_SUCSESS;

			// We are still walking.
			pathing.SetDestination(targetDestination);
			return EBTNodeState.STATE_RUNNING;
		}

		return EBTNodeState.STATE_FAILURE;
	}

	protected override void OnFirstEvaluate(AIContext context)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		Vector3 targetDestination = context.GetData<Vector3>(AIContextKeys.c_TargetDestination);

		if (executorTransform.TryGetComponent(out AIPathing pathing))
		{
			pathing.SetDestination(targetDestination);
			Debug.Log(targetDestination);
			Debug.Log("OKAY THIS IS NOT WORKING");
		}
	}

	protected override void OnNodeExited(AIContext context) { }

	protected override void OnNodeReset(AIContext context) { }
}
