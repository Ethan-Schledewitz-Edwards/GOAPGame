using BehaviourTrees;
using UnityEngine;
using UnityEngine.AI;

public class MoveToTargetDataTask : BTNodeBase
{
	private const float c_targetingRangeSqrt = 0.3f * 0.3f;

	public override EBTNodeState Evaluate(AIContext aiContext, float t)
	{
		base.Evaluate(aiContext, t);

		Transform executorTransform = aiContext.GetData<Transform>("ExecutorTransform");
		Vector3 targetPos = aiContext.GetData<Vector3>("TargetPosition");

		if (executorTransform.TryGetComponent(out AIPathing pathing)) 
		{
			if (targetPos != Vector3.zero)
			{
				Vector3 offset = targetPos - executorTransform.position;
				float sqrDist = offset.sqrMagnitude;

				// Compare against the pre-calculated squared range
				bool isInRange = (sqrDist <= c_targetingRangeSqrt);

				// Set destination if not within range
				if (!isInRange)
				{
					pathing.SetDestination(targetPos);
				}
				else
				{
					// Within range
					m_nodeState = EBTNodeState.STATE_SUCSESS;
					return m_nodeState;
				}
			}
			else
			{
				m_nodeState = EBTNodeState.STATE_FAILURE;
				return m_nodeState;
			}

			m_nodeState = EBTNodeState.STATE_RUNNING;
			return m_nodeState;
		}

		m_nodeState = EBTNodeState.STATE_FAILURE;
		return m_nodeState;
	}
}
