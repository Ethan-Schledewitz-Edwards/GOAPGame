using System.Collections.Generic;
using UnityEngine;

public class AgentPlan
{
	public AgentGoal GoalToAcheive {  get; private set; }
	public Stack<AgentAction> Actions { get; private set; } // The actions available to achieve the goal
	public float TotalCost { get; private set; } // The total cost of the actions

	public AgentPlan(AgentGoal goalToAcheive, Stack<AgentAction> actions, float totalCost)
	{
		GoalToAcheive = goalToAcheive;
		Actions = actions;
		TotalCost = totalCost;
	}
}
