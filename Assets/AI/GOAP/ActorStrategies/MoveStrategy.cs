using System;
using UnityEngine;
using UnityEngine.AI;

public class MoveStrategy : IActionStrategy
{
	readonly Actor m_actor;
	readonly Func<Vector3> m_destination;

	public bool IsStrategyPossible => !IsStrategyComplete;
	public bool IsStrategyComplete => !m_actor.IsCalculatingPath() && m_actor.PathDistRemaining() <= .25f;

	public MoveStrategy(Actor actor, Func<Vector3> destination)
	{
		m_actor = actor;
		m_destination = destination;
	}

	void IActionStrategy.StartStrategy()
	{
		m_actor.SetActorDestination(m_destination());
	}

	void IActionStrategy.StopStrategy() 
	{
		m_actor.ClearActorDestination();
	}
}
