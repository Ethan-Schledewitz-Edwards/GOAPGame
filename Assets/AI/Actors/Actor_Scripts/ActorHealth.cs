using UnityEngine;

public class ActorHealth : HealthComponent
{
	// Constants
	private const float m_hungerDegredation = 0.2f;
	private const float m_tirednessDegredation = 0.2f;

	[field: SerializeField] public int m_MaxHunger { get; private set; } = 100;
	private int m_hunger = 100;
	private float m_hungerInterval;

	[field: SerializeField] public int m_MaxTiredness { get; private set; } = 100;
	private int m_tiredness = 100;
	private float m_tirednessInterval;

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

	public void TickStats(float t)
	{
		// Degrade hunger while perserving overflow
		m_hungerInterval += t * m_hungerDegredation;
		while (m_hungerInterval >= 1f)
		{
			RemoveHunger(1);
			m_hungerInterval -= 1f;
		}

		// Degrade tiredness while perserving overflow
		m_tirednessInterval += t * m_tirednessDegredation;
		while (m_tirednessInterval >= 1f)
		{
			RemoveTiredness(1);
			m_tirednessInterval -= 1f;
		}
	}
}
