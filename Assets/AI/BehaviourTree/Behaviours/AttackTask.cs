using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

public class AttackTask : BTNodeBase
{
	private const int c_timeBetweenAttacks = 2;

	private float m_attackTimer;

	public override EBTNodeState Evaluate(AIContext aiContext, float t)
	{
		base.Evaluate(aiContext,t);

		Transform executorTransform = aiContext.GetData<Transform>("ExecutorTransform");
		Transform targetTransform = aiContext.GetData<Transform>("TargetTransform");
		HealthComponent harvestable = null;

		if(targetTransform != null && targetTransform.TryGetComponent(out HealthComponent health))
		{
			harvestable = health;

			m_attackTimer += t;
			if (m_attackTimer >= c_timeBetweenAttacks)
			{
				m_attackTimer = 0;
				Debug.Log("ATTACK");

				if (harvestable != null)
				{
					Vector3 harvestablePos = harvestable.transform.position;
					Vector3 attackDir = harvestable.transform.position - executorTransform.position;

					// Reduce object hitpoints
					harvestable.TryTakeDamage(2, harvestable.transform.position, attackDir);
				}
			}
		}

		m_nodeState = EBTNodeState.STATE_RUNNING;
		return m_nodeState;
	}
}