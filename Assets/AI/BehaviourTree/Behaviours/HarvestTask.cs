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
		Transform target = (Transform)GetData("target");
		HealthComponent harvestable = null;

		if(target.TryGetComponent(out HealthComponent health))
			harvestable = health;

		m_attackTimer += Time.deltaTime;
		if(m_attackTimer >= m_timeBetweenAttacks)
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

		if(harvestable != null)
		{
            bool isTargetDead = harvestable.GetIsDead();
            if (isTargetDead)
            {
                ClearData("target");
                m_actorComponent.SetTask(null);

				// Try to sort the harvested items
				TrySortItems(harvestable.transform.position);
			}
        }
		else
		{
			ClearData("target");
			m_actorComponent.SetTask(null);

			// Try to sort the harvested items
			TrySortItems(m_actorComponent.transform.position);
		}

		m_nodeState = EBTNodeState.STATE_RUNNING;
		return m_nodeState;
	}

	private void TrySortItems(Vector3 searchPosition)
	{
		BehaviourTree tree = new BehaviourTree();

		BTNodeBase root = new BTSelectorNode(new List<BTNodeBase>
		{
			new BTSequenceNode(new List<BTNodeBase>
			{
				new SortTask(m_actorComponent, searchPosition)
			})
		});

		root.SetData("searchPosition", searchPosition);

		tree.SetTree(root);

		m_actorComponent.SetBehaviourTree(tree);
		Debug.Log("START SEACHIN YO");
	}
}