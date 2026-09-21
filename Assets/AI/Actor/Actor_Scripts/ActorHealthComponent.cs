using SaveLoad.Core;
using System.Collections;
using UnityEngine;
using Entities.Core;

public class ActorHealthComponent : HealthComponent, ISaveableComponent
{
	// Constants
	private const float c_hungerDegredation = 0.15f;
	private const float c_tirednessDegredation = 0.15f;
	private const float c_baseHealthDegredation = 2f;

	[Header("Settings")]
	[field: SerializeField] public int MaxHunger { get; private set; } = 100;
	[field: SerializeField] public int MaxRest { get; private set; } = 100;
	[field: SerializeField] public int MaxHapiness { get; private set; } = 100;

	// Components
	private Actor m_actor;

	// System
	private float m_healthDegredationInterval;

	[field: SerializeField] public int Hunger { get; private set; } = 100;
	private float m_hungerDegredationInterval;

	[field: SerializeField] public int Rest { get; private set; } = 100;
	private float m_restDegredationInterval;

	[field: SerializeField] public int Hapiness { get; private set; } = 100;

	protected override void Awake()
	{
		base.Awake();
		m_actor = GetComponent<Actor>();

		if (GetComponent<ISavableEntity>() != null)
			GetComponent<ISavableEntity>().TransformRestored += HandleSpawned;
	}

	private void Start()
	{
		// Try to add the actor if they werent loaded (created at runtime)
		ActorManager.Instance.TryAddActor(m_actor);
	}

	private void OnDestroy()
	{
		if (GetComponent<ISavableEntity>() != null)
			GetComponent<ISavableEntity>().TransformRestored -= HandleSpawned;
	}

	private void HandleSpawned(Vector3 savedPosition, Quaternion savedRotation)
	{
		ActorManager.Instance.TryAddActor(m_actor);
		m_actor.Pathing.SetPosition(savedPosition);
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
		m_hungerDegredationInterval += t * c_hungerDegredation;
		while (m_hungerDegredationInterval >= 1f)
		{
			RemoveHunger(1);
			m_hungerDegredationInterval -= 1f;
		}

		// Degrade tiredness while perserving overflow
		m_restDegredationInterval += t * c_tirednessDegredation;
		while (m_restDegredationInterval >= 1f)
		{
			RemoveTiredness(1);
			m_restDegredationInterval -= 1f;
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
			m_healthDegredationInterval += t * (c_baseHealthDegredation * (1 + healthDegredation));
			while (m_healthDegredationInterval >= 1f)
			{
				//RemoveHealth(1);
				m_healthDegredationInterval -= 1f;
			}
		}
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
