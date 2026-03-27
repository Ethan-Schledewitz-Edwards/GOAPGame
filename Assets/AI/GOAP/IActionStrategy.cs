using UnityEngine;

public interface IActionStrategy
{
    public bool IsStrategyPossible { get; }
	public bool IsStrategyComplete { get; }

	public void StartStrategy(){}

	public void TickStrategy(float t){}

	public void StopStrategy(){}
}
