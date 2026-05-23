using BehaviourTrees;
using UnityEngine;

public class CheckForTargetRangeTask : BTNodeBase
{
	private static int m_interactionLayerMask = 1 << LayerMask.NameToLayer("Interaction");

	private BehaviourTreeExecutor m_behaviourTreeExecutor;
	private Transform m_executorTransform;

	/// <summary>
	/// Creates a task which is used to detect if a "target" data's game object is within range 
	/// </summary>
	/// <remarks>
	/// This is best for confirming if moving targets are in range
	/// </remarks>
	/// <param name="behaviourTreeExecutor">The target actor</param>
	/// <param name="executorTransform ">The actors transform</param>
	public CheckForTargetRangeTask(BehaviourTreeExecutor behaviourTreeExecutor, Transform executorTransform)
	{
		m_behaviourTreeExecutor = behaviourTreeExecutor;
		m_executorTransform = executorTransform;
	}

	public override EBTNodeState Evaluate(float t)
	{
		base.Evaluate(t);

		Transform targetTransform = (Transform)GetData("targetTransform");

		if (m_executorTransform.TryGetComponent(out Actor actor))
		{
			// Check surroundings
			Collider[] hitColliders = Physics.OverlapSphere(m_executorTransform.position,
					actor.InteractionDist,
					m_interactionLayerMask,
					QueryTriggerInteraction.Collide);


			if (targetTransform != null)
			{
				// Check if we are overlapping with the target
				foreach (Collider i in hitColliders)
				{
					if (i.transform != targetTransform)
						continue;

					Debug.Log("AT TARGET: " + m_executorTransform.name);

					m_nodeState = EBTNodeState.STATE_SUCSESS;
					return m_nodeState;
				}
			}
		}

		m_nodeState = EBTNodeState.STATE_FAILURE;
		return m_nodeState;
	}
}
