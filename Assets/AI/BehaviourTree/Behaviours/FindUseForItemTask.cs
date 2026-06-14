using BehaviourTrees;
using UnityEngine;

public class FindUseForItemTask : BTNodeBase
{
	private bool m_isStorageAvailable;

	public override EBTNodeState Evaluate(AIContext aiContext, float t)
	{
		base.Evaluate(aiContext,t);

		Transform executorTransform = aiContext.GetData<Transform>("ExecutorTransform");
		Vector3 executorPos = executorTransform.position;
		int heldItemID = aiContext.GetData<int>("HeldItemID");

		Settlement closestSettlement = SettlementManager.GetClosestSettlement(executorPos, true, true);
		InteractableObjectBase closestBlueprint = null;
		InteractableObjectBase closestStorage = null;

		if (closestSettlement == null)
		{
			// Fail if there are no settlements to bring the item to
			m_nodeState = EBTNodeState.STATE_FAILURE;
			return m_nodeState;
		}

		closestBlueprint = closestSettlement.FindBlueprint(executorPos);
		if (closestBlueprint != null)
		{
			aiContext.SetData<Transform>("TargetTransform", closestBlueprint.transform);
			aiContext.SetData<Vector3>("TargetPosition", closestBlueprint.GetInteractionPositon());

			m_nodeState = EBTNodeState.STATE_SUCSESS;
			return m_nodeState;
		}

		closestStorage = closestSettlement.FindItemStorage(executorPos);
		if (closestStorage != null)
		{
			aiContext.SetData<Transform>("TargetTransform", closestStorage.transform);
			aiContext.SetData<Vector3>("TargetPosition", closestStorage.GetInteractionPositon());

			m_nodeState = EBTNodeState.STATE_SUCSESS;
			return m_nodeState;
		}

		m_nodeState = EBTNodeState.STATE_FAILURE;
		return m_nodeState;
	}

	protected override void OnFirstEvaluate()
	{
		base.OnFirstEvaluate();

		Debug.Log("Begin storage search");
	}
}
