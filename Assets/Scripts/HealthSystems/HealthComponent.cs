using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource), typeof(Rigidbody))]
public class HealthComponent : MonoBehaviour
{
	// Components
	protected AudioSource m_audioSource;
	protected Rigidbody m_rb;

	[field: Header("Properties")]
	[field: SerializeField] public int MaxHealth { get; private set; } = 100;
	[field: SerializeField] public bool IsDestroyedOnDeath { get; private set; } = false;

	[field: Header("VFX")]
	[SerializeField] protected GameObject m_damageParticles;
	[SerializeField] protected GameObject m_destructionParticles;

	[field: Header("Sounds")]
	[SerializeField] private AudioClip[] m_damageSounds;
	[SerializeField] private AudioClip[] m_dieSounds;

	// System vars
	protected bool m_isSpawning;
	public int Health { get; private set; }
	public bool IsDead { get; private set; }
	protected LayerMask m_collisionLayerMask;

	#region Initialization Methods

	protected virtual void Awake()
	{
		m_audioSource = GetComponent<AudioSource>();

		m_rb = GetComponent<Rigidbody>();
		m_rb.isKinematic = true;

		SetHealth(MaxHealth);
	}

	protected virtual void Start()
	{
		m_collisionLayerMask = LayerMask.GetMask("Default", "Environment", "Interaction");

		StartCoroutine(TrySpawn());
	}
	#endregion

	#region Health State Methods

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

	/// <summary>
	/// Attempts to place an entity down
	/// </summary>
	protected virtual IEnumerator TrySpawn()
	{
		m_isSpawning = true;

		float startHeight = 1f;
		float rayLength = 3f;
		Vector3 rayStart = transform.position + (Vector3.up * startHeight);
		RaycastHit hit;

		// Raycast down to find the highest possible point
		while(!Physics.Raycast(rayStart, Vector3.down * rayLength, out hit, rayLength, m_collisionLayerMask, QueryTriggerInteraction.Ignore) && m_isSpawning)
			yield return null;

		transform.position = hit.point;

		m_isSpawning = false;
	}

	#endregion
}
