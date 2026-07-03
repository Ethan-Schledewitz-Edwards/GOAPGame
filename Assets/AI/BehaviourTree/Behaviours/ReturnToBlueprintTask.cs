using BehaviourTrees;
using UnityEngine;

public class ReturnToBlueprintTask : BTNodeBase
{
	protected override EBTNodeState OnUpdate(AIContext context, float t)
	{
		Transform blueprint = context.GetData<Transform>("BlueprintTransform");
		if (blueprint != null)
		{
			context.SetData<Transform>("TargetTransform", blueprint);
			return EBTNodeState.STATE_SUCSESS;
		}
		return EBTNodeState.STATE_FAILURE;
	}
}