using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public partial class HealthComponent : MonoBehaviour
{
	[field: Header("Properties")]
	[field: SerializeField] public int m_MaxHealth { get; private set; } = 100;
	[field: SerializeField] public bool m_IsDestroyedOnDeath { get; private set; } = false;

	[field: Header("VFX")]
	[SerializeField] protected GameObject m_damageParticles;
	[SerializeField] protected GameObject m_destrcutionParticles;

	[field: Header("Sounds")]
	[SerializeField] private AudioClip[] m_damageSounds;
	[SerializeField] private AudioClip[] m_dieSounds;

	// System vars
	private int m_health;
	private bool m_isDead;

	protected AudioSource m_audioSource;

	#region Initialization Methods

	protected virtual void Awake()
	{
		m_audioSource = GetComponent<AudioSource>();
		SetHealth(m_MaxHealth);
	}
	#endregion

	#region Health State Methods

	private void SetHealth(int newHealthValue)
	{
		// Play damage sounds if the component lost health
		bool wasHealthLost = newHealthValue < m_health;
		if (wasHealthLost && m_damageSounds.Length > 0)
		{
			m_audioSource.PlayOneShot(m_damageSounds[Random.Range(0, m_damageSounds.Length)], 0.6f);
		}

		m_health = Mathf.Clamp(newHealthValue, 0, m_MaxHealth);

		if (m_health <= 0 && !m_isDead)
			SetDead(true);
	}

	public void AddHealth(int value) => SetHealth(m_health + value);

	public void RemoveHealth(int value) => SetHealth(m_health - value);

	public void TryTakeDamage(int amount, Vector3 hitPos, Vector3 hitDir)
	{
		if (m_isDead)
			return;

		RemoveHealth(amount);

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
		m_isDead = isDeceased;

		if (m_isDead)
		{
			OnDie();
		}
		else
		{
			OnRevive();
		}
	}

	protected virtual void OnDie()
	{
		// Spawn destruction particles for all clients
		if (m_destrcutionParticles != null)
			Instantiate(m_destrcutionParticles, transform.position, Quaternion.identity, null);

		if (m_dieSounds.Length > 0)
		{
			m_audioSource.PlayOneShot(m_dieSounds[Random.Range(0, m_dieSounds.Length)], 0.6f);
		}

		if (m_IsDestroyedOnDeath)
			Destroy(gameObject);
	}

	protected virtual void OnRevive() { Debug.Log($"{gameObject.name} is alive."); }

	#endregion
}
