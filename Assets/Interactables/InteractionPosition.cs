using System.Collections.Generic;
using UnityEngine;

public class InteractionPosition : MonoBehaviour
{
	[Header("Settings")]
	[field: SerializeField] public int MaxInteractors { get; private set; } = 1;
	[field: SerializeField] public bool UseFormationRadius { get; private set; } = false;
	[field: SerializeField, Tooltip("If false, this position does not require pre-allocation or locking, allowing multiple actors (like doors or storage access points) to share it.")]
	public bool RequiresReservation { get; private set; } = true;
	[field: SerializeField, Tooltip("Dictates how far from the center of the InteractionPosition the actor can be before they begin interacting.")]
	public float InteractionDistance { get; private set; } = 0.5f;

	// System
	Vector3 WorldPosition => transform.position;

	private List<IInteractor> m_interactorsPresent = new List<IInteractor>();
	private List<IInteractor> m_reservedInteractors = new List<IInteractor>();

	public int ActorsPresent => m_interactorsPresent.Count;

	public int TotalOccupiedOrReserved => m_interactorsPresent.Count + m_reservedInteractors.Count;
	public bool HasAvailableCapacity => !RequiresReservation || (TotalOccupiedOrReserved < MaxInteractors);

	private void Awake()
	{
		m_interactorsPresent = new List<IInteractor>(MaxInteractors);
		m_reservedInteractors = new List<IInteractor>(MaxInteractors);
	}

	public bool TryReserve(IInteractor interactor)
	{
		if (!RequiresReservation)
			return true;

		if (!HasAvailableCapacity || m_interactorsPresent.Contains(interactor) || m_reservedInteractors.Contains(interactor))
			return false;

		m_reservedInteractors.Add(interactor);
		return true;
	}

	public void ReleaseReservation(IInteractor interactor)
	{
		if (!RequiresReservation) return;
		m_reservedInteractors.Remove(interactor);
	}

	/// <summary>
	/// Adds an interactor to the collection if the number of interactors 
	/// present falls below the maximum allowed number.
	/// </summary>
	/// <param name="interactor">The interactor to add.</param>
	/// <param name="interactorValue">The index assigned to the interactor if 
	/// the addition is successful. Otherwise, -1.</param>
	/// <returns>true if the interactor was added successfully; otherwise, false.</returns>
	public bool TryAddInteractor(IInteractor interactor, out int interactorValue)
	{
		m_reservedInteractors.Remove(interactor);

		if (!RequiresReservation)
		{
			if (!m_interactorsPresent.Contains(interactor))
				m_interactorsPresent.Add(interactor);

			interactorValue = m_interactorsPresent.IndexOf(interactor) + 1;
			return true;
		}

		if (m_interactorsPresent.Count < MaxInteractors && !m_interactorsPresent.Contains(interactor))
		{
			m_interactorsPresent.Add(interactor);
			interactorValue = m_interactorsPresent.Count;
			return true;
		}

		interactorValue = -1;
		return false;
	}

	public void TryRemoveInteractor(IInteractor interactor)
	{
		m_interactorsPresent.Remove(interactor);
	}

	/// <summary>
	/// Attempts to get a valid, dynamically offset position for a specific interactor.
	/// Automatically registers unreserved interactors on the fly.
	/// </summary>
	public bool TryGetInteractionPosition(IInteractor interactor, out Vector3 position)
	{
		position = transform.position;

		bool isPresent = m_interactorsPresent.Contains(interactor);
		bool isReserved = m_reservedInteractors.Contains(interactor);

		if (RequiresReservation && !isPresent && !isReserved)
			return false;

		if (!RequiresReservation && !isPresent && !isReserved)
		{
			if (m_interactorsPresent.Count < MaxInteractors)
			{
				m_interactorsPresent.Add(interactor);
			}
			else if (MaxInteractors > 0)
			{
				position = transform.position;
				return true;
			}
		}

		if (UseFormationRadius)
		{
			int slotIndex = 0;

			if (m_interactorsPresent.Contains(interactor))
			{
				slotIndex = m_interactorsPresent.IndexOf(interactor);
			}
			else if (m_reservedInteractors.Contains(interactor))
			{
				slotIndex = m_interactorsPresent.Count + m_reservedInteractors.IndexOf(interactor);
			}

			float angle = Mathf.Max(0, slotIndex) * Mathf.PI * 2f / Mathf.Max(1, MaxInteractors);
			float x = Mathf.Cos(angle) * InteractionDistance;
			float z = Mathf.Sin(angle) * InteractionDistance;

			position = transform.TransformPoint(new Vector3(x, 0, z));
		}

		return true;
	}

	/// <summary>
	/// Determines whether the specified world position is within the interaction range of the object.
	/// </summary>
	public bool GetPositionInRange(Vector3 worldPosition)
	{
		float distanceSquared = (worldPosition - transform.position).sqrMagnitude;
		float interactionDistanceSquared = InteractionDistance * InteractionDistance;
		return distanceSquared <= interactionDistanceSquared;
	}
}
