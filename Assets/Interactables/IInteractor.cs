using UnityEngine;

public interface IInteractor
{
	/// <summary>
	/// Initiates an interaction with a target interactable object.
	/// </summary>
	void InteractWith(InteractableObjectBase interactable, bool willReplaceJob);
}
