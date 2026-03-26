using UnityEngine;
using UnityEngine.AI;

public class WanderStrategy : IActionStrategy
{
	public bool IsStrategyPossible => true;

	public bool IsStrategyComplete { get; private set; }

	readonly NavMeshAgent m_navMeshAgent;
	readonly float m_duration;
	readonly float m_timer;
	readonly float m_wanderRadius;


	public WanderStrategy(NavMeshAgent navMeshAgent, float duration)
	{
		m_navMeshAgent = navMeshAgent;
	}
}
