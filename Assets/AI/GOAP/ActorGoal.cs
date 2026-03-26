using System.Collections.Generic;
using UnityEngine;

public class ActorGoal
{
	public string GoalName { get; private set; }
	public float Priority { get; private set; }
	public HashSet<ActorBelief> DesiredEffects { get; private set; } = new ();


	ActorGoal(string goalName)
	{
		GoalName = goalName;
	}

	public class GoalBuilder
	{
		readonly ActorGoal goal;

		public GoalBuilder(string goalName)
		{
			goal = new ActorGoal(goalName);
		}

		public GoalBuilder BuildWithPriority(float priority) 
		{ 
			goal.Priority = priority;
			return this;
		}

		public GoalBuilder BuildWithDesiredEffect(ActorBelief effect)
		{
			goal.DesiredEffects.Add(effect);
			return this;
		}

		public ActorGoal Build()
		{
			return goal;
		}
	}
}
