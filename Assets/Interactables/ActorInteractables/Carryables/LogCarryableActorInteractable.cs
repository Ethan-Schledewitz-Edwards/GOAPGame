using BehaviourTrees;
using Factions.Core;
using Interaction.ActorInteractables.Structures;
using Interaction.Core;
using ObjectTags;
using Settlements;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// This class needs to be made generic in the future (CarryableBaseActorInteractable)
[RequireComponent(typeof(Rigidbody), typeof(NavMeshAgent))]
public class LogCarryableActorInteractable : ActorInteractableBase, IInteractor, IDuplicatableResource
{
	private static BehaviourTree m_carryableBT = null; // Interacting with this object parents the
													   // interactor to it which does not need a BT.
	private const float c_logMeshCarryOffset = 0.5f;
	private const float c_logHalfHeight = .75f;
	private const float c_logColliderRadius = 0.25f;
	private const float c_logHullCastRadius = 0.1f;
	private const float c_searchRetryInterval = 1f;

	protected override int m_minActorsToFunction => 2;

	// IInteractable properties
	protected override string m_interactPrompt => string.Empty;

	public Transform Transform => transform;

	// IDuplicatableResource properties
	public NoddEnvelope[] NoddEnvelopes => m_noddEnvelopes;
	[SerializeField] private NoddEnvelope[] m_noddEnvelopes;

	[Header("Carry Settings")]
	[SerializeField] private StructureTag m_duplicatinatorTag;

	[Header("Mesh")]
	[SerializeField] private Transform m_logMeshTransform;

	// Components
	private Rigidbody m_rigidbody;
	private NavMeshAgent m_navMeshAgent;

	// System
	private LayerMask m_logOverlapLayermask;
	private readonly Collider[] m_hitResults = new Collider[8];

	private bool m_isBeingCarried;
	private ActorInteractableBase m_targetDuplicatinator;
	private InteractionPoint m_registeredPoint;

	private float m_searchCountdown;

	protected override void Awake()
	{
		base.Awake();

		m_rigidbody = GetComponent<Rigidbody>();
		m_navMeshAgent = GetComponent<NavMeshAgent>();
		m_navMeshAgent.enabled = false;

		m_logOverlapLayermask = LayerMask.GetMask("Default", "Interaction");
	}

	private void FixedUpdate()
	{
		if (!m_isBeingCarried)
			return;

		if (m_targetDuplicatinator == null && 
			Time.time >= m_searchCountdown)
		{
			var (duplicatinator, point) = FindClosestDuplicatinator();
			if (duplicatinator != null && point != null)
			{
				m_targetDuplicatinator = duplicatinator;
				m_registeredPoint = point;
			}
			else
			{
				m_searchCountdown = Time.time + c_searchRetryInterval;
			}
		}

		if (m_registeredPoint != null)
		{
			if (m_registeredPoint.TryGetInteractionPosition(this, out Vector3 destination))
			{
				m_navMeshAgent.SetDestination(destination);
			}
		}

		// Move towards the assigned interaction point and check distance
		if (m_registeredPoint != null && 
			m_targetDuplicatinator != null)
		{
			if (m_registeredPoint.TryGetInteractionPosition(this, out Vector3 destination))
			{
				m_navMeshAgent.SetDestination(destination);

				float distanceToDestination = Vector3.Distance(transform.position, destination);
				if (distanceToDestination <= m_registeredPoint.InteractionDistance)
				{
					UseInteractable(m_targetDuplicatinator);
				}
			}
		}
	}

	public override bool TryInteract(IInteractor interactor, Vector3 actorPosition, InteractionPoint assignedPosition)
	{
		if(base.TryInteract(interactor, actorPosition, assignedPosition))
		{
			if(assignedPosition.TryGetInteractionPosition(interactor, out Vector3 destination))
			{
				// Actor starts carrying the carryable
				interactor.BeginCarrying(transform,
					transform.InverseTransformPoint(destination));
				return true;
			}
		}

		return false;
	}

	protected override void ActorAssigned()
	{
		int actorsPresent = TotalActorsPresent();
		if (actorsPresent >= m_minActorsToFunction)
		{
			if (!m_isBeingCarried)
				HandleCarryStarted();

			UpdateMovementSpeed(actorsPresent);
		}
	}

	public override void StopInteract(IInteractor interactor, InteractionPoint assignedPosition)
	{
		base.StopInteract(interactor, assignedPosition);

		int actorsPresent = TotalActorsPresent();
		if (actorsPresent >= m_minActorsToFunction)
		{
			UpdateMovementSpeed(actorsPresent);
		}
	}

	public override void LostMinimumActors()
	{
		HandleCarryStopped();
	}

	public void UseInteractable(IInteractable interactable)
	{
		// Try to start the duplicatinator
		if(interactable != null &&
			interactable is ActorInteractableBase actorInteractable)
		{
			actorInteractable.TryInteract(this, transform.position, m_registeredPoint);
		}
	}

	private void HandleCarryStarted()
	{
		m_isBeingCarried = true;

		// Move the log mesh up to make it look like it's being carried
		if (m_logMeshTransform != null)
			m_logMeshTransform.localPosition = Vector3.up * c_logMeshCarryOffset;

		m_navMeshAgent.enabled = true;
	}

	private void HandleCarryStopped()
	{
		m_isBeingCarried = false;

		if(m_targetDuplicatinator != null && 
			m_registeredPoint != null)
		{
			m_targetDuplicatinator.CancelReservation(this, m_registeredPoint);
		}

		m_targetDuplicatinator = null;
		m_registeredPoint = null;

		// Move the log mesh to it's default position
		if (m_logMeshTransform != null)
			m_logMeshTransform.localPosition = Vector3.zero;

		m_navMeshAgent.enabled = false;
		m_navMeshAgent.speed = 0f;

		// Hull cast then snap to the ground
		if (ValidateLogPosition(transform.position,
			Vector3.forward,
			out Vector3 finalPosition,
			out Quaternion finalRotation))
		{
			// If using a Rigidbody, move it via Rigidbody to prevent physics stutter/snapping back
			if (m_rigidbody != null)
			{
				m_rigidbody.isKinematic = true;
				m_rigidbody.MovePosition(finalPosition);
				m_rigidbody.MoveRotation(finalRotation);
			}
			else
			{
				transform.position = finalPosition;
				transform.rotation = finalRotation;
			}
		}
	}

	public bool ValidateLogPosition(Vector3 targetPosition,
		Vector3 fallDirection,
		out Vector3 finalPosition,
		out Quaternion finalRotation)
	{
		finalPosition = targetPosition;
		finalRotation = Quaternion.identity;

		Vector3 groundCheckStart = targetPosition + Vector3.up * 1;

		// Check for ground to place the log on
		RaycastHit hit;
		if (Physics.Raycast(groundCheckStart,
			Vector3.down,
			out hit,
			2f,
			m_logOverlapLayermask,
			QueryTriggerInteraction.Ignore))
		{
			Vector3 slopeDirection =
				Vector3.ProjectOnPlane(fallDirection, hit.normal).normalized;

			// Logs resting position
			Vector3 spawnPosition = hit.point + (hit.normal * c_logColliderRadius);

			// Hull cast
			Vector3 topPoint = spawnPosition + (slopeDirection * c_logHalfHeight);
			Vector3 bottomPoint = spawnPosition - (slopeDirection * c_logHalfHeight);
			int collidersHit = Physics.OverlapCapsuleNonAlloc(
				topPoint,
				bottomPoint,
				c_logHullCastRadius,
				m_hitResults,
				m_logOverlapLayermask
			);

			// Return the final resting position and rotation of the log
			if (collidersHit == 0)
			{
				finalPosition = spawnPosition;

				finalRotation =
					Quaternion.LookRotation(slopeDirection, hit.normal);

				return true;
			}
		}

		return false;
	}

	private (ActorInteractableBase, InteractionPoint) FindClosestDuplicatinator()
	{
		EFaction executorFaction = EFaction.FACTION_PLAYER;// This should be the first guy that picks up the log

		Settlement closestFactionSettlement =
			SettlementManager.GetClosestSettlement(transform.position, executorFaction);

		if (closestFactionSettlement == null)
			return (null, null);

		HashSet<IStructure> invalidStructures = new HashSet<IStructure>();

		while (true)
		{
			// Find the closest duplicatinator excluding the invalid ones
			IStructure closestStructure = closestFactionSettlement.
				FindNearestStructureOfType(transform.position, m_duplicatinatorTag, invalidStructures);

			// No structures are left
			if (closestStructure == null)
				return (null, null);

			GameObject structureObject = closestStructure.Object;
			if (structureObject != null &&
				structureObject.TryGetComponent(out ActorInteractableBase interactable))
			{
				if (interactable.TryReserveClosestPosition(this, transform.position, out InteractionPoint assignedPosition))
				{
					return (interactable, assignedPosition);
				}
			}

			invalidStructures.Add(closestStructure);
		}
	}

	private void UpdateMovementSpeed(int actorsPresent)
	{
		m_navMeshAgent.speed = (actorsPresent * 1.2f);
	}

	public override BehaviourTree GetBehaviourTree() => m_carryableBT;

	public void BeginCarrying(Transform parent, Vector3 localPosition) { } // Logs can't carry anything silly

	public void StopCarrying() { }

	public void InteractionCanceled()
	{
		foreach (InteractionPoint point in InteractionPoints)
		{
			point.CancelAllInteractions();
		}
	}
}
