using UnityEngine;

public interface IInteractor
{
	Transform Transform { get; }
	InventoryComponent InventoryComponent { get; }

	public void InteractorInteracted(InteractableObjectBase actorInteractableObjectBase);
}
