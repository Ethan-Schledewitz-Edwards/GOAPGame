using AI.Actors;
using AI.Context;
using BehaviourTrees;
using Factions.Core;
using Interaction.Core;
using UnityEngine;

[RequireComponent(typeof(ActorHealthComponent), typeof(AIPathing))]
public abstract class ActorBase : MonoBehaviour, IInteractor, IActor
{
	[Header("Parameters")]
	[field: SerializeField] public EFaction ActorFaction { get; private set; }

	[SerializeField] private LayerMask m_interactionLayers;
	[SerializeField, Min(0)] private int m_attackDamage = 1;

	// Shared components
	public ActorHealthComponent ActorHealth { get; private set; }
	public AIPathing AIPathing { get; private set; }
	public BehaviourTreeExecutorBase BehaviourTreeExecutor { get; private set; }

	// IInteractor
	public Transform Transform => transform;

	// IActor
	public bool IsActorActive => enabled;

	// Shared interaction state
	protected Transform m_targetTransform;
	protected ActorInteractableBase m_targetInteractable;
	protected InteractionPoint m_assignedInteractionPosition;

	protected LayerMask InteractionLayers => m_interactionLayers;

	protected bool IsActorReady =>
		ActorHealth != null &&
		AIPathing != null &&
		BehaviourTreeExecutor != null;

	#region Lifecycle

	protected virtual void Awake()
	{
		ActorHealth = GetComponent<ActorHealthComponent>();
		AIPathing = GetComponent<AIPathing>();

		BehaviourTreeExecutor = new BehaviourTreeExecutorBase(transform);
		BehaviourTreeExecutor.ContextReset += HandleContextReset;

		// Initialize the context immediately.
		BehaviourTreeExecutor.ResetContext();
	}


	protected virtual void Start()
	{
		ActorManager.Instance.TryAddActor(this);
	}

	protected virtual void OnDestroy()
	{
		if (BehaviourTreeExecutor != null)
			BehaviourTreeExecutor.ContextReset -= HandleContextReset;

		if (m_assignedInteractionPosition != null)
			ReleaseInteractionPosition(m_assignedInteractionPosition);
	}

	#endregion

	#region Behaviour

	/// <summary>
	/// Default behaviour for actors that simply execute their assigned tree.
	/// PlayerActorBase overrides this to handle commands and jobs.
	/// </summary>
	public virtual void TickBehaviour(float t)
	{
		if (!IsActorReady)
			return;

		TickActorSystems(t);
		TryTickBehaviourTree(t, out _);
		FaceCurrentTarget();
	}

	/// <summary>
	/// Updates shared actor systems.
	/// </summary>
	protected void TickActorSystems(float t)
	{
		ActorHealth?.TickStats(t);
		AIPathing?.TickAIPathing();
	}

	/// <summary>
	/// Executes the currently assigned behaviour tree.
	/// </summary>
	protected bool TryTickBehaviourTree(
		float t,
		out EBTNodeState state)
	{
		if (BehaviourTreeExecutor == null ||
			BehaviourTreeExecutor.CurrentBehaviourTree == null)
		{
			state = default;
			return false;
		}

		state = BehaviourTreeExecutor.TickBehaviour(t);
		return true;
	}

	/// <summary>
	/// Assigns or clears the current behaviour tree.
	/// </summary>
	public void SetBehaviourTree(BehaviourTree behaviourTree)
	{
		if (BehaviourTreeExecutor != null)
			BehaviourTreeExecutor.SetCurrentBehaviourTree(behaviourTree);
	}

	#endregion

	#region Interactions

	public virtual void UseInteractable(IInteractable interactable)
	{
		if (interactable is not ActorInteractableBase actorInteractable)
			return;

		// Revalidate the reserved interaction position.
		if (m_assignedInteractionPosition != null)
		{
			if (!m_assignedInteractionPosition.TryGetInteractionPosition(
					this, out Vector3 validPosition))
				return;

			if (!AIPathing.IsWithinDistance(
					validPosition,
					m_assignedInteractionPosition.InteractionDistance))
				return;
		}

		m_targetTransform = actorInteractable.transform;
		m_targetInteractable = actorInteractable;

		if (m_assignedInteractionPosition != null)
		{
			m_assignedInteractionPosition.InteractionsCanceled -= InteractionCanceled;
		}

		bool interactionSucceeded = actorInteractable.TryInteract(
			this,
			transform.position,
			m_assignedInteractionPosition
		);

		if (!interactionSucceeded)
		{
			OnInteractionFailed();
			return;
		}

		if (m_assignedInteractionPosition != null)
		{
			m_assignedInteractionPosition.InteractionsCanceled += InteractionCanceled;
		}

		HandleInteractableBehaviourTree(
			actorInteractable.GetBehaviourTree()
		);

		Debug.Log(
			$"{transform} interacted with {actorInteractable.name} and was successful.",
			this
		);
	}

	/// <summary>
	/// Allows derived actors to customize how an interactable's
	/// behaviour tree is handled.
	/// </summary>
	protected virtual void HandleInteractableBehaviourTree(
		BehaviourTree behaviourTree)
	{
		if (behaviourTree != null)
			SetBehaviourTree(behaviourTree);
	}

	protected virtual void OnInteractionFailed()
	{
		ClearInteractionState();
	}

	public virtual void InteractionCanceled()
	{
		ClearInteractionState();
	}

	/// <summary>
	/// Clears interaction state without modifying carrying-related
	/// transform, collider or NavMeshAgent properties.
	/// </summary>
	protected void ClearInteractionState()
	{
		if (m_targetInteractable != null)
		{
			m_targetInteractable.StopInteract(
				this,
				m_assignedInteractionPosition
			);
		}

		ReleaseInteractionPosition(m_assignedInteractionPosition);

		m_targetInteractable = null;
		m_targetTransform = null;
		m_assignedInteractionPosition = null;

		if (BehaviourTreeExecutor != null)
		{
			BehaviourTreeExecutor.ResetContext();
			BehaviourTreeExecutor.SetCurrentBehaviourTree(null);
		}

		AIPathing?.ClearDestination();
		AIPathing?.SetPosition(transform.position);
	}

	/// <summary>
	/// Releases an interaction position reservation.
	/// </summary>
	protected void ReleaseInteractionPosition(
		InteractionPoint registeredPoint)
	{
		if (registeredPoint == null)
			return;

		registeredPoint.InteractionsCanceled -= InteractionCanceled;
		registeredPoint.ReleaseReservation(this);
		registeredPoint.TryRemoveInteractor(this);
	}

	#endregion

	#region AI Context

	protected virtual void HandleContextReset()
	{
		if (BehaviourTreeExecutor == null)
			return;

		AIContext context = BehaviourTreeExecutor.AIContext;

		context.ExecutorTransform = transform;
		context.ExecutorFaction = (int)ActorFaction;

		int interactionLayer = LayerMask.NameToLayer("Interaction");
		context.InteractionLayer = 1 << interactionLayer;

		context.AttackDamage = m_attackDamage;

		BehaviourTreeExecutor.AIContext = context;
	}

	#endregion

	protected void FaceCurrentTarget()
	{
		if (m_targetTransform != null &&
			AIPathing != null &&
			!AIPathing.IsMoving)
		{
			AIPathing.FaceTarget(m_targetTransform.position);
		}
	}
}