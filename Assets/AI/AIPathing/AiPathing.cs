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
	public NavMeshAgent NavAgent { get; private set; }

	[Header("Simulation & Navigation")]
	public Vector3 CurrentDestination { get; private set; }
	private EPathingSimFidelity m_simFidelity;
	private Vector3[] m_pathCorners;
	private int m_cornersPassed;// Used in non-realtime simulations
	private NavMeshPath m_currentPath;
	private Coroutine m_destinationCoroutine;

	// System
	// NOTE: There are inconsistent ways to access and modify stopping distance.
	// I think it would be better to avoid get and set functions in early stages
	// and use properties instead. In this example it would look like:
	/*
	public float StoppingDistance
	{
		get => m_navAgent.stoppingDistance;
		set => m_navAgent.stoppingDistance = value;
	}
	*/
	// There's also something to be said about the benefit of making almost everything
	// public. This can allow you to new and unexpected things without having to jump
	// through hoops. The purpose of private members is to stop bad programmers from
	// accidentally breaking things, but I don't think we'll have that problem.
	// Making things
	public float StoppingDistance => NavAgent.stoppingDistance;
	public bool HasPath { get; private set; }
	public bool IsMoving { get; private set; }

	private void Awake()
	{
		NavAgent = GetComponent<NavMeshAgent>();
	}

	#region Actor Pathing

	public void SetDestination(Vector3 destinationPosition)
	{
		if (destinationPosition == Vector3.zero)
		{
			ClearDestination();
			return;
		}

		CurrentDestination = destinationPosition;
		NavAgent.SetDestination(destinationPosition);
	}

	public void ClearDestination()
	{
		if (NavAgent.isActiveAndEnabled)
			NavAgent.ResetPath();

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
		if (NavAgent != null)
		{
			NavAgent.Warp(worldPosition);
		}
		else
		{
			transform.position = worldPosition;
		}
	}

	public void TickAIPathing()
	{
		if (m_simFidelity == EPathingSimFidelity.Realtime && NavAgent.isActiveAndEnabled)
		{
			HasPath = NavAgent.hasPath;
			IsMoving = NavAgent.velocity.sqrMagnitude > 0.01f;
		}
		else
		{
			HasPath = (CurrentDestination != Vector3.zero && m_pathCorners.Length > 0);
			IsMoving = (CurrentDestination != Vector3.zero && m_destinationCoroutine != null);
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

				transform.rotation = Quaternion.Slerp(transform.rotation,
					targetRotation,
					c_rotSpeed * Time.deltaTime);
			}
		}
	}

	/// <summary>
	/// Calculates the distance remaining of an actors current path 
	/// </summary>
	public float PathDistRemaining()
	{
		if (CurrentDestination == Vector3.zero)
			return 0.0f;

		// Fetch realtime path distance
		if (m_simFidelity == EPathingSimFidelity.Realtime && NavAgent.enabled)
		{
			if (NavAgent.pathPending)
				return float.MaxValue;

			if (!NavAgent.hasPath || NavAgent.pathStatus != NavMeshPathStatus.PathComplete)
				return float.MaxValue;

			return NavAgent.remainingDistance;
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
			NavAgent.enabled &&
			CurrentDestination != Vector3.zero)
		{
			return NavAgent.pathPending;
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

		// Check if the intended destination was reached
		if (IsWithinDistance(CurrentDestination, arrivalDistance))
			return true;

		// Check if we reached the end of the calculated NavMesh path
		if (m_simFidelity == EPathingSimFidelity.Realtime && NavAgent.isActiveAndEnabled)
		{
			if (!NavAgent.pathPending && NavAgent.hasPath)
			{
				if (NavAgent.remainingDistance <= arrivalDistance)
					return true;
			}
		}
		else if (m_destinationCoroutine == null)
		{
			return true; // Low-fidelity simulation completed its path
		}

		return false;
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

		if (m_simFidelity == EPathingSimFidelity.Realtime && NavAgent.isActiveAndEnabled)
		{
			if (NavAgent.pathPending)
				return false;

			if (!NavAgent.hasPath)
				return true;

			return NavAgent.pathStatus != NavMeshPathStatus.PathComplete;
		}

		return m_currentPath == null ||
			m_currentPath.status != NavMeshPathStatus.PathComplete ||
			m_pathCorners == null ||
			m_pathCorners.Length == 0;
	}
	#endregion

	public void SetStoppingDistance(float stoppingDistance) =>
		NavAgent.stoppingDistance = stoppingDistance;

	public void SetSpeed(float speed) =>
		NavAgent.speed = speed;
}
