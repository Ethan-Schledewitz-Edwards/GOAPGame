using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

public class AttackTask : BTNodeBase
{
	private float m_timeBetweenAttacks = 2f;
	private int m_attackDamage = 2;

	protected override EBTNodeState OnUpdate(AIContext context, float t)
	{
		string timerKey = GetContextKey("AttackTimer");

		Transform executorTransform = context.GetData<Transform>("ExecutorTransform");
		Transform targetTransform = context.GetData<Transform>("TargetTransform");

		if (targetTransform == null)
		{
			return EBTNodeState.STATE_FAILURE;
		}

		if (targetTransform.TryGetComponent(out HealthComponent health))
		{

			float currentTimer = context.GetData<float>(timerKey) + t;

			if (currentTimer >= m_timeBetweenAttacks)
			{
				context.SetData<float>(timerKey, 0f);
				Debug.Log("ATTACK");
				Vector3 attackDir = targetTransform.position - executorTransform.position;
				health.TryTakeDamage(m_attackDamage, targetTransform.position, attackDir);
			}
			else
			{
				context.SetData<float>(timerKey, currentTimer);
			}

			return EBTNodeState.STATE_RUNNING;
		}

		return EBTNodeState.STATE_FAILURE;
	}

	protected override void OnFirstEvaluate(AIContext context)
	{
		Debug.Log("Engaging Target!");
	}
}