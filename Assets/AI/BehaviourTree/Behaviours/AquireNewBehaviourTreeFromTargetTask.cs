using AI.AIContext;
using BehaviourTrees;
using Interaction.Core;
using UnityEngine;

/// <summary>
/// A behavior tree node that attempts to update the 
/// executors current behaviour tree to the one defined by their target.
/// </summary>
/// <remarks>
/// This node should be decorated with a <see cref="BTTimeoutNode"/>
/// </remarks>
public class AquireNewBehaviourTreeFromTargetTask : BTNodeBase
{
	protected override EBTNodeState OnNodeEvaluated(ref AIContext context, float t)
	{
		Transform executorTransform = context.ExecutorTransform;
		if (executorTransform == null)
		{
			Debug.LogWarning("[AquireNewBehaviourTreeFromTargetTask] Failed: " +
				"executorTransform is null.");
			return EBTNodeState.FAILURE;
		}

		IInteractor interactor = executorTransform.GetComponent<IInteractor>();
		if (interactor == null)
		{
			Debug.LogWarning("[AquireNewBehaviourTreeFromTargetTask] Failed: " +
				"interactor is null.");
			return EBTNodeState.FAILURE;
		}

		Transform targetTransform = context.TargetTransform;
		if (targetTransform == null)
		{
			Debug.LogWarning("[AquireNewBehaviourTreeFromTargetTask] Failed: " +
				"targetTransform is null.");
			return EBTNodeState.FAILURE;
		}

		ActorInteractableBase interactable =
			targetTransform.GetComponent<ActorInteractableBase>() ??
			targetTransform.GetComponentInParent<ActorInteractableBase>();

		if (interactable == null)
		{
			Debug.LogWarning($"[AquireNewBehaviourTreeFromTargetTask] Failed: " +
				$"No InteractableObjectBase on '{targetTransform.name}'.");
			return EBTNodeState.FAILURE;
		}

		interactor.InteractWith(interactable, true);
		return EBTNodeState.SUCSESS;
	}

	protected override void OnFirstEvaluate(ref AIContext context) 
	{
		Transform executorTransform = context.ExecutorTransform;
		Debug.Log($"[AquireNewBehaviourTreeFromTargetTask]: " +
			$"{executorTransform} is looking for a new behaviour tree.", executorTransform);
	}

	protected override void OnNodeExited(ref AIContext context) {}

	protected override void OnNodeReset(ref AIContext context) {}
}