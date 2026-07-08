using BehaviourTrees;
using UnityEngine;

public class FindUseForItemTask : BTNodeBase
{
	protected override EBTNodeState OnUpdate(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		Vector3 executorPos = executorTransform.position;

		Settlement closestSettlement = SettlementManager.GetClosestSettlement(executorPos, true, true);
		if (closestSettlement != null)
		{
			GameObject closestBlueprint = closestSettlement.FindBlueprint(executorPos);
			if (closestBlueprint != null &&
				closestBlueprint.TryGetComponent(out InteractableObjectBase interactable))
			{
				context.SetData<Transform>(AIContextKeys.c_TargetTransform, closestBlueprint.transform);
				context.SetData<Vector3>("TargetPosition", interactable.GetInteractionPositon());

				return EBTNodeState.STATE_SUCSESS;
			}

			InteractableObjectBase closestStorage = closestSettlement.FindItemStorage(executorPos);
			if (closestStorage != null)
			{
				context.SetData<Transform>(AIContextKeys.c_TargetTransform, closestStorage.transform);
				context.SetData<Vector3>("TargetPosition", closestStorage.GetInteractionPositon());

				return EBTNodeState.STATE_SUCSESS;
			}
		}

		return EBTNodeState.STATE_RUNNING;
	}

	protected override void OnFirstEvaluate(AIContext context)
	{
		Debug.Log("Trying to find a resource deposit.");
	}
}
