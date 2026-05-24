using BehaviourTrees;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(ActorHealthComponent), typeof(ActorInventory), typeof(AIPathing))]
public class Actor : Entity, IInteractor
{
	#region Constants
	private const float c_nearRange = 16.0f;
	private const float c_nearRangeSqrt = c_nearRange * c_nearRange;
	private const float c_distantRange = 32.0f;
	private const float c_distantRangeSqrt = c_distantRange * c_distantRange;

	private const float c_waitingForJobLimit = 10.0f;

	private const float c_followDist = 1.2f;
	private const float c_workingDist = 0.15f;

	private const float c_followSpeed = 5.2f;
	private const float c_workingSpeed = 4.5f;
	private const float c_offDutySpeed = 2f;

	private const float c_rotSpeed = 24.0f;
	#endregion

	// Components
	public ActorHealthComponent ActorHealth { get; private set; }
	public ActorInventory ActorInventory { get; private set; }
	public InventoryComponent InventoryComponent => ActorInventory;
	public AIPathing AIPathing { get; private set; }

	[Header("Parameters")]
	public float InteractionDist { get; private set; } = 3.0f;
	[SerializeField] private LayerMask m_interactionLayers;

	// Executors
	[field: SerializeField] public BehaviourTreeExecutor BehaviourTreeExecutor { get; private set; } = null;
	[field: SerializeField] public GOAPAgent GOAPAgentComp { get; private set; }

	// System
	public int SettlementID { get; private set; } = 0;

	private int m_houseID = 0;

	private EActorState m_logicExecutorState = default;
	private float m_timeFindingJob;

	private Transform m_targetFollowTransform; // Used while in the follow state
	private ActorInteractableObjectBase m_objective;

	#region Initialization

	private void Awake()
	{
		ActorHealth = GetComponent<ActorHealthComponent>();
		ActorInventory = GetComponent<ActorInventory>();
		AIPathing = GetComponent<AIPathing>();
		BehaviourTreeExecutor = GetComponent<BehaviourTreeExecutor>();
		GOAPAgentComp = GetComponent<GOAPAgent>();
	}

	private void Start()
	{
		SetLogicExecutorState(EActorState.STATE_OffDuty);
		UpdateActorKnowledge();
	}

	private void OnEnable()
	{
		if (GOAPAgentComp != null)
		{
			GOAPAgentComp.OnFoundDestination += AIPathing.SetDestination;
			GOAPAgentComp.OnClearDestination += AIPathing.ClearDestination;
		}
	}

	private void OnDisable()
	{
		if (GOAPAgentComp != null)
		{
			GOAPAgentComp.OnFoundDestination -= AIPathing.SetDestination;
			GOAPAgentComp.OnClearDestination -= AIPathing.ClearDestination;
		}
	}

	private void UpdateActorKnowledge()
	{
		if (GOAPAgentComp != null)
		{
			UpdateBeliefs();
			UpdateActions();
			UpdateGoals();
		}
	}

	private void UpdateBeliefs()
	{
		Dictionary<string, AgentBelief> beliefs = new Dictionary<string, AgentBelief>();
		BeliefFactory beliefFactory = new BeliefFactory(GOAPAgentComp, beliefs);

		beliefFactory.AddBelief("None", () => false);
		beliefFactory.AddBelief("ActorIdle", () => !AIPathing.NavAgent.hasPath);
		beliefFactory.AddBelief("ActorMoving", () => AIPathing.NavAgent.hasPath);

		beliefFactory.AddBelief("HungerLow", () => ActorHealth.Hunger < 45.0f);
		beliefFactory.AddBelief("HungerHealthy", () => ActorHealth.Hunger > 60.0f);
		beliefFactory.AddBelief("RestLow", () => ActorHealth.Rest < 20.0f);
		beliefFactory.AddBelief("RestHealthy", () => ActorHealth.Rest > 50.0f);

		//beliefFactory.AddPosBelief("ActorAtFoodStorage", 3f, m_foodStorage.GetInteractionPositon());
		//beliefFactory.AddPosBelief("ActortAtHome", 3f, m_home.transform.position);

		GOAPAgentComp.UpdateBeliefs(beliefs);
	}

	private void UpdateActions()
	{
		HashSet<AgentAction> actions = new HashSet<AgentAction>();

		//actions.Add(new AgentAction.ActionBuilder("Relax")
		//	.BuildWithStrategy(new IdleStrategy(5))
		//	.AddEffect(GOAPAgentComp.Beliefs["None"])
		//	.Build());

		//actions.Add(new AgentAction.ActionBuilder("Wander")
		//	.BuildWithStrategy(new WanderStrategy(this, 10))
		//	.AddEffect(GOAPAgentComp.Beliefs["ActorMoving"])
		//	.Build());

		//actions.Add(new AgentAction.ActionBuilder("MoveToFood")
		//	.BuildWithStrategy(new MoveStrategy(this, () => m_foodStorage.GetInteractionPositon()))
		//	.AddEffect(GOAPAgentComp.Beliefs["ActorAtFoodStorage"])
		//	.Build());

		//actions.Add(new AgentAction.ActionBuilder("Eat")
		//	.BuildWithStrategy(new EatStrategy(this, m_foodStorage, 5))
		//	.AddPrecondition(GOAPAgentComp.Beliefs["ActorAtFoodStorage"])
		//	.AddEffect(GOAPAgentComp.Beliefs["HungerHealthy"])
		//	.Build());

		GOAPAgentComp.UpdateActions(actions);
	}

	private void UpdateGoals()
	{
		HashSet<AgentGoal> goals = new HashSet<AgentGoal>();

		goals.Add(new AgentGoal.GoalBuilder("Rest")
			.BuildWithPriority(1)
			.BuildWithDesiredEffect(GOAPAgentComp.Beliefs["None"])
			.Build());

		goals.Add(new AgentGoal.GoalBuilder("Wander")
			.BuildWithPriority(1)
			.BuildWithDesiredEffect(GOAPAgentComp.Beliefs["ActorMoving"])
			.Build());

		goals.Add(new AgentGoal.GoalBuilder("KeepFed")
			.BuildWithPriority(2)
			.BuildWithDesiredEffect(GOAPAgentComp.Beliefs["HungerHealthy"])
			.Build());

		GOAPAgentComp.UpdateGoals(goals);
	}

	#endregion

	public void TickBehaviour(float t)
	{
		if(AIPathing == null ||
			BehaviourTreeExecutor == null || 
			GOAPAgentComp == null) 
			return;

		ActorHealth?.TickStats(t);

		TryTaskSearch(t);

		switch (m_logicExecutorState)
		{
			case EActorState.STATE_OffDuty:
				GOAPAgentComp.TickGoapPlanner(t);
				break;
			case EActorState.STATE_Follow:
				if (m_targetFollowTransform != null)
					AIPathing.SetDestination(m_targetFollowTransform.position);
				break;

			case EActorState.STATE_Working:
				if (BehaviourTreeExecutor != null)
					BehaviourTreeExecutor.BehaviourTree.TickBehaviourTree(t);
				break;
		}

		AIPathing.HandleRotation(m_objective.transform.position, t);
	}

	#region Actor Logic Executor

	/// <summary>
	/// Setting the logic executor state determines the method used by an Actor to calculate its behaviour.
	/// Off-Duty actors use GOAP to drive emergent behaviour.
	/// Following actors try to reach a specific destination.
	/// Working actors use a behaviour tree.
	/// </summary>
	public void SetLogicExecutorState(EActorState state)
	{
		m_logicExecutorState = state;

		switch (state)
		{
			case EActorState.STATE_OffDuty:
				AIPathing.NavAgent.speed = c_offDutySpeed;
				break;
			case EActorState.STATE_Follow:
				AIPathing.NavAgent.speed = c_followSpeed;
				break;
			case EActorState.STATE_SearchingForWork:
				AIPathing.NavAgent.speed = c_workingSpeed;
				break;
			case EActorState.STATE_Working:
				AIPathing.NavAgent.speed = c_workingSpeed;
				break;
		}

		AIPathing.NavAgent.stoppingDistance = state == EActorState.STATE_Follow ? c_followDist : c_workingDist;

		Debug.Log($"{transform.name}'s state: {m_logicExecutorState}");
	}

	private void ClearLogicExecutorState()
	{
		// Reset task
		if (m_objective != null)
		{
			m_objective.StopInteract();
			m_objective = null;
		}

		// Reset behaviour
		BehaviourTreeExecutor?.SetBehaviourTree(null);
		m_timeFindingJob = 0;

		// Try to drop item
		ActorInventory.Inventory.Slots[0].ClearSlot();

		SetLogicExecutorState(EActorState.STATE_OffDuty);
		AIPathing.NavAgent.stoppingDistance = c_workingDist;

		SetFollowTransform(null);
		AIPathing.ClearDestination();
	}
	#endregion

	#region Player Commands

	public void SetFollowTransform(Transform newTarget)
	{
		m_targetFollowTransform = newTarget;
	}

	public void FollowPlayer(Transform Player)
	{
		// Clear the actors state
		ClearLogicExecutorState();

		// Follow the player
		SetLogicExecutorState(EActorState.STATE_Follow);
		SetFollowTransform(Player);
	}

	public void InvestigatePosition(Vector3 destination)
	{
		SetLogicExecutorState(EActorState.STATE_SearchingForWork);
		SetFollowTransform(null);
		AIPathing.SetDestination(destination);
	}
	#endregion

	#region BT Tasks

	public void SetTask(ActorInteractableObjectBase newObjective)
	{
		if (m_objective == newObjective)
			return;

		m_objective = newObjective;

		// Ignore null references
		if (newObjective == null)
			return;

		m_objective.Interact(this);
		AIPathing.SetDestination(m_objective.GetInteractionPositon());

		// Set this actors behaviour tree
		BehaviourTreeExecutor?.SetBehaviourTree(m_objective.GetBehaviourTree(transform, BehaviourTreeExecutor));

		SetLogicExecutorState(EActorState.STATE_Working);
	}

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
	private void TryTaskSearch(float t)
	{
		bool isJobNeeded = m_logicExecutorState == EActorState.STATE_SearchingForWork &&
			m_objective == null;

		if (isJobNeeded && AIPathing.PathDistRemaining() < 1f)
		{
			// Track time spent searching for a job
			m_timeFindingJob += t;

			// Become off-duty
			if (m_timeFindingJob >= c_waitingForJobLimit)
			{
				// Clear the actors state
				ClearLogicExecutorState();
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
