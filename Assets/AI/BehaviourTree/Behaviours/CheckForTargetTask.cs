using BehaviourTrees;
using UnityEngine;

public class CheckForTargetTask : BTNodeBase
{
	private const float k_interactionDist = 3.0f;

	private static int m_interactionLayerMask = 1 << LayerMask.NameToLayer("Interaction");
	private Transform m_transform;

	public CheckForTargetTask(Transform transform)
	{
		m_transform = transform; 
	}

	public override EBTNodeState Evaluate()
	{
		object t = GetData("target");

		// Check surroundings
		Collider[] hitColliders = Physics.OverlapSphere(m_transform.position,
				k_interactionDist,
				m_interactionLayerMask,
				QueryTriggerInteraction.Collide);

		if (t == null) 
		{
			// Try to find a new target
			if (hitColliders.Length > 0) 
			{
				m_parentNode.GetParent().SetData("target", hitColliders[0].transform);
				m_nodeState = EBTNodeState.STATE_SUCSESS;
				return m_nodeState;
			}
		}
		else
		{
			// Check if we are overlapping with the target
			foreach (Collider i in hitColliders)
			{
				if (i.transform != (Transform)t)
					continue;

				Debug.Log("AT TARGET");

				m_nodeState = EBTNodeState.STATE_SUCSESS;
				return m_nodeState;
			}
		}

		m_nodeState = EBTNodeState.STATE_FAILURE;
		return m_nodeState;
	}
}
