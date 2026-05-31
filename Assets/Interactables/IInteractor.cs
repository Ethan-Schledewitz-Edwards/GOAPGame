using UnityEngine;

public interface IInteractor
{
	Transform Transform { get; }
	InventoryComponent InventoryComponent { get; }
}
