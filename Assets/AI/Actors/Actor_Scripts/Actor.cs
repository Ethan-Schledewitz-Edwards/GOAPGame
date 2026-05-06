using BehaviourTrees;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(ActorEntity), typeof(NavMeshAgent), typeof(ActorInventory))]
public class Actor : MonoBehaviour
{
	#region Constants
	private const float k_nearRange = 16.0f;
	private const float k_nearRangeSqrt = k_nearRange * k_nearRange;
	private const float k_distantRange = 32.0f;
	private const float k_distantRangeSqrt = k_distantRange * k_distantRange;

	private const float k_waitingForJobLimit = 10.0f;

	private const float k_followDist = 1.2f;
	private const float k_workingDist = 0.15f;

	private const float k_followSpeed = 5.2f;
	private const float k_workingSpeed = 4.5f;
	private const float k_offDutySpeed = 2f;

	private const float k_rotSpeed = 24.0f;
	#endregion

	// Components
	public ActorEntity ActorHealth { get; private set; }
	public ActorInventory ActorInventory { get; private set; }
	[field: SerializeField] public GameObject Mesh { get; private set; }
	public NavMeshAgent NavAgent { get; private set; }

	[Header("Parameters")]
	[SerializeField] private LayerMask m_interactionLayers;
	public float InteractionDist { get; private set; } = 3.0f;

	[Header("Simulation & Navigation")]
	private EActorSimFidelity m_simFidelity;
	private GameObject m_target;
	private Vector3 m_destination;
	private Vector3[] m_pathCorners;
	private int m_cornersPassed;// Used in non-realtime simulations
	public NavMeshPath CurrentPath { get; private set; }
	private Coroutine m_destinationCoroutine;

	// Executors
	public BehaviourTree BehaviourTree { get; private set; } = null;
	private IGoapPlanner m_goapPlanner;

	[Header("Knowledge")]
	private ActorHouseAIO m_home; // Where the actor sleeps
	private ItemStorageAIO m_foodStorage; // Where the actor sleeps
	private ItemStorageAIO m_woodStorage; // Where the actor sleeps
	private ItemStorageAIO m_stoneStorage; // Where the actor sleeps

	private ActorGoal m_lastGoal;
	private ActorGoal m_currentGoal;
	private ActionPlan m_actionPlan;
	private ActorAction m_currentAction;

	public Dictionary<string, ActorBelief> beliefs;
	public HashSet<ActorAction> actions;
	public HashSet<ActorGoal> goals;

	// System
	public int SettlementID { get; private set; } = 0;
	private int m_houseID = 0;

	private EActorState m_logicExecutorState = default;
	private float m_timeFindingJob;

	private Transform m_targetFollowTransform;// Used while in the follow state
	private ActorInteractableObjectBase m_objective;

	#region Initialization

	private void Awake()
	{
		ActorHealth = GetComponent<ActorEntity>();
		ActorInventory = GetComponent<ActorInventory>();
		NavAgent = GetComponent<NavMeshAgent>();

		m_goapPlanner = new GoapPlanner();
	}

	private void Start()
	{
		SetLogicExecutorState(EActorState.STATE_OffDuty);

		m_home = SettlementManager.Instance.WorldSettlements[SettlementID].TryFindActorHouse(m_houseID);
		m_foodStorage = SettlementManager.Instance.WorldSettlements[SettlementID].TryFindResourceStorage(2);
		m_woodStorage = SettlementManager.Instance.WorldSettlements[SettlementID].TryFindResourceStorage(0);
		m_stoneStorage = SettlementManager.Instance.WorldSettlements[SettlementID].TryFindResourceStorage(1);

		SetupBeliefs();
		SetupActions();
		SetupGoals();
	}

	private void SetupBeliefs()
	{
		beliefs = new Dictionary<string, ActorBelief>();
		BeliefFactory beliefFactory = new BeliefFactory(this, beliefs);

		beliefFactory.AddBelief("None", () => false);
		beliefFactory.AddBelief("ActorIdle", () => !NavAgent.hasPath);
		beliefFactory.AddBelief("ActorMoving", () => NavAgent.hasPath);

		beliefFactory.AddBelief("HungerLow", () => ActorHealth.Hunger < 45.0f);
		beliefFactory.AddBelief("HungerHealthy", () => ActorHealth.Hunger > 60.0f);
		beliefFactory.AddBelief("RestLow", () => ActorHealth.Rest < 20.0f);
		beliefFactory.AddBelief("RestHealthy", () => ActorHealth.Rest > 50.0f);

		beliefFactory.AddPosBelief("ActorAtFoodStorage", 3f, m_foodStorage.GetInteractionPositon());
		beliefFactory.AddPosBelief("ActortAtHome", 3f, m_home.transform.position);
	}

	private void SetupActions()
	{
		actions = new HashSet<ActorAction>();

		actions.Add(new ActorAction.ActionBuilder("Relax")
			.BuildWithStrategy(new IdleStrategy(5))
			.AddEffect(beliefs["None"])
			.Build());

		actions.Add(new ActorAction.ActionBuilder("Wander")
			.BuildWithStrategy(new WanderStrategy(this, 10))
			.AddEffect(beliefs["ActorMoving"])
			.Build());

		actions.Add(new ActorAction.ActionBuilder("MoveToFood")
			.BuildWithStrategy(new MoveStrategy(this, () => m_foodStorage.GetInteractionPositon()))
			.AddEffect(beliefs["ActorAtFoodStorage"])
			.Build());

		actions.Add(new ActorAction.ActionBuilder("Eat")
			.BuildWithStrategy(new EatStrategy(this, m_foodStorage, 5))
			.AddPrecondition(beliefs["ActorAtFoodStorage"])
			.AddEffect(beliefs["HungerHealthy"])
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

		goals.Add(new ActorGoal.GoalBuilder("KeepFed")
			.BuildWithPriority(2)
			.BuildWithDesiredEffect(beliefs["HungerHealthy"])
			.Build());
	}

	#endregion

	public void TickBehaviour(float t)
	{
		ActorHealth?.TickStats(t);

		TryTaskSearch(t);

		switch (m_logicExecutorState)
		{
			case EActorState.STATE_OffDuty:
				TickGoapPlanner(t);
				break;
			case EActorState.STATE_Follow:
				if (m_targetFollowTransform != null)
					SetActorDestination(m_targetFollowTransform.position);
				break;

			case EActorState.STATE_Working:
				if (BehaviourTree != null)
					BehaviourTree.TickBehaviourTree(t);
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
				NavAgent.speed = k_offDutySpeed;
				break;
			case EActorState.STATE_Follow:
				NavAgent.speed = k_followSpeed;
				break;
			case EActorState.STATE_SearchingForWork:
				NavAgent.speed = k_workingSpeed;
				break;
			case EActorState.STATE_Working:
				NavAgent.speed = k_workingSpeed;
				break;
		}

		NavAgent.stoppingDistance = state == EActorState.STATE_Follow ? k_followDist : k_workingDist;

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
		SetBehaviourTree(null);
		m_timeFindingJob = 0;

		// Try to drop item
		ActorInventory.Inventory.Slots[0].ClearSlot();

		SetLogicExecutorState(EActorState.STATE_OffDuty);
		NavAgent.stoppingDistance = k_workingDist;

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
		if (distToPlayerSqrt < k_nearRangeSqrt)
		{
			TrySetActorSimFidelity(EActorSimFidelity.Realtime);
		}
		else if (distToPlayerSqrt < k_distantRangeSqrt)
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
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, k_rotSpeed * t);
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
						transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, k_rotSpeed * t);
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

	#region GOAP

	private void HandleTargetChanged()
	{
		m_currentAction = null;
		m_currentGoal = null;
	}

	private void TickGoapPlanner(float t)
	{
		// If we don't have an action, try to get the next one from the CURRENT plan
		if (m_currentAction == null && m_actionPlan != null && m_actionPlan.Actions.Count > 0)
		{
			m_currentAction = m_actionPlan.Actions.Pop();

			if (m_currentAction.ActionPreconditions.All(b => b.Evaluate()))
			{
				m_currentAction.StartAction();
			}
			else
			{
				// Plan failed - preconditions not met for next step
				m_currentAction = null;
				m_actionPlan = null;
			}
		}

		// Update the actors plan and current action if they dont have one
		if (m_currentAction == null)
		{
			CalculatePlan();

			if (m_actionPlan != null && m_actionPlan.Actions.Count > 0)
			{
				// Reset path
				ClearActorDestination();

				m_currentGoal = m_actionPlan.GoalToAcheive;
				Debug.Log($"Goal: {m_currentGoal.GoalName} with {m_actionPlan.Actions.Count} actions in plan");

				m_currentAction = m_actionPlan.Actions.Pop();
				Debug.Log($"Popped action: {m_currentAction.ActionName}");

				// Verify all precodnitions
				if (m_currentAction.ActionPreconditions.All(b => b.Evaluate()))
				{
					m_currentAction.StartAction();
				}
				else
				{
					m_currentAction = null;
					m_currentGoal = null;
				}
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

				m_currentAction = null;

				if (m_actionPlan.Actions.Count == 0)
				{
					Debug.Log($"{this.name}'s plan is complete!");
					m_lastGoal = m_currentGoal;
					m_currentGoal = null;
					m_actionPlan = null;
				}
			}
		}
	}

	private void CalculatePlan()
	{
		float priorityLevel = m_currentGoal?.Priority ?? 0;

		HashSet<ActorGoal> goalsToCheck = goals;

		// Only check higher priority goals if the actor already has one
		if (m_currentGoal != null)
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
		SetActorDestination(m_objective.GetInteractionPositon());

		// Set this actors behaviour tree
		SetBehaviourTree(m_objective.GetBehaviourTree(transform, this));

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
			if (m_timeFindingJob >= k_waitingForJobLimit)
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
				if (aio.TryGetComponent(out Entity healthComp) &&
				healthComp.GetIsDead())
					return;

				SetTask(aio);
			}
		}
	}
	#endregion
}
