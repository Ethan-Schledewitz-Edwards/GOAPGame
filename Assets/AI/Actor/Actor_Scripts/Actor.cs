using BehaviourTrees;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(ActorHealthComponent), typeof(NavMeshAgent), typeof(ActorInventory))]
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
	[field: SerializeField] public GameObject Mesh { get; private set; }
	public NavMeshAgent NavAgent { get; private set; }

	[Header("Parameters")]
	public float InteractionDist { get; private set; } = 3.0f;
	[SerializeField] private LayerMask m_interactionLayers;

	[Header("Simulation & Navigation")]
	private EActorSimFidelity m_simFidelity;
	private GameObject m_target;
	private Vector3 m_destination;
	private Vector3[] m_pathCorners;
	private int m_cornersPassed;// Used in non-realtime simulations
	public NavMeshPath CurrentPath { get; private set; }
	private Coroutine m_destinationCoroutine;

	// Executors
	public BehaviourTreeExecutor BehaviourTreeExecutor { get; private set; } = null;
	public GOAPAgent GOAPAgentComp { get; private set; }

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
		NavAgent = GetComponent<NavMeshAgent>();
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
			GOAPAgentComp.OnClearDestination += ClearActorDestination;
	}

	private void OnDisable()
	{
		if (GOAPAgentComp != null)
			GOAPAgentComp.OnClearDestination -= ClearActorDestination;
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
		beliefFactory.AddBelief("ActorIdle", () => !NavAgent.hasPath);
		beliefFactory.AddBelief("ActorMoving", () => NavAgent.hasPath);

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
		ActorHealth?.TickStats(t);

		TryTaskSearch(t);

		switch (m_logicExecutorState)
		{
			case EActorState.STATE_OffDuty:
				GOAPAgentComp.TickGoapPlanner(t);
				break;
			case EActorState.STATE_Follow:
				if (m_targetFollowTransform != null)
					SetActorDestination(m_targetFollowTransform.position);
				break;

			case EActorState.STATE_Working:
				if (BehaviourTreeExecutor != null)
					BehaviourTreeExecutor.BehaviourTree.TickBehaviourTree(t);
				break;
		}

		HandleRotation(t);
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
				NavAgent.speed = c_offDutySpeed;
				break;
			case EActorState.STATE_Follow:
				NavAgent.speed = c_followSpeed;
				break;
			case EActorState.STATE_SearchingForWork:
				NavAgent.speed = c_workingSpeed;
				break;
			case EActorState.STATE_Working:
				NavAgent.speed = c_workingSpeed;
				break;
		}

		NavAgent.stoppingDistance = state == EActorState.STATE_Follow ? c_followDist : c_workingDist;

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
		BehaviourTreeExecutor.SetBehaviourTree(null);
		m_timeFindingJob = 0;

		// Try to drop item
		ActorInventory.Inventory.Slots[0].ClearSlot();

		SetLogicExecutorState(EActorState.STATE_OffDuty);
		NavAgent.stoppingDistance = c_workingDist;

		SetFollowTransform(null);
		ClearActorDestination();
	}
	#endregion

	#region Simulation Fidelity

	private void TrySetActorSimFidelity(EActorSimFidelity fidelity)
	{
		if (m_simFidelity == fidelity)
			return;

		m_simFidelity = fidelity;

		Mesh.SetActive(m_simFidelity == EActorSimFidelity.Realtime);
		NavAgent.enabled = (m_simFidelity == EActorSimFidelity.Realtime);

		// Swap preexisting path to the method used for the new simulation fidelity
		if (m_destination != Vector3.zero)
			ApplyPathingByFidelity();
	}

	public void UpdateActorSimFidelity(float distToPlayerSqrt)
	{
		if (distToPlayerSqrt < c_nearRangeSqrt)
		{
			TrySetActorSimFidelity(EActorSimFidelity.Realtime);
		}
		else if (distToPlayerSqrt < c_distantRangeSqrt)
		{
			TrySetActorSimFidelity(EActorSimFidelity.Near);
		}
		else
		{
			TrySetActorSimFidelity(EActorSimFidelity.Distant);
		}
	}
	#endregion

	#region Actor Pathing

	public void SetFollowTransform(Transform newTarget)
	{
		m_targetFollowTransform = newTarget;
	}

	public void ClearActorDestination()
	{
		if (NavAgent.isActiveAndEnabled)
			NavAgent.ResetPath();

		m_destination = Vector3.zero;
		m_pathCorners = new Vector3[0];
		m_cornersPassed = 0;

		if (m_destinationCoroutine != null)
			StopCoroutine(m_destinationCoroutine);
	}

	public void SetActorDestination(Vector3 destinationPos)
	{
		if (destinationPos == Vector3.zero)
		{
			ClearActorDestination();
			return;
		}

		// Ignore recalculating the path if it never changed
		if (destinationPos == m_destination)
			return;

		m_destination = destinationPos;

		ApplyPathingByFidelity();
	}

	/// <summary>
	/// Solves a path then then moves the Actor along it
	/// </summary>
	private void ApplyPathingByFidelity()
	{
		// Reset pathing (high-fidelity)
		if (NavAgent.isActiveAndEnabled)
			NavAgent.ResetPath();

		// Reset pathing (low-fidelity)
		if (m_destinationCoroutine != null)
			StopCoroutine(m_destinationCoroutine);

		// Reset pathing corners
		m_pathCorners = new Vector3[0];
		m_cornersPassed = 0;

		// Determine pathing solution
		switch (m_simFidelity)
		{
			case EActorSimFidelity.Realtime:
				if (NavAgent.SetDestination(m_destination))
				{
					CurrentPath = NavAgent.path;
					m_pathCorners = CurrentPath.corners;
				}
				break;

			case EActorSimFidelity.Near:
				NavMeshPath nearPath = new NavMeshPath();
				if (NavMesh.CalculatePath(transform.position, m_destination, NavMesh.AllAreas, nearPath))
				{
					CurrentPath = nearPath;
					m_pathCorners = CurrentPath.corners;
					m_destinationCoroutine = StartCoroutine(FollowPath(CurrentPath.corners, NavAgent.speed, true));
				}
				break;

			case EActorSimFidelity.Distant:
				NavMeshPath distantPath = new NavMeshPath();
				if (NavMesh.CalculatePath(transform.position, m_destination, NavMesh.AllAreas, distantPath))
				{
					CurrentPath = distantPath;
					m_pathCorners = CurrentPath.corners;
					m_destinationCoroutine = StartCoroutine(FollowPath(CurrentPath.corners, NavAgent.speed, false));
				}
				break;
		}
	}

	private void HandleRotation(float t)
	{
		if (m_objective != null)
		{
			Vector3 dirToTarget = m_objective.transform.position - transform.position;
			dirToTarget.y = 0;

			// Smoothly look at target
			Quaternion targetRotation = Quaternion.LookRotation(dirToTarget, Vector3.up);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, c_rotSpeed * t);
		}
	}

	private IEnumerator FollowPath(Vector3[] waypoints, float moveSpeed, bool isLerped)
	{
		m_cornersPassed = 0;

		for (int i = 0; i < waypoints.Length - 1; i++)
		{
			Vector3 start = waypoints[i];
			Vector3 end = waypoints[i + 1];

			float dist = Vector3.Distance(start, end);
			float travelTime = dist / moveSpeed;
			float inverseTime = 1f / travelTime;

			float t = 0.0f;
			while (t < 1.0f)
			{
				t += Time.deltaTime * inverseTime;

				if (isLerped)
				{
					transform.position = Vector3.Lerp(start, end, t);

					// Face destination
					Vector3 lookDir = end - transform.position;
					lookDir.y = 0;

					if (lookDir.sqrMagnitude > 0.1f)
					{
						Quaternion targetRotation = Quaternion.LookRotation(lookDir, Vector3.up);
						transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, c_rotSpeed * t);
					}
				}

				yield return null;
			}

			transform.position = end;
			m_cornersPassed++;

			// Face next waypoint
			if(i + 2 < waypoints.Length)
			{
				Vector3 lookDir = waypoints[i + 2] - transform.position;
				lookDir.y = 0;
				Quaternion targetRotation = Quaternion.LookRotation(lookDir, Vector3.up);
				transform.rotation = targetRotation;
			}
		}

		m_destination = Vector3.zero;
		CurrentPath = null;
		m_pathCorners = new Vector3[0];
		m_cornersPassed = 0;
		StopCoroutine(m_destinationCoroutine);
	}

	/// <summary>
	/// Calculates the distance remaining of an actors current path 
	/// </summary>
	public float PathDistRemaining()
	{
		if (CurrentPath == null)
			return 0.0f;

		// Wait for high-fidelty path to calculate
		if (m_simFidelity == EActorSimFidelity.Realtime && NavAgent.enabled)
			return NavAgent.pathPending ? float.MaxValue : NavAgent.remainingDistance;

		// Wait for path corners to calculate
		if (m_destination != Vector3.zero && (m_pathCorners == null || m_pathCorners.Length == 0))
			return float.MaxValue;

		// Use high-fidelty path distance for real time Actors
		if (m_simFidelity == EActorSimFidelity.Realtime)
			return NavAgent.remainingDistance;

		float distanceRemaining = 0.0f;
		Vector3 actorPos = transform.position;

		if (m_pathCorners.Length == 1)
			return Vector3.Distance(actorPos, m_pathCorners[0]);

		if (m_cornersPassed + 1 < m_pathCorners.Length)
		{
			// Distance to the immediate next waypoint
			distanceRemaining += Vector3.Distance(actorPos, m_pathCorners[m_cornersPassed + 1]);

			// Distance of all segments following the first waypoint
			for (int i = m_cornersPassed + 1; i < m_pathCorners.Length - 1; i++)
			{
				distanceRemaining += Vector3.Distance(m_pathCorners[i], m_pathCorners[i + 1]);
			}
		}

		return distanceRemaining;
	}

	/// <summary>
	/// Returns true if the actor has a destination but their bath is either pending when Realtime, or the corners are empty when low-fidelity.
	/// </summary>
	public bool IsCalculatingPath()
	{
		if (m_simFidelity == EActorSimFidelity.Realtime
			&& NavAgent.enabled
			&& NavAgent.hasPath
			&& NavAgent.pathPending
			)
		{
			return true;
		}
		else if (m_destination != Vector3.zero && (m_pathCorners == null || m_pathCorners.Length == 0))
		{
			return true;
		}

		return false;
	}
	#endregion

	#region Player Commands

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
		SetActorDestination(destination);
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
		SetActorDestination(m_objective.GetInteractionPositon());

		// Set this actors behaviour tree
		BehaviourTreeExecutor.SetBehaviourTree(m_objective.GetBehaviourTree(transform, BehaviourTreeExecutor));

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

		if (isJobNeeded && PathDistRemaining() < 1f)
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
