using BehaviourTrees;
using Factions.Core;
using Interaction.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using AI.Context;
using ObjectTags;
using AI.Actors;

[RequireComponent(typeof(ActorHealthComponent), typeof(AIPathing), typeof(GOAPAgent))]
public abstract class ActorBase : MonoBehaviour, IInteractor, IActor
{
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

	// IInteractor Properties
	public Transform Transform => gameObject.transform;

	// IActor Properties
	public bool IsActorActive => enabled;

	// Executors
	public GOAPAgent GOAPAgentComp { get; private set; }
	public BehaviourTreeExecutorBase BehaviourTreeExecutor { get; private set; }

	private float m_timeFindingJob;
	private float m_jobSearchCountdown = 0f;
	private bool m_isInvestigatingPositionForJob;
	private bool m_jobAssignedThisTick;

	protected Transform m_targetTransform;
	protected ActorInteractableBase m_targetInteractable;
	protected InteractionPoint m_assignedInteractionPosition;

	#region Lifecycle

	private void Awake()
	{
		BehaviourTreeExecutor = new BehaviourTreeExecutorBase(transform);
		BehaviourTreeExecutor.ContextReset += HandleContextRest;

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
		BehaviourTreeExecutor.ContextReset -= HandleContextRest;

		if (m_assignedInteractionPosition != null)
		{
			m_assignedInteractionPosition.InteractionsCanceled -= InteractionCanceled;
		}
	}

	#endregion

	public abstract void TickBehaviour(float t);

	public void TrySetActorJob(BehaviourTree behaviourTree)
	{
		// If no behaviour tree was passed, its probably intentional.
		// Certain interactions like carryables require instant interactions without behaviour trees.
		if (behaviourTree == null)
			return;

		// A new job can be acquired from inside an existing behaviour tree
		InteractionPoint previousPoint = BehaviourTreeExecutor.AIContext.ReservedInteractionPoint;
		if (previousPoint != null && previousPoint != m_assignedInteractionPosition)
			ReleaseInteractionPosition(previousPoint);

		AIContext context = BehaviourTreeExecutor.AIContext;
		context.TargetTransform = m_targetTransform;

		Vector3 targetDestination = m_targetTransform.position;
		if (m_assignedInteractionPosition != null &&
			m_assignedInteractionPosition.TryGetInteractionPosition(this, out Vector3 validPos))
		{
			targetDestination = validPos;
		}

		context.TargetDestination = targetDestination;
		context.ReservedInteractionPoint = m_assignedInteractionPosition;
		BehaviourTreeExecutor.AIContext = context;

		BehaviourTreeExecutor.SetCurrentBehaviourTree(behaviourTree);
		SetLogicExecutorState(EPlayerActorState.Working);
		m_jobAssignedThisTick = true;
	}

	public virtual void UseInteractable(IInteractable interactable)
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
			SetLogicExecutorState(EPlayerActorState.Carrying);

			AIPathing.NavAgent.enabled = false;
			AIPathing.ClearDestination();

			m_collider.isTrigger = true;

			transform.parent = parent;
			transform.localPosition = localPosition;
		}
	}

	protected void ReleaseInteractionPosition(InteractionPoint registeredPoint)
	{
		if (registeredPoint == null)
			return;

		registeredPoint.InteractionsCanceled -= InteractionCanceled;
		registeredPoint.ReleaseReservation(this);
		registeredPoint.TryRemoveInteractor(this);
	}

	protected abstract void HandleContextRest();
}