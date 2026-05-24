using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IGoapPlanner
{
    AgentPlan Plan(GOAPAgent goapAgent, HashSet<AgentGoal> goals, AgentGoal lastGoal);
}

public class GoapPlanner : IGoapPlanner
{
	public AgentPlan Plan(GOAPAgent goapAgent, HashSet<AgentGoal> goals, AgentGoal lastGoal)
	{
		// Order goals by priority
		List<AgentGoal> sortedGoals = goals
			.Where(g => g.DesiredEffects.Any(b => !b.Evaluate()))
			.OrderByDescending(g => g == lastGoal ? g.Priority -0.01 : g.Priority)
			.ToList();

		// Try to solve the goals in order
		foreach (AgentGoal g in sortedGoals)
		{
			PlannerNode goalNode = new PlannerNode(null, null, g.DesiredEffects, 0);

			if(FindPath(goalNode, goapAgent.Actions))
			{
				// If the goal has no leaves and no actions to perform, try a different goal
				if (goalNode.IsLeafDead)
					continue;

				Stack<AgentAction> actionStack = new Stack<AgentAction>();
				while(goalNode.ChildLeaves.Count > 0)
				{
					var cheapestLeafNode = goalNode.ChildLeaves.OrderBy(leaf => leaf.Cost).First();
					goalNode = cheapestLeafNode;
					actionStack.Push(cheapestLeafNode.Action);
				}

				return new AgentPlan(g, actionStack, goalNode.Cost);
			}
		}

		Debug.LogWarning("No plan found");
		return null;
	}

	bool FindPath(PlannerNode parent, HashSet<AgentAction> actorActions)
	{
		var orderedActions = actorActions.OrderBy(a => a.ActionCost);

		foreach (AgentAction action in orderedActions)
		{
			var requiredEffects = parent.RequiredEffects;

			// Remove any effects that evaluate as true (They are already done)
			requiredEffects.RemoveWhere(b => b.Evaluate());

			// If no effects need to be fulfilled, the plan is good.
			if (requiredEffects.Count == 0)
			{
				return true;
			}

			if (action.ActionEffects.Any(requiredEffects.Contains))
			{
				var newRequiredEffects = new HashSet<AgentBelief>(requiredEffects);
				newRequiredEffects.ExceptWith(action.ActionEffects);
				newRequiredEffects.UnionWith(action.ActionPreconditions);

				var newAvailableActions = new HashSet<AgentAction>(actorActions);
				newAvailableActions.Remove(action);

				var newNode = new PlannerNode(parent, action, newRequiredEffects, parent.Cost + action.ActionCost);

				// Explore the new node recursively
				if (FindPath(newNode, newAvailableActions))
				{
					parent.ChildLeaves.Add(newNode);
					newRequiredEffects.ExceptWith(newNode.Action.ActionPreconditions);
				}

				// if all effects at this depth are satisfied, return true (valid plan)
				if (newRequiredEffects.Count == 0)
				{
					return true;
				}
			}
		}

		return false;
	}
}

public class PlannerNode
{
	public PlannerNode Parent {  get; private set; }
	public AgentAction Action { get; private set; }
	public HashSet<AgentBelief> RequiredEffects { get; private set; }
	public List<PlannerNode> ChildLeaves { get; private set; }
	public float Cost { get; private set; }

	public bool IsLeafDead => ChildLeaves.Count == 0 && Action == null;

	public PlannerNode (PlannerNode parent, AgentAction action, HashSet<AgentBelief> requiredEffects, float cost)
	{
		Parent = parent;
		Action = action;
		RequiredEffects = new HashSet<AgentBelief>(requiredEffects);
		ChildLeaves = new List<PlannerNode>();
		Cost = cost;
	}
}
