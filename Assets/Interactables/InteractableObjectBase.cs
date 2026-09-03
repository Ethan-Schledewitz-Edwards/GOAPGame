using BehaviourTrees;
using UnityEngine;

public abstract class InteractableObjectBase : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField] private int m_actorsNeeded = 1;
	[SerializeField] private int m_maxActors = 1;

	[Header("Actor Interaction")]
	public abstract bool UseFormationRadius { get; }
	[SerializeField] protected Transform m_interactOffset;
	[SerializeField] private float m_formationRadius = 2;

	// System
	private int m_actorsPresent = 0; // How many actors are currently using the interactable

	public virtual bool TryInteract(IInteractor interactor, bool interactionTakesPriority)
	{
		return m_actorsPresent <= m_maxActors - 1;
	}

    public virtual void StopInteract()
    {
        ReleaseActor();
    }

	public abstract BehaviourTree GetBehaviourTree();

	#region Actor Handling

	protected bool TryAssignActor()
	{
		if(m_actorsPresent <= m_maxActors - 1)
		{
			m_actorsPresent++;

			if (m_actorsPresent > m_actorsNeeded)
				UpdateSpeed(m_actorsPresent - m_actorsNeeded);

			return true;
		}

		return false;
	}

	protected void ReleaseActor()
	{
		if (m_actorsPresent == 0)
			return;

		m_actorsPresent--;

		if (m_actorsPresent < m_actorsNeeded)
			StopInteract();
	}

    #endregion

	/// <summary>
	/// Notifies an interactable that it has extra actors to increase the speed of its function
	/// </summary>
	public abstract void UpdateSpeed(int extra);

	public void SetInteractionOffsetTransform(Transform transform, Vector3 localPosition)
	{
		m_interactOffset = transform;
		m_interactOffset.localPosition = localPosition;
	}

	public Transform GetInteractionOffsetTransform()
	{
		return m_interactOffset ? m_interactOffset : transform;
	}

	/// <summary>
	/// Returns a valid position for an actor to move to on the interactables formation radius
	/// </summary>
	public Vector3 GetInteractionPositon()
	{
		Transform targetTransform = GetInteractionOffsetTransform();

		if (UseFormationRadius)
		{
			float angle = m_actorsPresent * Mathf.PI * 2f / 12;

			float x = Mathf.Cos(angle) * m_formationRadius;
			float z = Mathf.Sin(angle) * m_formationRadius;

			return targetTransform.TransformPoint(new Vector3(x, 0, z));
		}

		return targetTransform.position;
	}

	public bool IsAtActorCapacity()
	{
		return m_actorsPresent == m_maxActors;
	}
}
