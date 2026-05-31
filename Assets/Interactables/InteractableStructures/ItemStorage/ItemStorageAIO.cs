using BehaviourTrees;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(InventoryComponent))]
public class ItemStorageAIO : ActorInteractableObjectBase, IInteractableStructure<ItemStorageAIO>
{
	private static BehaviourTree m_ItemStorageBT;

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

		if(m_ItemStorageBT == null)
		{
			BehaviourTree tree = new BehaviourTree();
			BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
			{
				
			});
			tree.SetTree(root);
			m_ItemStorageBT = tree;
		}
	}

	public override void Interact(IInteractor interactor)
	{
		base.Interact(interactor);

		if(interactor.Transform.TryGetComponent(out BehaviourTreeExecutor behaviourTreeExecutor))
		{
			behaviourTreeExecutor.AIContext.SetData<Transform>("TargetTransform", transform);
		}
	}

	public override void UpdateSpeed(int extra)
	{
		
	}

	public void AssignActor(out ItemStorageAIO structure)
	{
		structure = null; // Storage should not have anyone assigned to it
	}

	public override BehaviourTree GetBehaviourTree() => m_ItemStorageBT;
}