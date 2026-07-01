using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

public class BehaviourTreeExecutor : MonoBehaviour
{
	private const float c_baseInteractionDistance = 0.8f;

	// System
	public AIContext AIContext { get; private set; }
	public BehaviourTree CurrentBehaviourTree { get; private set; } = null;

	private void Awake()
	{
		AIContext = new AIContext();
		ResetContext();
	}

	public void SetCurrentBehaviourTree(BehaviourTree behaviourTree)
	{
		CurrentBehaviourTree = behaviourTree;
	}

	public void TickBehaviour(float t)
	{
		if (CurrentBehaviourTree != null && AIContext != null)
		{
			CurrentBehaviourTree.TickBehaviourTree(AIContext, t);
		}
	}

	public void ResetContext()
	{
		AIContext.ClearAllData();
		AIContext.SetData<Transform>("ExecutorTransform", transform);
		AIContext.SetData<float>("InteractionDist", c_baseInteractionDistance);
		AIContext.SetData<int>("InteractionLayer", 1 << LayerMask.NameToLayer("Interaction"));
	}
}
