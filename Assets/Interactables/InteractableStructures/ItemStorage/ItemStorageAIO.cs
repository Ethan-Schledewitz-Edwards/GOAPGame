using BehaviourTrees;
using UnityEngine;

[RequireComponent(typeof(InventoryComponent))]
public class ItemStorageAIO : ActorInteractableObjectBase, IInteractableStructure<ItemStorageAIO>
{
	public override bool UseFormationRadius { get => false; }

	[SerializeField] private float m_maxCapacity = 4f;
	[SerializeField] private float m_actorsAssigned = 0f;
	public float MaxCapacity => m_maxCapacity;
	public float ActorsAssigned => m_actorsAssigned;

	[Header("Storage Configuration")]
	[SerializeField] private ItemData m_itemType;
	public ItemData ItemType => m_itemType;

	// Components
	public InventoryComponent InventoryComponent { get; private set; }


	private void Awake()
	{
		InventoryComponent = GetComponent<InventoryComponent>();
	}

	public override void UpdateSpeed(int extra)
	{
		
	}

	public override BehaviourTree GetBehaviourTree(Transform actorTransform, BehaviourTreeExecutor behaviourTreeExecutor)
	{
		return null;
	}

	public void AssignActor(out ItemStorageAIO structure)
	{
		structure = null; // Storage should not have anyone assigned to it
	}
}