using BehaviourTrees;
using UnityEngine;

public class CheckForTargetRangeTask : BTNodeBase
{
	private static int m_interactionLayerMask = 1 << LayerMask.NameToLayer("Interaction");

	private Actor m_actorComponent;
	private Transform m_actorTransform;

	/// <summary>
	/// Creates a task which is used to detect if a "target" data's game object is within range 
	/// </summary>
	/// <remarks>
	/// This is best for confirming if moving targets are in range
	/// </remarks>
	/// <param name="actorComponent">The target actor</param>
	/// <param name="actorTransform">The actors transform</param>
	public CheckForTargetRangeTask(Actor actorComponent, Transform actorTransform)
	{
		m_actorComponent = actorComponent;
		m_actorTransform = actorTransform; 
	}

	public override EBTNodeState Evaluate(float t)
	{
		base.Evaluate(t);

		Transform targetTransform = (Transform)GetData("targetTransform");

		// Check surroundings
		Collider[] hitColliders = Physics.OverlapSphere(m_actorTransform.position,
				m_actorComponent.InteractionDist,
				m_interactionLayerMask,
				QueryTriggerInteraction.Collide);


		if (targetTransform != null)
		{
			// Check if we are overlapping with the target
			foreach (Collider i in hitColliders)
			{
				if (i.transform != targetTransform)
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
