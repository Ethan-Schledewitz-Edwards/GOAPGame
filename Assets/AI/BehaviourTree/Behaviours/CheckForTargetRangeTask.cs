using BehaviourTrees;
using UnityEngine;

public class CheckForTargetRangeTask : BTNodeBase
{
	private static int m_interactionLayerMask = 1 << LayerMask.NameToLayer("Interaction");

	private Actor m_actorComponent;
	private Transform m_actorTransform;

	float m_timeSearching;

	/// <summary>
	/// Creates a task which is used to detect if a "target" data's game object is within range
	/// </summary>
	/// <param name="actorComponent">The target actor</param>
	/// <param name="actorTransform">The actors transform</param>
	public CheckForTargetRangeTask(Actor actorComponent, Transform actorTransform)
	{
		m_actorComponent = actorComponent;
		m_actorTransform = actorTransform; 
	}

	public override EBTNodeState Evaluate()
	{
		base.Evaluate();

		object target = GetData("target");

		// Check surroundings
		Collider[] hitColliders = Physics.OverlapSphere(m_actorTransform.position,
				m_actorComponent.InteractionDist,
				m_interactionLayerMask,
				QueryTriggerInteraction.Collide);

		if (target == null) 
		{
			// Try to find a new target
			if (hitColliders.Length > 0) 
			{
				m_parentNode.SetData("target", hitColliders[0].transform);

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

		m_nodeState = EBTNodeState.STATE_FAILURE;
		return m_nodeState;
	}
}
