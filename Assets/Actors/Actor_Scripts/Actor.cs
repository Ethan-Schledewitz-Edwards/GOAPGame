using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(ActorStats), typeof(NavMeshAgent))]
public class Actor : MonoBehaviour
{

	private ActorStats m_actorHealth;
	private NavMeshAgent m_navAgent;

	private Transform m_target;
	public ActorInteractableObject objective;

	[HideInInspector]
	public EActorState state = default;

	private void Awake()
	{
		m_actorHealth = GetComponent<ActorStats>();
		m_navAgent = GetComponent<NavMeshAgent>();
	}

	public void SetTarget(Transform newTarget)
	{
		m_target = newTarget;
	}

	public void TickBehaviour()
	{
		if (m_navAgent.enabled && m_target != null)
			m_navAgent.SetDestination(m_target.position);
	}
}
