using BehaviourTrees;
using Interaction.Core;
using System;
using UnityEngine;

public abstract class ActorInteractableBase : MonoBehaviour, IInteractable
{
	[Header("Settings")]
	protected abstract int m_minActorsToFunction { get; set; }

	// IInteractable properties
	public string InteractPrompt => m_interactPrompt;
	protected abstract string m_interactPrompt { get; }

	public Vector3 LocalPromptOffset => m_localPromptOffset;
	[SerializeField] private Vector3 m_localPromptOffset;

	public bool HasTrackingDisplayInfo => m_hasTrackingDisplayInfo;
	protected abstract bool m_hasTrackingDisplayInfo { get; }

	public Transform InteractableTransform => transform;


	[Header("Actor Interaction")]
	[SerializeField] protected InteractionPoint[] m_interactPositions;

	public event Action OnInfoUpdated;
	public event Action InteractableDestroyed;

	protected virtual void OnDestroy()
	{
		InteractableDestroyed?.Invoke();
	}

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

			if (position == null || !position.HasAvailableCapacity())
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
	/// Begins an interaction using an assigned interaction position.
	/// </summary>
	public bool TryBeginInteraction(
		IInteractor interactor,
		Vector3 actorPosition,
		InteractionPoint assignedPosition,
		out int interactorValue)
	{
		interactorValue = -1;

		if (interactor == null ||
			assignedPosition == null ||
			!HasInteractionPosition(assignedPosition))
		{
			return false;
		}

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

	/// <summary>
	/// Performs the normal interaction entry point.
	/// Specialized interactables can extend this to acquire jobs
	/// or perform other interaction-specific actions.
	/// </summary>
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

			if (position != null && position.HasAvailableCapacity())
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

	public virtual void UpdateInteractableInfo()
	{
		OnInfoUpdated?.Invoke();
	}
}