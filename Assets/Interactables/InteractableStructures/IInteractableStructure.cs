using UnityEngine;

public interface IInteractableStructure<T> where T : IInteractableStructure<T>
{
	float MaxCapacity { get; }
	float ActorsAssigned { get; }

	void AssignActor(out T structure);
}
