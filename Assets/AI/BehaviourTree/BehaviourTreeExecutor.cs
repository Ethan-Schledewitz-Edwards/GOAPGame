using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

public class BehaviourTreeExecutor : MonoBehaviour
{
	[Header("Parameters")]
	private float m_interactionDist = 3.0f;

	// System
	public AIContext AIContext { get; private set; }
	public BehaviourTree CurrentBehaviourTree { get; private set; } = null;

	private void Awake()
	{
		AIContext = new AIContext();
		AIContext.SetData<Transform>("ExecutorTransform", transform);
		AIContext.SetData<float>("InteractionDist", m_interactionDist);
		AIContext.SetData<int>("InteractionLayer", 1 << LayerMask.NameToLayer("Interaction"));
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
}
