using BehaviourTrees;
using UnityEngine;

public class ActorHouseAIO : ActorInteractableObjectBase, IInteractableStructure<ActorHouseAIO>
{
	public override bool UseFormationRadius { get => false; }

	[SerializeField] private float m_maxCapacity = 4f;
	[SerializeField] private float m_actorsAssigned = 0f;
	public float MaxCapacity => m_maxCapacity;
	public float ActorsAssigned => m_actorsAssigned;

	private void Awake()
	{
		
	}

	public override void UpdateSpeed(int extra)
	{

	}

	public override BehaviourTree GetBehaviourTree(Transform actorTransform, BehaviourTreeExecutor behaviourTreeExecutor)
	{
		return null;
	}

	public void AssignActor(out ActorHouseAIO structure)
	{
		if (ActorsAssigned < MaxCapacity)
		{
			m_actorsAssigned++;
		}

		structure = this;
	}
}
