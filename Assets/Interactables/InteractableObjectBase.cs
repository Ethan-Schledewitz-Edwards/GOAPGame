using BehaviourTrees;
using System;
using UnityEngine;

public abstract class InteractableObjectBase : MonoBehaviour
{
	[Header("Settings")]
	[field: SerializeField] public bool RequiresReservation { get; private set; } = true;
	[SerializeField] private int m_actorsNeeded = 1;
	[SerializeField] private int m_maxActors = 1;

	[Header("Actor Interaction")]
	[SerializeField] protected InteractionPosition[] m_interactPositions;

	/// <summary>
	/// Attempts to find the closest available interaction position and reserves it for the interactor.
	/// </summary>
	public bool TryReserveClosestPosition(IInteractor interactor, Vector3 actorPosition, out InteractionPosition assignedPosition)
	{
		assignedPosition = null;

		if (GetTotalOccupiedOrReserved() >= m_maxActors ||
			m_interactPositions == null ||
			m_interactPositions.Length == 0)
			return false;

		InteractionPosition closestPosition = null;
		float minDistanceSqr = float.MaxValue;

		foreach (var pos in m_interactPositions)
		{
			if (pos == null || !pos.HasAvailableCapacity)
				continue;

			float distanceSqr = (pos.transform.position - actorPosition).sqrMagnitude;
			if (distanceSqr < minDistanceSqr)
			{
				minDistanceSqr = distanceSqr;
				closestPosition = pos;
			}
		}

		// Reserve the closest interaction position for the interactor
		if (closestPosition != null && closestPosition.TryReserve(interactor))
		{
			assignedPosition = closestPosition;
			return true;
		}

		return false;
	}

	/// <summary>
	/// Cancels a pending reservation on an interaction position.
	/// </summary>
	public virtual void CancelReservation(IInteractor interactor, InteractionPosition assignedPosition)
	{
		if (assignedPosition != null)
		{
			assignedPosition.ReleaseReservation(interactor);
		}
	}

	/// <summary>
	/// Called by the Interactor. Evaluates the request, attempts to reserve a position, 
	/// and returns the success state back to the Interactor.
	/// </summary>
	public virtual bool TryInteract(IInteractor interactor,
		Vector3 actorPosition,
		InteractionPosition reservedPosition,
		out int interactorValue)
	{
		interactorValue = -1;

		if (reservedPosition == null)
			return false;

		// Validate distance to the assigned position. Being slightly early is
		// not a failed reservation: the reservation exists specifically so the
		// actor can approach this position without another actor taking it.
		// The caller can release the reservation when it abandons the job.
		if (!reservedPosition.GetPositionInRange(interactor, actorPosition))
			return false;

		// Queue the interaction
		if (reservedPosition.TryAddInteractor(interactor, out interactorValue))
		{
			HandleActorAssigned();
			return true;
		}

		// Cleanup if adding the interactor fails
		CancelReservation(interactor, reservedPosition);
		return false;
	}

	public virtual void StopInteract(IInteractor interactor, InteractionPosition assignedPosition)
	{
		if (assignedPosition != null)
		{
			assignedPosition.TryRemoveInteractor(interactor);
		}

		int totalActors = GetTotalActorsPresent();

		if (totalActors < m_actorsNeeded)
			StopInteractSpeed();
	}

	public abstract BehaviourTree GetBehaviourTree();

	#region Actor Handling

	private void HandleActorAssigned()
	{
		int totalActors = GetTotalActorsPresent();

		if (totalActors > m_actorsNeeded)
			UpdateSpeed(totalActors - m_actorsNeeded);
	}
	#endregion

	/// <summary>
	/// Returns the total number of actors present across all interaction positions.
	/// </summary>
	public int GetTotalActorsPresent()
	{
		if (m_interactPositions == null || m_interactPositions.Length == 0)
			return 0;

		int total = 0;
		foreach (var pos in m_interactPositions)
		{
			if (pos != null)
			{
				total += pos.ActorsPresent;
			}
		}
		return total;
	}

	/// <summary>
	/// Returns total actors present plus incoming reservations.
	/// </summary>
	public int GetTotalOccupiedOrReserved()
	{
		if (m_interactPositions == null || m_interactPositions.Length == 0)
			return 0;

		int total = 0;
		foreach (var pos in m_interactPositions)
		{
			if (pos != null)
			{
				total += pos.TotalOccupiedOrReserved;
			}
		}
		return total;
	}

	public abstract void UpdateSpeed(int extra);

	public abstract void StopInteractSpeed();

	public bool IsAtActorCapacity() => GetTotalActorsPresent() >= m_maxActors;

	/// <summary>
	/// Returns true when the supplied interaction position belongs to this
	/// interactable. Used by behaviour-tree tasks to distinguish a position
	/// inherited from a previous target from a position belonging to the
	/// current target.
	/// </summary>
	public bool HasInteractionPosition(InteractionPosition position)
	{
		if (position == null || m_interactPositions == null)
			return false;

		return Array.IndexOf(m_interactPositions, position) >= 0;
	}
}
