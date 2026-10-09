using BehaviourTrees;
using System;
using UnityEngine;

// NOTE: Should probably be called InteractableBase for naming consistency,
// then dervied classes would be called WhateverInteractable. Or maybe
// BaseInteractable so the word order is consistent too?
// Maybe this could also be called BaseJobSite if there's no overlap with
// some other job feature, because it seems like the only purpose of this is to
// assign jobs to pikmin.
public abstract class ActorInteractableBase : MonoBehaviour
{
	[Header("Settings")]
	[field: SerializeField] protected int m_minActorsToFunction { get; private set; } = 1;

	[Header("Actor Interaction")]
	[SerializeField] protected InteractionPoint[] m_interactPositions;

	/// <summary>
	/// Finds the closest position this interactor can be assigned to.
	/// For reserved positions, assignment creates a reservation.
	/// For non-reserved positions, assignment is a position selection.
	/// </summary>
	public bool TryReserveClosestPosition(
		IInteractor interactor,
		Vector3 actorPosition,
		out InteractionPoint assignedPosition)
	{
		assignedPosition = null;

		if (interactor == null ||
			m_interactPositions == null ||
			m_interactPositions.Length == 0)
			return false;

		InteractionPoint closestPosition = null;
		float closestDistanceSqr = float.MaxValue;

		for (int i = 0; i < m_interactPositions.Length; i++)
		{
			InteractionPoint position = m_interactPositions[i];

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
	public virtual void CancelReservation(IInteractor interactor, InteractionPoint assignedPosition)
	{
		assignedPosition?.ReleaseReservation(interactor);
	}

	/// <summary>
	/// Begins an interaction using the previously assigned position.
	/// </summary>
	public virtual bool TryBeginInteraction(
		IInteractor interactor,
		Vector3 actorPosition,
		InteractionPoint assignedPosition,
		out int interactorValue)
	{
		interactorValue = -1;

		if (interactor == null ||
			assignedPosition == null ||
			!HasInteractionPosition(assignedPosition))
			return false;

		if (!assignedPosition.GetPositionInRange(interactor, actorPosition))
			return false;

		if (!assignedPosition.TryBeginInteraction(interactor, out interactorValue))
		{
			CancelReservation(interactor, assignedPosition);
			return false;
		}

		ActorAssigned();
		return true;
	}

	public virtual bool TryInteract(
		IInteractor interactor,
		Vector3 actorPosition,
		InteractionPoint assignedPosition,
		out int interactorValue)
	{
		return TryBeginInteraction
			(
				interactor,
				actorPosition,
				assignedPosition,
				out interactorValue
			);
	}

	public virtual void StopInteract(IInteractor interactor, InteractionPoint assignedPosition)
	{
		if (assignedPosition != null)
			assignedPosition.TryRemoveInteractor(interactor);

		int totalActors = GetTotalActorsPresent();

		if (totalActors < m_minActorsToFunction)
			LostMinimumActors();
	}

	public abstract BehaviourTree GetBehaviourTree();

	protected virtual void ActorAssigned() { }

	public virtual void LostMinimumActors() { }

	#region Availability

	public virtual bool HasAvailableWork(IInteractor interactor)
	{
		return !IsAtActorCapacity();
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
			InteractionPoint position = m_interactPositions[i];

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

		foreach (InteractionPoint position in m_interactPositions)
		{
			if (position != null)
				total += position.ActorsPresent;
		}

		return total;
	}
	#endregion

	public bool HasInteractionPosition(InteractionPoint position)
	{
		if (position == null || m_interactPositions == null)
			return false;

		return Array.IndexOf(m_interactPositions, position) >= 0;
	}
}