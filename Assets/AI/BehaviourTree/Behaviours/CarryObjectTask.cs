using BehaviourTrees;
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
	private const string c_IsCarryingKey = "IsCarryingState";

	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		if (executorTransform == null)
			return EBTNodeState.STATE_FAILURE;

		IInteractor interactor = executorTransform.GetComponent<IInteractor>();
		if (interactor == null)
			return EBTNodeState.STATE_FAILURE;

		Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);
		if (targetTransform == null)
			return EBTNodeState.STATE_FAILURE;

		ActorInteractableBase interactable = targetTransform.GetComponentInParent<ActorInteractableBase>();
		InteractionPoint assignedPos = context.GetData<InteractionPoint>(AIContextKeys.c_AssignedInteractionPosition);

		if (context.GetData<bool>(c_IsCarryingKey, false) == false)
		{
			context.SetData(c_IsCarryingKey, true);

			// Snap into calculated formation position
			if (assignedPos.TryGetInteractionPosition(interactor, out Vector3 formationWorldPos))
			{
				interactor.BeginCarrying(assignedPos.transform, assignedPos.transform.InverseTransformPoint(formationWorldPos));
			}
		}

		return EBTNodeState.STATE_RUNNING;
	}

	protected override void OnFirstEvaluate(AIContext context) { }

	protected override void OnNodeExited(AIContext context)
	{
		if (context.GetData<bool>(c_IsCarryingKey, false) != false)
		{
			context.ClearData(c_IsCarryingKey);

			Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
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
				InteractionPoint assignedPos = context.GetData<InteractionPoint>(AIContextKeys.c_AssignedInteractionPosition);
				Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);

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

	protected override void OnNodeReset(AIContext context)
	{
		OnNodeExited(context);
	}
}