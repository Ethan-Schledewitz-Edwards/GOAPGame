using BehaviourTrees;
using UnityEngine;
using UnityEngine.AI;

public class MoveToTargetTask : BTNode
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

		if (Vector3.Distance(m_transform.position, target.position) > 0.1f)
		{
			m_agent.SetDestination(target.position);
		}

		m_nodeState = EBTNodeState.STATE_RUNNING;
		return m_nodeState;
	}
}
