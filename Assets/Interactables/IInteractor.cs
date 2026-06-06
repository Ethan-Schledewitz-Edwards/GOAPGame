using UnityEngine;
using InventorySystem;

public interface IInteractor
{
	Transform Transform { get; }
	InventoryComponent InventoryComponent { get; }

	public void InteractorInteracted(InteractableObjectBase actorInteractableObjectBase);
}
