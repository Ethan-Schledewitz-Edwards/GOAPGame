using BehaviourTrees;
using UnityEngine;
using UnityEngine.AI;

public class MoveToTargetDataTask : BTNodeBase
{
	private Actor m_actorComponent;
	private Transform m_actorTransform;

	/// <summary>
	/// Creates a task which is used to move an actor to their "target" data
	/// </summary>
	/// <param name="actorTransform">The actors transform</param>
	/// <param name="navMeshAgent">The actors nav agent component</param>
	public MoveToTargetDataTask(Actor actorComponent, Transform actorTransform)
	{
		m_actorComponent = actorComponent;
		m_actorTransform = actorTransform;
	}

	public override EBTNodeState Evaluate()
	{
		base.Evaluate();

		Transform target = (Transform)GetData("target");

		if (target != null) 
		{
			// Set destination if not within range
			if (Vector3.Distance(m_actorTransform.position, target.position) > 0.1f)
			{
				m_actorComponent.NavAgent.SetDestination(target.position);
			}
			else
			{
				// Within range
				m_nodeState = EBTNodeState.STATE_SUCSESS;
				return m_nodeState;
			}
		}
		else
		{
			m_nodeState = EBTNodeState.STATE_FAILURE;
			return m_nodeState;
		}

		m_nodeState = EBTNodeState.STATE_RUNNING;
		return m_nodeState;
	}
}
