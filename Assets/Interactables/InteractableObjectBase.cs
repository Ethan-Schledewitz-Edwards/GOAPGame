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

	// Event
	public event Action InteractableBecameInvalid;

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
		out InteractionPosition assignedPosition,
		out int interactorValue)
	{
		interactorValue = -1;
		assignedPosition = null;

		// Validate distance to the assigned position
		if (!assignedPosition.GetPositionInRange(actorPosition))
		{
			CancelReservation(interactor, assignedPosition);
			assignedPosition = null;
			return false;
		}

		// Queue the interaction
		if (assignedPosition.TryAddInteractor(interactor, out interactorValue))
		{
			HandleActorAssigned();
			return true;
		}

		// Cleanup if adding the interactor fails
		CancelReservation(interactor, assignedPosition);
		assignedPosition = null;
		return false;
	}

	public virtual void StopInteract(IInteractor interactor, InteractionPosition assignedPosition)
	{
		if (assignedPosition != null)
		{
			assignedPosition.TryRemoveInteractor(interactor);
		}
		ReleaseActor();
	}

	public abstract BehaviourTree GetBehaviourTree();

	#region Actor Handling

	private void HandleActorAssigned()
	{
		int totalActors = GetTotalActorsPresent();

		if (totalActors > m_actorsNeeded)
			UpdateSpeed(totalActors - m_actorsNeeded);

		if (totalActors >= m_maxActors)
			InteractableBecameInvalid?.Invoke();
	}

	protected void ReleaseActor()
	{
		int totalActors = GetTotalActorsPresent();

		if (totalActors < m_actorsNeeded)
			StopInteractSpeed();
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
}
