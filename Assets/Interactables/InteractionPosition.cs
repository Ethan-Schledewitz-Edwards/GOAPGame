using System.Collections.Generic;
using UnityEngine;

// NOTE: I propose renaming this to InteractionPoint or InteractionNode since position implies
// a fixed point in space that never moves. This also does a lot more that simply represent
// a position and I think the name should reflect that.
public class InteractionPosition : MonoBehaviour
{
	[SerializeField] public InteractionPositionSettings m_settings;

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
		!m_settings.RequiresReservation ||
		TotalOccupiedOrReserved < m_settings.MaxInteractors;

	private void Awake()
	{
		m_interactorsPresent = new List<IInteractor>(m_settings.MaxInteractors);
		m_reservedInteractors = new List<IInteractor>(m_settings.MaxInteractors);
	}

	public void ConfigureInteractionPosition(
		int maxInteractors,
		bool useFormationRadius = false,
		float formationRadius = 1.5f,
		bool requiresReservation = true,
		float interactionDistance = 0.3f)
	{
		m_settings.MaxInteractors = Mathf.Max(1, maxInteractors);
		m_settings.UseFormationRadius = useFormationRadius;
		m_settings.FormationRadius = formationRadius;
		m_settings.RequiresReservation = requiresReservation;
		m_settings.InteractionDistance = interactionDistance;

		if (m_interactorsPresent == null)
		{
			m_interactorsPresent = new List<IInteractor>(m_settings.MaxInteractors);
			m_reservedInteractors = new List<IInteractor>(m_settings.MaxInteractors);
		}
		else
		{
			m_interactorsPresent.Capacity = Mathf.Max(
				m_interactorsPresent.Count,
				m_settings.MaxInteractors);

			m_reservedInteractors.Capacity = Mathf.Max(
				m_reservedInteractors.Count,
				m_settings.MaxInteractors);
		}
	}

	#region Reservation

	// NOTE: I see no point in having non-reserved interactables, seems like
	// everything should have some limit. I also don't see why formation radius
	// should be togglable either, why not just set it to 0 with capacity of 1?
	// The reservation system seems to be a holdover from a more complex time
	// and I think it can do with a simplification pass along with the new
	// job search flow. Mostly for the reason of reservations should happen at
	// click time rather than arrival time so that pikmin can follow moving enemies.
	// This means the whole system doesn't need to be nearly as complex as it is now.

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
		if (!m_settings.RequiresReservation)
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

		int existingIndex = m_interactorsPresent.IndexOf(interactor);
		if (existingIndex >= 0)
		{
			interactorValue = existingIndex + 1;
			return true;
		}

		if (m_settings.RequiresReservation)
		{
			// Reserved positions require pre-allocation.
			int reservationIndex = m_reservedInteractors.IndexOf(interactor);

			if (reservationIndex < 0)
				return false;

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
	/// </remarks>
	public bool TryGetInteractionPosition(
		IInteractor interactor,
		out Vector3 position)
	{
		position = transform.position;

		bool isPresent = m_interactorsPresent.Contains(interactor);
		bool isReserved = m_reservedInteractors.Contains(interactor);

		if (m_settings.RequiresReservation && !isPresent && !isReserved)
			return false;

		if (!m_settings.UseFormationRadius)
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

		float angle = Mathf.Max(0, slotIndex) * Mathf.PI * 2f / Mathf.Max(1, m_settings.MaxInteractors);
		float x = Mathf.Cos(angle) * m_settings.FormationRadius;
		float z = Mathf.Sin(angle) * m_settings.FormationRadius;

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
			return distanceSquared <= m_settings.InteractionDistance * m_settings.InteractionDistance;
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
		Gizmos.DrawWireSphere(transform.position, m_settings.InteractionDistance);

		// Formation radius if it is enabled
		if (m_settings.UseFormationRadius)
		{
			Gizmos.color = new Color(1, 1, 0, 0.2f);
			Gizmos.DrawWireSphere(transform.position, m_settings.FormationRadius);
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
