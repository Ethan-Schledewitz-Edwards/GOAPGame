using System;
using System.Collections.Generic;
using UnityEngine;

public class BeliefFactory
{
	readonly Actor actor;
	readonly Dictionary<string, ActorBelief> beliefs;

	public BeliefFactory(Actor actor, Dictionary<string, ActorBelief> beliefs)
	{
		this.actor = actor;
		this.beliefs = beliefs;
	}

	public void AddBelief(string key, Func<bool> condition)
	{
		beliefs.Add(key,
			new ActorBelief.BeliefBuilder(key).BuildWithCondition(condition).Build());
	}

	// Sensors
	public void AddSensorBelief(string key, ActorSensor sensor) 
	{ 
		beliefs.Add(key, new ActorBelief.BeliefBuilder(key)
			.BuildWithCondition(() => sensor.IsTargetInRange)
			.BuildWithPosition(() => sensor.TargetPosition) 
			.Build());
	}

	bool IsInRangeOfBelief (Vector3 pos, float range) => Vector3.Distance(actor.transform.position, pos) < range;

	public void AddPosBelief(string key, float distance, Vector3 position)
	{
		beliefs.Add(key,
			new ActorBelief.BeliefBuilder(key)
			.BuildWithCondition(() => IsInRangeOfBelief(position, distance))
			.BuildWithPosition(() => position)
			.Build());
	}
}

public class ActorBelief
{
    public string BeliefName {  get; private set; }

	Func<bool> condition = () => false;
	Func<Vector3> observedPosition = () => Vector3.zero;

	public Vector3 Position => observedPosition();

	ActorBelief(string name) 
	{ 
		BeliefName = name;
	}

	public bool Evaluate() => condition();

	public class BeliefBuilder
	{
		readonly ActorBelief belief;

		public BeliefBuilder(string name) 
		{ 
			belief = new ActorBelief(name);
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

		public ActorBelief Build() 
		{
			return belief;
		}
	}
}
