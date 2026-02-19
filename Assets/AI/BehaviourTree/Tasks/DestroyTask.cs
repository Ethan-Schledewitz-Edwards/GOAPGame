using UnityEngine;
using BehaviourTrees;

public class DestroyTask : BTNode
{
	private const int m_timeBetweenAttacks = 2;

	private Transform m_transform;
	private float m_attackTimer;

	public DestroyTask(Transform transform)
	{
		m_transform = transform;
	}

	public override EBTNodeState Evaluate()
	{
		Transform target = (Transform)GetData("target");

		m_attackTimer += Time.deltaTime;
		Debug.Log(m_attackTimer);
		if(m_attackTimer >= m_timeBetweenAttacks)
		{
			m_attackTimer = 0;
			Debug.Log("ATTACK");
		}

		bool isTargetDead = false;
		if (isTargetDead)
		{
			ClearData("target");
		}

		m_nodeState = EBTNodeState.STATE_RUNNING;
		return m_nodeState;
	}
}