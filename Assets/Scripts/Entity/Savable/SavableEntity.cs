using Entities.Core;
using AssetIndex.Core;
using SaveLoad.Core;
using SaveLoad.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WorldManagement.AuthoredTiles;
using WorldManagement.Core;

namespace Entities.Savable
{
	[RequireComponent(typeof(Entities.Core.Entity))]
	public class SavableEntity : MonoBehaviour, ISavableEntity
	{
		public string GUID => m_guid;

		[SerializeField, HideInInspector]
		private string m_guid = "";

		[SerializeField, HideInInspector]
		private bool m_isManuallyAuthored = false;

		[SerializeField] private SavableEntityPrefabData m_savablePrefabData;

		// ISavableEntity properties
		public bool SavedByChunks => true;
		public Vector2Int ChunkXZ => m_chunkXZ;
		public bool IsRegisteredToChunk => m_isRegisteredToChunk;

		// Components
		private Entity m_entity;
		private Collider m_collider;
		private Rigidbody m_rigidbody;

		// Events
		public event Action DataRestored;
		public event Action<Vector3, Quaternion> TransformRestored;

		// System
		private Vector2Int m_chunkXZ;
		private bool m_isRegisteredToChunk = false;

		private bool m_useGravityByDefault;
		private bool m_isKinematicByDefault;

		private LayerMask m_collisionLayerMask;

		private Coroutine m_registerOnEnabledRoutine;

		#region GUID

		private static readonly Dictionary<string, SavableEntity> s_entitiesByGuid = new();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticState()
		{
			s_entitiesByGuid.Clear();
		}

		public static bool TryGetByGuid(string guid, out SavableEntity entity)
		{
			entity = null;

			if (string.IsNullOrEmpty(guid))
				return false;

			if (!s_entitiesByGuid.TryGetValue(guid, out entity))
				return false;

			// Unity object has been destroyed but dictionary entry remains.
			if (entity == null)
			{
				s_entitiesByGuid.Remove(guid);
				entity = null;
				return false;
			}

			return true;
		}

		private void RegisterGuid()
		{
			if (string.IsNullOrEmpty(m_guid))
			{
				Debug.LogError($"[SavableEntity] '{name}' has no GUID.", this);
				return;
			}

			if (s_entitiesByGuid.TryGetValue(m_guid, out SavableEntity existing) &&
				existing != null &&
				existing != this)
			{
				Debug.LogError($"[SavableEntity] Duplicate GUID '{m_guid}' detected on " +
					$"'{name}' and '{existing.name}'.", this);

				return;
			}

			s_entitiesByGuid[m_guid] = this;
		}

		private void SetGuid(string newGuid)
		{
			if (string.IsNullOrEmpty(newGuid))
				return;

			if (m_guid == newGuid)
			{
				s_entitiesByGuid[m_guid] = this;
				return;
			}

			// Remove our previous registration.
			if (!string.IsNullOrEmpty(m_guid) &&
				s_entitiesByGuid.TryGetValue(m_guid, out SavableEntity existing) &&
				existing == this)
			{
				s_entitiesByGuid.Remove(m_guid);
			}

			m_guid = newGuid;

			// Prevent duplicate GUID's
			if (s_entitiesByGuid.TryGetValue(m_guid, out SavableEntity other) &&
				other != null &&
				other != this)
			{
				Debug.LogError($"[SavableEntity] Cannot assign GUID '{m_guid}' to '{name}'. " +
					$"It is already owned by '{other.name}'.", this);

				return;
			}

			s_entitiesByGuid[m_guid] = this;
		}

		#endregion

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (UnityEditor.EditorApplication.isPlaying)
				return;

			// Never give Prefabs a GUID
			if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
			{
				if (!string.IsNullOrEmpty(m_guid) || m_isManuallyAuthored)
				{
					m_guid = string.Empty;
					m_isManuallyAuthored = false;

					UnityEditor.EditorUtility.SetDirty(this);
				}

				return;
			}

			// Give instances a unique GUID
			if (gameObject.scene.IsValid() && !m_isManuallyAuthored)
			{
				if (string.IsNullOrEmpty(m_guid))
					m_guid = Guid.NewGuid().ToString();

				m_isManuallyAuthored = true;

				UnityEditor.EditorUtility.SetDirty(this);
			}
		}
#endif

		private void Awake()
		{
			if (!m_isManuallyAuthored && 
				string.IsNullOrEmpty(m_guid))
			{
				m_guid = Guid.NewGuid().ToString();
			}

			m_entity = GetComponent<Entity>();

			if (m_entity != null)
				m_entity.EntityChunkChanged += OnEntityChunkChanged;

			m_collider = GetComponent<Collider>();
			m_rigidbody = GetComponent<Rigidbody>();

			if (m_rigidbody != null)
			{
				m_useGravityByDefault = m_rigidbody.useGravity;
				m_isKinematicByDefault = m_rigidbody.isKinematic;
			}

			DisablePhysicsAndCollision();

			m_collisionLayerMask = LayerMask.GetMask("Default", "Environment", "Interaction");
		}

		private void Start()
		{
			RegisterGuid();
			EnablePhysicsAndCollision();

			Vector2Int chunkXZ = CoordinateUtility.WorldToChunkXZ(transform.position);
			UpdateChunkParent(chunkXZ);
		}

		private void OnEnable()
		{
			StopRegistrationRoutine();

			// Try registering to current position chunk with for two seconds
			Vector2Int chunkXZ = CoordinateUtility.WorldToChunkXZ(transform.position);
			m_registerOnEnabledRoutine = StartCoroutine(RegisterToChunkWithTimeoutRoutine(chunkXZ, 2.0f));
		}

		private void OnDisable()
		{
			StopRegistrationRoutine();
		}

		private void OnDestroy()
		{
			if (m_entity != null)
				m_entity.EntityChunkChanged -= OnEntityChunkChanged;

			UnregisterFromCurrentChunk();

			if (!string.IsNullOrEmpty(GUID))
			{
				s_entitiesByGuid.Remove(GUID);
			}
		}

		#region Chunk Registration

		private void OnEntityChunkChanged(Vector2Int newChunkXZ)
		{
			StopRegistrationRoutine();
			m_registerOnEnabledRoutine = StartCoroutine(RegisterToChunkWithTimeoutRoutine(newChunkXZ, 2.0f));
		}

		private IEnumerator RegisterToChunkWithTimeoutRoutine(Vector2Int chunkXZ, float timeoutSeconds)
		{
			float timer = 0f;
			while (timer < timeoutSeconds)
			{
				if (TryRegisterToChunk(chunkXZ))
				{
					m_registerOnEnabledRoutine = null;
					yield break; // Registered
				}

				timer += Time.deltaTime;
				yield return null;
			}

			Debug.LogWarning($"[SaveableEntity] '{name}' failed to register to " +
				$"chunk {chunkXZ} after {timeoutSeconds} seconds (Timed Out).", this);

			m_registerOnEnabledRoutine = null;
			gameObject.SetActive(false);
		}

		private bool TryRegisterToChunk(Vector2Int chunkXZ)
		{
			if (!WorldManager.TryGetActiveChunkData(chunkXZ, out TerrainChunk newChunk))
			{
				return false;
			}

			// Unregister from old chunk if changing locations
			if (m_isRegisteredToChunk && m_chunkXZ != chunkXZ)
			{
				UnregisterFromCurrentChunk();
			}

			m_chunkXZ = chunkXZ;
			m_isRegisteredToChunk = true;

			newChunk.RegisterEntity(gameObject);
			UpdateChunkParent(chunkXZ);

			return true;
		}

		private void UnregisterFromCurrentChunk()
		{
			if (!m_isRegisteredToChunk)
				return;

			if (WorldManager.TryGetActiveChunkData(m_chunkXZ, out TerrainChunk chunk))
			{
				chunk.UnregisterEntity(gameObject);
			}

			m_isRegisteredToChunk = false;
		}

		private void UpdateChunkParent(Vector2Int chunkXZ)
		{
			bool isAuthoredWorld = TerrainChunkManager.s_BuilderMethod == 
				TerrainChunkManager.EChunkBuilderMethod.Authored;

			if (isAuthoredWorld)
			{
				if (AuthoredTileLoader.s_AuthoredChunks.TryGetValue(chunkXZ, out GameObject authoredChunk) &&
					authoredChunk != null)
				{
					transform.SetParent(authoredChunk.transform, true);
				}
			}
			else
			{
				if (WorldManager.s_ActiveChunks.TryGetValue(chunkXZ, out var activeChunk) && 
					activeChunk.gameObject != null)
				{
					transform.SetParent(activeChunk.gameObject.transform, true);
				}
			}
		}

		private void StopRegistrationRoutine()
		{
			if (m_registerOnEnabledRoutine != null)
			{
				StopCoroutine(m_registerOnEnabledRoutine);
				m_registerOnEnabledRoutine = null;
			}
		}

		#endregion

		#region Physics

		private void EnablePhysicsAndCollision()
		{
			if (m_collider != null)
				m_collider.enabled = true;

			if (m_rigidbody != null)
			{
				m_rigidbody.useGravity = m_useGravityByDefault;
				m_rigidbody.isKinematic = m_isKinematicByDefault;
			}
		}

		private void DisablePhysicsAndCollision()
		{
			if (m_collider != null)
				m_collider.enabled = false;

			if (m_rigidbody != null)
			{
				m_rigidbody.useGravity = false;
				m_rigidbody.isKinematic = true;
			}
		}

		#endregion

		#region ISavableEntity Methods

		/// <summary>
		/// Gathers data from all ISaveableComponent scripts on this GameObject
		/// </summary>
		public SerializableEntityData GenerateSaveData()
		{
			if (m_savablePrefabData == null)
			{
				Debug.LogError($"[SaveableEntity] '{name}' has no SavableEntityPrefabData assigned.", this);

				return null;
			}

			if (string.IsNullOrEmpty(GUID))
			{
				Debug.LogError($"[SaveableEntity] '{name}' has no GUID and cannot be saved.", this);

				return null;
			}

			Vector3 position = transform.position;
			Quaternion rotation = transform.rotation;

			SerializableEntityData data = new SerializableEntityData
			{
				GUID = GUID,

				PrefabKey = m_savablePrefabData.ID,
				PosX = position.x,
				PosY = position.y,
				PosZ = position.z,

				RotX = rotation.x,
				RotY = rotation.y,
				RotZ = rotation.z,
				RotW = rotation.w,
			};

			// Serialize the entities savable components
			ISaveableComponent[] components = GetComponentsInChildren<ISaveableComponent>(true);
			HashSet<string> componentIds = new();
			foreach (ISaveableComponent component in components)
			{
				string componentId = component.GetComponentId();

				if (string.IsNullOrEmpty(componentId))
				{
					Debug.LogError($"[SaveableEntity] Component '{component.GetType().Name}' on '{name}' " +
						$"returned an empty component ID.", this);

					continue;
				}

				if (!componentIds.Add(componentId))
				{
					Debug.LogError($"[SaveableEntity] Duplicate component ID '{componentId}' on '{name}'. " +
						$"Component IDs must be unique per entity.", this);

					continue;
				}

				object rawData = component.GenerateComponentData();
				if (rawData is string dataString)
				{
					data.ComponentData.Add(new ComponentSaveData{K = componentId, V = dataString});
				}
				else
				{
					Debug.LogError($"[SaveableEntity] Component '{component.GetType().Name}' on '{name}' " +
						$"must return a string from GenerateComponentData().", this);
				}
			}

			return data;
		}

		/// <summary>
		/// Pushes the loaded data back into the individual components
		/// </summary>
		public bool RestoreFromSaveData(SerializableEntityData data)
		{
			if (data == null)
				return false;

			if (!m_isManuallyAuthored)
			{
				SetGuid(data.GUID);
			}
			else if (m_guid != data.GUID)
			{
				Debug.LogWarning(
					$"[SavableEntity] Manually authored entity '{name}' " +
					$"has GUID '{m_guid}', but save data contains '{data.GUID}'. " +
					$"Keeping the authored GUID.",
					this);
			}

			Vector3 position = new Vector3(data.PosX, data.PosY, data.PosZ);

			Quaternion rotation =
				new Quaternion(data.RotX, data.RotY, data.RotZ, data.RotW).normalized;

			transform.SetPositionAndRotation(position, rotation);
			TransformRestored?.Invoke(position, rotation);

			// Attempt to register to chunk, fallback to timed routine if chunk not ready.
			Vector2Int targetChunkXZ = CoordinateUtility.WorldToChunkXZ(position);
			if (!TryRegisterToChunk(targetChunkXZ))
			{
				StopRegistrationRoutine();
				m_registerOnEnabledRoutine = StartCoroutine(RegisterToChunkWithTimeoutRoutine(targetChunkXZ, 2.0f));
			}

			// Restore components
			ISaveableComponent[] components = GetComponentsInChildren<ISaveableComponent>(true);
			Dictionary<string, ComponentSaveData> savedComponents = new();

			if (data.ComponentData != null)
			{
				foreach (ComponentSaveData componentData in data.ComponentData)
				{
					if (string.IsNullOrEmpty(componentData.K))
						continue;

					if (!savedComponents.TryAdd(componentData.K, componentData))
					{
						Debug.LogError($"[SaveableEntity] Duplicate saved component ID " +
							$"'{componentData.K}' for entity GUID '{data.GUID}'.", this);
					}
				}
			}

			HashSet<string> foundComponentIds = new();
			foreach (ISaveableComponent component in components)
			{
				string componentId =
					component.GetComponentId();

				if (string.IsNullOrEmpty(componentId))
					continue;

				if (!foundComponentIds.Add(componentId))
				{
					Debug.LogError($"[SaveableEntity] Duplicate live component ID " +
						$"'{componentId}' on '{name}'.", this);

					continue;
				}

				if (savedComponents.TryGetValue(componentId, out ComponentSaveData componentData))
				{
					try
					{
						component.RestoreComponentData(componentData.V);
					}
					catch (Exception exception)
					{
						Debug.LogError($"[SaveableEntity] Failed restoring component " +
							$"'{componentId}' on '{name}'.\n{exception}", this);
					}
				}
				else
				{
					Debug.LogWarning($"[SaveableEntity] No saved data found for " +
						$"component '{componentId}' on '{name}'.", this);
				}
			}

			DataRestored?.Invoke();
			EnablePhysicsAndCollision();
			return true;
		}
		#endregion
	}
}