using BehaviourTrees;
using UnityEngine;

public class FindStorageTask : BTNodeBase
{
	private Actor m_actorComponent;
	private int m_settlementID;
	private int m_itemID;

	private bool m_isStorageAvailable;

	public FindStorageTask(Actor actor, int itemID)
	{
		m_actorComponent = actor;
		m_settlementID = actor.SettlementID;	
		m_itemID = itemID;
	}

	public override EBTNodeState Evaluate()
	{
		base.Evaluate();

		ItemStorageAIO closestStorage = SettlementManager.Instance.WorldSettlements[m_settlementID].TryFindResourceStorage(m_itemID);

		if (closestStorage)
		{
			
		}
		else
		{
			m_nodeState = EBTNodeState.STATE_FAILURE;
			return m_nodeState;
		}

		m_nodeState = EBTNodeState.STATE_RUNNING;
		return m_nodeState;
	}

	protected override void OnFirstEvaluate()
	{
		base.OnFirstEvaluate();

		Debug.Log("Begin storage search");
	}
}
