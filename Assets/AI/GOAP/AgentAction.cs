using System.Collections.Generic;
using UnityEngine;

public class AgentAction
{
	public string ActionName { get; private set; }
	public float ActionCost { get; private set; }

	public HashSet<AgentBelief> ActionPreconditions { get; private set; } = new(); // What the actor belives to be true before they execute an action
	public HashSet<AgentBelief> ActionEffects { get; private set; } = new(); // What the actor belives the results of the action are

	private IActionStrategy actionStrategy;
	public bool Complete => actionStrategy.IsStrategyComplete;

	public AgentAction(string actionName)
	{
		ActionName = actionName;
	}

	public void StartAction()
	{
		actionStrategy.StartStrategy();
	}

	public void TickAction(float t)
	{
		// Ensure the action is possible
		if (actionStrategy.IsStrategyPossible)
			actionStrategy.TickStrategy(t);

		// Stop if the strategy is still executing
		if (!actionStrategy.IsStrategyComplete)
			return;

		// Apply effects
		foreach (AgentBelief effect in ActionEffects)
		{
			effect.Evaluate();
		}
	}

	public void StopAction()
	{
		actionStrategy.StopStrategy();
	}

	public class ActionBuilder
	{
		readonly AgentAction action;

		public ActionBuilder(string name)
		{
			action = new AgentAction(name)
			{
				ActionCost = 1
			};
		}

		public ActionBuilder BuildWithCost(float cost) 
		{ 
			action.ActionCost = cost;
			return this;
		}

		public ActionBuilder BuildWithStrategy(IActionStrategy strategy)
		{
			action.actionStrategy = strategy;
			return this;
		}

		public ActionBuilder AddPrecondition(AgentBelief precondition) 
		{ 
			action.ActionPreconditions.Add(precondition);
			return this;
		}

		public ActionBuilder AddEffect(AgentBelief effect) 
		{ 
			action.ActionEffects.Add(effect);
			return this;
		}

		public AgentAction Build() 
		{ 
			return action;
		}
	}
}
