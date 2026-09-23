using BehaviourTrees;
using System;
using UnityEngine;

public abstract class InteractableObjectBase : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField] private int m_minActorsNeededToOperate = 1;

	[Header("Actor Interaction")]
	[SerializeField] protected InteractionPosition[] m_interactPositions;

	/// <summary>
	/// Finds the closest position this interactor can be assigned to.
	/// For reserved positions, assignment creates a reservation.
	/// For non-reserved positions, assignment is a position selection.
	/// </summary>
	public bool TryReserveClosestPosition(
		IInteractor interactor,
		Vector3 actorPosition,
		out InteractionPosition assignedPosition)
	{
		assignedPosition = null;

		if (interactor == null ||
			m_interactPositions == null ||
			m_interactPositions.Length == 0)
			return false;

		InteractionPosition closestPosition = null;
		float closestDistanceSqr = float.MaxValue;

		for (int i = 0; i < m_interactPositions.Length; i++)
		{
			InteractionPosition position = m_interactPositions[i];

			if (position == null || !position.HasAvailableCapacity)
				continue;

			float distanceSqr =
				(position.transform.position - actorPosition).sqrMagnitude;

			if (distanceSqr < closestDistanceSqr)
			{
				closestDistanceSqr = distanceSqr;
				closestPosition = position;
			}
		}

		if (closestPosition == null)
			return false;

		if (!closestPosition.TryReservePosition(interactor))
			return false;

		assignedPosition = closestPosition;
		return true;
	}

	/// <summary>
	/// Cancels a pending reservation.
	/// </summary>
	public virtual void CancelReservation(
		IInteractor interactor,
		InteractionPosition assignedPosition)
	{
		assignedPosition?.ReleaseReservation(interactor);
	}

	/// <summary>
	/// Begins an interaction using the previously assigned position.
	/// </summary>
	public virtual bool TryBeginInteraction(
		IInteractor interactor,
		Vector3 actorPosition,
		InteractionPosition assignedPosition,
		out int interactorValue)
	{
		interactorValue = -1;

		if (interactor == null ||
			assignedPosition == null ||
			!HasInteractionPosition(assignedPosition))
			return false;

		if (!assignedPosition.GetPositionInRange(interactor, actorPosition))
			return false;

		if (!assignedPosition.TryBeginInteraction(
			interactor,
			out interactorValue))
		{
			CancelReservation(interactor, assignedPosition);
			return false;
		}

		HandleActorAssigned();
		return true;
	}

	public virtual bool TryInteract(
		IInteractor interactor,
		Vector3 actorPosition,
		InteractionPosition assignedPosition,
		out int interactorValue)
	{
		return TryBeginInteraction(
			interactor,
			actorPosition,
			assignedPosition,
			out interactorValue);
	}

	public virtual void StopInteract(
		IInteractor interactor,
		InteractionPosition assignedPosition)
	{
		if (assignedPosition != null)
			assignedPosition.TryRemoveInteractor(interactor);

		int totalActors = GetTotalActorsPresent();

		if (totalActors < m_minActorsNeededToOperate)
			StopInteractSpeed();
	}

	public abstract BehaviourTree GetBehaviourTree();

	#region Actor Handling

	private void HandleActorAssigned()
	{
		int totalActors = GetTotalActorsPresent();

		if (totalActors > m_minActorsNeededToOperate)
			UpdateSpeed(totalActors - m_minActorsNeededToOperate);
	}

	#endregion

	#region Availability

	public virtual bool HasAvailableWork(IInteractor interactor)
	{
		if (m_interactPositions == null ||
			m_interactPositions.Length == 0)
			return false;

		for (int i = 0; i < m_interactPositions.Length; i++)
		{
			InteractionPosition position = m_interactPositions[i];

			if (position != null && position.HasAvailableCapacity)
				return true;
		}

		return false;
	}

	/// <summary>
	/// Returns true when every interaction position is unavailable.
	/// </summary>
	public bool IsAtActorCapacity()
	{
		if (m_interactPositions == null ||
			m_interactPositions.Length == 0)
			return true;

		for (int i = 0; i < m_interactPositions.Length; i++)
		{
			InteractionPosition position = m_interactPositions[i];

			if (position != null && position.HasAvailableCapacity)
				return false;
		}

		return true;
	}

	public int GetTotalActorsPresent()
	{
		if (m_interactPositions == null ||
			m_interactPositions.Length == 0)
			return 0;

		int total = 0;

		foreach (InteractionPosition position in m_interactPositions)
		{
			if (position != null)
				total += position.ActorsPresent;
		}

		return total;
	}

	public int GetTotalOccupiedOrReserved()
	{
		if (m_interactPositions == null ||
			m_interactPositions.Length == 0)
			return 0;

		int total = 0;

		foreach (InteractionPosition position in m_interactPositions)
		{
			if (position != null)
				total += position.TotalOccupiedOrReserved;
		}

		return total;
	}

	#endregion

	public bool HasInteractionPosition(InteractionPosition position)
	{
		if (position == null || m_interactPositions == null)
			return false;

		return Array.IndexOf(m_interactPositions, position) >= 0;
	}

	public abstract void UpdateSpeed(int extra);

	public abstract void StopInteractSpeed();
}