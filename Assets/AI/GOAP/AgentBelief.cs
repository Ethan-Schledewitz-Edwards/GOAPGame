using System;
using System.Collections.Generic;
using UnityEngine;

public class BeliefFactory
{
	readonly GOAPAgent goapAgent;
	readonly Dictionary<string, AgentBelief> beliefs;

	public BeliefFactory(GOAPAgent goapAgent, Dictionary<string, AgentBelief> beliefs)
	{
		this.goapAgent = goapAgent;
		this.beliefs = beliefs;
	}

	public void AddBelief(string key, Func<bool> condition)
	{
		beliefs.Add(key,
			new AgentBelief.BeliefBuilder(key).BuildWithCondition(condition).Build());
	}

	// Sensors
	public void AddSensorBelief(string key, AgentSensor sensor) 
	{ 
		beliefs.Add(key, new AgentBelief.BeliefBuilder(key)
			.BuildWithCondition(() => sensor.IsTargetInRange)
			.BuildWithPosition(() => sensor.TargetPosition) 
			.Build());
	}

	bool IsInRangeOfBelief (Vector3 pos, float range) => Vector3.Distance(goapAgent.transform.position, pos) < range;

	public void AddPosBelief(string key, float distance, Vector3 position)
	{
		beliefs.Add(key,
			new AgentBelief.BeliefBuilder(key)
			.BuildWithCondition(() => IsInRangeOfBelief(position, distance))
			.BuildWithPosition(() => position)
			.Build());
	}
}

public class AgentBelief
{
    public string BeliefName {  get; private set; }

	Func<bool> condition = () => false;
	Func<Vector3> observedPosition = () => Vector3.zero;

	public Vector3 Position => observedPosition();

	AgentBelief(string name) 
	{ 
		BeliefName = name;
	}

	public bool Evaluate() => condition();

	public class BeliefBuilder
	{
		readonly AgentBelief belief;

		public BeliefBuilder(string name) 
		{ 
			belief = new AgentBelief(name);
		}

		public BeliefBuilder BuildWithCondition(Func<bool> condition)
		{
			belief.condition = condition;
			return this;
		}

		public BeliefBuilder BuildWithPosition(Func<Vector3> observedPosition)
		{
			belief.observedPosition = observedPosition;
			return this;
		}

		public AgentBelief Build() 
		{
			return belief;
		}
	}
}
