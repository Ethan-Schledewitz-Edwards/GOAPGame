using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

public class BehaviourTreeExecutorBase : MonoBehaviour
{
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

	public EBTNodeState TickBehaviour(float t)
	{
		if (CurrentBehaviourTree != null && AIContext != null)
		{
			return CurrentBehaviourTree.TickBehaviourTree(AIContext, t);
		}

		return EBTNodeState.STATE_FAILURE;
	}

	public virtual void ResetContext()
	{
		AIContext.ClearAllData();
	}
}
