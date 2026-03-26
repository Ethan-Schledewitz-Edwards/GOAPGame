using System.Collections.Generic;
using UnityEngine;

public class ActorAction
{
	public string ActionName { get; private set; }
	public float ActionCost { get; private set; }

	public HashSet<ActorBelief> ActionPreconditions { get; private set; } = new(); // What the actor belives to be tru before they execute an action
	public HashSet<ActorBelief> ActionEffects { get; private set; } = new(); // What the actor belives the results of the action are

	private IActionStrategy actionStrategy;
	public bool Complete => actionStrategy.IsStrategyComplete;

	public ActorAction(string actionName)
	{
		ActionName = actionName;
	}

	private void StartAction()
	{
		actionStrategy.StartStrategy();
	}

	private void TickAction(float deltaTime)
	{
		// Ensure the action is possible
		if (actionStrategy.IsStrategyPossible)
			actionStrategy.TickStrategy(deltaTime);

		// Stop if the strategy is still executing
		if (!actionStrategy.IsStrategyComplete)
			return;

		// Apply effects
		foreach (ActorBelief effect in ActionEffects)
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
		readonly ActorAction action;

		public ActionBuilder(string name)
		{
			action = new ActorAction(name)
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

		public ActionBuilder AddPrecondition(ActorBelief precondition) 
		{ 
			action.ActionPreconditions.Add(precondition);
			return this;
		}

		public ActionBuilder AddEffect(ActorBelief effect) 
		{ 
			action.ActionEffects.Add(effect);
			return this;
		}

		public ActorAction Build() 
		{ 
			return action;
		}
	}
}
