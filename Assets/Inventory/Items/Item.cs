using BehaviourTrees;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Item : ActorInteractableObjectBase
{
	[field: SerializeField] public ItemData ItemData { get; private set; }


	public override BehaviourTree GetBehaviourTree(Transform userTransform, Actor userActorComp)
	{
		throw new System.NotImplementedException();
	}

	public override void Interact(Actor actor)
	{
		// Pick up item
	}

	public override void StopInteract()
	{
		throw new System.NotImplementedException();
	}

	public override void UpdateSpeed(int extra)
	{
		throw new System.NotImplementedException();
	}
}
