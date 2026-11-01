using AI.Context;
using BehaviourTrees;
using Interaction.Core;
using UnityEngine;

namespace AI.Actors
{
	public abstract class PlayerActorBase : ActorBase
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

		// System
		public EPlayerActorState LogicExecutorState { get; private set; } = default;

		private float m_timeFindingJob;
		private float m_jobSearchCountdown = 0f;
		private bool m_isInvestigatingPositionForJob;
		private bool m_jobAssignedThisTick;

		/// <summary>
		/// Setting the logic executor state determines the method used by an Actor to calculate its behaviour.
		/// Off-Duty actors use GOAP to drive emergent behaviour.
		/// Following actors try to reach a specific destination.
		/// Working actors use a behaviour tree.
		/// </summary>
		public void SetLogicExecutorState(EPlayerActorState state)
		{
			LogicExecutorState = state;

			switch (state)
			{
				case EPlayerActorState.OffDuty:
					AIPathing.SetSpeed(c_offDutySpeed);
					break;
				case EPlayerActorState.Follow:
					AIPathing.SetSpeed(c_followSpeed);
					break;
				case EPlayerActorState.SearchingForWork:
				case EPlayerActorState.Working:
					AIPathing.SetSpeed(c_workingSpeed);
					break;
				case EPlayerActorState.Carrying:
					AIPathing.SetSpeed(0);
					break;
			}

			float newStoppingDistance = state == EPlayerActorState.Follow ? c_followDist : c_workingDist;
			AIPathing.SetStoppingDistance(newStoppingDistance);
		}

		public override void TickBehaviour(float t)
		{
			if (AIPathing == null ||
			BehaviourTreeExecutor == null ||
			GOAPAgentComp == null)
				return;

			if (LogicExecutorState == EPlayerActorState.Carrying)
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
					case EPlayerActorState.OffDuty:
						GOAPAgentComp.TickGoapPlanner(t);
						break;
					case EPlayerActorState.Follow:
						if (m_targetTransform != null)
							AIPathing.SetDestination(m_targetTransform.position);
						break;

					case EPlayerActorState.Working:
						if (BehaviourTreeExecutor != null && BehaviourTreeExecutor.CurrentBehaviourTree != null)
						{
							SyncJobStateFromContext();

							m_jobAssignedThisTick = false;
							EBTNodeState treeState = BehaviourTreeExecutor.TickBehaviour(t);

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

		public void FollowPlayer(Transform player)
		{
			// Clear state
			InteractionCanceled();
			ClearJobState(false);

			// Follow
			SetLogicExecutorState(EPlayerActorState.Follow);
			m_targetTransform = player;
		}

		public void InvestigatePosition(Vector3 destination)
		{
			m_isInvestigatingPositionForJob = true;
			m_targetTransform = null;

			ClearJobState();
			SetLogicExecutorState(EPlayerActorState.SearchingForWork);

			AIPathing.SetDestination(destination);
		}

		protected void ClearJobState(bool applyCooldown = false)
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
			if (BehaviourTreeExecutor != null)
			{
				BehaviourTreeExecutor.ResetContext();
				BehaviourTreeExecutor.SetCurrentBehaviourTree(null);
			}

			transform.parent = null;
			m_collider.isTrigger = false;

			AIPathing.NavAgent.enabled = true;
			AIPathing?.ClearDestination();
			AIPathing.SetPosition(transform.position);

			if (applyCooldown)
			{
				m_jobSearchCountdown = c_jobSearchCooldownDuration;
				SetLogicExecutorState(EPlayerActorState.SearchingForWork);
			}
			else
				SetLogicExecutorState(EPlayerActorState.OffDuty);
		}

		private void SyncJobStateFromContext()
		{
			AIContext context = BehaviourTreeExecutor.AIContext;

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
			bool isJobFinished = (LogicExecutorState == EPlayerActorState.Working &&
				BehaviourTreeExecutor.CurrentBehaviourTree == null);

			return LogicExecutorState == EPlayerActorState.SearchingForWork || isJobFinished;
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
}
