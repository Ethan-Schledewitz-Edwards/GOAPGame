using BehaviourTrees;
using UnityEngine;

[RequireComponent (typeof(InventoryComponent))]
public class ItemStorageAIO : ActorInteractableObjectBase
{
	public override bool UseFormationRadius { get => false; }

	public InventoryComponent InventoryComponent { get; private set; }

	[Header("Building Configuration")]
	[SerializeField] private ItemData m_itemType;
	public ItemData ItemType => m_itemType;

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
}