using AI.Context;
using BehaviourTrees;
using System;
using System.Collections.Generic;
using UnityEngine;

public class BehaviourTreeExecutorBase
{
	public event Action ContextReset;

	public AIContext AIContext;

	public BehaviourTree CurrentBehaviourTree { get; private set; } = null;

	protected Transform m_objectTransform;

	public BehaviourTreeExecutorBase(Transform objectTransform)
	{
		AIContext = new AIContext();
		m_objectTransform = objectTransform;
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
		ContextReset?.Invoke();
	}
}
