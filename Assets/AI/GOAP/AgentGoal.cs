using System.Collections.Generic;
using UnityEngine;

public class AgentGoal
{
	public string GoalName { get; private set; }
	public float Priority { get; private set; }
	public HashSet<AgentBelief> DesiredEffects { get; private set; } = new ();


	AgentGoal(string goalName)
	{
		GoalName = goalName;
	}

	public class GoalBuilder
	{
		readonly AgentGoal goal;

		public GoalBuilder(string goalName)
		{
			goal = new AgentGoal(goalName);
		}

		public GoalBuilder BuildWithPriority(float priority) 
		{ 
			goal.Priority = priority;
			return this;
		}

		public GoalBuilder BuildWithDesiredEffect(AgentBelief effect)
		{
			goal.DesiredEffects.Add(effect);
			return this;
		}

		public AgentGoal Build()
		{
			return goal;
		}
	}
}
