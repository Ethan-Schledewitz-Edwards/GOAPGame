using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

public class BehaviourTreeExecutor : MonoBehaviour
{
	[Header("Parameters")]
	public float InteractionDist { get; private set; } = 3.0f;

	// System
	public AIContext AIContext { get; private set; } = new AIContext();
	public BehaviourTree CurrentBehaviourTree { get; private set; } = null;

	public void SetCurrentBehaviourTree(BehaviourTree behaviourTree)
	{
		if (CurrentBehaviourTree != null)
			CurrentBehaviourTree = behaviourTree;
	}

	public void TickBehaviour(float t)
	{
		if (CurrentBehaviourTree != null && AIContext != null)
		{
			CurrentBehaviourTree.TickBehaviourTree(AIContext, t);
		}
	}

	public struct BehaviourTreeDefinition
	{
		public string TreeName;
		public BehaviourTree Tree;
	}
}
