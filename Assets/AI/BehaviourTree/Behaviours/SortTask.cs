using BehaviourTrees;
using UnityEngine;

public class SortTask : BTNodeBase
{
	private Actor m_actorComponent;


	public SortTask(Actor actor)
	{
		m_actorComponent = actor;
	}

	public override EBTNodeState Evaluate()
	{
		base.Evaluate();

		Vector3 searchPosition = (Vector3)GetData("searchPosition");

		m_nodeState = EBTNodeState.STATE_RUNNING;
		return m_nodeState;
	}

	protected override void OnFirstEvaluate()
	{
		base.OnFirstEvaluate();

		Debug.Log("Begin item search");

		SetData("searchPosition", m_actorComponent.transform.position);
	}
}
