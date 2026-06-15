using BehaviourTrees;
using UnityEngine;
using InventorySystem;
using InventorySystem.Items;

public class DepositTask : BTNodeBase
{
	private bool m_doneDepositing;

	public override EBTNodeState Evaluate(AIContext aiContext, float t)
	{
		base.Evaluate(aiContext, t);

		Transform executorTransform = aiContext.GetData<Transform>("ExecutorTransform");
		Transform targetTransform = aiContext.GetData<Transform>("TargetTransform");

		if (executorTransform.TryGetComponent(out InventoryComponent executorInvComp))
		{
			if (targetTransform != null && targetTransform.TryGetComponent(out InventoryComponent storageInvComp))
			{
				Inventory executorInventory = executorInvComp.Inventory;

				// Add the actors first held item
				for (int i = 0; i < executorInventory.Slots.Count; i++)
				{
					InventorySlot slot = executorInventory.Slots[i];

					ItemData itemData = slot.SlotsItem;
					int stackSize = slot.AmountInSlot;

					Debug.Log($"Checking Slot {i}: Item={slot.SlotsItem}, Amount={slot.AmountInSlot}");

					// Skip empty slots
					if (itemData == null || stackSize <= 0)
						continue;

					// Attempt Deposit
					bool isItemAdded = storageInvComp.Inventory.TryAddItem(itemData, stackSize);

					// Clear actors slot
					if (isItemAdded)
					{
						Debug.Log("PUT ITEM IN deposit");
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

		float prevTimeout = aiContext.GetData<float>("Timeout");
		aiContext.SetData<float>("Timeout", prevTimeout + t);
		m_nodeState = EBTNodeState.STATE_FAILURE;
		return m_nodeState;
	}

	protected override void OnFirstEvaluate()
	{
		base.OnFirstEvaluate();

		Debug.Log("Begin deposit");
	}
}
