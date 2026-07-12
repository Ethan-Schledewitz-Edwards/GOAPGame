using BehaviourTrees;
using InventorySystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(ActorHealthComponent), typeof(ActorInventory), typeof(AIPathing))]
public class Actor : Entity, IInteractor
{
	#region Constants
	private const float c_waitingForJobLimit = 5.0f;
	private const float c_followDist = 1.2f;
	private const float c_workingDist = 0.15f;
	private const float c_followSpeed = 5.2f;
	private const float c_workingSpeed = 4.5f;
	private const float c_offDutySpeed = 2f;
	private const float c_searchForJobRange = 3.0f;
	private const float c_searchForJobStoppingDistance = 0.25f;

	private const float c_baseInteractionDistance = 0.8f;
	#endregion

	[Header("Parameters")]
	[SerializeField] private LayerMask m_interactionLayers;

	// Components
	public Transform Transform => gameObject.transform;
	public ActorHealthComponent ActorHealth { get; private set; }
	public ActorInventory ActorInventory { get; private set; }
	public AIPathing Pathing { get; private set; }

	// Executors
	[field: SerializeField] public GOAPAgent GOAPAgentComp { get; private set; }
	public BehaviourTreeExecutorBase BehaviourTreeExecutor => m_behaviourTreeExecutor;
	private BehaviourTreeExecutorBase m_behaviourTreeExecutor;

	// Events
	public event Action<int> OnSettlementUpdated;
	public event Action<int> OnHouseUpdated;
	public event Action<float> InteractionDistanceChanged;

	// System
	public float InteractionDistance => m_interactionDistance;
	private float m_interactionDistance;

	public int SettlementID { get; private set; } = 0;
	public int HouseID { get; private set; } = 0;

	private EActorState m_logicExecutorState = default;
	private float m_timeFindingJob;
	private float m_timeIdleAtWork;

	private bool m_isInteracting;
	private Transform m_targetTransform; // Used while in the follow state

	#region Initialization

	private void Awake()
	{
		ActorHealth = GetComponent<ActorHealthComponent>();
		ActorInventory = GetComponent<ActorInventory>();
		Pathing = GetComponent<AIPathing>();
		m_behaviourTreeExecutor = GetComponent<BehaviourTreeExecutorBase>();
		GOAPAgentComp = GetComponent<GOAPAgent>();
	}

	private void Start()
	{
		SetLogicExecutorState(EActorState.STATE_OffDuty);
		SetInteractionDistance(c_baseInteractionDistance);
	}

	private void OnEnable()
	{
		if (GOAPAgentComp != null)
		{
			GOAPAgentComp.OnFoundDestination += Pathing.SetDestination;
			GOAPAgentComp.OnClearDestination += Pathing.ClearDestination;
		}
	}

	private void OnDisable()
	{
		if (GOAPAgentComp != null)
		{
			GOAPAgentComp.OnFoundDestination -= Pathing.SetDestination;
			GOAPAgentComp.OnClearDestination -= Pathing.ClearDestination;
		}
	}

	#endregion

	public void TickBehaviour(float t)
	{
		if(Pathing == null ||
			BehaviourTreeExecutor == null || 
			GOAPAgentComp == null) 
			return;

		ActorHealth?.TickStats(t);
		Pathing?.TickAIPathing();

		if(IsJobNeeded())
		{
			if (m_targetTransform == null)
				m_targetTransform = TryGetNearbyJob(t);

			if (m_targetTransform == null)
				return;

			// Interact once in range of target
			float distanceRemaining = Pathing.PathDistRemaining();
			float interactionDistance = 1f;
			if (distanceRemaining <= interactionDistance &&
				m_targetTransform.TryGetComponent(out InteractableObjectBase interactableObject))
			{
				interactableObject.TryInteract(this, true);
			}
		}
		else
		{
			switch (m_logicExecutorState)
			{
				case EActorState.STATE_OffDuty:
					GOAPAgentComp.TickGoapPlanner(t);
					break;
				case EActorState.STATE_Follow:
					if (m_targetTransform != null)
						Pathing.SetDestination(m_targetTransform.position);
					break;

				case EActorState.STATE_Working:
					if (BehaviourTreeExecutor != null &&
						BehaviourTreeExecutor.CurrentBehaviourTree != null)
					{
						BehaviourTree activeTreeBeforeTick = BehaviourTreeExecutor.CurrentBehaviourTree;

						EBTNodeState treeState = BehaviourTreeExecutor.TickBehaviour(t);

						// Reset the actor if it's BehaviourTree never changed and it either finished or timed out
						if(BehaviourTreeExecutor.CurrentBehaviourTree == activeTreeBeforeTick)
						{
							if (treeState == EBTNodeState.STATE_SUCSESS ||
								treeState == EBTNodeState.STATE_FAILURE ||
								BehaviourTreeExecutor.AIContext.GetData<bool>(AIContextKeys.c_Timeout))
							{
								ClearLogicExecutorState();
							}
						}
					}
					break;
			}
		}

		if (m_targetTransform != null)
			Pathing.HandleRotation(m_targetTransform.position);
	}

	#region Actor Knowledge

	public void SetInteractionDistance(float interactionDistance)
	{
		m_interactionDistance = interactionDistance;
		InteractionDistanceChanged?.Invoke(interactionDistance);
	}

	public void SetSettlementID(int id)
	{
		SettlementID = id;
		OnSettlementUpdated?.Invoke(SettlementID);
	}

	public void SetHouseID(int id)
	{
		HouseID = id;
		OnHouseUpdated?.Invoke(id);
	}

	#endregion

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
				Pathing.SetSpeed(c_offDutySpeed);
				break;
			case EActorState.STATE_Follow:
				Pathing.SetSpeed(c_followSpeed);
				break;
			case EActorState.STATE_SearchingForWork:
				Pathing.SetSpeed(c_workingSpeed);
				break;
			case EActorState.STATE_Working:
				Pathing.SetSpeed(c_workingSpeed);
				break;
		}

		float newStoppingDistance = state == EActorState.STATE_Follow ? c_followDist : c_workingDist;
		Pathing.SetStoppingDistance(newStoppingDistance);

		Debug.Log($"{transform.name}'s state: {m_logicExecutorState}");
	}

	private void ClearLogicExecutorState()
	{
		// Reset task
		if (m_targetTransform != null)
		{
			if(m_isInteracting &&
				m_targetTransform.TryGetComponent(out InteractableObjectBase interactableObject))
				interactableObject.StopInteract();

			m_targetTransform = null;
		}

		// Reset behaviour
		if(BehaviourTreeExecutor != null)
		{
			BehaviourTreeExecutor.SetCurrentBehaviourTree(null);
			BehaviourTreeExecutor.ResetContext();
		}
		m_timeFindingJob = 0;

		// Drop the actors held slot
		int amountToDrop = ActorInventory.HeldItemSlot.AmountInSlot;
		if (amountToDrop > 0)
			ActorInventory.Inventory.Slots[0].RemoveFromStack(amountToDrop, out var _, true, ActorInventory.DropItemTransform.position);

		SetLogicExecutorState(EActorState.STATE_OffDuty);
		Pathing.SetStoppingDistance(c_workingDist);
		Pathing.ClearDestination();
	}
	#endregion

	#region Player Commands

	public void FollowPlayer(Transform Player)
	{
		// Clear the actors state
		ClearLogicExecutorState();

		// Follow the player
		SetLogicExecutorState(EActorState.STATE_Follow);
		m_targetTransform = Player;
	}

	public void InvestigatePosition(Vector3 destination)
	{
		SetLogicExecutorState(EActorState.STATE_SearchingForWork);
		m_targetTransform = null;
		Pathing.SetDestination(destination);
	}
	#endregion

	#region BT Tasks

	/// <summary>
	/// Attempts to assign a new objective to the actor if the objective is valid and takes precedence over the current
	/// job.
	/// </summary>
	/// <remarks>
	/// The Actor is returned to the default state before any new data is assigned.
	/// </remarks>
	public void TrySetActorJob(InteractableObjectBase newObjective, bool newJobTakesPrecedence)
	{
		if (newObjective == null)
			return;

		// Ignore an incoming job if it does not take precedence
		if (BehaviourTreeExecutor.CurrentBehaviourTree != null && !newJobTakesPrecedence)
			return;

		// Set targeting
		m_targetTransform = newObjective.transform;
		m_behaviourTreeExecutor.AIContext.SetData<Transform>(AIContextKeys.c_TargetTransform, m_targetTransform);
		m_behaviourTreeExecutor.AIContext.SetData<Vector3>(AIContextKeys.c_TargetDestination, newObjective.GetInteractionPositon());

		BehaviourTreeExecutor.SetCurrentBehaviourTree(newObjective.GetBehaviourTree());
		SetLogicExecutorState(EActorState.STATE_Working);
	}

	public void OnInteractWithObject(InteractableObjectBase actorInteractableObjectBase, bool takesPriority)
	{
		TrySetActorJob(actorInteractableObjectBase, takesPriority);
	}

	/// <summary>
	/// Checks if this actor should be searching for a job
	/// </summary>
	private bool IsJobNeeded()
	{
		bool isJobFinished = (m_logicExecutorState == EActorState.STATE_Working &&
			BehaviourTreeExecutor.CurrentBehaviourTree == null);

		return m_logicExecutorState == EActorState.STATE_SearchingForWork || 
			isJobFinished;
	}

	/// <summary>
	/// Searches for an actor interactable object within a radius
	/// </summary>
	private InteractableObjectBase SearchForTask()
	{
		InteractableObjectBase closestTask = null;

		// Try to select actors
		Vector3 pos = transform.position;
		Collider[] hitColliders = Physics.OverlapSphere(pos, c_searchForJobRange, m_interactionLayers, QueryTriggerInteraction.Collide);

		float closestDist = Mathf.Infinity;
		foreach (Collider i in hitColliders)
		{
			if (i == null)
				continue;

			// Try to get interactable component
			if (i.TryGetComponent(out InteractableObjectBase aio))
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

	/// <summary>
	/// Searches for the nearest available job and updates the actor's duty state based on the time spent searching.
	/// </summary>
	/// <param name="t">The time increment to add to the job search timer.</param>
	/// <returns>The transform of the nearest interactable object if found; otherwise, null.</returns>
	private Transform TryGetNearbyJob(float t)
	{
		bool canSearchForJob = true;

		// Only allow travelling actors to job search when close to their destination
		if (Pathing.HasPath)
		{
			float distanceRemaining = Pathing.PathDistRemaining();
			float stoppingDistance = c_searchForJobStoppingDistance;

			if (distanceRemaining > stoppingDistance)
				canSearchForJob = false;
		}

		if (canSearchForJob)
		{
			// Track time spent searching for a job
			m_timeFindingJob += t;

			// Become off-duty
			if (m_timeFindingJob >= c_waitingForJobLimit)
			{
				// Clear the actors state
				ClearLogicExecutorState();
				return null;
			}

			// Search for the nearest task
			InteractableObjectBase aio = SearchForTask();
			if (aio != null)
			{
				m_timeFindingJob = 0;
				Pathing.SetDestination(aio.GetInteractionPositon());
				return aio.transform;
			}
		}

		return null;
	}
	#endregion
}
