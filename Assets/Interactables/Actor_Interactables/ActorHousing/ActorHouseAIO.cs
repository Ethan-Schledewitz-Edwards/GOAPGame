using BehaviourTrees;
using UnityEngine;

public class ActorHouseAIO : ActorInteractableObjectBase
{
	[Header("Building Configuration")]
	[field: SerializeField] public Transform EntrancePosition { get; private set; }


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
