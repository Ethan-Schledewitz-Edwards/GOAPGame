using UnityEngine;

public interface IInteractor
{
	Transform Transform { get; }
	public void InteractorInteracted(InteractableObjectBase actorInteractableObjectBase);
}
