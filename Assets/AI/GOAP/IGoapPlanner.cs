using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IGoapPlanner
{
    ActionPlan Plan(Actor actor, HashSet<ActorGoal> goals, ActorGoal lastGoal);
}

public class GoapPlanner : IGoapPlanner
{
	public ActionPlan Plan(Actor actor, HashSet<ActorGoal> goals, ActorGoal lastGoal)
	{
		// Order goals by priority
		List<ActorGoal> sortedGoals = goals
			.Where(g => g.DesiredEffects.Any(b => !b.Evaluate()))
			.OrderByDescending(g => g == lastGoal ? g.Priority -0.01 : g.Priority)
			.ToList();

		// Try to solve the goals in order
		foreach (ActorGoal g in sortedGoals)
		{
			PlannerNode goalNode = new PlannerNode(null, null, g.DesiredEffects, 0);

			if(FindPath(goalNode, actor.actions))
			{
				// If the goal has no leaves and no actions to perform, try a different goal
				if (goalNode.IsLeafDead)
					continue;

				Stack<ActorAction> actionStack = new Stack<ActorAction>();
				while(goalNode.ChildLeaves.Count > 0)
				{
					var cheapestLeafNode = goalNode.ChildLeaves.OrderBy(leaf => leaf.Cost).First();
					goalNode = cheapestLeafNode;
					actionStack.Push(cheapestLeafNode.Action);
				}

				return new ActionPlan(g, actionStack, goalNode.Cost);
			}
		}

		Debug.LogWarning("No plan found");
		return null;
	}

	bool FindPath(PlannerNode parent, HashSet<ActorAction> actorActions)
	{
		foreach (ActorAction action in actorActions)
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
				var newRequiredEffects = new HashSet<ActorBelief>(requiredEffects);
				newRequiredEffects.ExceptWith(action.ActionEffects);
				newRequiredEffects.UnionWith(action.ActionPreconditions);

				var newAvailableActions = new HashSet<ActorAction>(actorActions);
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
	public ActorAction Action { get; private set; }
	public HashSet<ActorBelief> RequiredEffects { get; private set; }
	public List<PlannerNode> ChildLeaves { get; private set; }
	public float Cost { get; private set; }

	public bool IsLeafDead => ChildLeaves.Count == 0 && Action == null;

	public PlannerNode (PlannerNode parent, ActorAction action, HashSet<ActorBelief> requiredEffects, float cost)
	{
		Parent = parent;
		Action = action;
		RequiredEffects = new HashSet<ActorBelief>(requiredEffects);
		ChildLeaves = new List<PlannerNode>();
		Cost = cost;
	}
}
