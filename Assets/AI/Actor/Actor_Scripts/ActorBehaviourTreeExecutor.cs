using Factions.Core;
using UnityEngine;

[RequireComponent(typeof(Actor))]
public class ActorBehaviourTreeExecutor : BehaviourTreeExecutorBase
{
	// Components
	private Actor m_actor;

	protected override void Awake()
	{
		m_actor = GetComponent<Actor>();

		base.Awake();
	}

	public override void ResetContext()
	{
		base.ResetContext();

		AIContext.SetData<Transform>(AIContextKeys.c_ExecutorTransform, transform);
		AIContext.SetData<int>(AIContextKeys.c_InteractionLayer, 1 << LayerMask.NameToLayer("Interaction"));
		AIContext.SetData<EFaction>(AIContextKeys.c_ExecutorFaction, m_actor.ActorFaction);
		AIContext.SetData<float>(AIContextKeys.c_InteractionDistanceSqrt, m_actor.InteractionDistanceSqrt);
		AIContext.SetData<float>(AIContextKeys.c_JobSearchRange, m_actor.JobSearchRange);
	}
}
