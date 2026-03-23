using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

public class HarvestTask : BTNodeBase
{
	private const int m_timeBetweenAttacks = 2;

	private Actor m_actorComponent;

	private float m_attackTimer;

	public HarvestTask(Actor actor)
	{
		m_actorComponent = actor;
	}

	public override EBTNodeState Evaluate()
	{
		base.Evaluate();

		Transform target = (Transform)GetData("target");
		HealthComponent harvestable = null;

		if(target != null && target.TryGetComponent(out HealthComponent health))
		{
			harvestable = health;

			m_attackTimer += Time.deltaTime;
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