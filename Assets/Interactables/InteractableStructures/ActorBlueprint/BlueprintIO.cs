using BehaviourTrees;
using Construction;
using InventorySystem;
using InventorySystem.Items;
using Settlements;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Interaction.InteractableStructures.Blueprints
{
	[RequireComponent(typeof(BoxCollider), typeof(BluerprintInventoryComponent), (typeof(BlueprintCancelation)))]
	public class BlueprintIO : InteractableObjectBase, IStructure<BlueprintIO>, IBlueprintObject
	{
		private static BehaviourTree s_cachedBlueprintBT;

		private const string c_interactionLayer = "Interaction";

		[SerializeField] private Material m_blueprintMaterial;

		// Components
		[SerializeField] private MeshFilter m_meshFilter;
		[SerializeField] private MeshRenderer m_meshRenderer;

		private BluerprintInventoryComponent m_bluerprintInventory;
		private BlueprintCancelation m_cancelBlueprint;
		private ItemQuantity[] m_requiredItems;
		private BoxCollider m_boxCollider;

		// Events
		public event Action<IBlueprintObject> BlueprintCompleted;
		public event Action<IBlueprintObject> BlueprintCanceled;

		// System
		public string StructureTypeKey => "Blueprint";
		public int StructureID { get => m_structureID; set => m_structureID = value; }
		private int m_structureID;

		public GameObject StructureObject => gameObject;

		public int MaxCapacity => m_maxCapacity;
		[SerializeField] private int m_maxCapacity = 4;

		public int ActorsAssigned => m_actorsAssigned;
		[SerializeField] private int m_actorsAssigned = 0;

		public int SettlementID => m_settlementID;
		private int m_settlementID;
		public int SettlementBlueprintID => m_structureID;
		public int StructureBlueprintID => m_structureBlueprintID;
		private int m_structureBlueprintID;
		public GameObject BlueprintObject => gameObject;

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
					new DepositHeldItemTask(),
				});
				tree.SetTree(root);
				s_cachedBlueprintBT = tree;
			}

			gameObject.layer = LayerMask.NameToLayer(c_interactionLayer);

			m_bluerprintInventory = GetComponent<BluerprintInventoryComponent>();
			m_bluerprintInventory.BlueprintItemsAchieved += HandleBlueprintCompleted;

			m_cancelBlueprint = GetComponent<BlueprintCancelation>();
			m_cancelBlueprint.CanceledBlueprint += HandleBlueprintCanceled;

		}

		private void OnDestroy()
		{
			m_bluerprintInventory.BlueprintItemsAchieved -= HandleBlueprintCompleted;
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

			//executor?.AIContext.SetData<int>(AIContextKeys.c_BlueprintID, Sett);

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

		public void HandleBlueprintStarted
			(
				int settlementID,
				int settlementBlueprintID,
				StructureBlueprintData structureBlueprintData,
				Vector3 position,
				Quaternion rotation
			)
		{
			m_settlementID = settlementID;
			m_structureBlueprintID = structureBlueprintData.StructureBlueprintID;

			m_bluerprintInventory.InitializeBlueprintInventory(structureBlueprintData.RequiredItems);
			m_requiredItems = structureBlueprintData.RequiredItems;

			SetBlueprintMesh(structureBlueprintData.BlueprintMesh);
			SetInteractionOffsetTransform(m_interactOffset, structureBlueprintData.InteractionLocalOffset);

			transform.position = position;
			transform.rotation = rotation;
		}

		public void HandleBlueprintCompleted()
		{
			Debug.Log($"A blueprint of SettlementBlueprintID:{SettlementBlueprintID} was completed in settlement:{SettlementID}.");
			BlueprintCompleted?.Invoke(this);
		}

		public void HandleBlueprintCanceled()
		{
			Debug.Log($"A blueprint of SettlementBlueprintID:{SettlementBlueprintID} was canceld in settlement:{SettlementID}.");
			BlueprintCanceled?.Invoke(this);

			foreach (InventorySlot slot in m_bluerprintInventory.Slots)
			{
				slot.RemoveFromStack(slot.AmountInSlot, out var _, true, transform.position);
			}

			Destroy(gameObject);
		}

		private void SetBlueprintMesh(Mesh blueprintMesh)
		{
			m_meshFilter.mesh = blueprintMesh;
			m_meshRenderer.material = m_blueprintMaterial;
		}

		public override BehaviourTree GetBehaviourTree() => s_cachedBlueprintBT;
	}
}
