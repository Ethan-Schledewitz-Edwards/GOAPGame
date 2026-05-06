using BehaviourTrees;
using UnityEngine;
using UnityEngine.AI;

public class MoveToTargetDataTask : BTNodeBase
{
	private Actor m_actor;
	private Transform m_actorTransform;

	/// <summary>
	/// Creates a task which is used to move an actor to their "target" data
	/// </summary>
	/// <param name="actorTransform">The actors transform</param>
	/// <param name="navMeshAgent">The actors nav agent component</param>
	public MoveToTargetDataTask(Actor actorComponent, Transform actorTransform)
	{
		m_actor = actorComponent;
		m_actorTransform = actorTransform;
	}

	public override EBTNodeState Evaluate(float t)
	{
		base.Evaluate(t);

		Transform targetPosition = (Transform)GetData("targetPositionTransform");

		if (targetPosition != null) 
		{
			Vector3 targetPos = targetPosition.position;
			float dist = Vector3.Distance(m_actorTransform.position, targetPos);

			bool isInRange = (dist <= 0.3f);

			// Set destination if not within range
			if (!isInRange)
			{
				m_actor.SetActorDestination(targetPos);
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
}
