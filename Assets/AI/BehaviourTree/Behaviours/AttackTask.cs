using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

public class AttackTask : BTNodeBase
{
	private const string c_attackTimerKey = "AttackTimer";

	private const float c_timeBetweenAttacks = 2f;
	private const int c_attackDamage = 2;

	protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
	{
		string attackTimerKey = GetContextKey(c_attackTimerKey);

		Transform executorTransform = context.GetData<Transform>(AIContextKeys.c_ExecutorTransform);
		Transform targetTransform = context.GetData<Transform>(AIContextKeys.c_TargetTransform);

		if (targetTransform == null)
		{
			return EBTNodeState.STATE_FAILURE;
		}

		if (targetTransform.TryGetComponent(out HealthComponent health))
		{

			float currentTimer = context.GetData<float>(attackTimerKey) + t;

			if (currentTimer >= c_timeBetweenAttacks)
			{
				context.SetData<float>(attackTimerKey, 0f);
				Debug.Log("ATTACK");
				Vector3 attackDir = targetTransform.position - executorTransform.position;
				health.TryTakeDamage(c_attackDamage, targetTransform.position, attackDir);
			}
			else
			{
				context.SetData<float>(attackTimerKey, currentTimer);
			}

			return EBTNodeState.STATE_RUNNING;
		}

		return EBTNodeState.STATE_FAILURE;
	}

	protected override void OnFirstEvaluate(AIContext context)
	{
		Debug.Log("Engaging Target!");
	}

	protected override void OnNodeExited(AIContext context) 
	{
		context.ClearData(GetContextKey(c_attackTimerKey));
	}

	protected override void OnNodeReset(AIContext context)
	{
		context.ClearData(GetContextKey(c_attackTimerKey));
	}
}