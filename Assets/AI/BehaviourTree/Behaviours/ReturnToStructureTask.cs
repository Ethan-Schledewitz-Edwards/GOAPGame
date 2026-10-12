using AI.AIContext;
using BehaviourTrees;
using Interaction.Core;
using Settlements;
using UnityEngine;

public class ReturnToStructureTask : BTNodeBase
{
	protected override EBTNodeState OnNodeEvaluated(ref AIContext context, float t)
	{
		Transform executorTransform = context.ExecutorTransform;
		if (executorTransform == null)
			return EBTNodeState.FAILURE;

		IInteractor interactor = executorTransform.GetComponent<IInteractor>();
		if (interactor == null)
			return EBTNodeState.FAILURE;

		Vector3 executorPosition = executorTransform.position;

		int settlementID = context.StructureSettlementID;
		int structureID = context.StructureID;

		if (SettlementManager.s_WorldSettlements.TryGetValue(settlementID, out Settlement settlement))
		{
			IStructure structure = settlement.SettlementStructures[structureID];

			if (structure != null && structure.Object != null)
			{
				GameObject structureObject = structure.Object;
				if (structureObject.TryGetComponent(out ActorInteractableBase interactable))
				{
					// Try to reserve the closest position on the structure's interactable component
					if (interactable.TryReserveClosestPosition(interactor, executorPosition, out InteractionPoint assignedPosition))
					{
						if (assignedPosition != null && assignedPosition.TryGetInteractionPosition(interactor, out Vector3 validDestination))
						{
							Debug.Log($"An Actor set their target to StructureID:{structureID} in SettlementID:{settlementID}.");
							context.TargetTransform = structureObject.transform;
							context.TargetDestination = validDestination;

							// Store the reserved position in the AI context so interaction nodes can retrieve it
							context.AssignedInteractionPosition = assignedPosition;

							// Cleanup delegate in case the behavior tree aborts
							System.Action cleanup = () =>
							{
								if (interactable != null && interactor != null && assignedPosition != null)
								{
									interactable.CancelReservation(interactor, assignedPosition);
								}
							};
							context.ReservationCleanup = cleanup;

							return EBTNodeState.SUCSESS;
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

		return EBTNodeState.FAILURE;
	}

	protected override void OnFirstEvaluate(ref AIContext context) { }

	protected override void OnNodeExited(ref AIContext context) { }

	protected override void OnNodeReset(ref AIContext context) { }
}