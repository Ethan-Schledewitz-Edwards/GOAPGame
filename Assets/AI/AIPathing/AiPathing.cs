using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AIPathing : MonoBehaviour
{
	#region Constants
	private const float c_nearRange = 24.0f;
	private const float c_nearRangeSqrt = c_nearRange * c_nearRange;
	private const float c_distantRange = 36.0f;
	private const float c_distantRangeSqrt = c_distantRange * c_distantRange;

	private const float c_rotSpeed = 32.0f;
	#endregion

	// Components
	private NavMeshAgent m_navAgent;
	[field: SerializeField] public GameObject Mesh { get; private set; }

	[Header("Simulation & Navigation")]
	private EPathingSimFidelity m_simFidelity;
	private Vector3 m_destination;
	private Vector3[] m_pathCorners;
	private int m_cornersPassed;// Used in non-realtime simulations
	public NavMeshPath CurrentPath { get; private set; }
	private Coroutine m_destinationCoroutine;

	// System
	public float StoppingDistance => m_navAgent.stoppingDistance;
	public bool HasPath { get; private set; }
	public bool IsMoving{ get; private set; }

	private void Awake()
	{
		m_navAgent = GetComponent<NavMeshAgent>();
	}

	#region Simulation Fidelity

	private void TrySetActorSimFidelity(EPathingSimFidelity fidelity)
	{
		if (m_simFidelity == fidelity)
			return;

		m_simFidelity = fidelity;

		Mesh.SetActive(m_simFidelity == EPathingSimFidelity.Realtime);
		m_navAgent.enabled = (m_simFidelity == EPathingSimFidelity.Realtime);

		// Swap preexisting path to the method used for the new simulation fidelity
		if (m_destination != Vector3.zero)
			ApplyPathingByFidelity();
	}

	public void UpdateActorSimFidelity(float distToPlayerSqrt)
	{
		if (distToPlayerSqrt < c_nearRangeSqrt)
		{
			TrySetActorSimFidelity(EPathingSimFidelity.Realtime);
		}
		else if (distToPlayerSqrt < c_distantRangeSqrt)
		{
			TrySetActorSimFidelity(EPathingSimFidelity.Near);
		}
		else
		{
			TrySetActorSimFidelity(EPathingSimFidelity.Distant);
		}
	}
	#endregion

	#region Actor Pathing

	public void ClearDestination()
	{
		if (m_navAgent.isActiveAndEnabled)
			m_navAgent.ResetPath();

		m_destination = Vector3.zero;
		m_pathCorners = new Vector3[0];
		m_cornersPassed = 0;

		if (m_destinationCoroutine != null)
			StopCoroutine(m_destinationCoroutine);
	}

	public void SetDestination(Vector3 destinationPos)
	{
		if (destinationPos == Vector3.zero)
		{
			ClearDestination();
			return;
		}

		// Ignore recalculating the path if it never changed
		if (destinationPos == m_destination)
			return;

		m_destination = destinationPos;

		ApplyPathingByFidelity();
	}

	public void TickAIPathing()
	{
		if (m_simFidelity == EPathingSimFidelity.Realtime && m_navAgent.isActiveAndEnabled)
		{
			HasPath = m_navAgent.hasPath;
			IsMoving = m_navAgent.velocity.sqrMagnitude > 0.01f;
		}
		else
		{
			HasPath = (m_destination != Vector3.zero && m_pathCorners.Length > 0);
			IsMoving = (m_destination != Vector3.zero && m_destinationCoroutine != null);
		}
	}

	/// <summary>
	/// Solves a path then then moves the Actor along it
	/// </summary>
	private void ApplyPathingByFidelity()
	{
		// Reset pathing (high-fidelity)
		if (m_navAgent.isActiveAndEnabled)
			m_navAgent.ResetPath();

		// Reset pathing (low-fidelity)
		if (m_destinationCoroutine != null)
			StopCoroutine(m_destinationCoroutine);

		// Reset pathing corners
		m_pathCorners = new Vector3[0];
		m_cornersPassed = 0;

		// Determine pathing solution
		switch (m_simFidelity)
		{
			case EPathingSimFidelity.Realtime:
				if (m_navAgent.SetDestination(m_destination))
				{
					CurrentPath = m_navAgent.path;
					m_pathCorners = CurrentPath.corners;
				}
				break;

			case EPathingSimFidelity.Near:
				NavMeshPath nearPath = new NavMeshPath();
				if (NavMesh.CalculatePath(transform.position, m_destination, NavMesh.AllAreas, nearPath))
				{
					CurrentPath = nearPath;
					m_pathCorners = CurrentPath.corners;
					m_destinationCoroutine = StartCoroutine(FollowPath(CurrentPath.corners, m_navAgent.speed, true));
				}
				break;

			case EPathingSimFidelity.Distant:
				NavMeshPath distantPath = new NavMeshPath();
				if (NavMesh.CalculatePath(transform.position, m_destination, NavMesh.AllAreas, distantPath))
				{
					CurrentPath = distantPath;
					m_pathCorners = CurrentPath.corners;
					m_destinationCoroutine = StartCoroutine(FollowPath(CurrentPath.corners, m_navAgent.speed, false));
				}
				break;
		}
	}

	public void FaceTarget(Vector3 targetLocation)
	{
		if (targetLocation != Vector3.zero)
		{
			Vector3 dirToTarget = targetLocation - transform.position;
			dirToTarget.y = 0;

			// Smoothly look at target
			if (dirToTarget.sqrMagnitude > 0.001f)
			{
				Quaternion targetRotation = Quaternion.LookRotation(dirToTarget, Vector3.up);
				transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, c_rotSpeed * Time.deltaTime);
			}
		}
	}

	private IEnumerator FollowPath(Vector3[] waypoints, float moveSpeed, bool isLerped)
	{
		m_cornersPassed = 0;

		for (int i = 0; i < waypoints.Length - 1; i++)
		{
			Vector3 start = waypoints[i];
			Vector3 end = waypoints[i + 1];

			float dist = Vector3.Distance(start, end);
			float travelTime = dist / moveSpeed;
			float inverseTime = 1f / travelTime;

			float t = 0.0f;
			while (t < 1.0f)
			{
				t += Time.deltaTime * inverseTime;

				if (isLerped)
				{
					transform.position = Vector3.Lerp(start, end, t);

					// Face destination
					Vector3 lookDir = end - transform.position;
					lookDir.y = 0;

					if (lookDir.sqrMagnitude > 0.1f)
					{
						Quaternion targetRotation = Quaternion.LookRotation(lookDir, Vector3.up);
						transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, c_rotSpeed * t);
					}
				}

				yield return null;
			}

			transform.position = end;
			m_cornersPassed++;

			// Face next waypoint
			if (i + 2 < waypoints.Length)
			{
				Vector3 lookDir = waypoints[i + 2] - transform.position;
				lookDir.y = 0;
				Quaternion targetRotation = Quaternion.LookRotation(lookDir, Vector3.up);
				transform.rotation = targetRotation;
			}
		}

		m_destination = Vector3.zero;
		CurrentPath = null;
		m_pathCorners = new Vector3[0];
		m_cornersPassed = 0;

		if (m_destinationCoroutine != null)
			StopCoroutine(m_destinationCoroutine);
	}

	/// <summary>
	/// Calculates the distance remaining of an actors current path 
	/// </summary>
	public float PathDistRemaining()
	{
		if (CurrentPath == null)
			return 0.0f;

		// Wait for high-fidelty path to calculate
		if (m_simFidelity == EPathingSimFidelity.Realtime && m_navAgent.enabled)
			return m_navAgent.pathPending ? float.MaxValue : m_navAgent.remainingDistance;

		// Wait for path corners to calculate
		if (m_destination != Vector3.zero && (m_pathCorners == null || m_pathCorners.Length == 0))
			return float.MaxValue;

		// Use high-fidelty path distance for real time Actors
		if (m_simFidelity == EPathingSimFidelity.Realtime)
			return m_navAgent.remainingDistance;

		float distanceRemaining = 0.0f;
		Vector3 actorPos = transform.position;

		if (m_pathCorners.Length == 1)
			return Vector3.Distance(actorPos, m_pathCorners[0]);

		if (m_cornersPassed + 1 < m_pathCorners.Length)
		{
			// Distance to the immediate next waypoint
			distanceRemaining += Vector3.Distance(actorPos, m_pathCorners[m_cornersPassed + 1]);

			// Distance of all segments following the first waypoint
			for (int i = m_cornersPassed + 1; i < m_pathCorners.Length - 1; i++)
			{
				distanceRemaining += Vector3.Distance(m_pathCorners[i], m_pathCorners[i + 1]);
			}
		}

		return distanceRemaining;
	}

	/// <summary>
	/// Returns true if the actor has a destination but their path is either pending when Realtime, or the corners are empty when low-fidelity.
	/// </summary>
	public bool IsCalculatingPath()
	{
		if (m_simFidelity == EPathingSimFidelity.Realtime
			&& m_navAgent.enabled
			&& m_navAgent.hasPath
			&& m_navAgent.pathPending
			)
		{
			return true;
		}
		else if (m_destination != Vector3.zero && (m_pathCorners == null || m_pathCorners.Length == 0))
		{
			return true;
		}

		return false;
	}
	#endregion

	public void SetStoppingDistance(float stoppingDistance)
	{
		m_navAgent.stoppingDistance = stoppingDistance;
	}

	public void SetSpeed(float speed)
	{
		m_navAgent.speed = speed;
	}
}
