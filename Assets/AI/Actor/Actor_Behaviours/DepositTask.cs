using BehaviourTrees;
using UnityEngine;

public class DepositTask : BTNodeBase
{
	private BehaviourTreeExecutor m_behaviourTreeExecutor;
	private Transform m_executorTransform;

	private bool m_doneDepositing;

	/// <summary>
	/// Attempts to deposit an entire inventory into an item storage actor interactable object
	/// </summary>
	/// <param name="behaviourTreeExecutor">The target actor</param>
	/// <param name="executorTransform">The actors transform</param>
	public DepositTask(BehaviourTreeExecutor behaviourTreeExecutor, Transform executorTransform)
	{
		m_behaviourTreeExecutor = behaviourTreeExecutor;
		m_executorTransform = executorTransform;
	}

	public override EBTNodeState Evaluate(float t)
	{
		base.Evaluate(t);

		Transform targetTransform = (Transform)GetData("targetTransform");
		if (m_executorTransform.TryGetComponent(out Actor actor))
		{
			if (targetTransform != null && targetTransform.TryGetComponent(out ItemStorageAIO storage))
			{
				Inventory actorInventory = actor.ActorInventory.Inventory;

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

					// Attempt Deposit
					bool isItemAdded = storage.InventoryComponent.Inventory.TryAddItem(itemData, stackSize);

					// Clear actors slot
					if (isItemAdded)
					{
						Debug.Log("PUT ITEM IN deposit");
						actor.ActorInventory.TryDestroyHeldItem();
						slot.ClearSlot();
					}
				}

				m_doneDepositing = true;
			}
			else if (targetTransform == null)
			{
				m_nodeState = EBTNodeState.STATE_FAILURE;
				return m_nodeState;
			}

			if (m_doneDepositing)
			{
				m_nodeState = EBTNodeState.STATE_SUCSESS;
				return m_nodeState;
			}

			m_nodeState = EBTNodeState.STATE_RUNNING;
			return m_nodeState;
		}

		m_nodeState = EBTNodeState.STATE_FAILURE;
		return m_nodeState;
	}

	protected override void OnFirstEvaluate()
	{
		base.OnFirstEvaluate();

		Debug.Log("Begin deposit");
	}
}
