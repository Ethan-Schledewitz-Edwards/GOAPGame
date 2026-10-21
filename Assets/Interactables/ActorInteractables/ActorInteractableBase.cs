using BehaviourTrees;
using Interaction.Core;
using ObjectTags;
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class ActorInteractableBase : MonoBehaviour, IInteractable, ITaggable<ResourceTypeTag>
{
	[Header("Settings")]
	protected abstract int m_minActorsToFunction { get; }

	// IInteractable properties
	public string InteractPrompt => m_interactPrompt;
	protected abstract string m_interactPrompt { get; }

	public Vector3 LocalInteractionPromptOffset => m_localInteractionPromptOffset;
	[SerializeField] private Vector3 m_localInteractionPromptOffset;

	public Transform InteractableTransform => transform;

	public InteractionPoint[] InteractionPoints => m_interactionPoints;
	private InteractionPoint[] m_interactionPoints;

	// ITaggableProperties
	[SerializeField] private ResourceTypeTag m_resourceTypeTag;

	private HashSet<ResourceTypeTag> m_runtimeTagSet;
	public HashSet<ResourceTypeTag> RuntimeTagSet
	{
		get
		{
			if (m_runtimeTagSet == null)
			{
				m_runtimeTagSet = new HashSet<ResourceTypeTag>();

				if (m_resourceTypeTag != null)
					m_runtimeTagSet.Add(m_resourceTypeTag);
			}
			return m_runtimeTagSet;
		}
	}

	// ITrackableObject Properties
	public bool IsTrackingEnabled { get; set; } = false;

	public Vector3 LocalTrackingPointOffset => Vector3.up * 2;

	public Transform ObjectTransform => transform;

	public event Action ObjectInfoUpdated;
	public event Action ObjectDestroyed;

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

	/// <summary>
	/// Finds the closest position this interactor can be assigned to.
	/// For reserved positions, assignment creates a reservation.
	/// For non-reserved positions, assignment is a position selection.
	/// </summary>
	public virtual bool TryReserveClosestPosition(
		IInteractor interactor,
		Vector3 actorPosition,
		out InteractionPoint reservedPoint)
	{
		reservedPoint = null;

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

		reservedPoint = closestPosition;
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
		InteractionPoint reservedPoint
		)
	{
		if (interactor == null ||
			reservedPoint == null ||
			!OwnsInteractionPoint(reservedPoint))
		{
			return false;
		}

		if (!reservedPoint.GetPositionInRange(interactor, actorPosition))
			return false;

		if (!reservedPoint.TryBeginInteraction(interactor, out _))
		{
			CancelReservation(interactor, reservedPoint);
			return false;
		}

		ActorAssigned();
		return true;
	}

	protected virtual void ActorAssigned() { }

	public virtual void StopInteract(IInteractor interactor, InteractionPoint reservedPoint)
	{
		int actorCountBeforeRemoval = TotalActorsPresent();

		if (reservedPoint != null)
			reservedPoint.TryRemoveInteractor(interactor);

		// Actor count fell below requirement
		int actorCountAfterRemoval = TotalActorsPresent();
		if (actorCountBeforeRemoval >= m_minActorsToFunction &&
			actorCountAfterRemoval < m_minActorsToFunction)
			LostMinimumActors();
	}

	public virtual void LostMinimumActors() { }

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