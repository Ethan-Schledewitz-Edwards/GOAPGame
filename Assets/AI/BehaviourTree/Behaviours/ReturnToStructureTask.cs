using BehaviourTrees;
using Settlements;
using UnityEngine;

public class ReturnToStructureTask : BTNodeBase
{
	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		if (executorTransform == null)
			return EBTNodeState.STATE_FAILURE;

		IInteractor interactor = executorTransform.GetComponent<IInteractor>();
		if (interactor == null)
			return EBTNodeState.STATE_FAILURE;

		Vector3 executorPosition = executorTransform.position;

		int settlementID = context.GetData<int>(AIContextKeys.c_StructureSettlementID);
		int structureID = context.GetData<int>(AIContextKeys.c_StructureID);

		if (SettlementManager.s_WorldSettlements.TryGetValue(settlementID, out Settlement settlement))
		{
			IStructure structure = settlement.SettlementStructures[structureID];

			if (structure != null && structure.Object != null)
			{
				GameObject structureObject = structure.Object;
				if (structureObject.TryGetComponent(out InteractableObjectBase interactableObject))
				{
					// Try to reserve the closest position on the structure's interactable component
					if (interactableObject.TryReserveClosestPosition(interactor, executorPosition, out InteractionPosition assignedPosition))
					{
						if (assignedPosition != null && assignedPosition.TryGetInteractionPosition(interactor, out Vector3 validDestination))
						{
							Debug.Log($"An Actor set their target to StructureID:{structureID} in SettlementID:{settlementID}.");
							context.SetData<Transform>(AIContextKeys.c_TargetTransform, assignedPosition.transform);
							context.SetData<Vector3>(AIContextKeys.c_TargetDestination, validDestination);

							return EBTNodeState.STATE_SUCSESS;
						}
					}
				}
			}
			else
			{
				Debug.LogWarning($"No structure with an ID of:{structureID} was found in settlement:{settlementID}.");
			}
		}
		else
		{
			Debug.LogWarning($"No settlement with an ID of:{settlementID} was found.");
		}

		return EBTNodeState.STATE_FAILURE;
	}

	protected override void OnFirstEvaluate(AIContext context) { }

	protected override void OnNodeExited(AIContext context) { }

	protected override void OnNodeReset(AIContext context) { }
}