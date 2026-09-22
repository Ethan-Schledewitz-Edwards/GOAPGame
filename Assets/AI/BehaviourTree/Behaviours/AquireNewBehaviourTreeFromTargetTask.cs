using BehaviourTrees;
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
	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		if (executorTransform == null)
		{
			Debug.LogWarning("[AquireTask] Failed: executorTransform is null.");
			return EBTNodeState.STATE_FAILURE;
		}

		IInteractor interactor = executorTransform.GetComponent<IInteractor>();
		if (interactor == null)
		{
			Debug.LogWarning("[AquireTask] Failed: interactor is null.");
			return EBTNodeState.STATE_FAILURE;
		}

		Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);
		if (targetTransform == null)
		{
			Debug.LogWarning("[AquireTask] Failed: targetTransform is null.");
			return EBTNodeState.STATE_FAILURE;
		}

		InteractableObjectBase interactable =
			targetTransform.GetComponent<InteractableObjectBase>() ??
			targetTransform.GetComponentInParent<InteractableObjectBase>();

		if (interactable == null)
		{
			Debug.LogWarning($"[AquireTask] Failed: No InteractableObjectBase on '{targetTransform.name}'.");
			return EBTNodeState.STATE_FAILURE;
		}

		interactor.InteractWith(interactable, true);
		return EBTNodeState.STATE_SUCSESS;
	}

	protected override void OnFirstEvaluate(AIContext context) 
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		Debug.Log($"{executorTransform} is looking for a new behaviour tree.", executorTransform);
	}

	protected override void OnNodeExited(AIContext context) {}

	protected override void OnNodeReset(AIContext context) {}
}