using BehaviourTrees;
using UnityEngine;

public class SortTask : BTNodeBase
{
	private const int m_timeBetweenAttacks = 2;

	private Actor m_actorComponent;
	private Vector3 m_searchPosition;


	private float m_attackTimer;

	public SortTask(Actor actor, Vector3 searchPosition)
	{
		m_actorComponent = actor;
		m_searchPosition = searchPosition;
	}

	public override EBTNodeState Evaluate()
	{
		Vector3 searchPosition = (Vector3)GetData("searchPosition");
		HealthComponent harvestable = null;

		if (target.TryGetComponent(out HealthComponent health))
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

		if (harvestable != null)
		{
			bool isTargetDead = harvestable.GetIsDead();
			if (isTargetDead)
			{
				ClearData("target");
				m_actorComponent.SetTask(null);
			}
		}

		m_nodeState = EBTNodeState.STATE_RUNNING;
		return m_nodeState;
	}
}
