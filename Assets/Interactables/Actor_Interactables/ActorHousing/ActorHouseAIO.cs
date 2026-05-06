using BehaviourTrees;
using UnityEngine;

public class ActorHouseAIO : ActorInteractableObjectBase
{
	public override bool UseFormationRadius { get => false; }


	private void Awake()
	{
		
	}

	public override void UpdateSpeed(int extra)
	{

	}

	public override BehaviourTree GetBehaviourTree(Transform actorTransform, Actor actorComponent)
	{
		return null;
	}
}
