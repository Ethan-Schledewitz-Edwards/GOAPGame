using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody), typeof(NavMeshAgent))]
public class LogCarryableActorInteractable : ActorInteractableBase
{
	private static BehaviourTree m_logBT;

	private const float c_logMeshCarryOffset = 0.5f;

	protected override int m_minActorsToFunction { get; set; } = 2;

	[Header("Carry Settings")]
	[SerializeField] private float m_baseMoveSpeed = 3f;

	[Header("Mesh")]
	[SerializeField] private Transform m_logMesh;

	// Components
	private Rigidbody m_rigidbody;
	private NavMeshAgent m_navMeshAgent;
	private Transform m_currentTarget;

	// System
	private bool m_isBeingCarried;

	private void Awake()
	{
		m_rigidbody = GetComponent<Rigidbody>();
		m_navMeshAgent = GetComponent<NavMeshAgent>();
		m_navMeshAgent.enabled = false;

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
		Transform closestDuplicatinator = FindClosestDuplicatinator();
		if(m_currentTarget != closestDuplicatinator)
		{
			m_currentTarget = closestDuplicatinator;
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

		m_navMeshAgent.enabled = false;
		m_navMeshAgent.speed = 0f;
	}

	private Transform FindClosestDuplicatinator()
	{
		return null;
	}

	public override BehaviourTree GetBehaviourTree() => m_logBT;
}
