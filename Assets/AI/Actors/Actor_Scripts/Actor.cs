using BehaviourTrees;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(ActorStats), typeof(NavMeshAgent))]
public class Actor : MonoBehaviour
{
	// Constants
	private const float k_interactionDist = 3.0f;
	private const float k_waitingForJobLimit = 10.0f;

    private const float k_rotSpeed = 24.0f;

    // Components
    [SerializeField] private LayerMask m_interactionLayers;

    public ActorStats ActorHealth { get; private set; }
    public NavMeshAgent NavAgent { get; private set; }

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
		ActorHealth = GetComponent<ActorStats>();
        NavAgent = GetComponent<NavMeshAgent>();
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
		if (m_objective == newObjective)
			return;

		m_objective = newObjective;

		// Ignore null references
        if (newObjective == null)
            return;

        m_objective.Interact(this);
        NavAgent.SetDestination(m_objective.GetActorPositon());

        // Set this actors behaviour tree
        BehaviourTree behaviourTree = m_objective.GetBehaviourTree(transform, this);
		m_behaviourTree = behaviourTree;
    }

    public void TickBehaviour()
	{
        HandleTaskSearch();

        if (NavAgent.enabled)
		{
			switch (m_actorState)
			{
				case EActorState.STATE_OffDuty:
					break;
				case EActorState.STATE_Follow:
					if(m_targetFollowTransform != null)
						NavAgent.SetDestination(m_targetFollowTransform.position);
					break;

				case EActorState.STATE_Working:
					if (m_behaviourTree != null)
						m_behaviourTree.TickBehaviourTree();
					break;
			}
		}

		HandleRotation();
    }

	private void HandleRotation()
	{
		if(m_objective != null)
		{
            Vector3 dirToTarget = m_objective.transform.position - transform.position;
			dirToTarget.y = 0;

			// Smoothly look at target
            Quaternion targetRotation = Quaternion.LookRotation(dirToTarget, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, k_rotSpeed * Time.deltaTime);
        }
    }

	#region Commands

	public void FollowPlayer(Transform Player)
	{
		// Reset task
		if(m_objective != null)
		{
            m_objective.StopInteract();
            m_objective = null;
		}

		SetState(EActorState.STATE_Follow);
		SetFollowTransform(Player);
	}

	public void GoToDestination(Vector3 destination)
	{
		SetState(EActorState.STATE_Working);
		SetFollowTransform(null);

		NavAgent.SetDestination(destination);
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

		if (isJobNeeded && NavAgent.remainingDistance < 1)
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
			if (aio != null && 
				aio.TryGetComponent(out HealthComponent healthComp) && 
				!healthComp.GetIsDead())
			{
				SetTask(aio);
			}
		}
	}
	#endregion
}
