using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

public class BehaviourTreeExecutorBase : MonoBehaviour
{
	public AIContext AIContext { get; private set; }
	public BehaviourTree CurrentBehaviourTree { get; private set; } = null;

	[field: SerializeField] public string CurrentNodeNameDebug { get; private set; } = "None";


	protected virtual void Awake()
	{
		AIContext = new AIContext();
		ResetContext();
	}

	public void SetCurrentBehaviourTree(BehaviourTree behaviourTree)
	{
		// Clear the debug name
		if (behaviourTree == null)
		{
			AIContext.SetData<string>(AIContextKeys.c_CurrentBTNode, "None");
			CurrentNodeNameDebug = AIContext.GetData<string>(AIContextKeys.c_CurrentBTNode, "None");
		}

		CurrentBehaviourTree = behaviourTree;
	}

	public EBTNodeState TickBehaviour(float t)
	{
		if (CurrentBehaviourTree != null && AIContext != null)
		{
			EBTNodeState nodeState = CurrentBehaviourTree.TickBehaviourTree(AIContext, t);
			CurrentNodeNameDebug = AIContext.GetData<string>(AIContextKeys.c_CurrentBTNode, "None");
			return nodeState;
		}

		CurrentNodeNameDebug = "None";
		return EBTNodeState.STATE_FAILURE;
	}

	public virtual void ResetContext()
	{
		AIContext.ClearAllData();
	}
}
