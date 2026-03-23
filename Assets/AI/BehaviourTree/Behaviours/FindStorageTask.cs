using BehaviourTrees;
using UnityEngine;

public class FindStorageTask : BTNodeBase
{
	private Actor m_actorComponent;
	private int m_settlementID;
	private int m_itemID;

	private bool m_isStorageAvailable;

	/// <summary>
	/// Creates a task which is used to confirm a friendly storage can be set as a "target" 
	/// </summary>
	/// <param name="actorComponent">The target actor</param>
	/// <param name="itemID">The item identifier used to find a friendly container</param>
	public FindStorageTask(Actor actorComponent, int itemID)
	{
		m_actorComponent = actorComponent;
		m_settlementID = actorComponent.SettlementID;	
		m_itemID = itemID;
	}

	public override EBTNodeState Evaluate()
	{
		base.Evaluate();

		SettlementManager settlementManager = SettlementManager.Instance;
		ItemStorageAIO closestStorage = null;

		if (settlementManager.WorldSettlements.Count > 0)
		{
			// Find closest friendly settlement
			closestStorage = settlementManager.WorldSettlements[m_settlementID].TryFindResourceStorage(m_itemID);
		}
		else
		{
			// Fail if there are no settlements to bring the item to
			m_nodeState = EBTNodeState.STATE_FAILURE;
			return m_nodeState;
		}

		if (closestStorage != null)
		{
			Debug.Log($"{m_actorComponent.name} found storage: {closestStorage.name}");

			SetData("target", closestStorage);
			m_nodeState = EBTNodeState.STATE_SUCSESS;
			return m_nodeState;
		}
		else
		{
			// Fail if no friendly storage was found
			m_nodeState = EBTNodeState.STATE_FAILURE;
			return m_nodeState;
		}
	}

	protected override void OnFirstEvaluate()
	{
		base.OnFirstEvaluate();

		Debug.Log("Begin storage search");
	}
}
