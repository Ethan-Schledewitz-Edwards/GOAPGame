using BehaviourTrees;
using UnityEngine;

/// <summary>
/// A behavior tree node that searches for the nearest available interactable object, 
/// reserves its closest interaction position, and sets it as the target destination.
/// </summary>
/// <remarks>
/// This node should always be decorated with a timeout node.
/// </remarks>
public class SearchForClosestJobTask : BTNodeBase
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

		InteractableObjectBase closestInteractable = SearchForTask(executorPosition, context);
		if (closestInteractable != null)
		{
			InteractionPosition assignedPosition = null;
			Vector3 validDestination = Vector3.zero;

			// Check if this interactable requires reservations
			if (closestInteractable.RequiresReservation)
			{
				if (closestInteractable.TryReserveClosestPosition(interactor, executorPosition, out assignedPosition))
				{
					if (assignedPosition != null)
						assignedPosition.TryGetInteractionPosition(interactor, out validDestination);
				}
			}
			else
			{
				validDestination = closestInteractable.transform.position;
				if (closestInteractable.TryGetComponent(out InteractionPosition sharedPos))
				{
					assignedPosition = sharedPos;
					sharedPos.TryGetInteractionPosition(interactor, out validDestination);
				}
			}

			if (validDestination != Vector3.zero)
			{
				context.SetData<Transform>(AIContextKeys.c_TargetTransform, assignedPosition != null ? assignedPosition.transform : closestInteractable.transform);
				context.SetData<Vector3>(AIContextKeys.c_TargetDestination, validDestination);
				return EBTNodeState.STATE_SUCSESS;
			}
		}

		return EBTNodeState.STATE_RUNNING;
	}

	protected override void OnFirstEvaluate(AIContext context) { }

	protected override void OnNodeExited(AIContext context) { }

	protected override void OnNodeReset(AIContext context) { }

	/// <summary>
	/// Searches for an actor interactable object within a radius.
	/// </summary>
	private InteractableObjectBase SearchForTask(Vector3 executorPosition, AIContext context)
	{
		InteractableObjectBase closestTask = null;

		Vector3 pos = executorPosition;
		float interactionRadius = context.GetData<float>(AIContextKeys.c_InteractionDistance, 3.0f);
		int interactionLayers = context.GetData<int>(AIContextKeys.c_InteractionLayer);

		Collider[] hitColliders = Physics.OverlapSphere(pos, interactionRadius, interactionLayers, QueryTriggerInteraction.Collide);

		float closestDist = Mathf.Infinity;
		foreach (Collider i in hitColliders)
		{
			if (i == null)
				continue;

			if (i.TryGetComponent(out InteractableObjectBase aio))
			{
				// Skip targets that are already at capacity
				if (aio.IsAtActorCapacity())
					continue;

				float dist = Vector3.Distance(pos, aio.transform.position);
				if (dist < closestDist)
				{
					closestTask = aio;
					closestDist = dist;
				}
			}
		}

		return closestTask;
	}
}
