using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemySwordsmanAI : MonoBehaviour
{
	static float c_AITickDelta;

	public float MaxWanderDist = 40;
	public float MinWanderTime = 5;
	public float MaxWanderTime = 15;

	private NavMeshAgent m_NavMeshAgent;

	private Vector3 m_WanderOrigin;
	private float m_WanderTimer;

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
		// Just wander for now

		if (!m_NavMeshAgent.isOnNavMesh)
		{
			Debug.LogWarning($"{name} has left the navmesh", gameObject);
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
