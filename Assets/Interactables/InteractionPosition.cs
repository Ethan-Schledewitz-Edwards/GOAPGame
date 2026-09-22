using System.Collections.Generic;
using UnityEngine;

public class InteractionPosition : MonoBehaviour
{
	[Header("Settings")]
	[field: SerializeField] public int MaxInteractors { get; private set; } = 1;
	[field: SerializeField] public bool UseFormationRadius { get; private set; } = false;
	[field: SerializeField] public float FormationRadius { get; private set; } = 1.5f;

	[field: SerializeField, Tooltip("If false, this position does not require pre-allocation or locking, allowing multiple actors (like doors or storage access points) to share it.")]
	public bool RequiresReservation { get; private set; } = true;
	[field: SerializeField, Tooltip("Dictates how far from the center of the InteractionPosition the actor can be before they begin interacting.")]
	public float InteractionDistance { get; private set; } = 0.3f;

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

	/// <summary>
	/// Dynamically configures the parameters for this interaction position at runtime.
	/// </summary>
	public void ConfigureInteractionPosition(
		int maxInteractors,
		bool useFormationRadius = false,
		float formationRadius = 1.5f,
		bool requiresReservation = true,
		float interactionDistance = 0.3f
		)
	{
		MaxInteractors = Mathf.Max(1, maxInteractors);
		UseFormationRadius = useFormationRadius;
		FormationRadius = formationRadius;
		RequiresReservation = requiresReservation;
		InteractionDistance = interactionDistance;

		// Re-initialize capacities
		if (m_interactorsPresent == null)
		{
			m_interactorsPresent = new List<IInteractor>(MaxInteractors);
			m_reservedInteractors = new List<IInteractor>(MaxInteractors);
		}
		else
		{
			m_interactorsPresent.Capacity = Mathf.Max(m_interactorsPresent.Count, MaxInteractors);
			m_reservedInteractors.Capacity = Mathf.Max(m_reservedInteractors.Count, MaxInteractors);
		}
	}

	/// <summary>
	/// Attempts to reserve a position for the specified interactor if reservation is required and capacity is available.
	/// </summary>
	public bool TryReservePosition(IInteractor interactor)
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
		if (!RequiresReservation) 
			return;

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
	public bool TryConvertReservationToActiveInteractor(IInteractor interactor, out int interactorValue)
	{
		if (m_reservedInteractors.Contains(interactor))
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
		}
	
		interactorValue = -1;
		return false;
	}

	public void TryRemoveInteractor(IInteractor interactor)
	{
		if (m_interactorsPresent.Contains(interactor))
			m_interactorsPresent.Remove(interactor);
	}

	/// <summary>
	/// Attempts to get a valid, dynamically offset position for a specific interactor.
	/// Automatically registers unreserved interactors.
	/// </summary>
	public bool TryGetInteractionPosition(IInteractor interactor, out Vector3 position)
	{
		position = transform.position;

		bool isPresent = m_interactorsPresent.Contains(interactor);
		bool isReserved = m_reservedInteractors.Contains(interactor);

		if (RequiresReservation && !isPresent && !isReserved)
			return false;

		if (!RequiresReservation && !isPresent && !isReserved && m_interactorsPresent.Count >= MaxInteractors)
		{
			return false;
		}

		if (UseFormationRadius)
		{
			int slotIndex = 0;

			if (isPresent)
			{
				slotIndex = m_interactorsPresent.IndexOf(interactor);
			}
			else if (isReserved)
			{
				slotIndex = m_interactorsPresent.Count + m_reservedInteractors.IndexOf(interactor);
			}
			else
			{
				// For unreserved walk-ins actors who are checking where they would go
				slotIndex = m_interactorsPresent.Count;
			}

			float angle = Mathf.Max(0, slotIndex) * Mathf.PI * 2f / Mathf.Max(1, MaxInteractors);
			float x = Mathf.Cos(angle) * FormationRadius;
			float z = Mathf.Sin(angle) * FormationRadius;

			position = transform.TransformPoint(new Vector3(x, 0, z));
		}

		return true;
	}

	/// <summary>
	/// Determines whether the specified world position is within the interaction range of the object.
	/// Always validates against the dynamically assigned position so moving objects update correctly.
	/// </summary>
	public bool GetPositionInRange(IInteractor interactor, Vector3 worldPosition)
	{
		if (TryGetInteractionPosition(interactor, out Vector3 targetPos))
		{
			Vector3 worldPosFlat = new Vector3(worldPosition.x, 0, worldPosition.z);
			Vector3 targetPosFlat = new Vector3(targetPos.x, 0, targetPos.z);

			float distanceSquared = (worldPosFlat - targetPosFlat).sqrMagnitude;
			return distanceSquared <= InteractionDistance * InteractionDistance;
		}

		return false;
	}

#if UNITY_EDITOR

	private void OnDrawGizmos()
	{
		// Interaction distance and center
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(transform.position, 0.2f);

		Gizmos.color = new Color(0, 1, 1, 0.2f);
		Gizmos.DrawWireSphere(transform.position, InteractionDistance);

		// Formation radius if it is enabled
		if (UseFormationRadius)
		{
			Gizmos.color = new Color(1, 1, 0, 0.2f);
			Gizmos.DrawWireSphere(transform.position, FormationRadius);
		}

		// Green lines to all actively present actors
		if (m_interactorsPresent != null)
		{
			Gizmos.color = Color.green;
			for (int i = 0; i < m_interactorsPresent.Count; i++)
			{
				var interactor = m_interactorsPresent[i];
				if (interactor != null && interactor is Component comp)
				{
					Gizmos.DrawLine(transform.position, comp.transform.position);
					Gizmos.DrawWireCube(comp.transform.position + Vector3.up, Vector3.one * 0.3f);
				}
			}
		}

		// Yellow lines to all actors holding a reservation
		if (m_reservedInteractors != null)
		{
			Gizmos.color = Color.yellow;
			for (int i = 0; i < m_reservedInteractors.Count; i++)
			{
				var interactor = m_reservedInteractors[i];
				if (interactor != null && interactor is Component comp)
				{
					Gizmos.DrawLine(transform.position, comp.transform.position);
					Gizmos.DrawWireSphere(comp.transform.position + Vector3.up, 0.3f);
				}
			}
		}
	}

#endif
}
