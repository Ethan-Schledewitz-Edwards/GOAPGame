using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

[RequireComponent(typeof(NavMeshAgent))]
public class AIPathing : MonoBehaviour
{
	#region Constants
	private const float c_nearRange = 24.0f;
	private const float c_nearRangeSqrt = c_nearRange * c_nearRange;
	private const float c_distantRange = 36.0f;
	private const float c_distantRangeSqrt = c_distantRange * c_distantRange;

	private const float c_rotSpeed = 16.0f;
	#endregion

	// Components
	[field: SerializeField] public GameObject Mesh { get; private set; }
	private NavMeshAgent m_navAgent;

	[Header("Simulation & Navigation")]
	public Vector3 CurrentDestination { get; private set; }
	private EPathingSimFidelity m_simFidelity;
	private Vector3[] m_pathCorners;
	private int m_cornersPassed;// Used in non-realtime simulations
	private NavMeshPath m_currentPath;
	private Coroutine m_destinationCoroutine;

	// System
	public float StoppingDistance => m_navAgent.stoppingDistance;
	public bool HasPath { get; private set; }
	public bool IsMoving { get; private set; }

	private void Awake()
	{
		m_navAgent = GetComponent<NavMeshAgent>();
	}

	#region Simulation Fidelity

	public void TrySetActorSimFidelity(EPathingSimFidelity fidelity)
	{
		if (m_simFidelity == fidelity)
			return;

		m_simFidelity = fidelity;

		Mesh.SetActive(m_simFidelity == EPathingSimFidelity.Realtime);
		m_navAgent.enabled = (m_simFidelity == EPathingSimFidelity.Realtime);

		// Swap preexisting path to the method used for the new simulation fidelity
		if (CurrentDestination != Vector3.zero)
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

	public void SetDestination(Vector3 destinationPosition)
	{
		if (destinationPosition == Vector3.zero)
		{
			ClearDestination();
			return;
		}

		// Only rebuild the path if the difference between
		// the new and previous destinations is significant
		if ((destinationPosition - CurrentDestination).sqrMagnitude < 0.01f)
		{
			if (m_simFidelity == EPathingSimFidelity.Realtime &&
				m_navAgent.isActiveAndEnabled &&
				(m_navAgent.pathPending || m_navAgent.hasPath))
				return;

			if (m_simFidelity != EPathingSimFidelity.Realtime &&
				m_destinationCoroutine != null)
				return;
		}

		CurrentDestination = destinationPosition;

		ApplyPathingByFidelity();
	}

	public void ClearDestination()
	{
		if (m_navAgent.isActiveAndEnabled)
			m_navAgent.ResetPath();

		CurrentDestination = Vector3.zero;
		m_pathCorners = new Vector3[0];
		m_cornersPassed = 0;
		m_currentPath = null;

		if (m_destinationCoroutine != null)
		{
			StopCoroutine(m_destinationCoroutine);
			m_destinationCoroutine = null;
		}
		HasPath = false;
		IsMoving = false;
	}

	public void SetPosition(Vector3 worldPosition)
	{
		if (m_navAgent != null)
		{
			m_navAgent.Warp(worldPosition);
		}
		else
		{
			transform.position = worldPosition;
		}
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
			HasPath = (CurrentDestination != Vector3.zero && m_pathCorners.Length > 0);
			IsMoving = (CurrentDestination != Vector3.zero && m_destinationCoroutine != null);
		}
	}

	/// <summary>
	/// Solves a path then then moves the Actor along it.
	/// </summary>
	private void ApplyPathingByFidelity()
	{
		// Reset pathing (high-fidelity)
		if (m_navAgent.isActiveAndEnabled)
			m_navAgent.ResetPath();

		// Reset pathing (low-fidelity)
		if (m_destinationCoroutine != null)
		{
			StopCoroutine(m_destinationCoroutine);
			m_destinationCoroutine = null;
		}

		// Reset pathing corners
		m_pathCorners = new Vector3[0];
		m_cornersPassed = 0;

		// Determine pathing solution
		switch (m_simFidelity)
		{
			case EPathingSimFidelity.Realtime:
				m_navAgent.SetDestination(CurrentDestination);

				m_currentPath = null;
				m_pathCorners = new Vector3[0];
				break;

			case EPathingSimFidelity.Near:
				NavMeshPath nearPath = new NavMeshPath();
				NavMesh.CalculatePath(transform.position, CurrentDestination, NavMesh.AllAreas, nearPath);
				m_currentPath = nearPath;

				if (nearPath.status == NavMeshPathStatus.PathComplete)
				{
					m_pathCorners = nearPath.corners;
					m_destinationCoroutine = StartCoroutine(FollowPath(nearPath.corners, m_navAgent.speed, true));
				}
				break;

			case EPathingSimFidelity.Distant:
				NavMeshPath distantPath = new NavMeshPath();
				NavMesh.CalculatePath(transform.position, CurrentDestination, NavMesh.AllAreas, distantPath);
				m_currentPath = distantPath;

				if (distantPath.status == NavMeshPathStatus.PathComplete)
				{
					m_pathCorners = distantPath.corners;
					m_destinationCoroutine = StartCoroutine(FollowPath(distantPath.corners, m_navAgent.speed, false));
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
			if (dist <= 0.001f)
			{
				transform.position = end;
				m_cornersPassed++;
				continue;
			}

			float safeMoveSpeed = Mathf.Max(0.01f, moveSpeed);
			float travelTime = dist / safeMoveSpeed;
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

		CurrentDestination = Vector3.zero;
		m_currentPath = null;
		m_pathCorners = new Vector3[0];
		m_cornersPassed = 0;

		m_destinationCoroutine = null;
	}

	/// <summary>
	/// Calculates the distance remaining of an actors current path 
	/// </summary>
	public float PathDistRemaining()
	{
		if (CurrentDestination == Vector3.zero)
			return 0.0f;

		// Fetch realtime path distance
		if (m_simFidelity == EPathingSimFidelity.Realtime && m_navAgent.enabled)
		{
			if (m_navAgent.pathPending)
				return float.MaxValue;

			if (!m_navAgent.hasPath || m_navAgent.pathStatus != NavMeshPathStatus.PathComplete)
				return float.MaxValue;

			return m_navAgent.remainingDistance;
		}

		// Ignore incomplete low-fi paths
		if (m_currentPath == null || m_currentPath.status != NavMeshPathStatus.PathComplete)
			return float.MaxValue;

		// Ignore incomplete low-fi paths
		if (m_pathCorners == null || m_pathCorners.Length == 0)
			return float.MaxValue;

		// Calculate the distance remaining for a low-fi path
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
		if (m_simFidelity == EPathingSimFidelity.Realtime &&
			m_navAgent.enabled &&
			CurrentDestination != Vector3.zero)
		{
			return m_navAgent.pathPending;
		}

		return false;
	}

	/// <summary>
	/// Returns true when the transform is physically within the 
	/// horizontal distance of a world-space position. 
	/// </summary>
	/// <remarks>
	/// All actor/behaviour-tree distance checks should use this method
	/// so they share the exact same X/Z distance calculation.
	/// </remarks>
	public bool IsWithinDistance(Vector3 targetPosition, float distance)
	{
		float effectiveDistance = Mathf.Max(distance, 0.05f);

		Vector3 flatDelta = transform.position - targetPosition;
		flatDelta.y = 0f;

		return flatDelta.sqrMagnitude <= effectiveDistance * effectiveDistance;
	}

	/// <summary>
	/// Returns true when the transform is physically within 
	/// arrival distance of its current destination.
	/// </summary>
	/// <remarks>
	/// The arrival distance is the larger value of NavMeshAgent.stoppingDistance, 
	/// the supplied tolerance, and a small minimum tolerance.
	/// </remarks>
	public bool HasReachedDestination(float additionalTolerance = 0f)
	{
		if (CurrentDestination == Vector3.zero)
			return true;

		float arrivalDistance = Mathf.Max(StoppingDistance, additionalTolerance, 0.05f);

		return IsWithinDistance(CurrentDestination, arrivalDistance);
	}

	/// <summary>
	/// Returns true when the current destination cannot be reached by the
	/// current navigation mode.
	/// </summary>
	public bool IsDestinationInvalid()
	{
		if (CurrentDestination == Vector3.zero)
			return false;

		// Being physically close to the requested point takes precedence over path status.
		if (HasReachedDestination(Mathf.Max(StoppingDistance, 0.05f)))
			return false;

		if (m_simFidelity == EPathingSimFidelity.Realtime && m_navAgent.isActiveAndEnabled)
		{
			if (m_navAgent.pathPending)
				return false;

			if (!m_navAgent.hasPath)
				return true;

			return m_navAgent.pathStatus != NavMeshPathStatus.PathComplete;
		}

		return m_currentPath == null ||
			m_currentPath.status != NavMeshPathStatus.PathComplete ||
			m_pathCorners == null ||
			m_pathCorners.Length == 0;
	}

	#endregion

	public void SetStoppingDistance(float stoppingDistance) =>
		m_navAgent.stoppingDistance = stoppingDistance;

	public void SetSpeed(float speed) =>
		m_navAgent.speed = speed;
}
