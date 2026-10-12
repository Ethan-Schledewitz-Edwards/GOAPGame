using AI.AIContext;
using BehaviourTrees;
using Interaction.Core;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A behavior tree node that attempts to parrent the interactor to the followPoint of a carryable object.
/// </summary>
/// <remarks>
/// This node should always be decorated with a timeout node.
/// </remarks>
public class CarryObjectTask : BTNodeBase
{
	protected override EBTNodeState OnNodeEvaluated(ref AIContext context, float t)
	{
		Transform executorTransform = context.ExecutorTransform;
		if (executorTransform == null)
			return EBTNodeState.FAILURE;

		IInteractor interactor = executorTransform.GetComponent<IInteractor>();
		if (interactor == null)
			return EBTNodeState.FAILURE;

		Transform targetTransform = context.TargetTransform;
		if (targetTransform == null)
			return EBTNodeState.FAILURE;

		ActorInteractableBase interactable = targetTransform.GetComponentInParent<ActorInteractableBase>();
		InteractionPoint assignedPos = context.AssignedInteractionPosition;

		if (context.IsCarryingObject == false)
		{
			context.IsCarryingObject = true;

			// Snap into calculated formation position
			if (assignedPos.TryGetInteractionPosition(interactor, out Vector3 formationWorldPos))
			{
				interactor.BeginCarrying(assignedPos.transform, assignedPos.transform.InverseTransformPoint(formationWorldPos));
			}
		}

		return EBTNodeState.RUNNING;
	}

	protected override void OnFirstEvaluate(ref AIContext context) { }

	protected override void OnNodeExited(ref AIContext context)
	{
		if (context.IsCarryingObject)
		{
			context.IsCarryingObject = false;

			Transform executorTransform = context.ExecutorTransform;
			if (executorTransform != null)
			{
				// Unparent the executor
				executorTransform.SetParent(null);

				// Re-enable generic unity components
				if (executorTransform.TryGetComponent(out NavMeshAgent agent)) 
					agent.enabled = true;
				if (executorTransform.TryGetComponent(out Rigidbody rb)) 
					rb.isKinematic = false;

				IInteractor interactor = executorTransform.GetComponent<IInteractor>();
				InteractionPoint assignedPos = context.AssignedInteractionPosition;
				Transform targetTransform = context.TargetTransform;

				if (interactor != null && assignedPos != null && targetTransform != null)
				{
					ActorInteractableBase interactable = targetTransform.GetComponentInParent<ActorInteractableBase>();
					if (interactable != null)
					{
						interactable.StopInteract(interactor, assignedPos);
					}
				}
			}
		}
	}

	protected override void OnNodeReset(ref AIContext context)
	{
		OnNodeExited(ref context);
	}
}