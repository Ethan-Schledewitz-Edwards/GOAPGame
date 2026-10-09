using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody), typeof(NavMeshAgent))]
public class LogCarryableActorInteractable : ActorInteractableBase
{
	private static BehaviourTree m_logBT;

	[Header("Carry Settings")]
	[SerializeField] private float m_baseMoveSpeed = 3f;
	[SerializeField] private Transform m_deliveryTarget;

	private Rigidbody m_rigidbody;
	private NavMeshAgent m_navMeshAgent;
	private int m_currentPathIndex;

	public bool HasReachedDestination { get; private set; }

	private void Awake()
	{
		m_rigidbody = GetComponent<Rigidbody>();
		m_navMeshAgent = GetComponent<NavMeshAgent>();

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

	protected override void ActorAssigned()
	{
		int actorsPresent = GetTotalActorsPresent();

		if (actorsPresent >= m_minActorsToFunction)
		{
			m_navMeshAgent.enabled = true;
			m_navMeshAgent.speed = m_baseMoveSpeed + (actorsPresent * 1.5f);
		}
	}

	public override void LostMinimumActors()
	{
		m_navMeshAgent.enabled = false;
		m_navMeshAgent.speed = 0f;
	}

	public override BehaviourTree GetBehaviourTree() => m_logBT;
}
