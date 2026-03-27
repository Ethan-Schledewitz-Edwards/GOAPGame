using BehaviourTrees;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(ActorHealth), typeof(NavMeshAgent), typeof(ActorInventory))]
public class Actor : MonoBehaviour
{
	// Constants
	private const float k_waitingForJobLimit = 10.0f;

	private const float k_followDist = 1.2f;
	private const float k_workingDist = 0.15f;

	private const float k_followSpeed = 5.2f;
	private const float k_workingSpeed = 4.5f;
	private const float k_offDutySpeed = 2f;

	private const float k_rotSpeed = 24.0f;
	public float InteractionDist { get; private set; } = 3.0f;

	[Header("Parameters")]
	[SerializeField] private LayerMask m_interactionLayers;

	// Components
	public ActorHealth ActorHealth { get; private set; }
	public ActorInventory ActorInventory { get; private set; }
	public NavMeshAgent NavAgent { get; private set; }

	// Executors
	public BehaviourTree BehaviourTree { get; private set; }  = null;
	private IGoapPlanner m_goapPlanner;

	[Header("Sensors")]
	[SerializeField] ActorSensor destinationSensor;
	[SerializeField] ActorSensor attackSensor;

	[Header("Knowledge")]
	[SerializeField] Transform m_home; // Where the actor sleeps
	[SerializeField] Transform m_foodStorage; // Where the actor sleeps
	[SerializeField] Transform m_woodStorage; // Where the actor sleeps
	[SerializeField] Transform m_stoneStorage; // Where the actor sleeps

	private GameObject m_target;
	private Vector3 m_destination;

	private ActorGoal m_lastGoal;
	private ActorGoal m_currentGoal;
	private ActionPlan m_actionPlan;
	private ActorAction m_currentAction;

	public Dictionary<string, ActorBelief> beliefs;
	public HashSet<ActorAction> actions;
	public HashSet<ActorGoal> goals;


	// System
	public int SettlementID { get; private set; } = 0;
	private EActorState m_actorState = default;
	private float m_timeFindingJob;

	private Transform m_targetFollowTransform;// Used while in the follow state
	private ActorInteractableObjectBase m_objective;

	#region Initialization

	private void Awake()
	{
		ActorHealth = GetComponent<ActorHealth>();
		ActorInventory = GetComponent<ActorInventory>();
        NavAgent = GetComponent<NavMeshAgent>();

		m_goapPlanner = new GoapPlanner();
	}

	private void Start()
	{
		SetupBeliefs();
		SetupActions();
		SetupGoals();
	}

	private void OnEnable()
	{
		destinationSensor.OnTargetChanged += HandleTargetChanged;
	}

	private void OnDisable()
	{
		destinationSensor.OnTargetChanged -= HandleTargetChanged;
	}

	private void SetupBeliefs()
	{
		beliefs = new Dictionary<string, ActorBelief>();
		BeliefFactory beliefFactory = new BeliefFactory(this, beliefs);

		beliefFactory.AddBelief("None", () => false);
		beliefFactory.AddBelief("ActorIdle", () => !NavAgent.hasPath);
		beliefFactory.AddBelief("ActorMoving", () => NavAgent.hasPath);
	}

	private void SetupActions()
	{
		actions = new HashSet<ActorAction>();

		actions.Add(new ActorAction.ActionBuilder("Relax")
			.BuildWithStrategy(new IdleStrategy(5))
			.AddEffect(beliefs["None"])
			.Build());

		actions.Add(new ActorAction.ActionBuilder("Wander")
			.BuildWithStrategy(new WanderStrategy(NavAgent, 10))
			.AddEffect(beliefs["ActorMoving"])
			.Build());
	}

	private void SetupGoals()
	{
		goals = new HashSet<ActorGoal>();

		goals.Add(new ActorGoal.GoalBuilder("Rest")
			.BuildWithPriority(1)
			.BuildWithDesiredEffect(beliefs["None"])
			.Build());

		goals.Add(new ActorGoal.GoalBuilder("Wander")
			.BuildWithPriority(1)
			.BuildWithDesiredEffect(beliefs["ActorMoving"])
			.Build());
	}

	#endregion

	public void SetState(EActorState state)
	{
		m_actorState = state;

		switch (state)
		{
			case EActorState.STATE_OffDuty:
				NavAgent.speed = k_offDutySpeed;
				break;
			case EActorState.STATE_Follow:
				NavAgent.speed = k_followSpeed;
				break;
			case EActorState.STATE_Working:
				NavAgent.speed = k_workingSpeed;
				break;
		}

		NavAgent.stoppingDistance = state == EActorState.STATE_Follow ? k_followDist : k_workingDist;

		Debug.Log($"{transform.name}'s state: { m_actorState}");
	}

	private void ClearState()
	{
		// Reset task
		if (m_objective != null)
		{
			m_objective.StopInteract();
			m_objective = null;
		}

		// Reset behaviour
		SetBehaviourTree(null);
		m_timeFindingJob = 0;

		// Try to drop item

		// Follow the player
		SetState(EActorState.STATE_OffDuty);
		NavAgent.stoppingDistance = k_workingDist;
	}

	public void SetFollowTransform(Transform newTarget)
	{
		m_targetFollowTransform = newTarget;
	}

	public void SetBehaviourTree(BehaviourTree behaviourTree)
	{
		BehaviourTree = behaviourTree;
	}

	public void SetTask(ActorInteractableObjectBase newObjective)
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
		SetBehaviourTree(m_objective.GetBehaviourTree(transform, this));
	}

    public void TickBehaviour(float t)
	{
		ActorHealth?.TickStats(t);

		HandleTaskSearch();

        if (NavAgent.enabled)
		{
			switch (m_actorState)
			{
				case EActorState.STATE_OffDuty:
					TickGoapPlanner(t);
					break;
				case EActorState.STATE_Follow:
					if(m_targetFollowTransform != null)
						NavAgent.SetDestination(m_targetFollowTransform.position);
					break;

				case EActorState.STATE_Working:
					if (BehaviourTree != null)
						BehaviourTree.TickBehaviourTree();
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

	#region GOAP

	private void HandleTargetChanged()
	{
		m_currentAction = null;
		m_currentGoal = null;
	}

	private void TickGoapPlanner(float t)
	{
		// Update the actors plan and current action if they dont have one
		if(m_currentAction == null)
		{
			CalculatePlan();

			if(m_actionPlan != null && m_actionPlan.Actions.Count > 0)
			{
				NavAgent.ResetPath();

				m_currentGoal = m_actionPlan.GoalToAcheive;
				m_currentAction = m_actionPlan.Actions.Pop();
				m_currentAction.StartAction();

				Debug.Log($"Goal: {m_currentGoal.GoalName} with {m_actionPlan.Actions.Count} actions in plan");
				Debug.Log($"Popped action: {m_currentAction.ActionName}");
			}
		}

		// If we have a current action, execute it
		if (m_actionPlan != null && m_currentAction != null) 
		{
			m_currentAction.TickAction(t);

			if (m_currentAction.Complete)
			{
				Debug.Log($"{m_currentAction.ActionName} is complete");
				m_currentAction.StopAction();

				if (m_actionPlan.Actions.Count == 0) 
				{
					Debug.Log($"{this.name}'s plan is complete!");
					m_lastGoal = m_currentGoal;
					m_currentGoal = null;
					m_currentAction = null;
				}
			}
		}
	}

	private void CalculatePlan()
	{
		float priorityLevel = m_currentGoal?.Priority ?? 0;

		HashSet<ActorGoal> goalsToCheck = goals;

		// Only check higher priority goals if the actor already has one
		if(m_currentGoal != null)
		{
			goalsToCheck = new HashSet<ActorGoal>(goals.Where(g => g.Priority > priorityLevel));
		}

		ActionPlan potentialPlan = m_goapPlanner.Plan(this, goalsToCheck, m_lastGoal);
		if (potentialPlan != null)
		{
			m_actionPlan = potentialPlan;
		}
	}
	#endregion

	#region Commands

	public void FollowPlayer(Transform Player)
	{
		// Clear the actors state
		ClearState();

		// Follow the player
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
	private ActorInteractableObjectBase SearchForTask()
	{
		ActorInteractableObjectBase closestTask = null;

		// Try to select actors
		Vector3 pos = transform.position;
		Collider[] hitColliders = Physics.OverlapSphere(pos, InteractionDist, m_interactionLayers, QueryTriggerInteraction.Collide);

		float closestDist = Mathf.Infinity;
		foreach (Collider i in hitColliders)
		{
			if (i == null)
				continue;

			// Try to get interactable component
			if (i.TryGetComponent(out ActorInteractableObjectBase aio))
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
				// Clear the actors state
				ClearState();

				return;
			}

			// Search for the nearest task
			ActorInteractableObjectBase aio = SearchForTask();

			// Set objective to the closest task.
			if (aio != null)
			{
				// Skip dead AIOs
				if (aio.TryGetComponent(out HealthComponent healthComp) &&
				healthComp.GetIsDead())
					return;

				SetTask(aio);
			}
		}
	}
	#endregion
}
