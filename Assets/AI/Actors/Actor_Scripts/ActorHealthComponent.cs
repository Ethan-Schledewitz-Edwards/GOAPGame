using UnityEngine;

public class ActorHealthComponent : HealthComponent
{
	// Constants
	private const float c_hungerDegredation = 0.2f;
	private const float c_tirednessDegredation = 0.2f;
	private const float c_baseHealthDegredation = 2f;

	private float m_healthInterval;

	[field: SerializeField] public int MaxHunger { get; private set; } = 100;
	[field: SerializeField] public int Hunger { get; private set; } = 100;
	private float m_hungerInterval;

	[field: SerializeField] public int MaxRest { get; private set; } = 100;
	[field: SerializeField] public int Rest { get; private set; } = 100;
	private float m_restInterval;

	[field: SerializeField] public int MaxHapiness { get; private set; } = 100;
	[field: SerializeField] public int Hapiness { get; private set; } = 100;


	private void SetHunger(int newHungerValue)
	{
		Hunger = Mathf.Clamp(newHungerValue, 0, MaxHunger);
	}

	public void AddHunger(int value) => SetHunger(Hunger + value);

	public void RemoveHunger(int value) => SetHunger(Hunger - value);

	private void SetTiredness(int newTirednessValue)
	{
		Rest = Mathf.Clamp(newTirednessValue, 0, MaxRest);
	}

	public void AddTiredness(int value) => SetTiredness(Rest + value);

	public void RemoveTiredness(int value) => SetTiredness(Rest - value);

	private void SetHapiness(int newHapinessValue)
	{
		Hapiness = Mathf.Clamp(newHapinessValue, 0, MaxHapiness);
	}

	public void AddHapiness(int value) => SetHapiness(Hapiness + value);

	public void RemoveHapiness(int value) => SetHapiness(Hapiness - value);

	public void TickStats(float t)
	{
		// Degrade hunger while perserving overflow
		m_hungerInterval += t * c_hungerDegredation;
		while (m_hungerInterval >= 1f)
		{
			RemoveHunger(1);
			m_hungerInterval -= 1f;
		}

		// Degrade tiredness while perserving overflow
		m_restInterval += t * c_tirednessDegredation;
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
			m_healthInterval += t * (c_baseHealthDegredation * (1 + healthDegredation));
			while (m_healthInterval >= 1f)
			{
				RemoveHealth(1);
				m_healthInterval -= 1f;
			}
		}
	}
}
