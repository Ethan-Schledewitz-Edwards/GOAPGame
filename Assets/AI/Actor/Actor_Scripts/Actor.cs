using BehaviourTrees;
using Factions.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(ActorHealthComponent), typeof(AIPathing))]
public class Actor : MonoBehaviour, IInteractor
{
	private const float c_waitingForJobLimit = 2.0f;
	private const float c_followDist = 1.2f;
	private const float c_workingDist = 0.3f;
	private const float c_followSpeed = 7.2f;
	private const float c_workingSpeed = 5.8f;
	private const float c_offDutySpeed = 2f;
	private const float c_searchForJobRange = 1.5f;
	private const float c_searchForJobStoppingDistance = 0.25f;
	private const float c_jobSearchCooldownDuration = 2.0f;
	private const float c_interactionDistance = 0.3f;

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
		1. Make ActorHealthComponent a partial class.

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
	public Transform Transform => gameObject.transform;

	// Executors
	[field: SerializeField] public GOAPAgent GOAPAgentComp { get; private set; }
	private BehaviourTreeExecutorBase m_behaviourTreeExecutor;

	// Events
	public event Action<int> OnSettlementUpdated;

	// System Properties
	public float JobSearchRange => c_searchForJobRange;
	public float InteractionDistanceSqrt { get; private set; }
	public int SettlementID { get; private set; } = 0; // Settlement ID actor inhabits

	// Internal State
	public EActorState LogicExecutorState { get; private set; } = default;

	private float m_timeFindingJob;
	private float m_jobSearchCooldown = 0f;
	private bool m_isInvestigating;	// NOTE: I assume this is an unused feature? It can never be set to true and should be removed.
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
		GOAPAgentComp = GetComponent<GOAPAgent>();
		InteractionDistanceSqrt = c_interactionDistance * c_interactionDistance;
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
		if (m_isInvestigating)
		{
			if (!AIPathing.HasReachedDestination(c_searchForJobStoppingDistance))
				return;

			// Resume job searching
			m_isInvestigating = false;
			AIPathing.ClearDestination();
		}

		// Tick search cooldown timer
		if (m_jobSearchCooldown > 0f)
			m_jobSearchCooldown -= t;

		if (IsJobNeeded())
		{
			if (m_jobSearchCooldown <= 0f)
				FindClosestJob(t);

			if (m_targetInteractable != null && m_assignedInteractionPosition != null)
			{
				if (m_assignedInteractionPosition.TryGetInteractionPosition(this, out Vector3 validPos))
				{
					AIPathing.SetDestination(validPos);

					// Check distance and attempt interaction directly
					float interactionDistance = m_assignedInteractionPosition.m_settings.InteractionDistance;
					if (AIPathing.IsWithinDistance(validPos, interactionDistance))
					{
						InteractWith(m_targetInteractable, true);
					}
				}
				else
				{
					HandleFailedInteraction();
					return;
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
							(treeState == EBTNodeState.STATE_SUCSESS ||
							 treeState == EBTNodeState.STATE_FAILURE))
						{
							BeginOffDuty();
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
		StopCarrying();
		BeginOffDuty();
		m_behaviourTreeExecutor.ResetContext();

		// Follow
		SetLogicExecutorState(EActorState.Follow);
		m_targetTransform = Player;
	}

	public void InvestigatePosition(Vector3 destination)
	{
		m_isInvestigating = true;
		m_targetTransform = null;
		BeginJobSearch();
		AIPathing.SetDestination(destination);
	}

	public void InteractWith(ActorInteractableBase actorInteractableObjectBase, bool willReplaceJob)
	{
		if (m_behaviourTreeExecutor != null)
		{
			InteractionPoint contextPosition = m_behaviourTreeExecutor.AIContext
				.GetData<InteractionPoint>(AIContextKeys.c_AssignedInteractionPosition);

			if (contextPosition != null && contextPosition != m_assignedInteractionPosition)
			{
				ReleaseInteractionPosition(m_assignedInteractionPosition);
				m_assignedInteractionPosition = contextPosition;
			}
		}

		// Recalculate the interaction position in case it moved between ticks.
		if (m_assignedInteractionPosition != null)
		{
			if (!m_assignedInteractionPosition.TryGetInteractionPosition(this, out Vector3 validPos))
				return;

			float interactionDistance = m_assignedInteractionPosition.m_settings.InteractionDistance;
			if (!AIPathing.IsWithinDistance(validPos, interactionDistance))
				return;
		}

		m_targetTransform = actorInteractableObjectBase.transform;
		bool isInteractionSuccessful = actorInteractableObjectBase.TryInteract
		(
			this,
			transform.position,
			m_assignedInteractionPosition,
			out int interactorValue
		);

		if (!isInteractionSuccessful)
		{
			HandleFailedInteraction();
			return;
		}

		if (willReplaceJob)
		{
			m_targetTransform = actorInteractableObjectBase.transform;
			TrySetActorJob(actorInteractableObjectBase.GetBehaviourTree());
			Debug.Log($"{transform} interacted with {m_targetTransform.name} and was succsessful.", this);
		}
	}

	public void BeginCarrying(Transform parent, Vector3 localPosition)
	{
		if(parent != null)
		{
			SetLogicExecutorState(EActorState.Carrying);

			AIPathing.NavAgent.enabled = false;
			AIPathing.ClearDestination();

			m_collider.isTrigger = true;

			transform.parent = parent;
			transform.localPosition = localPosition;
		}
	}

	public void StopCarrying()
	{
		transform.parent = null;
		m_collider.isTrigger = false;

		AIPathing.NavAgent.enabled = true;
		AIPathing.SetPosition(transform.position);
	}

	private void TrySetActorJob(BehaviourTree behaviourTree)
	{
		// A successful interaction converts the reservation into an active
		// interactor. If there is no behaviour tree, release that position now.
		if (behaviourTree == null)
		{
			ReleaseInteractionPosition(m_assignedInteractionPosition);
			m_assignedInteractionPosition = null;
			m_targetInteractable = null;
			m_targetTransform = null;
			AIPathing.ClearDestination();
			m_jobSearchCooldown = c_jobSearchCooldownDuration;
			return;
		}

		// A new job can be acquired from inside an existing behaviour tree
		InteractionPoint previousPosition = m_behaviourTreeExecutor.AIContext.GetData<InteractionPoint>( AIContextKeys.c_AssignedInteractionPosition);
		if (previousPosition != null && previousPosition != m_assignedInteractionPosition)
			ReleaseInteractionPosition(previousPosition);

		m_behaviourTreeExecutor.AIContext.SetData<Transform>(AIContextKeys.c_TargetTransform, m_targetTransform);

		Vector3 targetDestination = m_targetTransform.position;
		if (m_assignedInteractionPosition != null &&
			m_assignedInteractionPosition.TryGetInteractionPosition(this, out Vector3 validPos))
		{
			targetDestination = validPos;
		}

		m_behaviourTreeExecutor.AIContext.SetData<Vector3>(AIContextKeys.c_TargetDestination, targetDestination);
		m_behaviourTreeExecutor.AIContext.SetData<InteractionPoint>(AIContextKeys.c_AssignedInteractionPosition, m_assignedInteractionPosition);
		m_behaviourTreeExecutor.SetCurrentBehaviourTree(behaviourTree);
		SetLogicExecutorState(EActorState.Working);
		m_jobAssignedThisTick = true;
	}

	private void ReleaseInteractionPosition(InteractionPoint position)
	{
		if (position == null)
			return;

		position.ReleaseReservation(this);
		position.TryRemoveInteractor(this);
	}

	private void SyncJobStateFromContext()
	{
		AIContext context = m_behaviourTreeExecutor.AIContext;

		Transform contextTarget = context.GetData<Transform>(AIContextKeys.c_TargetTransform);
		InteractionPoint contextPosition = context.GetData<InteractionPoint>(AIContextKeys.c_AssignedInteractionPosition);
		if (contextTarget != m_targetTransform)
		{
			m_targetTransform = contextTarget;

			if (contextTarget != null)
				m_targetInteractable = contextTarget.GetComponent<ActorInteractableBase>() ??
					contextTarget.GetComponentInParent<ActorInteractableBase>();
			else
				m_targetInteractable = null;
		}

		if (contextPosition != m_assignedInteractionPosition)
		{
			ReleaseInteractionPosition(m_assignedInteractionPosition);
			m_assignedInteractionPosition = contextPosition;
		}
	}

	private void ClearJobState() 
	{ 
		ReleaseInteractionPosition(m_assignedInteractionPosition); 
		m_targetInteractable = null; 
		m_targetTransform = null; 
		m_assignedInteractionPosition = null; 
		m_isInvestigating = false; 
		m_behaviourTreeExecutor.AIContext.ClearData(AIContextKeys.c_AssignedInteractionPosition); 
		m_timeFindingJob = 0; 
		m_behaviourTreeExecutor.SetCurrentBehaviourTree(null); 
		AIPathing.ClearDestination(); 
	}

	private void BeginJobSearch() 
	{ 
		ClearJobState(); 
		SetLogicExecutorState(EActorState.SearchingForWork); 
	}

	private void BeginOffDuty() 
	{ 
		ClearJobState(); 
		SetLogicExecutorState(EActorState.OffDuty); 
		AIPathing.SetStoppingDistance(c_workingDist); 
	}

	private void HandleFailedInteraction()
	{
		Debug.Log($"{transform} has failed an interaction with {m_targetTransform}.", this);
		ReleaseInteractionPosition(m_assignedInteractionPosition);

		m_targetInteractable = null;
		m_targetTransform = null;
		m_assignedInteractionPosition = null;
		AIPathing.ClearDestination();

		m_jobSearchCooldown = c_jobSearchCooldownDuration;
	}

	/// <summary>
	/// Checks if this actor should be searching for a job.
	/// </summary>
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
		Collider[] hitColliders = Physics.OverlapSphere(pos, c_searchForJobRange, m_interactionLayers, QueryTriggerInteraction.Collide);

		float closestDist = Mathf.Infinity;
		foreach (Collider i in hitColliders)
		{
			if (i == null)
				continue;

			if (i.TryGetComponent(out ActorInteractableBase aio))
			{
				if (!aio.HasAvailableWork(this))
					continue;

				float dist = (pos - aio.transform.position).sqrMagnitude;
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
				BeginOffDuty();
				m_behaviourTreeExecutor.ResetContext();
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