using AI.AIContext;
using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

public class BehaviourTreeExecutorBase : MonoBehaviour
{
	public AIContext AIContext;
	public BehaviourTree CurrentBehaviourTree { get; private set; } = null;

	[field: SerializeField] public string CurrentNodeNameDebug { get; private set; } = "None";

	protected virtual void Awake()
	{
		AIContext = new AIContext();
		ResetContext();
	}

	public void SetCurrentBehaviourTree(BehaviourTree behaviourTree)
	{
		if (behaviourTree == null)
		{
			AIContext.CurrentNodeID = "None";
			CurrentNodeNameDebug = "None";
		}

		CurrentBehaviourTree = behaviourTree;
	}

	public EBTNodeState TickBehaviour(float t)
	{
		if (CurrentBehaviourTree != null)
		{
			EBTNodeState nodeState = CurrentBehaviourTree.TickBehaviourTree(ref AIContext, t);
			CurrentNodeNameDebug = AIContext.CurrentNodeID;
			return nodeState;
		}

		CurrentNodeNameDebug = "None";
		return EBTNodeState.FAILURE;
	}

	public virtual void ResetContext()
	{
		System.Action cleanupAction = AIContext.ReservationCleanup;
		cleanupAction?.Invoke();

		AIContext.ClearAllData();
	}
}
