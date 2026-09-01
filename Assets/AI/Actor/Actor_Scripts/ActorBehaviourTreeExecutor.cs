using Factions.Core;
using UnityEngine;

[RequireComponent(typeof(Actor))]
public class ActorBehaviourTreeExecutor : BehaviourTreeExecutorBase
{
	// Components
	private Actor m_actor;

	// System
	private float m_interactionDistance;

	protected override void Awake()
	{
		m_actor = GetComponent<Actor>();
		m_actor.InteractionDistanceChanged += SetInteractionDistance;

		base.Awake();
	}

	private void OnDestroy()
	{
		if(m_actor != null)
			m_actor.InteractionDistanceChanged -= SetInteractionDistance;
	}

	private void SetInteractionDistance(float interactionDistance)
	{
		m_interactionDistance = interactionDistance;
		AIContext.SetData<float>(AIContextKeys.c_InteractionDistance, interactionDistance);
	}

	public override void ResetContext()
	{
		base.ResetContext();

		AIContext.SetData<Transform>(AIContextKeys.c_ExecutorTransform, transform);
		AIContext.SetData<int>(AIContextKeys.c_InteractionLayer, 1 << LayerMask.NameToLayer("Interaction"));
		AIContext.SetData<EFaction>(AIContextKeys.c_ExecutorFaction, m_actor.ActorFaction);
		SetInteractionDistance(m_actor.InteractionDistance);
	}
}
