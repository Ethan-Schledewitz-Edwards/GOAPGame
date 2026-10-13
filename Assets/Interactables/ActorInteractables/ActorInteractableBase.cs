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
	private InteractionPoint[] m_interactionPoints;

	public event Action OnInfoUpdated;
	public event Action InteractableDestroyed;

	protected virtual void Awake()
	{
		m_interactionPoints = GetComponentsInChildren<InteractionPoint>();
		int interactionPointsCount = m_interactionPoints.Length;

		if(interactionPointsCount == 0)
		{
			// Fallback to this component
			m_interactionPoints = GetComponents<InteractionPoint>();
			interactionPointsCount = m_interactionPoints.Length;

			// Warning if truly no components were found
			if(interactionPointsCount == 0)
			{
				Debug.LogWarning("$[ActorInteractableBase] no interaction points were " +
					$"attached to {this.name}. Interactions will not be possible without any.", this);
			}
		}
	}

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
			m_interactionPoints == null ||
			m_interactionPoints.Length == 0)
			return false;

		InteractionPoint closestPosition = null;
		float closestDistanceSqr = float.MaxValue;

		for (int i = 0; i < m_interactionPoints.Length; i++)
		{
			InteractionPoint position = m_interactionPoints[i];

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

	public virtual bool TryInteract(
		IInteractor interactor,
		Vector3 actorPosition,
		InteractionPoint assignedPosition
		)
	{

		if (interactor == null ||
			assignedPosition == null ||
			!OwnsInteractionPoint(assignedPosition))
		{
			return false;
		}

		if (!assignedPosition.GetPositionInRange(interactor, actorPosition))
			return false;

		if (!assignedPosition.TryBeginInteraction(interactor, out _))
		{
			CancelReservation(interactor, assignedPosition);
			return false;
		}

		ActorAssigned();
		return true;
	}

	public virtual void StopInteract(IInteractor interactor, InteractionPoint assignedPosition)
	{
		if (assignedPosition != null)
			assignedPosition.TryRemoveInteractor(interactor);

		int totalActors = TotalActorsPresent();
		if (totalActors < m_minActorsToFunction)
			LostMinimumActors();
	}

	protected virtual void ActorAssigned() { }

	public virtual void LostMinimumActors() { }

	public virtual void UpdateInteractableInfo()
	{
		OnInfoUpdated?.Invoke();
	}

	public virtual bool HasWorkForInteractor(IInteractor interactor)
	{
		return !AllInteractionPointsOccupied();
	}

	public int TotalActorsPresent()
	{
		if (m_interactionPoints == null ||
			m_interactionPoints.Length == 0)
			return 0;

		int total = 0;

		foreach (InteractionPoint position in m_interactionPoints)
		{
			if (position != null)
				total += position.ActorsPresent;
		}

		return total;
	}

	public bool OwnsInteractionPoint(InteractionPoint position)
	{
		if (position == null || m_interactionPoints == null)
			return false;

		return Array.IndexOf(m_interactionPoints, position) >= 0;
	}

	public abstract BehaviourTree GetBehaviourTree();

	/// <summary>
	/// Returns true if every interaction position is unavailable.
	/// </summary>
	private bool AllInteractionPointsOccupied()
	{
		if (m_interactionPoints == null ||
			m_interactionPoints.Length == 0)
			return true;

		for (int i = 0; i < m_interactionPoints.Length; i++)
		{
			InteractionPoint position = m_interactionPoints[i];

			if (position != null && position.HasAvailableCapacity())
				return false;
		}

		return true;
	}
}