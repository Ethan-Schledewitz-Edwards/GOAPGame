using UnityEngine;

public class Stone_AIO : ActorInteractableObject_Base
{
	public override void Interact(Actor actor)
	{
		base.Interact(actor);
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
