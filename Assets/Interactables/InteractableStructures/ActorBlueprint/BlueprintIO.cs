using BehaviourTrees;
using InventorySystem;
using InventorySystem.Items;
using System;
using System.Collections.Generic;
using UnityEditor.Graphs;
using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(BluerprintInventoryComponent))]
public class BlueprintIO : InteractableObjectBase, IInteractableStructure<BlueprintIO>
{
	private static BehaviourTree s_cachedBlueprintBT;

	private const string c_interactionLayer = "Interaction";

	public event Action<BlueprintIO> BlueprintCompleted;
	public event Action<BlueprintIO> BlueprintCanceled;

	public int BlueprintID { get; private set; }
	public int SettlementID { get; private set; }
	public Vector3 Position { get; private set; }
	public Quaternion Rotation { get; private set; }

	private BluerprintInventoryComponent m_bluerprintInventory;
	private ItemQuantity[] m_requiredItems;

	[SerializeField] private float m_maxCapacity = 4f;
	[SerializeField] private float m_actorsAssigned = 0f;
	public float MaxCapacity => m_maxCapacity;
	public float ActorsAssigned => m_actorsAssigned;
	public override bool UseFormationRadius { get => false; }

	private void Awake()
	{
		if (s_cachedBlueprintBT == null)
		{
			BehaviourTree tree = new BehaviourTree();
			BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
			{
				// search for itesm (storage than ground)
				// Go to item
				// Pickup item
				// return
				// Deposit
			});
			tree.SetTree(root);
			s_cachedBlueprintBT = tree;
		}

		gameObject.layer = LayerMask.NameToLayer(c_interactionLayer);

		m_bluerprintInventory = GetComponent<BluerprintInventoryComponent>();
		m_bluerprintInventory.BlueprintItemsAchieved += CompleteBlueprint;
	}

	private void OnDestroy()
	{
		m_bluerprintInventory.BlueprintItemsAchieved -= CompleteBlueprint;
	}

	public override void UpdateSpeed(int extra)
	{

	}

	public void AssignActor(out BlueprintIO structure)
	{
		if (ActorsAssigned < MaxCapacity)
		{
			m_actorsAssigned++;
		}

		structure = this;
	}

	public void InitializeBlueprint(int blueprintID, int settlementID, ItemQuantity[] requiredItems, Vector3 position, Quaternion rotation)
	{
		BlueprintID = blueprintID;
		SettlementID = settlementID;
		Position = position;
		Rotation = rotation;
		m_bluerprintInventory.InitializeBlueprintInventory(requiredItems);
		m_requiredItems = requiredItems;
	}

	private void CompleteBlueprint()
	{
		Debug.Log($"A blueprint of Blueprint ID:{BlueprintID} was completed in settlement:{SettlementID}.");
		BlueprintCompleted?.Invoke(this);
	}

	public void CancleBlueprint()
	{
		Debug.Log($"A blueprint of Blueprint ID:{BlueprintID} was canceld in settlement:{SettlementID}.");
		BlueprintCanceled?.Invoke(this);

		foreach (InventorySlot slot in m_bluerprintInventory.Slots)
		{
			slot.RemoveFromStack(slot.AmountInSlot, transform.position);
		}

		Destroy(gameObject);
	}

	public override BehaviourTree GetBehaviourTree() => s_cachedBlueprintBT;
}
