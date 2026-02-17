using UnityEngine;

public class ActorStats : HealthComponent
{
	[field: SerializeField] public int m_MaxHunger { get; private set; } = 100;
	private int m_hunger = 100;

	[field: SerializeField] public int m_MaxTiredness { get; private set; } = 100;
	private int m_tiredness = 100;

	[field: SerializeField] public int m_MaxHapiness { get; private set; } = 100;
	private int m_hapiness = 100;

	private void SetHunger(int newHungerValue)
	{
		m_hunger = Mathf.Clamp(newHungerValue, 0, m_MaxHunger);
	}

	public void AddHunger(int value) => SetHunger(m_hunger + value);

	public void RemoveHunger(int value) => SetHunger(m_hunger - value);

	private void SetTiredness(int newTirednessValue)
	{
		m_tiredness = Mathf.Clamp(newTirednessValue, 0, m_MaxTiredness);
	}

	public void AddTiredness(int value) => SetTiredness(m_tiredness + value);

	public void RemoveTiredness(int value) => SetTiredness(m_tiredness - value);

	private void SetHapiness(int newHapinessValue)
	{
		m_hapiness = Mathf.Clamp(newHapinessValue, 0, m_MaxHapiness);
	}

	public void AddHapiness(int value) => SetHapiness(m_hapiness + value);

	public void RemoveHapiness(int value) => SetHapiness(m_hapiness - value);
}
