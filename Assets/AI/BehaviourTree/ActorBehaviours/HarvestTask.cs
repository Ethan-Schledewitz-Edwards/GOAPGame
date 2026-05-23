using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

public class HarvestTask : BTNodeBase
{
	private const int m_timeBetweenAttacks = 2;

	private Actor m_actorComponent;

	private float m_attackTimer;

	/// <summary>
	/// Creates a task which is used to harvest a "target"
	/// </summary>
	/// <param name="actor">The target actor</param>
	public HarvestTask(Actor actorComponent)
	{
		m_actorComponent = actorComponent;
	}

	public override EBTNodeState Evaluate(float t)
	{
		base.Evaluate(t);

		Transform target = (Transform)GetData("targetTransform");
		HealthComponent harvestable = null;

		if(target != null && target.TryGetComponent(out HealthComponent health))
		{
			harvestable = health;

			m_attackTimer += t;
			if (m_attackTimer >= m_timeBetweenAttacks)
			{
				m_attackTimer = 0;
				Debug.Log("ATTACK");

				if (harvestable != null)
				{
					Vector3 harvestablePos = harvestable.transform.position;
					Vector3 attackDir = harvestable.transform.position - m_actorComponent.transform.position;

					// Reduce object hitpoints
					harvestable.TryTakeDamage(2, harvestable.transform.position, attackDir);
				}
			}
		}

		m_nodeState = EBTNodeState.STATE_RUNNING;
		return m_nodeState;
	}
}