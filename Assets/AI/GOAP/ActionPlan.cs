using System.Collections.Generic;
using UnityEngine;

public class ActionPlan
{
	public ActorGoal GoalToAcheive {  get; private set; }
	public Stack<ActorAction> Actions { get; private set; } // The actions available to achieve the goal
	public float TotalCost { get; private set; } // The total cost of the actions

	public ActionPlan(ActorGoal goalToAcheive, Stack<ActorAction> actions, float totalCost)
	{
		GoalToAcheive = goalToAcheive;
		Actions = actions;
		TotalCost = totalCost;
	}
}
