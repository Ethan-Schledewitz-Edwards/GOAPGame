using UnityEngine;
using UnityEngine.AI;

public class WanderStrategy : IActionStrategy
{
	public bool IsStrategyPossible => !IsStrategyComplete;
	public bool IsStrategyComplete => m_navMeshAgent.remainingDistance <= .5f && !m_navMeshAgent.pathPending;

	readonly NavMeshAgent m_navMeshAgent;
	readonly float m_wanderRadius;

	public WanderStrategy(NavMeshAgent navMeshAgent, float wanderRadius)
	{
		m_navMeshAgent = navMeshAgent;
		m_wanderRadius = wanderRadius;
	}

	void IActionStrategy.StartStrategy()
	{
		// Try to find a random location nearby
		for (int i = 0; i < 8; i++)
		{
			Vector3 randomDir = (Random.insideUnitSphere * m_wanderRadius);
			randomDir.y = 0;

			NavMeshHit hit;
			if(NavMesh.SamplePosition(m_navMeshAgent.transform.position + randomDir, out hit, m_wanderRadius, 1))
			{
				m_navMeshAgent.SetDestination(hit.position);
				return;
			}
		}
	}
}
