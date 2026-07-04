using System;
using System.Collections;
using System.Collections.Generic;
using BehaviourTrees;
using InventorySystem;
using InventorySystem.Items;
using UnityEngine;


namespace Interaction.Blueprint
{
	[RequireComponent(typeof(BoxCollider), typeof(BluerprintInventoryComponent), (typeof(BlueprintCancelation)))]
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
		private BlueprintCancelation m_cancelBlueprint;
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
				BTNodeBase findUseTask = new FindItemTask();
				BTTimeoutNode timeoutFind = new BTTimeoutNode(findUseTask, 1f, "Timeout");

				BehaviourTree tree = new BehaviourTree();
				BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
				{
					timeoutFind,
					new MoveToTargetDataTask(),
					new CheckForTargetRangeTask(),
					new InteractWithTargetTask(),
					new ReturnToBlueprintTask(),
					new MoveToTargetDataTask(),
					new CheckForTargetRangeTask(),
					new DepositItemTask(),
				});
				tree.SetTree(root);
				s_cachedBlueprintBT = tree;
			}

			gameObject.layer = LayerMask.NameToLayer(c_interactionLayer);

			m_bluerprintInventory = GetComponent<BluerprintInventoryComponent>();
			m_cancelBlueprint = GetComponent<BlueprintCancelation>();

			m_bluerprintInventory.BlueprintItemsAchieved += CompleteBlueprint;
			m_cancelBlueprint.CanceledBlueprint += CancleBlueprint;

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

		public override bool TryInteract(IInteractor interactor)
		{
			base.TryInteract(interactor);

			// ONLY GIVE OUT THE FIND ITEM TASK ONE AT A TIME SO WE DON'T GRAB UNECESSARY ITEMS 
			BehaviourTreeExecutorBase executor = interactor.Transform.GetComponent<BehaviourTreeExecutorBase>();
			if (executor != null)
			{
				foreach (ItemQuantity item in m_requiredItems)
				{
					if (item.itemType != null)
					{
						executor?.AIContext.SetData<int>("ItemIDToFind", item.itemType.ItemID);
						return true;
					}
				}
			}

			return false;
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
}
