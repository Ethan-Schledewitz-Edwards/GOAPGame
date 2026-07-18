using BehaviourTrees;
using Settlements;
using UnityEngine;

public class ReturnToBlueprintTask : BTNodeBase
{
	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		int settlementID = context.GetData<int>(AIContextKeys.c_HomeSettlementID);
		int structureID = context.GetData<int>(AIContextKeys.c_StructureID);

		Settlement settlement = SettlementManager.s_WorldSettlements[settlementID];
		IStructure blueprintStructure = settlement.SettlementStructures[structureID];

		if (blueprintStructure != null && 
			blueprintStructure.StructureObject != null)
		{
			GameObject structureObject = blueprintStructure.StructureObject;
			if (structureObject.TryGetComponent(out InteractableObjectBase interactableObject))
			{
				Debug.Log($"An Actor set their target to StructureID:{structureID} in SettlementID:{settlementID}.");
				context.SetData<Transform>(AIContextKeys.c_TargetTransform, structureObject.transform);
				context.SetData<Vector3>(AIContextKeys.c_TargetDestination, interactableObject.GetInteractionPositon());

				return EBTNodeState.STATE_SUCSESS;
			}
		}

		return EBTNodeState.STATE_FAILURE;
	}

	protected override void OnFirstEvaluate(AIContext context) { }

	protected override void OnNodeExited(AIContext context) { }

	protected override void OnNodeReset(AIContext context) { }
}