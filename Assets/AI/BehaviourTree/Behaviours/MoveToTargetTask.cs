using BehaviourTrees;
using UnityEngine;
using UnityEngine.AI;

public class MoveToTargetTask : BTNodeBase
{
	private Transform m_transform;
	private NavMeshAgent m_agent;

	public MoveToTargetTask(Transform transform, NavMeshAgent navMeshAgent)
	{
		m_transform = transform;
		m_agent = navMeshAgent;
	}

	public override EBTNodeState Evaluate()
	{
		Transform target = (Transform)GetData("target");

		// Set destination if not within range
		if (Vector3.Distance(m_transform.position, target.position) > 0.1f)
		{
			m_agent.SetDestination(target.position);
		}
		else
		{
			// Within range
            m_nodeState = EBTNodeState.STATE_SUCSESS;
            return m_nodeState;
        }

		m_nodeState = EBTNodeState.STATE_RUNNING;
		return m_nodeState;
	}
}
