using BehaviourTrees;
using Factions.Core;
using Interaction.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using AI.AIContext;
using ObjectTags;

[RequireComponent(typeof(ActorHealthComponent), typeof(AIPathing), typeof(GOAPAgent))]
public class Actor : MonoBehaviour, IInteractor
{
	public const float c_JobSearchRange = 1.5f;
	private const float c_waitingForJobLimit = 2.0f;
	private const float c_followDist = 1.2f;
	private const float c_workingDist = 0.3f;
	private const float c_followSpeed = 7.2f;
	private const float c_workingSpeed = 5.8f;
	private const float c_offDutySpeed = 2f;
	private const float c_searchForJobStoppingDistance = 0.25f;
	private const float c_jobSearchCooldownDuration = 2.0f;

	[Header("Parameters")]
	[field: SerializeField] public EFaction ActorFaction { get; private set; }
	[SerializeField] private LayerMask m_interactionLayers;

	/*
		
		NOTE: The actor components should not be separate components since they are
		intrinsically linked to an actor. 
	
		Having separate components creates complex reference keeping that makes things 
		confusing and error prone.
	
		Inventory has the excuse that it could be used on other objects (like chests) in
		the future, but imo this is a bad excuse and it shouldn't be developed that way 
		unless that feature is currently in development. It's currently very linked to
		actors anyways.
	
		ActorHealthComponent is referenced by Actor, and also keeps a reference to Actor,
		functionally making it one component anyways.
		
		My suggestion:
1. 		Make ActorHealthComponent a partial class.

		File Actor.Health.cs
		public partial class Actor
		{
			health stuff
		}

		2. I would suggest making ActorInventory a regular class like this:

		File: ActorInventory.cs
		public class ActorInventory
		{
			inventory stuff
		}

		But because its behaviour seems to be very very connected to actors
		I think making it a partial class as well would be better.

		File: Actor.Inventory.cs
		public partial class Actor
		{
			inventory stuff
		}

		File Actor.cs
		public partial class Actor : Monobehaviour et al
		{
			ActorInventory m_inventory; // if regular class is chosen
		}

	*/

	// Components
	public ActorHealthComponent ActorHealth { get; private set; }
	public AIPathing AIPathing { get; private set; }
	private Collider m_collider;
	private Rigidbody m_rigidbody;

	// IInteractorProperties
	public Transform Transform => gameObject.transform;

	// Executors
	public GOAPAgent GOAPAgentComp { get; private set; }
	private BehaviourTreeExecutorBase m_behaviourTreeExecutor;

	// System
	public EActorState LogicExecutorState { get; private set; } = default;

	private float m_timeFindingJob;
	private float m_jobSearchCountdown = 0f;
	private bool m_isInvestigatingPositionForJob;
	private bool m_jobAssignedThisTick;

	private Transform m_targetTransform;
	private ActorInteractableBase m_targetInteractable;
	private InteractionPoint m_assignedInteractionPosition;

	#region Lifecycle

	private void Awake()
	{
		m_behaviourTreeExecutor = GetComponent<BehaviourTreeExecutorBase>();
		ActorHealth = GetComponent<ActorHealthComponent>();
		AIPathing = GetComponent<AIPathing>();
		m_collider = GetComponent<Collider>();
		m_rigidbody = GetComponent<Rigidbody>();
		GOAPAgentComp = GetComponent<GOAPAgent>();
	}

	private void Start()
	{
		ActorManager.Instance.TryAddActor(this);
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

	private void OnDestroy()
	{
		if (m_assignedInteractionPosition != null)
		{
			m_assignedInteractionPosition.InteractionsCanceled -= InteractionCanceled;
		}
	}

	#endregion

	public void TickBehaviour(float t)
	{
		if (AIPathing == null ||
			m_behaviourTreeExecutor == null ||
			GOAPAgentComp == null)
			return;

		if (LogicExecutorState == EActorState.Carrying)
			return;

		ActorHealth?.TickStats(t);
		AIPathing?.TickAIPathing();

		// Prevent job acquisition until the investigation destination has been reached.
		if (m_isInvestigatingPositionForJob)
		{
			if (!AIPathing.HasReachedDestination(c_searchForJobStoppingDistance))
				return;

			// Resume job searching
			m_isInvestigatingPositionForJob = false;
			AIPathing.ClearDestination();
		}

		// Tick search cooldown timer
		if (m_jobSearchCountdown > 0f)
			m_jobSearchCountdown -= t;

		if (IsJobNeeded())
		{
			if (m_jobSearchCountdown <= 0f)
				FindClosestJob(t);

			if (m_targetInteractable != null && m_assignedInteractionPosition != null)
			{
				if (m_assignedInteractionPosition.TryGetInteractionPosition(this, out Vector3 validPos))
				{
					AIPathing.SetDestination(validPos);

					// Check distance and attempt interaction directly
					float interactionDistance = m_assignedInteractionPosition.InteractionDistance;
					if (AIPathing.IsWithinDistance(validPos, interactionDistance))
					{
						UseInteractable(m_targetInteractable);
					}
				}
			}
		}
		else
		{
			switch (LogicExecutorState)
			{
				case EActorState.OffDuty:
					GOAPAgentComp.TickGoapPlanner(t);
					break;
				case EActorState.Follow:
					if (m_targetTransform != null)
						AIPathing.SetDestination(m_targetTransform.position);
					break;

				case EActorState.Working:
					if (m_behaviourTreeExecutor != null && m_behaviourTreeExecutor.CurrentBehaviourTree != null)
					{
						SyncJobStateFromContext();

						m_jobAssignedThisTick = false;
						EBTNodeState treeState = m_behaviourTreeExecutor.TickBehaviour(t);

						// Clear if the behaviour tree completed and was not replaced this frame.
						if (!m_jobAssignedThisTick &&
							(treeState == EBTNodeState.SUCSESS ||
							 treeState == EBTNodeState.FAILURE))
						{
							ClearJobState(false);
						}
					}
					break;
			}
		}

		if (m_targetTransform != null && !AIPathing.IsMoving)
			AIPathing.FaceTarget(m_targetTransform.position);
	}

	/// <summary>
	/// Setting the logic executor state determines the method used by an Actor to calculate its behaviour.
	/// Off-Duty actors use GOAP to drive emergent behaviour.
	/// Following actors try to reach a specific destination.
	/// Working actors use a behaviour tree.
	/// </summary>
	public void SetLogicExecutorState(EActorState state)
	{
		LogicExecutorState = state;

		switch (state)
		{
			case EActorState.OffDuty:
				AIPathing.SetSpeed(c_offDutySpeed);
				break;
			case EActorState.Follow:
				AIPathing.SetSpeed(c_followSpeed);
				break;
			case EActorState.SearchingForWork:
			case EActorState.Working:
				AIPathing.SetSpeed(c_workingSpeed);
				break;
			case EActorState.Carrying:
				AIPathing.SetSpeed(0);
				break;
		}

		float newStoppingDistance = state == EActorState.Follow ? c_followDist : c_workingDist;
		AIPathing.SetStoppingDistance(newStoppingDistance);
	}

	public void FollowPlayer(Transform Player)
	{
		// Clear state
		InteractionCanceled();
		ClearJobState(false);

		// Follow
		SetLogicExecutorState(EActorState.Follow);
		m_targetTransform = Player;
	}

	public void InvestigatePosition(Vector3 destination)
	{
		m_isInvestigatingPositionForJob = true;
		m_targetTransform = null;

		ClearJobState();
		SetLogicExecutorState(EActorState.SearchingForWork);

		AIPathing.SetDestination(destination);
	}

	public void TrySetActorJob(BehaviourTree behaviourTree)
	{
		// If no behaviour tree was passed, its probably intentional.
		// Certain interactions like carryables require instant interactions without behaviour trees.
		if (behaviourTree == null)
			return;

		// A new job can be acquired from inside an existing behaviour tree
		InteractionPoint previousPoint = m_behaviourTreeExecutor.AIContext.ReservedInteractionPoint;
		if (previousPoint != null && previousPoint != m_assignedInteractionPosition)
			ReleaseInteractionPosition(previousPoint);

		AIContext context = m_behaviourTreeExecutor.AIContext;
		context.TargetTransform = m_targetTransform;

		Vector3 targetDestination = m_targetTransform.position;
		if (m_assignedInteractionPosition != null &&
			m_assignedInteractionPosition.TryGetInteractionPosition(this, out Vector3 validPos))
		{
			targetDestination = validPos;
		}

		context.TargetDestination = targetDestination;
		context.ReservedInteractionPoint = m_assignedInteractionPosition;
		m_behaviourTreeExecutor.AIContext = context;

		m_behaviourTreeExecutor.SetCurrentBehaviourTree(behaviourTree);
		SetLogicExecutorState(EActorState.Working);
		m_jobAssignedThisTick = true;
	}

	public void UseInteractable(IInteractable interactable)
	{
		if(interactable is ActorInteractableBase actorInteractable)
		{
			// Recalculate the interaction position in case it moved between ticks.
			if (m_assignedInteractionPosition != null)
			{
				if (!m_assignedInteractionPosition.TryGetInteractionPosition(this, out Vector3 validPos))
					return;

				float interactionDistance = m_assignedInteractionPosition.InteractionDistance;
				if (!AIPathing.IsWithinDistance(validPos, interactionDistance))
					return;
			}

			m_targetTransform = actorInteractable.transform;

			if (m_assignedInteractionPosition != null)
			{
				m_assignedInteractionPosition.InteractionsCanceled -= InteractionCanceled;
			}

			bool isInteractionSuccessful = actorInteractable.TryInteract
			(
				this,
				transform.position,
				m_assignedInteractionPosition
			);

			if (!isInteractionSuccessful)
			{
				ClearJobState(false);
				return;
			}

			m_assignedInteractionPosition.InteractionsCanceled += InteractionCanceled;
			TrySetActorJob(actorInteractable.GetBehaviourTree());
			Debug.Log($"{transform} interacted with {actorInteractable.name} and was successful.", this);
		}
	}

	public void InteractionCanceled()
	{
		ClearJobState();
	}

	public void BeginCarrying(Transform parent, Vector3 localPosition)
	{
		if (parent != null)
		{
			SetLogicExecutorState(EActorState.Carrying);

			AIPathing.NavAgent.enabled = false;
			AIPathing.ClearDestination();

			m_collider.isTrigger = true;

			transform.parent = parent;
			transform.localPosition = localPosition;
		}
	}

	private void ReleaseInteractionPosition(InteractionPoint registeredPoint)
	{
		if (registeredPoint == null)
			return;

		registeredPoint.InteractionsCanceled -= InteractionCanceled;
		registeredPoint.ReleaseReservation(this);
		registeredPoint.TryRemoveInteractor(this);
	}

	private void ClearJobState(bool applyCooldown = false)
	{
		if (m_targetInteractable != null)
			m_targetInteractable.StopInteract(this, m_assignedInteractionPosition);

		ReleaseInteractionPosition(m_assignedInteractionPosition);

		// Reset local references
		m_targetInteractable = null;
		m_targetTransform = null;
		m_assignedInteractionPosition = null;
		m_isInvestigatingPositionForJob = false;
		m_timeFindingJob = 0;

		// Reset AI Context to avoid stale data
		if (m_behaviourTreeExecutor != null)
		{
			m_behaviourTreeExecutor.ResetContext();
			m_behaviourTreeExecutor.SetCurrentBehaviourTree(null);
		}

		transform.parent = null;
		m_collider.isTrigger = false;

		AIPathing.NavAgent.enabled = true;
		AIPathing?.ClearDestination();
		AIPathing.SetPosition(transform.position);

		if (applyCooldown)
		{
			m_jobSearchCountdown = c_jobSearchCooldownDuration;
			SetLogicExecutorState(EActorState.SearchingForWork);
		}
		else
			SetLogicExecutorState(EActorState.OffDuty);
	}

	private void SyncJobStateFromContext()
	{
		AIContext context = m_behaviourTreeExecutor.AIContext;

		Transform contextTarget = context.TargetTransform;
		InteractionPoint reservedPoint = context.ReservedInteractionPoint;
		if (contextTarget != m_targetTransform)
		{
			m_targetTransform = contextTarget;

			if (contextTarget != null)
				m_targetInteractable = contextTarget.GetComponent<ActorInteractableBase>() ??
					contextTarget.GetComponentInParent<ActorInteractableBase>();
			else
				m_targetInteractable = null;
		}

		if (reservedPoint != m_assignedInteractionPosition)
		{
			ReleaseInteractionPosition(m_assignedInteractionPosition);
			m_assignedInteractionPosition = reservedPoint;
		}
	}

	private bool IsJobNeeded()
	{
		bool isJobFinished = (LogicExecutorState == EActorState.Working &&
			m_behaviourTreeExecutor.CurrentBehaviourTree == null);

		return LogicExecutorState == EActorState.SearchingForWork || isJobFinished;
	}

	/// <summary>
	/// Searches for an actor interactable object within a radius.
	/// </summary>
	private ActorInteractableBase SearchForTask()
	{
		ActorInteractableBase closestTask = null;

		Vector3 pos = transform.position;
		Collider[] hitColliders = Physics.OverlapSphere(pos, c_JobSearchRange, m_interactionLayers, QueryTriggerInteraction.Collide);

		float closestDist = Mathf.Infinity;
		foreach (Collider i in hitColliders)
		{
			if (i == null)
				continue;

			if (i.TryGetComponent(out ActorInteractableBase actorInteractable))
			{
				if (!actorInteractable.HasWorkForInteractor(this))
					continue;

				float dist = (pos - actorInteractable.transform.position).sqrMagnitude;
				if (dist < closestDist)
				{
					closestTask = actorInteractable;
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
		if (m_assignedInteractionPosition != null)
			return;

		// Allow job search when within range of the target destination
		bool canSearchForJob = AIPathing.HasReachedDestination(c_searchForJobStoppingDistance);
		if (canSearchForJob)
		{
			m_timeFindingJob += t;

			// Stop the job search if its been too long
			if (m_timeFindingJob >= c_waitingForJobLimit)
			{
				ClearJobState(true);
				return;
			}

			// Try to reserve an interaction position, then move to it
			m_targetInteractable = SearchForTask();
			if (m_targetInteractable != null)
			{
				if (m_targetInteractable.TryReserveClosestPosition(this, transform.position, out m_assignedInteractionPosition))
				{
					m_timeFindingJob = 0;
					Debug.Log($"Found Job: {m_targetInteractable} \n Interaction Position {m_assignedInteractionPosition}.", this);
				}
			}
		}
	}
}