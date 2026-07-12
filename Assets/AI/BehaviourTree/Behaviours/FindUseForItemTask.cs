using BehaviourTrees;
using Settlements;
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
			GameObject closestBlueprint = closestSettlement.FindNearestStructureOfType(executorPos, "Blueprint").StructureObject;
			if (closestBlueprint != null &&
				closestBlueprint.TryGetComponent(out InteractableObjectBase interactable))
			{
				context.SetData<Transform>(AIContextKeys.c_TargetTransform, closestBlueprint.transform);
				context.SetData<Vector3>(AIContextKeys.c_TargetDestination, interactable.GetInteractionPositon());

				return EBTNodeState.STATE_SUCSESS;
			}

			GameObject closestStorage = closestSettlement.FindNearestStructureOfType(executorPos, "Storage").StructureObject;
			if (closestStorage != null &&
				closestStorage.TryGetComponent(out InteractableObjectBase interactableObject))
			{
				context.SetData<Transform>(AIContextKeys.c_TargetTransform, closestStorage.transform);
				context.SetData<Vector3>(AIContextKeys.c_TargetDestination, interactableObject.GetInteractionPositon());

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
