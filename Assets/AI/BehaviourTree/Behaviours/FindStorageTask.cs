using BehaviourTrees;
using UnityEngine;

public class FindStorageTask : BTNodeBase
{
	private bool m_isStorageAvailable;

	public override EBTNodeState Evaluate(AIContext aiContext, float t)
	{
		base.Evaluate(aiContext,t);

		SettlementManager settlementManager = SettlementManager.Instance;
		Transform executorTransform = aiContext.GetData<Transform>("ExecutorTransform");
		int settlementID = aiContext.GetData<int>("SettlementID");
		int heldItemID = aiContext.GetData<int>("HeldItemID");

		ActorInteractableObjectBase closestStorage = null;
		if (settlementManager.WorldSettlements.Count > 0)
		{
			// Find closest friendly settlement
			closestStorage = settlementManager.WorldSettlements[settlementID].TryFindResourceStorage();
		}
		else
		{
			// Fail if there are no settlements to bring the item to
			m_nodeState = EBTNodeState.STATE_FAILURE;
			return m_nodeState;
		}

		if (closestStorage != null)
		{
			Debug.Log($"{executorTransform.gameObject.name} found storage: {closestStorage.name}");

			aiContext.SetData<Transform>("TargetTransform", closestStorage.transform);
			aiContext.SetData<Vector3>("TargetPosition", closestStorage.GetInteractionPositon());

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
