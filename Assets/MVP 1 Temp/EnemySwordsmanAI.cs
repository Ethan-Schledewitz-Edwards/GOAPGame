using Player.Core;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemySwordsmanAI : MonoBehaviour
{
	static float c_AITickDelta;

	public float MaxWanderDist = 40;
	public float MinWanderTime = 5;
	public float MaxWanderTime = 15;
	public float VisionRange = 10;
	public float ForgetRange = 20;

	private NavMeshAgent m_NavMeshAgent;

	private Vector3 m_WanderOrigin;
	private float m_WanderTimer;
	private GameObject m_AttackingObject;

	enum EState
	{
		WANDERING,
		ATTACKING,
	}

	private EState m_CurrentState = EState.WANDERING;

	void Awake()
	{
		m_NavMeshAgent = GetComponent<NavMeshAgent>();
		m_WanderOrigin = m_NavMeshAgent.nextPosition;
	}

	void FixedUpdate()
	{
		// TODO: Evaluate less frequently for perf reasons. c_AITickDelta should reflect that.
		c_AITickDelta = Time.fixedDeltaTime;
		AIThink();
	}

	void AIThink()
	{
		if (!m_NavMeshAgent.isOnNavMesh)
		{
			Debug.LogWarning($"{name} has left the navmesh", gameObject);
			return;
		}

		switch (m_CurrentState)
		{
			case EState.WANDERING:
				WanderThink();
				break;

			case EState.ATTACKING:
				AttackThink();
				break;
		}
	}

	private void WanderThink()
	{
		// Find closest player actor we can see
		GameObject closestObjectToAttack = null;
		{
			float minSqrDistance = VisionRange * VisionRange;
			var allAttackableObjects = FindObjectsByType<PlayerEntity>();
			foreach (var attackableObject in allAttackableObjects)
			{
				//if (attackableObject.ActorFaction != Factions.Core.EFaction.FACTION_PLAYER)
				//	continue;

				float sqrDistance = Vector3.SqrMagnitude(attackableObject.transform.position - transform.position);

				if (sqrDistance < minSqrDistance)
				{
					closestObjectToAttack = attackableObject.gameObject;
					minSqrDistance = sqrDistance;
				}
			}
		}

		if (closestObjectToAttack)
		{
			// Switch state to attacking when in range of a player actor
			m_CurrentState = EState.ATTACKING;
			m_AttackingObject = closestObjectToAttack;
			m_WanderTimer = 0;
			return;
		}

		m_WanderTimer -= c_AITickDelta;

		if (m_WanderTimer <= 0)
		{
			m_WanderTimer = UnityEngine.Random.Range(MinWanderTime, MaxWanderTime);

			if (TryFindWanderingSpot(out Vector3 wanderingSpot))
			{
				m_NavMeshAgent.destination = wanderingSpot;
			}
		}
	}

	private void AttackThink()
	{
		if (!m_AttackingObject ||
			Vector3.SqrMagnitude(m_AttackingObject.transform.position - m_NavMeshAgent.nextPosition) > ForgetRange * ForgetRange)
		{
			m_CurrentState = EState.WANDERING;
			m_AttackingObject = null;
			return;
		}

		m_NavMeshAgent.destination = m_AttackingObject.transform.position;
	}

	private bool TryFindWanderingSpot(out Vector3 position)
	{
		// Wandering stores the origin to prevent going too far away
		position = Vector3.zero;

		for (int attempt = 0; attempt < 3; ++attempt)
		{
			Vector3 startPosition = m_WanderOrigin;

			float angle = UnityEngine.Random.Range(0, Mathf.PI * 2);
			float dist = UnityEngine.Random.Range(0, MaxWanderDist);

			Vector3 directionVector = new(Mathf.Cos(angle), 0, Mathf.Sin(angle));
			Vector3 randomPosition = startPosition + (directionVector * dist);

			bool foundSpot = NavMesh.SamplePosition(
				randomPosition,
				out NavMeshHit navHit,
				m_NavMeshAgent.height * 2, m_NavMeshAgent.areaMask
			);

			if (!foundSpot)
				continue;

			position = navHit.position;
			return true;
		}

		return false;
	}
}
