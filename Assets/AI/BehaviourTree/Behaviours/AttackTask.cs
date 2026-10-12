using AI.AIContext;
using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

public class AttackTask : BTNodeBase
{
	private const float c_timeBetweenAttacks = 2f;
	private const int c_attackDamage = 2;

	protected override EBTNodeState OnNodeEvaluated(ref AIContext context, float t)
	{
		Transform executorTransform = context.ExecutorTransform;
		if(executorTransform == null)
			return EBTNodeState.FAILURE;

		Transform targetTransform = context.TargetTransform;
		if (targetTransform == null)
			return EBTNodeState.FAILURE;

		if (targetTransform.TryGetComponent(out HealthComponent health) &&
			!health.IsDead)
		{
			float currentTimer = context.Cooldown + t;

			if (currentTimer >= c_timeBetweenAttacks)
			{
				context.Cooldown = 0f;
				Vector3 attackDir = targetTransform.position - executorTransform.position;
				health.TryTakeDamage(c_attackDamage, targetTransform.position, attackDir);
			}
			else
			{
				context.Cooldown = currentTimer;
			}

			return EBTNodeState.RUNNING;
		}
		else if (health.IsDead)
		{
			return EBTNodeState.SUCSESS;
		}

		return EBTNodeState.FAILURE;
	}

	protected override void OnFirstEvaluate(ref AIContext context) 
	{
		context.Cooldown = 0f;
	}

	protected override void OnNodeExited(ref AIContext context) 
	{
		context.Cooldown = 0f;
	}

	protected override void OnNodeReset(ref AIContext context)
	{
		context.Cooldown = 0f;
	}
}