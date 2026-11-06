using AI.Context;
using BehaviourTrees;
using Interaction.Core;
using ObjectTags;
using UnityEngine;

namespace AI.Actors
{
	[RequireComponent(typeof(GOAPAgent))]
	public abstract class PlayerActorBase : ActorBase
	{
		[SerializeField] private ENoddTypes m_noddType = ENoddTypes.NODD_BASE;
		
		public const float c_JobSearchRange = 1.5f;

		private const float c_waitingForJobLimit = 2.0f;
		private const float c_followDist = 1.2f;
		private const float c_workingDist = 0.3f;

		private const float c_followSpeed = 7.2f;
		private const float c_workingSpeed = 5.8f;
		private const float c_offDutySpeed = 2f;

		private const float c_searchForJobStoppingDistance = 0.25f;
		private const float c_jobSearchCooldownDuration = 2.0f;

		public EPlayerActorState LogicExecutorState { get; private set; }
			= EPlayerActorState.OffDuty;

		public GOAPAgent GOAPAgentComp { get; private set; }

		public ENoddTypes NoddType => m_noddType;
		
		private float m_timeFindingJob;
		private float m_jobSearchCountdown;

		private bool m_isInvestigatingPositionForJob;
		private bool m_jobAssignedThisTick;

		#region Lifecycle

		protected override void Awake()
		{
			base.Awake();

			GOAPAgentComp = GetComponent<GOAPAgent>();
		}

		protected override void Start()
		{
			base.Start();

			SetLogicExecutorState(EPlayerActorState.OffDuty);
		}

		private void OnEnable()
		{
			if (GOAPAgentComp == null)
				GOAPAgentComp = GetComponent<GOAPAgent>();

			if (GOAPAgentComp != null && AIPathing != null)
			{
				GOAPAgentComp.OnFoundDestination += AIPathing.SetDestination;
				GOAPAgentComp.OnClearDestination += AIPathing.ClearDestination;
			}
		}

		private void OnDisable()
		{
			if (GOAPAgentComp != null && AIPathing != null)
			{
				GOAPAgentComp.OnFoundDestination -= AIPathing.SetDestination;
				GOAPAgentComp.OnClearDestination -= AIPathing.ClearDestination;
			}
		}

		protected override void HandleContextReset()
		{
			base.HandleContextReset();

			AIContext context = BehaviourTreeExecutor.AIContext;
			context.JobSearchDistance = c_JobSearchRange;

			BehaviourTreeExecutor.AIContext = context;
		}

		#endregion

		#region Logic Executor

		public void SetLogicExecutorState(EPlayerActorState state)
		{
			LogicExecutorState = state;

			switch (state)
			{
				case EPlayerActorState.OffDuty:
					AIPathing.SetSpeed(c_offDutySpeed);
					AIPathing.SetStoppingDistance(c_workingDist);
					break;

				case EPlayerActorState.Follow:
					AIPathing.SetSpeed(c_followSpeed);
					AIPathing.SetStoppingDistance(c_followDist);
					break;

				case EPlayerActorState.SearchingForWork:
				case EPlayerActorState.Working:
					AIPathing.SetSpeed(c_workingSpeed);
					AIPathing.SetStoppingDistance(c_workingDist);
					break;
			}
		}

		#endregion

		#region Behaviour

		public override void TickBehaviour(float t)
		{
			if (!isActiveAndEnabled || GOAPAgentComp == null)
				return;

			TickActorSystems(t);

			// Wait until the investigation destination is reached
			// before searching for jobs
			if (m_isInvestigatingPositionForJob)
			{
				if (!AIPathing.HasReachedDestination(
						c_searchForJobStoppingDistance))
				{
					FaceCurrentTarget();
					return;
				}

				m_isInvestigatingPositionForJob = false;
				AIPathing.ClearDestination();
			}

			// Job search cooldown
			if (m_jobSearchCountdown > 0f)
				m_jobSearchCountdown -= t;

			if (IsJobNeeded())
			{
				if (m_jobSearchCountdown <= 0f)
					FindClosestJob(t);

				if (m_targetInteractable != null &&
					m_assignedInteractionPosition != null &&
					m_assignedInteractionPosition.TryGetInteractionPosition(
						this,
						out Vector3 validPosition))
				{
					AIPathing.SetDestination(validPosition);

					if (AIPathing.IsWithinDistance(
							validPosition,
							m_assignedInteractionPosition.InteractionDistance))
					{
						UseInteractable(m_targetInteractable);
					}
				}
			}
			else
			{
				switch (LogicExecutorState)
				{
					case EPlayerActorState.OffDuty:

						GOAPAgentComp.TickGoapPlanner(t);
						break;

					case EPlayerActorState.Follow:

						if (m_targetTransform != null)
							AIPathing.SetDestination(m_targetTransform.position);

						break;

					case EPlayerActorState.Working:

						SyncJobStateFromContext();

						m_jobAssignedThisTick = false;

						if (TryTickBehaviourTree(t, out EBTNodeState treeState))
						{
							if ((treeState == EBTNodeState.SUCSESS ||
								 treeState == EBTNodeState.FAILURE) &&
								!m_jobAssignedThisTick)
							{
								ClearJobState();
							}
						}

						break;
				}
			}

			FaceCurrentTarget();
		}

		#endregion

		#region Commands

		/// <summary>
		/// Commands the actor to follow a player.
		/// </summary>
		public void FollowPlayer(Transform player)
		{
			ClearJobState();

			m_targetTransform = player;

			SetLogicExecutorState(EPlayerActorState.Follow);
		}

		/// <summary>
		/// Commands the actor to investigate a position.
		/// Once reached, it begins searching for jobs.
		/// </summary>
		public void InvestigatePosition(Vector3 destination)
		{
			ClearJobState();

			m_isInvestigatingPositionForJob = true;

			SetLogicExecutorState(EPlayerActorState.SearchingForWork);

			AIPathing.SetDestination(destination);
		}

		#endregion

		#region Interactions

		public override void InteractionCanceled()
		{
			ClearJobState();
		}

		protected override void OnInteractionFailed()
		{
			ClearJobState();
		}

		protected override void HandleInteractableBehaviourTree(BehaviourTree behaviourTree)
		{
			TrySetActorJob(behaviourTree);
		}

		/// <summary>
		/// Assigns a job supplied by an interactable.
		/// </summary>
		private void TrySetActorJob(BehaviourTree behaviourTree)
		{
			// Null trees are allowed for immediate interactions
			if (behaviourTree == null)
				return;

			InteractionPoint previousPoint =
				BehaviourTreeExecutor.AIContext.ReservedInteractionPoint;

			if (previousPoint != null &&
				previousPoint != m_assignedInteractionPosition)
			{
				ReleaseInteractionPosition(previousPoint);
			}

			AIContext context = BehaviourTreeExecutor.AIContext;

			context.TargetTransform = m_targetTransform;

			Vector3 targetDestination = m_targetTransform != null
				? m_targetTransform.position
				: transform.position;

			if (m_assignedInteractionPosition != null &&
				m_assignedInteractionPosition.TryGetInteractionPosition(
					this,
					out Vector3 validPosition))
			{
				targetDestination = validPosition;
			}

			context.TargetDestination = targetDestination;
			context.ReservedInteractionPoint = m_assignedInteractionPosition;

			BehaviourTreeExecutor.AIContext = context;

			SetBehaviourTree(behaviourTree);

			SetLogicExecutorState(EPlayerActorState.Working);

			m_jobAssignedThisTick = true;
		}

		/// <summary>
		/// Clears all current job and interaction information.
		/// </summary>
		protected void ClearJobState(bool applyCooldown = false)
		{
			// Release the actor from whatever it is carrying
			if (this is ICarryingInteractor carryingInteractor)
				carryingInteractor.EndCarrying();

			ClearInteractionState();

			m_isInvestigatingPositionForJob = false;
			m_timeFindingJob = 0f;

			if (applyCooldown)
			{
				m_jobSearchCountdown = c_jobSearchCooldownDuration;

				SetLogicExecutorState(
					EPlayerActorState.SearchingForWork
				);
			}
			else
			{
				SetLogicExecutorState(EPlayerActorState.OffDuty);
			}
		}

		#endregion

		#region Job Management

		private void SyncJobStateFromContext()
		{
			AIContext context = BehaviourTreeExecutor.AIContext;

			Transform contextTarget = context.TargetTransform;
			InteractionPoint reservedPoint = context.ReservedInteractionPoint;

			if (contextTarget != m_targetTransform)
			{
				m_targetTransform = contextTarget;

				m_targetInteractable = contextTarget != null
					? contextTarget.GetComponent<ActorInteractableBase>() ??
					  contextTarget.GetComponentInParent<ActorInteractableBase>()
					: null;
			}

			if (reservedPoint != m_assignedInteractionPosition)
			{
				ReleaseInteractionPosition(m_assignedInteractionPosition);

				m_assignedInteractionPosition = reservedPoint;
			}
		}

		private bool IsJobNeeded()
		{
			bool workingWithoutTree =
				LogicExecutorState == EPlayerActorState.Working &&
				BehaviourTreeExecutor.CurrentBehaviourTree == null;

			return LogicExecutorState == EPlayerActorState.SearchingForWork ||
				   workingWithoutTree;
		}

		/// <summary>
		/// Finds the closest available interactable task.
		/// </summary>
		private ActorInteractableBase SearchForTask()
		{
			ActorInteractableBase closestTask = null;

			Vector3 position = transform.position;

			Collider[] hitColliders = Physics.OverlapSphere(
				position,
				c_JobSearchRange,
				InteractionLayers,
				QueryTriggerInteraction.Collide
			);

			float closestDistance = Mathf.Infinity;

			foreach (Collider hitCollider in hitColliders)
			{
				if (hitCollider == null ||
					!hitCollider.TryGetComponent(
						out ActorInteractableBase actorInteractable))
				{
					continue;
				}

				if (!actorInteractable.HasWorkForInteractor(this))
					continue;

				float distance =
					(position - actorInteractable.transform.position).sqrMagnitude;

				if (distance < closestDistance)
				{
					closestTask = actorInteractable;
					closestDistance = distance;
				}
			}

			return closestTask;
		}

		/// <summary>
		/// Searches for the nearest available job and reserves
		/// an interaction position.
		/// </summary>
		private void FindClosestJob(float t)
		{
			if (m_assignedInteractionPosition != null ||
				!AIPathing.HasReachedDestination(
					c_searchForJobStoppingDistance))
			{
				return;
			}

			m_timeFindingJob += t;

			// Stop searching if it takes too long
			if (m_timeFindingJob >= c_waitingForJobLimit)
			{
				ClearJobState(applyCooldown: true);
				return;
			}

			m_targetInteractable = SearchForTask();

			if (m_targetInteractable != null &&
				m_targetInteractable.TryReserveClosestPosition(
					this,
					transform.position,
					out m_assignedInteractionPosition))
			{
				m_timeFindingJob = 0f;

				Debug.Log(
					$"Found Job: {m_targetInteractable}\n" +
					$"Interaction Position {m_assignedInteractionPosition}.",
					this
				);
			}
		}

		#endregion
	}
}