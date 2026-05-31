using BehaviourTrees;
using UnityEngine;

public abstract class ActorInteractableObjectBase : MonoBehaviour
{
	[Header("Variables")]
	[SerializeField] private int m_actorsNeeded = 1;
	[SerializeField] private Transform m_interactOffset;
	public abstract bool UseFormationRadius { get; }
	[SerializeField] private float m_formationRadius = 2;

	// System
	private int m_actorsPresent = 0; // How many actors are currently using the interactable

    public virtual void Interact(IInteractor interactor)
    {
        AssignActor();

		BehaviourTreeExecutor executor = interactor.Transform.GetComponent<BehaviourTreeExecutor>();
		if (executor != null)
		{
			executor.SetCurrentBehaviourTree(GetBehaviourTree());
		}
	}

    public virtual void StopInteract()
    {
        ReleaseActor();
    }

	public abstract BehaviourTree GetBehaviourTree();

	#region Actor Handling

	private void AssignActor()
	{
		m_actorsPresent++;

		if (m_actorsPresent > m_actorsNeeded)
			UpdateSpeed(m_actorsPresent - m_actorsNeeded);
	}

	private void ReleaseActor()
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

	#region Utility

	public Transform GetInteractionOffsetTransform()
	{
		return m_interactOffset ? m_interactOffset : transform;
	}

	/// <summary>
	/// Returns a valid position for an actor to move to on the interactables formation radius
	/// </summary>
	public Vector3 GetInteractionPositon()
	{
		Vector3 interactOffset = GetInteractionOffsetTransform().position;

		if (UseFormationRadius)
		{
			float angle = m_actorsPresent * Mathf.PI * 2f / 12;

			float x = Mathf.Cos(angle) * m_formationRadius;
			float z = Mathf.Sin(angle) * m_formationRadius;

			return interactOffset + new Vector3(x, 0, z);
		}

		return interactOffset;
    }
    #endregion
}
