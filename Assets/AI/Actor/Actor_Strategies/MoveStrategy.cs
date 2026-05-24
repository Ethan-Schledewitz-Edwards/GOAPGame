using System;
using UnityEngine;
using UnityEngine.AI;

public class MoveStrategy : IActionStrategy
{
	readonly GOAPAgent m_agent;
	readonly AIPathing m_aiPathing;
	readonly Func<Vector3> m_destination;

	public bool IsStrategyPossible => !IsStrategyComplete;
	public bool IsStrategyComplete => CheckPath();

	public MoveStrategy(GOAPAgent agent, Func<Vector3> destination)
	{
		m_agent = agent;
		m_aiPathing = m_agent.transform.GetComponent<AIPathing>();
		m_destination = destination;
	}

	void IActionStrategy.StartStrategy()
	{
		m_agent.NotifyNewDestination(m_destination());
	}

	void IActionStrategy.StopStrategy() 
	{
		m_agent.NotifyClearDestination();
	}

	private bool CheckPath()
	{
		if (m_aiPathing == null)
			return false;

		bool isPathComplete = !m_aiPathing.IsCalculatingPath() && m_aiPathing.PathDistRemaining() <= .25f;
		return isPathComplete;
	}
}
