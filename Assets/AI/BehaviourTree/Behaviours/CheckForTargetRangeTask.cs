using BehaviourTrees;
using UnityEngine;

public class CheckForTargetRangeTask : BTNodeBase
{
	private static int m_interactionLayerMask = 1 << LayerMask.NameToLayer("Interaction");

	public override EBTNodeState Evaluate(AIContext aiContext, float t)
	{
		base.Evaluate(aiContext, t);

		Transform executorTransform = aiContext.GetData<Transform>("ExecutorTransform");
		Transform targetTransform = aiContext.GetData<Transform>("TargetTransform");
		float interactionRange = aiContext.GetData<float>("InteractionDist");

		// Check surroundings
		Collider[] hitColliders = Physics.OverlapSphere(executorTransform.position,
				interactionRange,
				m_interactionLayerMask,
				QueryTriggerInteraction.Collide);


		if (targetTransform != null)
		{
			// Check if we are overlapping with the target
			foreach (Collider i in hitColliders)
			{
				if (i.transform != targetTransform)
					continue;

				Debug.Log("AT TARGET: " + executorTransform.name);

				m_nodeState = EBTNodeState.STATE_SUCSESS;
				return m_nodeState;
			}
		}

		m_nodeState = EBTNodeState.STATE_FAILURE;
		return m_nodeState;
	}
}
