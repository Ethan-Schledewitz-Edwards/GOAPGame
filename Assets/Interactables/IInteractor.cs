using UnityEngine;

public interface IInteractor
{
	public float InteractionDistance { get; }

	Transform Transform { get; }
	public void InteractorInteracted(InteractableObjectBase actorInteractableObjectBase);
}
