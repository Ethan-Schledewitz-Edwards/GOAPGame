using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class HealthComponent : MonoBehaviour
{
	// Components
	protected AudioSource m_audioSource;

	[field: Header("Settings")]
	[field: SerializeField] public int MaxHealth { get; private set; } = 100;
	[field: SerializeField] public bool IsDestroyedOnDeath { get; private set; } = false;

	[field: Header("VFX")]
	[SerializeField] protected GameObject m_damageParticles;
	[SerializeField] protected GameObject m_destructionParticles;

	[field: Header("Sounds")]
	[SerializeField] private AudioClip[] m_damageSounds;
	[SerializeField] private AudioClip[] m_dieSounds;

	// System vars
	public int Health { get; private set; }
	public bool IsDead { get; private set; }

	protected virtual void Awake()
	{
		m_audioSource = GetComponent<AudioSource>();

		SetHealth(MaxHealth);
	}

	public void SetHealth(int newHealthValue)
	{
		// Play damage sounds if the component lost health
		bool wasHealthLost = newHealthValue < Health;
		if (wasHealthLost && m_damageSounds.Length > 0)
		{
			m_audioSource.PlayOneShot(m_damageSounds[Random.Range(0, m_damageSounds.Length)], 0.6f);
		}

		Health = Mathf.Clamp(newHealthValue, 0, MaxHealth);

		if (Health <= 0 && !IsDead)
			SetDead(true);
	}

	public void AddHealth(int value) => SetHealth(Health + value);

	public void RemoveHealth(int value) => SetHealth(Health - value);

	public void TryTakeDamage(int amount, Vector3 hitPos, Vector3 hitDir)
	{
		if (IsDead)
			return;

		RemoveHealth(amount);
		OnTakeDamage();

		TrySpawnBloodEffects(hitPos, hitDir);
	}

	private void TrySpawnBloodEffects(Vector3 hitPos, Vector3 hitDir)
	{
		if (m_damageParticles != null)
		{
			Vector3 spawnOffset = hitDir * 0.2f;

			// Spawn VFX client-side
			GameObject blood = Instantiate(m_damageParticles, hitPos + spawnOffset, Quaternion.identity, null);
			blood.transform.forward = hitDir;
		}
	}

	private void SetDead(bool isDeceased)
	{
		IsDead = isDeceased;

		if (IsDead)
		{
			OnDie();
		}
		else
		{
			OnRevive();
		}
	}

	protected virtual void OnTakeDamage()
	{
		if (IsDead) 
			return;
	}

    protected virtual void OnDie()
	{
		// Spawn destruction particles for all clients
		if (m_destructionParticles != null)
			Instantiate(m_destructionParticles, transform.position, Quaternion.identity, null);

		if (m_dieSounds.Length > 0)
		{
			m_audioSource.PlayOneShot(m_dieSounds[Random.Range(0, m_dieSounds.Length)], 0.6f);
		}

		if (IsDestroyedOnDeath)
			Destroy(gameObject);
	}

	protected virtual void OnRevive() { Debug.Log($"{gameObject.name} is alive."); }
}
