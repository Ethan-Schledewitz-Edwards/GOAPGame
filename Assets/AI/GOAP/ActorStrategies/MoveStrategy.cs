using System;
using UnityEngine;
using UnityEngine.AI;

public class MoveStrategy : IActionStrategy
{
	readonly NavMeshAgent m_navMeshAgent;
	readonly Func<Vector3> m_destination;

	public bool IsStrategyPossible => !IsStrategyComplete;
	public bool IsStrategyComplete => m_navMeshAgent.remainingDistance <= .5f && !m_navMeshAgent.pathPending;

	public MoveStrategy(NavMeshAgent navMeshAgent, Func<Vector3> destination)
	{
		m_navMeshAgent = navMeshAgent;
		m_destination = destination;
	}

	void IActionStrategy.StartStrategy()
	{
		m_navMeshAgent.SetDestination(m_destination());
	}

	void IActionStrategy.StopStrategy() 
	{
		m_navMeshAgent.ResetPath();
	}
}
