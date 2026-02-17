using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(ActorStats), typeof(NavMeshAgent))]
public class Actor : MonoBehaviour
{
	// Constants
	private const float k_interactionDist = 3.0f;
	private const float k_waitingForJobLimit = 10.0f;

	// Components
	[SerializeField] private LayerMask m_interactionLayers;

	private ActorStats m_actorHealth;
	private NavMeshAgent m_navAgent;

	// Executors
	private BehaviourTree m_behaviourTree = null;

	// System
	private Transform m_targetFollowTransform;
	private ActorInteractableObject_Base m_objective;

	private EActorState m_actorState = default;

	private float m_timeFindingJob;

	#region Initialization

	private void Awake()
	{
		m_actorHealth = GetComponent<ActorStats>();
		m_navAgent = GetComponent<NavMeshAgent>();
	}
	#endregion

	#region Monobehaviour Callbacks

	private void Update()
	{
		HandleTaskSearch();
	}
	#endregion

	public void SetState(EActorState state)
	{
		m_actorState = state;
	}

	public void SetFollowTransform(Transform newTarget)
	{
		m_targetFollowTransform = newTarget;
	}

	public void SetTask(ActorInteractableObject_Base newObjective)
	{
		if (newObjective == null)
			return;

		m_objective = newObjective;
		m_navAgent.SetDestination(m_objective.transform.position);
	}

	public void TickBehaviour()
	{
		if(m_actorState == EActorState.STATE_Follow && m_navAgent.enabled && 
			m_targetFollowTransform != null)
		{
			m_navAgent.SetDestination(m_targetFollowTransform.position);
		}
	}

	#region Commands

	public void FollowPlayer(Transform Player)
	{
		SetState(EActorState.STATE_Follow);
		SetFollowTransform(Player);
	}

	public void GoToDestination(Vector3 destination)
	{
		SetState(EActorState.STATE_Working);
		SetFollowTransform(null);

		m_navAgent.SetDestination(destination);
	}
	#endregion

	#region Working

	// Searches for a task within a radius
	private ActorInteractableObject_Base SearchForTask()
	{
		ActorInteractableObject_Base closestTask = null;

		// Try to select actors
		Vector3 pos = transform.position;
		Collider[] hitColliders = Physics.OverlapSphere(pos, k_interactionDist, m_interactionLayers, QueryTriggerInteraction.Collide);

		float closestDist = Mathf.Infinity;
		foreach (Collider i in hitColliders)
		{
			if (i == null)
				continue;

			// Try to get interactable component
			if (i.TryGetComponent(out ActorInteractableObject_Base aio))
			{
				float dist = Vector3.Distance(transform.position, aio.transform.position);
				if (dist < closestDist)
				{
					closestTask = aio;
					closestDist = dist;
				}
			}
		}

		return closestTask;
	}

	// Checks if this actor should be searching for a task, then attempts to assign one if needed.
	private void HandleTaskSearch()
	{
		bool isJobNeeded = m_actorState == EActorState.STATE_Working &&
			m_objective == null;

		if (isJobNeeded && m_navAgent.remainingDistance < 1)
		{
			// Track time spent searching for a job
			m_timeFindingJob += Time.deltaTime;

			// Become off-duty
			if (m_timeFindingJob >= k_waitingForJobLimit)
			{
				SetState(EActorState.STATE_OffDuty);
				m_timeFindingJob = 0;
				return;
			}

			// Search for the nearest task
			ActorInteractableObject_Base aio = SearchForTask();

			// Set objective to the closest task.
			if (aio != null)
			{
				SetTask(aio);
			}
		}
	}
	#endregion
}
