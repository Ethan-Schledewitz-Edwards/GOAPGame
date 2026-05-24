using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AIPathing : MonoBehaviour
{
	#region Constants
	private const float c_nearRange = 16.0f;
	private const float c_nearRangeSqrt = c_nearRange * c_nearRange;
	private const float c_distantRange = 32.0f;
	private const float c_distantRangeSqrt = c_distantRange * c_distantRange;

	private const float c_waitingForJobLimit = 10.0f;

	private const float c_followDist = 1.2f;
	private const float c_workingDist = 0.15f;

	private const float c_followSpeed = 5.2f;
	private const float c_workingSpeed = 4.5f;
	private const float c_offDutySpeed = 2f;

	private const float c_rotSpeed = 24.0f;
	#endregion

	// Components
	public NavMeshAgent NavAgent { get; private set; }
	[field: SerializeField] public GameObject Mesh { get; private set; }

	[Header("Simulation & Navigation")]
	private EPathingSimFidelity m_simFidelity;
	private Vector3 m_destination;
	private Vector3[] m_pathCorners;
	private int m_cornersPassed;// Used in non-realtime simulations
	public NavMeshPath CurrentPath { get; private set; }
	private Coroutine m_destinationCoroutine;

	private void Awake()
	{
		NavAgent = GetComponent<NavMeshAgent>();
	}

	#region Simulation Fidelity

	private void TrySetActorSimFidelity(EPathingSimFidelity fidelity)
	{
		if (m_simFidelity == fidelity)
			return;

		m_simFidelity = fidelity;

		Mesh.SetActive(m_simFidelity == EPathingSimFidelity.Realtime);
		NavAgent.enabled = (m_simFidelity == EPathingSimFidelity.Realtime);

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
		if (NavAgent.isActiveAndEnabled)
			NavAgent.ResetPath();

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

	/// <summary>
	/// Solves a path then then moves the Actor along it
	/// </summary>
	private void ApplyPathingByFidelity()
	{
		// Reset pathing (high-fidelity)
		if (NavAgent.isActiveAndEnabled)
			NavAgent.ResetPath();

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
				if (NavAgent.SetDestination(m_destination))
				{
					CurrentPath = NavAgent.path;
					m_pathCorners = CurrentPath.corners;
				}
				break;

			case EPathingSimFidelity.Near:
				NavMeshPath nearPath = new NavMeshPath();
				if (NavMesh.CalculatePath(transform.position, m_destination, NavMesh.AllAreas, nearPath))
				{
					CurrentPath = nearPath;
					m_pathCorners = CurrentPath.corners;
					m_destinationCoroutine = StartCoroutine(FollowPath(CurrentPath.corners, NavAgent.speed, true));
				}
				break;

			case EPathingSimFidelity.Distant:
				NavMeshPath distantPath = new NavMeshPath();
				if (NavMesh.CalculatePath(transform.position, m_destination, NavMesh.AllAreas, distantPath))
				{
					CurrentPath = distantPath;
					m_pathCorners = CurrentPath.corners;
					m_destinationCoroutine = StartCoroutine(FollowPath(CurrentPath.corners, NavAgent.speed, false));
				}
				break;
		}
	}

	public void HandleRotation(Vector3 targetLocation, float t)
	{
		if (targetLocation != Vector3.zero)
		{
			Vector3 dirToTarget = targetLocation - transform.position;
			dirToTarget.y = 0;

			// Smoothly look at target
			Quaternion targetRotation = Quaternion.LookRotation(dirToTarget, Vector3.up);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, c_rotSpeed * t);
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
		if (m_simFidelity == EPathingSimFidelity.Realtime && NavAgent.enabled)
			return NavAgent.pathPending ? float.MaxValue : NavAgent.remainingDistance;

		// Wait for path corners to calculate
		if (m_destination != Vector3.zero && (m_pathCorners == null || m_pathCorners.Length == 0))
			return float.MaxValue;

		// Use high-fidelty path distance for real time Actors
		if (m_simFidelity == EPathingSimFidelity.Realtime)
			return NavAgent.remainingDistance;

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
			&& NavAgent.enabled
			&& NavAgent.hasPath
			&& NavAgent.pathPending
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
}
