using BehaviourTrees;
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
	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		if (executorTransform == null)
			return EBTNodeState.STATE_FAILURE;

		IInteractor interactor = executorTransform.GetComponent<IInteractor>();
		if (interactor == null)
			return EBTNodeState.STATE_FAILURE;

		Vector3 executorPosition = executorTransform.position;
		InteractableObjectBase closestInteractable = SearchForTask(interactor, executorPosition, context);
		if (closestInteractable != null)
		{
			Debug.Log($"[SearchForClosestJobTask]: {executorTransform} found an object to interact with", executorTransform);
			context.SetData<Transform>(AIContextKeys.c_TargetTransform, closestInteractable.transform);

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
	private InteractableObjectBase SearchForTask(IInteractor interactor, Vector3 executorPosition, AIContext context)
	{
		InteractableObjectBase closestTask = null;

		float interactionRadius = context.GetData<float>(AIContextKeys.c_JobSearchRange, 1.5f);
		int interactionLayers = context.GetData<int>(AIContextKeys.c_InteractionLayer);

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

			if (i.TryGetComponent(out InteractableObjectBase aio))
			{
				if (!aio.HasAvailableWork(interactor))
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
