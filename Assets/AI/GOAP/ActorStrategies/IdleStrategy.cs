using UnityEngine;

public class IdleStrategy : IActionStrategy
{
	public bool IsStrategyPossible => true; // Actors can always idle because it has the lowest priority

	public bool IsStrategyComplete {get; private set;}

	private float m_duration;
	private float m_timer;

	public IdleStrategy (float duration)
	{
		m_duration = duration;
	}

	void IActionStrategy.StartStrategy() 
	{
		m_timer = 0;
	}

	void IActionStrategy.TickStrategy(float t) 
	{
		m_timer += t;
		if(m_timer > m_duration)
		{
			m_timer = 0;
			IsStrategyComplete = true;
			return;
		}

		IsStrategyComplete = false;
	}
}
