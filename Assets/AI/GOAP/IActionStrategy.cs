using UnityEngine;

public interface IActionStrategy
{
    public bool IsStrategyPossible { get; }
	public bool IsStrategyComplete { get; }

	void StartStrategy(){}

	void TickStrategy(float t){}

	void StopStrategy(){}
}
