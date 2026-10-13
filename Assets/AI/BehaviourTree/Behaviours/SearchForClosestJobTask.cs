using AI.AIContext;
using BehaviourTrees;
using Interaction.Core;
using UnityEngine;

/// <summary>
/// A behavior tree node that searches for the nearest available interactable object, 
/// reserves its closest interaction position, and sets it as the target destination.
/// </summary>
/// <remarks>
/// This node should be decorated with a <see cref="BTTimeoutNode"/> 
/// and must be followed by a <see cref="ReserveInteractionPositionTask"/> node to reserve the target.
/// </remarks>
public class SearchForClosestJobTask : BTNodeBase
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
		ActorInteractableBase closestInteractable = SearchForTask(interactor, executorPosition, context);
		if (closestInteractable != null)
		{
			Debug.Log($"[SearchForClosestJobTask]: {executorTransform} found an object to interact with", executorTransform);
			context.TargetTransform = closestInteractable.transform;

			return EBTNodeState.SUCSESS;
		}

		return EBTNodeState.RUNNING;
	}

	protected override void OnFirstEvaluate(ref AIContext context) { }

	protected override void OnNodeExited(ref AIContext context) { }

	protected override void OnNodeReset(ref AIContext context) { }

	/// <summary>
	/// Searches for an actor interactable object within a radius.
	/// </summary>
	private ActorInteractableBase SearchForTask(IInteractor interactor, Vector3 executorPosition, AIContext context)
	{
		ActorInteractableBase closestTask = null;

		float interactionRadius = context.JobSearchDistance;
		int interactionLayers = context.InteractionLayer;

		Collider[] hitColliders = Physics.OverlapSphere
		(
			executorPosition, 
			interactionRadius, 
			interactionLayers, 
			QueryTriggerInteraction.Collide
		);

		float closestDist = Mathf.Infinity;
		foreach (Collider i in hitColliders)
		{
			if (i == null)
				continue;

			if (i.TryGetComponent(out ActorInteractableBase aio))
			{
				if (!aio.HasWorkForInteractor(interactor))
					continue;

				float dist = Vector3.Distance(executorPosition, aio.transform.position);
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
