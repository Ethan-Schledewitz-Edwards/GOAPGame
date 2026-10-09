using UnityEngine;

public interface IInteractor
{
	public Transform Transform { get; }

	/// <summary>
	/// Initiates an interaction with a target interactable object.
	/// </summary>
	void InteractWith(ActorInteractableBase interactable, bool willReplaceJob);

	void BeginCarrying(Transform parent, Vector3 localPosition);

	void StopCarrying();
}
