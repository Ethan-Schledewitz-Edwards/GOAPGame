using TMPro;
using UnityEngine;

public abstract class ActorInteractableObject : MonoBehaviour
{
	[Header("Variables")]
	[SerializeField] private int m_actorsNeeded = 1;
	[SerializeField] private float m_formationRadius = 1;

	// System
	private int m_actorsPresent = 0;

	public void AssignActor()
	{
		m_actorsPresent++;

		if (m_actorsPresent == m_actorsNeeded)
			Interact();

		if (m_actorsPresent > m_actorsNeeded)
			UpdateSpeed(m_actorsPresent - m_actorsNeeded);
	}

	public void ReleaseActor()
	{
		if (m_actorsPresent == 0)
			return;

		m_actorsPresent--;

		if (m_actorsPresent < m_actorsNeeded)
			StopInteract();
	}

	/// <summary>
	/// Returns a valid position for an actor to move to on the interactables formation radius
	/// </summary>
	public Vector3 GetActorPositon()
	{
		float angle = m_actorsPresent * Mathf.PI * 2f / m_actorsNeeded;
		return transform.position + new Vector3(Mathf.Cos(angle) * m_formationRadius, 0, Mathf.Sin(angle) * m_formationRadius);
	}
	public abstract void Interact();

	public abstract void StopInteract();

	/// <summary>
	/// Notifies an interactable that it has extra actors to increase the speed of its function
	/// </summary>
	public abstract void UpdateSpeed(int extra);
}
