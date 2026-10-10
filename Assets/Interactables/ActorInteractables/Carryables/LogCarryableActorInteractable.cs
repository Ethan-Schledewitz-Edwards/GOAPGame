using BehaviourTrees;
using Factions.Core;
using InventorySystem.Items;
using ObjectTags;
using Settlements;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody), typeof(NavMeshAgent))]
public class LogCarryableActorInteractable : ActorInteractableBase
{
	private static BehaviourTree m_logBT;

	private const float c_logMeshCarryOffset = 0.5f;
	private const float c_logHalfHeight = .75f;
	private const float c_logColliderRadius = 0.25f;
	private const float c_logHullCastRadius = 0.1f;

	protected override int m_minActorsToFunction { get; set; } = 2;

	[Header("Carry Settings")]
	[SerializeField] private StructureTag m_duplicatinatorTag;
	[SerializeField] private float m_baseMoveSpeed = 3f;

	[Header("Mesh")]
	[SerializeField] private Transform m_logMesh;

	// Components
	private Rigidbody m_rigidbody;
	private NavMeshAgent m_navMeshAgent;
	private Transform m_currentTarget;

	// System
	private bool m_isBeingCarried;
	private LayerMask m_logOverlapLayermask;
	private readonly Collider[] m_hitResults = new Collider[8];

	private void Awake()
	{

		m_rigidbody = GetComponent<Rigidbody>();
		m_navMeshAgent = GetComponent<NavMeshAgent>();
		m_navMeshAgent.enabled = false;

		m_logOverlapLayermask = LayerMask.GetMask("Default", "Interaction");

		if (m_logBT != null) 
			return;

		BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
		{
			new MoveToInteractionPositionTask(),
			new BTTimeoutNode(new CheckForDestinationRangeTask(), 2f),
			new CarryObjectTask()
		});
		BehaviourTree tree = new BehaviourTree();
		tree.SetTree(root);

		m_logBT = tree;
	}

	private void FixedUpdate()
	{
		if (m_isBeingCarried)
		{
			Transform closestDuplicatinator = FindClosestDuplicatinator();
			if (m_currentTarget != closestDuplicatinator)
			{
				Debug.Log("FOUND!");

				m_currentTarget = closestDuplicatinator;
				m_navMeshAgent.SetDestination(m_currentTarget.transform.position);
			}
		}
	}

	protected override void ActorAssigned()
	{
		int actorsPresent = GetTotalActorsPresent();

		if (actorsPresent >= m_minActorsToFunction)
		{
			if (!m_isBeingCarried)
				HandleCarryStarted();

			m_navMeshAgent.speed = m_baseMoveSpeed + (actorsPresent * 1.5f);
		}
	}

	public override void LostMinimumActors()
	{
		HandleCarryStopped();
	}

	private void HandleCarryStarted()
	{
		m_isBeingCarried = true;

		// Move the log mesh up to make it look like it's being carried
		if (m_logMesh != null)
			m_logMesh.localPosition = Vector3.up * c_logMeshCarryOffset;

		m_navMeshAgent.enabled = true;
	}

	private void HandleCarryStopped()
	{
		m_isBeingCarried = false;

		// Move the log mesh to it's default position
		if (m_logMesh != null)
			m_logMesh.localPosition = Vector3.zero;

		// Hull cast then snap to the ground
		if (ValidateLogPosition(transform.position,
			Vector3.forward,
			out Vector3 finalPosition,
			out Quaternion finalRotation))
		{
			transform.position = finalPosition;
			transform.rotation = finalRotation;
			m_navMeshAgent.Warp(finalPosition);
		}

		m_navMeshAgent.enabled = false;
		m_navMeshAgent.speed = 0f;
	}

	private Transform FindClosestDuplicatinator()
	{
		EFaction executorFaction = EFaction.FACTION_PLAYER;// This should be the first guy that picks up the log

		Settlement closestFactionSettlement =
			SettlementManager.GetClosestSettlement(transform.position, executorFaction);

		if (closestFactionSettlement == null)
			return null;

		IStructure closestStructure = closestFactionSettlement.
			FindNearestStructureOfType(transform.position, m_duplicatinatorTag);

		if (closestStructure == null)
			return null;

		GameObject structureObject = closestStructure.Object;
		if (structureObject != null)
			return structureObject.transform;

		return null;
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

	public override BehaviourTree GetBehaviourTree() => m_logBT;
}
