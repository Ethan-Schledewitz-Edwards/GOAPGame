using BehaviourTrees;
using UnityEngine;
using InventorySystem;
using InventorySystem.Items;

public class DepositTask : BTNodeBase
{
	private float m_depositCooldown = 0.5f;

	protected override EBTNodeState OnUpdate(AIContext context, float t)
	{
		string doneKey = GetContextKey("DoneDepositing");
		string cooldownKey = GetContextKey("CooldownTimer");

		if (context.GetData<bool>(doneKey))
		{
			return EBTNodeState.STATE_SUCSESS;
		}

		Transform executorTransform = context.GetData<Transform>("ExecutorTransform");
		Transform targetTransform = context.GetData<Transform>("TargetTransform");

		if (targetTransform == null)
		{
			return EBTNodeState.STATE_FAILURE;
		}

		if (executorTransform.TryGetComponent(out InventoryComponent executorInventoryComponent) &&
			targetTransform.TryGetComponent(out InventoryComponent targetInventoryComponent))
		{
			float currentCooldown = context.GetData<float>(cooldownKey) - t;

			if (currentCooldown <= 0f)
			{
				context.SetData<float>(cooldownKey, m_depositCooldown);

				Inventory executorInventory = executorInventoryComponent.Inventory;
				bool foundAnItemToDeposit = false;

				for (int i = 0; i < executorInventory.Slots.Count; i++)
				{
					InventorySlot slot = executorInventory.Slots[i];
					ItemData itemData = slot.SlotsItem;
					int stackSize = slot.AmountInSlot;

					if (itemData == null || stackSize <= 0)
						continue;

					foundAnItemToDeposit = true;
					bool isItemAdded = targetInventoryComponent.TryAddItem(itemData, stackSize, slot.PhysicalItemObjects.Pop());
					if (isItemAdded)
					{
						Debug.Log($"Deposited slot {i}.");
						slot.ClearSlot();
					}
					else
						return EBTNodeState.STATE_FAILURE;

					break; // Only deposit one item per tick
				}

				if (!foundAnItemToDeposit)
				{
					context.SetData<bool>(doneKey, true);
					return EBTNodeState.STATE_SUCSESS;
				}
			}
			else
			{
				context.SetData<float>(cooldownKey, currentCooldown);
			}

			return EBTNodeState.STATE_RUNNING;
		}

		return EBTNodeState.STATE_FAILURE;
	}

	protected override void OnFirstEvaluate(AIContext context){}
}
