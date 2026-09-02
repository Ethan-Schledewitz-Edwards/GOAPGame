using Entities.Savable;
using SaveLoad.Core;
using System.Collections;
using UnityEngine;

public class ActorHealthComponent : HealthComponent, ISaveableComponent
{
	// Constants
	private const float c_hungerDegredation = 0.2f;
	private const float c_tirednessDegredation = 0.2f;
	private const float c_baseHealthDegredation = 2f;

	private Actor m_actor;
	private SaveableEntity m_saveableEntity;

	private float m_healthInterval;

	[field: SerializeField] public int MaxHunger { get; private set; } = 100;
	[field: SerializeField] public int Hunger { get; private set; } = 100;
	private float m_hungerInterval;

	[field: SerializeField] public int MaxRest { get; private set; } = 100;
	[field: SerializeField] public int Rest { get; private set; } = 100;
	private float m_restInterval;

	[field: SerializeField] public int MaxHapiness { get; private set; } = 100;
	[field: SerializeField] public int Hapiness { get; private set; } = 100;

	protected override void Awake()
	{
		base.Awake();
		m_actor = GetComponent<Actor>();
		m_saveableEntity = GetComponent<SaveableEntity>();
	}

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

	protected override IEnumerator TrySpawn()
	{
		ActorManager.Instance.AddActor(m_actor);
		return base.TrySpawn();
	}

	protected override void OnDie()
	{
		ActorManager.Instance.RemoveActor(m_actor);
		base.OnDie();
	}

	public string GetComponentId() => "ActorHealth";

	public object GenerateComponentData()
	{
		return new ActorHealthData
		{
			Hunger = this.Hunger,
			Rest = this.Rest,
			Happiness = this.Hapiness,
			CurrentHealth = this.Health
		};
	}

	public void RestoreComponentData(object data)
	{
		if (data is ActorHealthData healthData)
		{
			SetHunger(healthData.Hunger);
			SetTiredness(healthData.Rest);
			SetHapiness(healthData.Happiness);
			SetHealth(healthData.CurrentHealth);
		}
	}

	[System.Serializable]
	public class ActorHealthData
	{
		public int Hunger;
		public int Rest;
		public int Happiness;
		public int CurrentHealth;
	}
}
