using BehaviourTrees;
using UnityEngine;

public class CheckForTargetTask : BTNodeBase
{
	private const float k_interactionDist = 3.0f;
	private static int m_interactionLayerMask = 1 << LayerMask.NameToLayer("Interaction");

	private Actor m_actorComponent;
	private Transform m_actorTransform;

	float m_timeSearching;

	public CheckForTargetTask(Actor actorComponent, Transform actorTransform)
	{
		m_actorComponent = actorComponent;
		m_actorTransform = actorTransform; 
	}

	public override EBTNodeState Evaluate()
	{
		object target = GetData("target");

		// Check surroundings
		Collider[] hitColliders = Physics.OverlapSphere(m_actorTransform.position,
				k_interactionDist,
				m_interactionLayerMask,
				QueryTriggerInteraction.Collide);

		if (target == null) 
		{
			// Try to find a new target
			if (hitColliders.Length > 0) 
			{
				m_parentNode.GetParentNode().SetData("target", hitColliders[0].transform);

				m_nodeState = EBTNodeState.STATE_SUCSESS;
				return m_nodeState;
			}
		}
		else
		{
			// Check if we are overlapping with the target
			foreach (Collider i in hitColliders)
			{
				if (i.transform != (Transform)target)
					continue;

				Debug.Log("AT TARGET: " + m_actorTransform.name);

				m_nodeState = EBTNodeState.STATE_SUCSESS;
				return m_nodeState;
			}
		}

		// Break out of target search
		m_timeSearching += Time.deltaTime;
		if(m_timeSearching > 10.0f)
		{
			ClearData("target");
			m_actorComponent.SetTask(null);
		}

		m_nodeState = EBTNodeState.STATE_FAILURE;
		return m_nodeState;
	}
}
