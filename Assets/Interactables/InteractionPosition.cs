using System.Collections.Generic;
using UnityEngine;

public class InteractionPosition : MonoBehaviour
{
	#region Settings

	[Header("Settings")]
	[field: SerializeField] public int MaxInteractors { get; private set; } = 1;
	[field: SerializeField] public bool UseFormationRadius { get; private set; } = false;
	[field: SerializeField] public float FormationRadius { get; private set; } = 1.5f;

	[field: SerializeField]
	[Tooltip("If false, actors may interact without reserving this position first.")]
	public bool RequiresReservation { get; private set; } = true;

	[field: SerializeField]
	[Tooltip("Maximum horizontal distance from the assigned interaction position.")]
	public float InteractionDistance { get; private set; } = 0.3f;

	#endregion

	private List<IInteractor> m_interactorsPresent;
	private List<IInteractor> m_reservedInteractors;

	public int ActorsPresent => m_interactorsPresent.Count;

	public int ReservedInteractors => m_reservedInteractors.Count;

	public int TotalOccupiedOrReserved =>
		m_interactorsPresent.Count + m_reservedInteractors.Count;

	/// <summary>
	/// For non-reserved positions there is never a capacity restriction.
	/// For reserved positions, capacity includes both active and reserved actors.
	/// </summary>
	public bool HasAvailableCapacity =>
		!RequiresReservation ||
		TotalOccupiedOrReserved < MaxInteractors;

	private void Awake()
	{
		m_interactorsPresent = new List<IInteractor>(MaxInteractors);
		m_reservedInteractors = new List<IInteractor>(MaxInteractors);
	}

	public void ConfigureInteractionPosition(
		int maxInteractors,
		bool useFormationRadius = false,
		float formationRadius = 1.5f,
		bool requiresReservation = true,
		float interactionDistance = 0.3f)
	{
		MaxInteractors = Mathf.Max(1, maxInteractors);
		UseFormationRadius = useFormationRadius;
		FormationRadius = formationRadius;
		RequiresReservation = requiresReservation;
		InteractionDistance = interactionDistance;

		if (m_interactorsPresent == null)
		{
			m_interactorsPresent = new List<IInteractor>(MaxInteractors);
			m_reservedInteractors = new List<IInteractor>(MaxInteractors);
		}
		else
		{
			m_interactorsPresent.Capacity = Mathf.Max(
				m_interactorsPresent.Count,
				MaxInteractors);

			m_reservedInteractors.Capacity = Mathf.Max(
				m_reservedInteractors.Count,
				MaxInteractors);
		}
	}

	#region Reservation

	/// <summary>
	/// Attempts to assign this position to an interactor.
	///
	/// Reserved positions create a reservation.
	/// Non-reserved positions require no bookkeeping at assignment time.
	/// </summary>
	public bool TryReservePosition(IInteractor interactor)
	{
		if (interactor == null)
			return false;

		// Non-reserved positions do not need pre-allocation.
		if (!RequiresReservation)
			return true;

		// Already active or already reserved is a valid assignment.
		if (m_interactorsPresent.Contains(interactor) ||
			m_reservedInteractors.Contains(interactor))
			return true;

		if (!HasAvailableCapacity)
			return false;

		m_reservedInteractors.Add(interactor);
		return true;
	}

	public void ReleaseReservation(IInteractor interactor)
	{
		if (interactor == null)
			return;

		m_reservedInteractors.Remove(interactor);
	}

	#endregion

	#region Interaction

	/// <summary>
	/// Begins interaction at this position. Non-reserved interactors 
	/// are added directly to the active list. Reserved interactor must already hold a reservation, 
	/// which is then converted into an active interaction.
	/// </summary>
	public bool TryBeginInteraction(
		IInteractor interactor,
		out int interactorValue)
	{
		interactorValue = -1;

		if (interactor == null)
			return false;

		// Already interacting is idempotent.
		int existingIndex = m_interactorsPresent.IndexOf(interactor);
		if (existingIndex >= 0)
		{
			interactorValue = existingIndex + 1;
			return true;
		}

		if (RequiresReservation)
		{
			// Reserved positions require pre-allocation.
			int reservationIndex = m_reservedInteractors.IndexOf(interactor);

			if (reservationIndex < 0)
				return false;

			// Reservation -> active.
			m_reservedInteractors.RemoveAt(reservationIndex);
		}

		// Non-reserved positions arrive here directly.
		m_interactorsPresent.Add(interactor);
		interactorValue = m_interactorsPresent.Count;

		return true;
	}

	public void StopInteract(IInteractor interactor)
	{
		TryRemoveInteractor(interactor);
	}

	public void TryRemoveInteractor(IInteractor interactor)
	{
		if (m_interactorsPresent.Contains(interactor))
			m_interactorsPresent.Remove(interactor);
	}

	#endregion

	#region Position

	/// <summary>
	/// Gets the world-space position this interactor should use.
	/// </summary>
	/// <remarks>
	/// A reserved position requires either a reservation or an existing
	/// active interaction. A non-reserved position is always valid.
	/// </remarks>>
	public bool TryGetInteractionPosition(
		IInteractor interactor,
		out Vector3 position)
	{
		position = transform.position;

		bool isPresent = m_interactorsPresent.Contains(interactor);
		bool isReserved = m_reservedInteractors.Contains(interactor);

		if (RequiresReservation && !isPresent && !isReserved)
			return false;

		if (!UseFormationRadius)
			return true;

		int slotIndex;

		if (isPresent)
		{
			slotIndex = m_interactorsPresent.IndexOf(interactor);
		}
		else if (isReserved)
		{
			slotIndex =
				m_interactorsPresent.Count +
				m_reservedInteractors.IndexOf(interactor);
		}
		else
		{
			// Non-reserved actor checking where it would stand.
			slotIndex = m_interactorsPresent.Count;
		}

		float angle = Mathf.Max(0, slotIndex) * Mathf.PI * 2f / Mathf.Max(1, MaxInteractors);
		float x = Mathf.Cos(angle) * FormationRadius;
		float z = Mathf.Sin(angle) * FormationRadius;

		position = transform.TransformPoint(new Vector3(x, 0, z));

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
			Vector3 interactorPositionFlat = new Vector3(worldPosition.x, 0, worldPosition.z);
			Vector3 targetPositionFlat = new Vector3(targetPos.x, 0, targetPos.z);

			float distanceSquared = (interactorPositionFlat - targetPositionFlat).sqrMagnitude;
			return distanceSquared <= InteractionDistance * InteractionDistance;
		}

		return false;
	}

	#endregion

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
