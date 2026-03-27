using UnityEngine;

public class ActorHealth : HealthComponent
{
	// Constants
	private const float m_hungerDegredation = 0.2f;
	private const float m_tirednessDegredation = 0.2f;
	private const float m_baseHealthDegredation = 0.5f;

	private float m_healthInterval;

	[field: SerializeField] public int m_MaxHunger { get; private set; } = 100;
	[field: SerializeField] public int Hunger { get; private set; } = 100;
	private float m_hungerInterval;

	[field: SerializeField] public int m_MaxRest { get; private set; } = 100;
	[field: SerializeField] public int Rest { get; private set; } = 100;
	private float m_restInterval;

	[field: SerializeField] public int m_MaxHapiness { get; private set; } = 100;
	private int m_hapiness = 100;

	private void SetHunger(int newHungerValue)
	{
		Hunger = Mathf.Clamp(newHungerValue, 0, m_MaxHunger);
	}

	public void AddHunger(int value) => SetHunger(Hunger + value);

	public void RemoveHunger(int value) => SetHunger(Hunger - value);

	private void SetTiredness(int newTirednessValue)
	{
		Rest = Mathf.Clamp(newTirednessValue, 0, m_MaxRest);
	}

	public void AddTiredness(int value) => SetTiredness(Rest + value);

	public void RemoveTiredness(int value) => SetTiredness(Rest - value);

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
		m_restInterval += t * m_tirednessDegredation;
		while (m_restInterval >= 1f)
		{
			RemoveTiredness(1);
			m_restInterval -= 1f;
		}

		// Calculate health degredation
		float healthDegredation = 0;
		if (Hunger <= 0)
			healthDegredation += .5f;
		if (Rest <= 0)
			healthDegredation += .5f;


		// Degrade health if too tired or hungry
		if (healthDegredation > 0)
		{
			m_healthInterval += t * (m_baseHealthDegredation * (1 + healthDegredation));
			while (m_healthInterval >= 1f)
			{
				RemoveHealth(1);
				m_healthInterval -= 1f;
			}
		}
	}
}
