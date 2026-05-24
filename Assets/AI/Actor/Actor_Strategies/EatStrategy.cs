using UnityEngine;

public class EatStrategy : IActionStrategy
{
	readonly GOAPAgent m_agent;
	readonly Actor m_actor;
	private ItemStorageAIO m_storage;
	private float m_duration;
	private float m_timer;

	public bool IsStrategyPossible => m_storage != null && m_storage.InventoryComponent.Inventory.Slots[0].SlotsItem;
	public bool IsStrategyComplete { get; private set; }

	public EatStrategy(GOAPAgent agent, ItemStorageAIO itemStorageAIO, float duration)
	{
		m_agent = agent;
		m_actor = m_agent.transform.GetComponent<Actor>();
		m_storage = itemStorageAIO;
		m_duration = duration;
	}

	void IActionStrategy.StartStrategy()
	{
		Debug.Log("START EATING");

		m_timer = 0;
		IsStrategyComplete = false;

		// Transfer item to actor
		ItemData foodItem = null;
		if (m_storage.InventoryComponent.Inventory.Slots[0].SlotsItem != null)
		{
			foodItem = m_storage.InventoryComponent.Inventory.Slots[0].SlotsItem;
			m_storage.InventoryComponent.Inventory.Slots[0].RemoveFromStack(1);
		}

		if (foodItem != null)
		{
			m_actor.ActorInventory.Inventory.TryAddItem(foodItem, 1);
		}
		else
		{
			// If storage was empty unexpectedly, fail the strategy
			IsStrategyComplete = true;
		}
	}

	void IActionStrategy.TickStrategy(float t)
	{
		if (IsStrategyComplete) 
			return;

		m_timer += t;
		if (m_timer >= m_duration)
		{
			CompleteEating();
		}
	}

	private void CompleteEating()
	{
		m_actor.ActorInventory.Inventory.Slots[0].RemoveFromStack(1);

		m_actor.ActorHealth.AddHunger(30);

		IsStrategyComplete = true;
	}
}

