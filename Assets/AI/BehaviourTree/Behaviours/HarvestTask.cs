using UnityEngine;
using BehaviourTrees;

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
		Transform target = (Transform)GetData("target");
		HarvestableHealth harvestable = null;

		if(target.TryGetComponent(out HarvestableHealth health))
			harvestable = health;

		m_attackTimer += Time.deltaTime;
		if(m_attackTimer >= m_timeBetweenAttacks)
		{
			m_attackTimer = 0;
			Debug.Log("ATTACK");

			// Reduce object hitpoints
			harvestable?.RemoveHealth(2);
        }

		if(harvestable != null)
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