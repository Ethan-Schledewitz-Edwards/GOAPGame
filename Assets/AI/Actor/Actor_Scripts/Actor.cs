using BehaviourTrees;
using Entities.Core;
using Entities.Savable;
using Factions.Core;
using InventorySystem;
using SaveLoad.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(ActorHealthComponent), typeof(ActorInventory), typeof(AIPathing))]
public class Actor : Entity, IInteractor, ISaveableComponent
{
	private const float c_waitingForJobLimit = 5.0f;
	private const float c_followDist = 1.2f;
	private const float c_workingDist = 0.15f;
	private const float c_followSpeed = 6.2f;
	private const float c_workingSpeed = 4.5f;
	private const float c_offDutySpeed = 2f;
	private const float c_searchForJobRange = 3.0f;
	private const float c_searchForJobStoppingDistance = 0.25f;
	private const float c_jobSearchCooldownDuration = 2.0f;
	private const float c_baseInteractionDistance = 0.8f;

	[Header("Parameters")]
	[field: SerializeField] public EFaction ActorFaction { get; private set; }
	[SerializeField] private LayerMask m_interactionLayers;

	// Components
	public Transform Transform => gameObject.transform;
	public ActorHealthComponent ActorHealth { get; private set; }
	public ActorInventory ActorInventory { get; private set; }
	public AIPathing Pathing { get; private set; }

	// Executors
	[field: SerializeField] public GOAPAgent GOAPAgentComp { get; private set; }
	private BehaviourTreeExecutorBase m_behaviourTreeExecutor;

	// Events
	public event Action<int> OnSettlementUpdated;
	public event Action<float> InteractionDistanceChanged;

	// System Properties
	public float InteractionDistance => m_interactionDistance;
	public int SettlementID { get; private set; } = 0; // Settlement ID actor inhabits
	public int WorkstationID { get; private set; } = 0; // Structure ID actor resides in

	// Internal State
	private EActorState m_logicExecutorState = default;
	private float m_interactionDistance;
	private int m_jobAssignmentID = 0;
	private float m_timeFindingJob;
	private float m_jobSearchCooldown = 0f;

	private Transform m_targetTransform;
	private InteractableObjectBase m_targetInteractable;
	private InteractionPosition m_assignedInteractionPosition;

	#region Lifecycle

	private void Awake()
	{
		ActorHealth = GetComponent<ActorHealthComponent>();
		ActorInventory = GetComponent<ActorInventory>();
		m_behaviourTreeExecutor = GetComponent<BehaviourTreeExecutorBase>();
		GOAPAgentComp = GetComponent<GOAPAgent>();
		Pathing = GetComponent<AIPathing>();
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
		if (Pathing == null || m_behaviourTreeExecutor == null || GOAPAgentComp == null)
			return;

		ActorHealth?.TickStats(t);
		Pathing?.TickAIPathing();

		// Tick search cooldown timer
		if (m_jobSearchCooldown > 0f)
		{
			m_jobSearchCooldown -= t;
		}

		if (IsJobNeeded())
		{
			if (m_jobSearchCooldown <= 0f)
				FindClosestJob(t);

			if (m_targetInteractable != null && m_assignedInteractionPosition != null)
			{
				// Dynamically update pathing
				if (Pathing.HasPath)
				{
					if (m_assignedInteractionPosition.TryGetInteractionPosition(this, out Vector3 validPos))
					{
						Pathing.SetDestination(validPos);
					}
					else // Reservation became null or unauthorized
					{
						HandleFailedInteraction();
						return;
					}
				}

				float distanceRemaining = Pathing.PathDistRemaining();
				if (distanceRemaining <= m_interactionDistance)
					InteractWith(m_targetInteractable, true);
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
					if (m_behaviourTreeExecutor != null &&
						m_behaviourTreeExecutor.CurrentBehaviourTree != null)
					{
						if (m_assignedInteractionPosition != null)
						{
							if (m_assignedInteractionPosition.TryGetInteractionPosition(this, out Vector3 validPos))
							{
								Pathing.SetDestination(validPos);
							}
							else
							{
								ClearJob(); // Lost spot, abandon job
								DropHeldItem();
								return;
							}
						}

						// Tick the behaviour tree
						int jobAssignmentBeforeTick = m_jobAssignmentID;
						EBTNodeState treeState = m_behaviourTreeExecutor.TickBehaviour(t);

						// Reset if the tree finished and we are still on the same job
						if (m_jobAssignmentID == jobAssignmentBeforeTick)
						{
							if (treeState == EBTNodeState.STATE_SUCSESS ||
								treeState == EBTNodeState.STATE_FAILURE)
							{
								ClearJob();
								DropHeldItem();
							}
						}
					}
					break;
			}
		}

		if (m_targetTransform != null && !Pathing.IsMoving)
			Pathing.FaceTarget(m_targetTransform.position);
	}

	public void SetInteractionDistance(float interactionDistance)
	{
		m_interactionDistance = interactionDistance;
		InteractionDistanceChanged?.Invoke(interactionDistance);
	}

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
			case EActorState.STATE_Working:
				Pathing.SetSpeed(c_workingSpeed);
				break;
		}

		float newStoppingDistance = state == EActorState.STATE_Follow ? c_followDist : c_workingDist;
		Pathing.SetStoppingDistance(newStoppingDistance);

		Debug.Log($"{transform.name}'s state: {m_logicExecutorState}");
	}

	public void FollowPlayer(Transform Player)
	{
		// Clear state
		ClearJob();
		DropHeldItem();
		m_behaviourTreeExecutor.ResetContext();

		// Follow
		SetLogicExecutorState(EActorState.STATE_Follow);
		m_targetTransform = Player;
	}

	public void InvestigatePosition(Vector3 destination)
	{
		SetLogicExecutorState(EActorState.STATE_SearchingForWork);
		m_targetTransform = null;
		Pathing.SetDestination(destination);
	}

	public void InteractWith(InteractableObjectBase actorInteractableObjectBase, bool willReplaceJob)
	{
		bool isInteractionSuccessful = actorInteractableObjectBase.TryInteract
		(
			this,
			transform.position,
			out InteractionPosition interactionPosition,
			out int interactorValue
		);

		if (!isInteractionSuccessful)
		{
			HandleFailedInteraction();
			return;
		}

		m_assignedInteractionPosition = interactionPosition;
		if (willReplaceJob)
		{
			m_targetTransform = actorInteractableObjectBase.transform;
			TrySetActorJob(actorInteractableObjectBase.GetBehaviourTree());
		}
	}

	private void TrySetActorJob(BehaviourTree behaviourTree)
	{
		if (behaviourTree == null)
			return;

		if (m_behaviourTreeExecutor.CurrentBehaviourTree != null)
			ClearJob();

		m_jobAssignmentID++;

		m_behaviourTreeExecutor.AIContext.SetData<Transform>(AIContextKeys.c_TargetTransform, m_targetTransform);
		Vector3 targetDestination = m_targetTransform.position;
		if (m_assignedInteractionPosition != null && m_assignedInteractionPosition.TryGetInteractionPosition(this, out Vector3 validPos))
		{
			targetDestination = validPos;
		}

		m_behaviourTreeExecutor.AIContext.SetData<Vector3>(AIContextKeys.c_TargetDestination, targetDestination);
		m_behaviourTreeExecutor.SetCurrentBehaviourTree(behaviourTree);
		SetLogicExecutorState(EActorState.STATE_Working);
	}

	private void SetTargetInteractable(InteractableObjectBase newTarget)
	{
		if (m_targetInteractable == newTarget)
			return;

		// Unsubscribe from the old target
		if (m_targetInteractable != null)
		{
			m_targetInteractable.InteractableBecameInvalid -= ClearJob;
		}

		m_targetInteractable = newTarget;

		// Subscribe to the new target
		if (m_targetInteractable != null)
		{
			m_targetInteractable.InteractableBecameInvalid += ClearJob;
		}
	}

	private void ClearJob()
	{
		if (m_assignedInteractionPosition != null)
		{
			m_assignedInteractionPosition.ReleaseReservation(this);
			m_assignedInteractionPosition.TryRemoveInteractor(this);
		}

		SetTargetInteractable(null);
		m_targetTransform = null;
		m_assignedInteractionPosition = null;

		m_timeFindingJob = 0;
		m_behaviourTreeExecutor.SetCurrentBehaviourTree(null);
		SetLogicExecutorState(EActorState.STATE_OffDuty);
		Pathing.SetStoppingDistance(c_workingDist);
		Pathing.ClearDestination();
	}

	private void HandleFailedInteraction()
	{
		if (m_assignedInteractionPosition != null)
		{
			m_assignedInteractionPosition.ReleaseReservation(this);
			m_assignedInteractionPosition.TryRemoveInteractor(this);
		}

		SetTargetInteractable(null);
		m_assignedInteractionPosition = null;
		Pathing.ClearDestination();

		m_jobSearchCooldown = c_jobSearchCooldownDuration;
	}

	/// <summary>
	/// Drops all items currently held in the actor's inventory slot.
	/// </summary>
	private void DropHeldItem()
	{
		int amountToDrop = ActorInventory.HeldItemSlot.AmountInSlot;
		if (amountToDrop > 0)
			ActorInventory.Inventory.Slots[0].RemoveFromStack(amountToDrop, out var _, true, ActorInventory.DropItemTransform.position);
	}

	/// <summary>
	/// Checks if this actor should be searching for a job.
	/// </summary>
	private bool IsJobNeeded()
	{
		bool isJobFinished = (m_logicExecutorState == EActorState.STATE_Working &&
			m_behaviourTreeExecutor.CurrentBehaviourTree == null);

		return m_logicExecutorState == EActorState.STATE_SearchingForWork || isJobFinished;
	}

	/// <summary>
	/// Searches for an actor interactable object within a radius.
	/// </summary>
	private InteractableObjectBase SearchForTask()
	{
		InteractableObjectBase closestTask = null;

		Vector3 pos = transform.position;
		Collider[] hitColliders = Physics.OverlapSphere(pos, c_searchForJobRange, m_interactionLayers, QueryTriggerInteraction.Collide);

		float closestDist = Mathf.Infinity;
		foreach (Collider i in hitColliders)
		{
			if (i == null) continue;

			if (i.TryGetComponent(out InteractableObjectBase aio))
			{
				if (aio.IsAtActorCapacity())
					continue;

				float dist = Vector3.Distance(pos, aio.transform.position);
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
	private void FindClosestJob(float t)
	{
		bool canSearchForJob = true;

		// Only allow travelling actors to job search when close to their destination
		if (Pathing.HasPath)
		{
			float distanceRemaining = Pathing.PathDistRemaining();
			if (distanceRemaining > c_searchForJobStoppingDistance)
				canSearchForJob = false;
		}

		if (canSearchForJob)
		{
			m_timeFindingJob += t;

			// Stop the job search if its been too long
			if (m_timeFindingJob >= c_waitingForJobLimit)
			{
				ClearJob();
				DropHeldItem();
				m_behaviourTreeExecutor.ResetContext();
				return;
			}

			InteractableObjectBase foundTask = SearchForTask();
			SetTargetInteractable(foundTask);

			// Try to reserve an interaction position, then move to it
			if (m_targetInteractable != null)
			{
				if (m_targetInteractable.TryReserveClosestPosition(this, transform.position, out m_assignedInteractionPosition))
					m_timeFindingJob = 0;
			}
		}
	}

	#region ISaveableComponent Implementation

	public string GetComponentId() => "Actor";

	public object GenerateComponentData()
	{
		return new ActorSaveData
		{
			LogicState = m_logicExecutorState,
			IsFollowingPlayer = (m_logicExecutorState == EActorState.STATE_Follow),
			SettlementID = this.SettlementID,
			WorkstationID = this.WorkstationID
		};
	}

	public void RestoreComponentData(object data)
	{
		if (data is ActorSaveData actorData)
		{
			if (actorData.IsFollowingPlayer)
				FollowPlayer(GameManager.Instance.PlayerObject.transform);
		}
	}

	[System.Serializable]
	public class ActorSaveData
	{
		public EActorState LogicState;
		public bool IsFollowingPlayer;
		public int SettlementID;
		public int WorkstationID;
	}

	#endregion
}
