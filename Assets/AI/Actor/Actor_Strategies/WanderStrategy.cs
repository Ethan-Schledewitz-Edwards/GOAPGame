using UnityEngine;
using UnityEngine.AI;

public class WanderStrategy : IActionStrategy
{
	public bool IsStrategyPossible => !IsStrategyComplete;
	public bool IsStrategyComplete => !m_actor.IsCalculatingPath() && m_actor.PathDistRemaining() <= .25f;

	readonly Actor m_actor;
	readonly float m_wanderRadius;

	public WanderStrategy(Actor actor, float wanderRadius)
	{
		m_actor = actor;
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
			if(NavMesh.SamplePosition(m_actor.transform.position + randomDir, out hit, m_wanderRadius, 1))
			{
				m_actor.SetActorDestination(hit.position);
				return;
			}
		}
	}
}
