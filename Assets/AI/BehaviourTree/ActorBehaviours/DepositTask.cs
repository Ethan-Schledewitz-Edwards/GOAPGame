using BehaviourTrees;
using UnityEngine;

public class DepositTask : BTNodeBase
{
	private Actor m_actorComponent;
	private Transform m_actorTransform;

	private bool m_doneDepositing;

	/// <summary>
	/// Attempts to deposit an entire inventory into an item storage actor interactable object
	/// </summary>
	/// <param name="actorComponent">The target actor</param>
	/// <param name="actorTransform">The actors transform</param>
	public DepositTask(Actor actorComponent, Transform actorTransform)
	{
		m_actorComponent = actorComponent;
		m_actorTransform = actorTransform;
	}

	public override EBTNodeState Evaluate()
	{
		base.Evaluate();

		Transform target = (Transform)GetData("target");
		ItemStorageAIO itemStorage = null;

		if (target != null && target.TryGetComponent(out ItemStorageAIO storage))
		{
			itemStorage = storage;

			Inventory actorInventory = m_actorComponent.ActorInventory.Inventory;

			// Add the actors first held item
			for (int i = 0; i < actorInventory.Slots.Count; i++)
			{
				InventorySlot slot = actorInventory.Slots[i];

				ItemData itemData = slot.SlotsItem;
				int stackSize = slot.AmountInSlot;

				Debug.Log($"Checking Slot {i}: Item={slot.SlotsItem}, Amount={slot.AmountInSlot}");

				// Skip empty slots
				if (itemData == null || stackSize <= 0)
					continue;

				Debug.Log("WHAT");

				// Attempt Deposit
				bool isItemAdded = storage.InventoryComponent.Inventory.TryAddItem(itemData, stackSize);

				// Clear actors slot
				if (isItemAdded)
				{
					Debug.Log("PUT ITEM IN deposit");
					slot.ClearSlot();
				}
			}

			m_doneDepositing = true;
		}

		if (m_doneDepositing)
		{
			m_nodeState = EBTNodeState.STATE_SUCSESS;
			return m_nodeState;
		}

		m_nodeState = EBTNodeState.STATE_RUNNING;
		return m_nodeState;
	}

	protected override void OnFirstEvaluate()
	{
		base.OnFirstEvaluate();

		Debug.Log("Begin deposit");
	}
}
