using BehaviourTrees;
using UnityEngine;

public class ReturnToBlueprintTask : BTNodeBase
{
	protected override EBTNodeState OnUpdate(AIContext context, float t)
	{
		int blueprintID = context.GetData<int>(AIContextKeys.c_BlueprintID);

		/*

		if (blueprint != null)
		{
			context.SetData<Transform>(AIContextKeys.c_TargetTransform, blueprint);
			return EBTNodeState.STATE_SUCSESS;
		}
		return EBTNodeState.STATE_FAILURE;
		*/

		return EBTNodeState.STATE_FAILURE;
	}
}