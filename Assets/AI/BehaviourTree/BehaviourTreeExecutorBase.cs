using AI.AIContext;
using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

public class BehaviourTreeExecutorBase : MonoBehaviour
{
	public AIContext AIContext;
	public BehaviourTree CurrentBehaviourTree { get; private set; } = null;

	protected virtual void Awake()
	{
		AIContext = new AIContext();
	}

	private void Start()
	{
		ResetContext();
	}

	public void SetCurrentBehaviourTree(BehaviourTree behaviourTree)
	{
		CurrentBehaviourTree = behaviourTree;
	}

	public EBTNodeState TickBehaviour(float t)
	{
		if (CurrentBehaviourTree != null)
		{
			EBTNodeState nodeState = CurrentBehaviourTree.TickBehaviourTree(ref AIContext, t);
			return nodeState;
		}
		return EBTNodeState.FAILURE;
	}

	public virtual void ResetContext()
	{
		System.Action cleanupAction = AIContext.ReservationCleanup;
		cleanupAction?.Invoke();

		AIContext.ClearAllData();
	}
}
