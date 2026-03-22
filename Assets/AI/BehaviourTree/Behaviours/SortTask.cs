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

		m_nodeState = EBTNodeState.STATE_RUNNING;
		return m_nodeState;
	}
}
