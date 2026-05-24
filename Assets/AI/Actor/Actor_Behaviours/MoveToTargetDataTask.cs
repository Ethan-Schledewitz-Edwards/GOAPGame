using BehaviourTrees;
using UnityEngine;
using UnityEngine.AI;

public class MoveToTargetDataTask : BTNodeBase
{
	private BehaviourTreeExecutor m_behaviourTreeExecutor;
	private Transform m_actorTransform;

	/// <summary>
	/// Creates a task which is used to move an actor to their "target" data
	/// </summary>
	/// <param name="behaviourTreeExecutor">The actors transform</param>
	/// <param name="navMeshAgent">The actors nav agent component</param>
	public MoveToTargetDataTask(BehaviourTreeExecutor behaviourTreeExecutor, Transform actorTransform)
	{
		m_behaviourTreeExecutor = behaviourTreeExecutor;
		m_actorTransform = actorTransform;
	}

	public override EBTNodeState Evaluate(float t)
	{
		base.Evaluate(t);

		Transform targetPosition = (Transform)GetData("targetPositionTransform");

		if (m_behaviourTreeExecutor.TryGetComponent(out Actor actor)) 
		{
			if (targetPosition != null)
			{
				Vector3 targetPos = targetPosition.position;
				float dist = Vector3.Distance(m_actorTransform.position, targetPos);

				bool isInRange = (dist <= 0.3f);

				// Set destination if not within range
				if (!isInRange)
				{
					actor.AIPathing.SetDestination(targetPos);
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
