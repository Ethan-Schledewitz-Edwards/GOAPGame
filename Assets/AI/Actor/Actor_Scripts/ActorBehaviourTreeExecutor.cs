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

		AIContext.ExecutorTransform = transform;
		AIContext.ExecutorFaction = (int)m_actor.ActorFaction;
		AIContext.JobSearchDistance = Actor.c_JobSearchRange;
		AIContext.InteractionLayer = 1 << LayerMask.NameToLayer("Interaction");
		AIContext.AttackDamage = 1; // Hard coded for now
	}
}
