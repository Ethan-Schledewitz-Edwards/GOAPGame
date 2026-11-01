using UnityEngine;
using UnityEngine.AI;

public class WanderStrategy : IActionStrategy
{
	public bool IsStrategyPossible => !IsStrategyComplete;
	public bool IsStrategyComplete => CheckPath();

	readonly GOAPAgent m_agent;
	readonly AIPathing m_aiPathing;
	readonly float m_wanderRadius;

	public WanderStrategy(GOAPAgent agent, float wanderRadius)
	{
		m_agent = agent;
		m_aiPathing = m_agent.transform.GetComponent<AIPathing>();
		m_wanderRadius = wanderRadius;
	}

	void IActionStrategy.StartStrategy()
	{
		if (m_aiPathing == null)
			return;

		// Try to find a random location nearby
		for (int i = 0; i < 8; i++)
		{
			Vector3 randomDir = (Random.insideUnitSphere * m_wanderRadius);
			randomDir.y = 0;

			NavMeshHit hit;
			if (NavMesh.SamplePosition(m_agent.transform.position + randomDir, out hit, m_wanderRadius, 1))
			{
				m_aiPathing.SetDestination(hit.position);
				return;
			}
		}
	}

	private bool CheckPath()
	{
		if (m_aiPathing == null)
			return false;

		bool isPathComplete = !m_aiPathing.IsCalculatingPath() && m_aiPathing.PathDistRemaining() <= .25f;
		return isPathComplete;
	}
}
