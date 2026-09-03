using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

public class AttackTask : BTNodeBase
{
	private readonly string m_cooldownKey;

	private const float c_timeBetweenAttacks = 2f;
	private const int c_attackDamage = 2;

	public AttackTask() : base()
	{
		m_cooldownKey = $"{NodeID}_AttackCooldown";
	}

	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);

		if (targetTransform == null)
			return EBTNodeState.STATE_FAILURE;

		if (targetTransform.TryGetComponent(out HealthComponent health) &&
			!health.IsDead)
		{
			float currentTimer = context.GetData<float>(m_cooldownKey) + t;

			if (currentTimer >= c_timeBetweenAttacks)
			{
				context.SetData<float>(m_cooldownKey, 0f);
				Debug.Log($"Actor:{executorTransform} attacked target:{targetTransform}.", targetTransform);
				Vector3 attackDir = targetTransform.position - executorTransform.position;
				health.TryTakeDamage(c_attackDamage, targetTransform.position, attackDir);
			}
			else
			{
				context.SetData<float>(m_cooldownKey, currentTimer);
			}

			return EBTNodeState.STATE_RUNNING;
		}
		else if (health.IsDead)
		{
			return EBTNodeState.STATE_SUCSESS;
		}

		return EBTNodeState.STATE_FAILURE;
	}

	protected override void OnFirstEvaluate(AIContext context)
	{
		Debug.Log("Engaging Target!");
	}

	protected override void OnNodeExited(AIContext context) 
	{
		context.ClearData(m_cooldownKey);
	}

	protected override void OnNodeReset(AIContext context)
	{
		context.ClearData(m_cooldownKey);
	}
}