using BehaviourTrees;
using UnityEngine;

/// <summary>
/// A behavior tree node that searches for the nearest interactable object and attempts to interact with it.
/// </summary>
/// <remarks>
/// This node should always be decorated with a timeout node.
/// </remarks>
public class SearchForInteractionTask : BTNodeBase
{
	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		IInteractor interactor = executorTransform.GetComponent<IInteractor>();
		Vector3 executorPosition = executorTransform.position;

		InteractableObjectBase closestInteractable = SearchForTask(executorPosition, context);

		// Try to interact with the target
		if (closestInteractable != null)
		{
			if (closestInteractable.TryInteract(interactor, true))
				return EBTNodeState.STATE_SUCSESS;
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

			// Try to get interactable component
			if (i.TryGetComponent(out InteractableObjectBase aio))
			{
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
