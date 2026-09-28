using Entities.Core;
using GenericIndex;
using SaveLoad.Core;
using SaveLoad.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using WorldManagement.AuthoredTiles;
using WorldManagement.Core;

namespace Entities.Savable
{
	[RequireComponent(typeof(Entities.Core.Entity))]
	public class SaveableEntity : MonoBehaviour, ISavableEntity
	{
		[SerializeField] private SavableEntityPrefabData m_savablePrefabData;

		[SerializeField] private string m_guid = "";
		public string GetGUID() => m_guid;

		[field: SerializeField, Tooltip("Should be true when an object is not spawned at run-time.")] 
		public bool IsManuallyAuthored { get; private set; } = false;

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

		private static readonly Dictionary<string, SaveableEntity> s_entitiesByGuid = new();

		#region Static State

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticState()
		{
			s_entitiesByGuid.Clear();
		}

		public static bool TryGetByGuid(string guid, out SaveableEntity entity)
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

		private bool TryRegisterGuid(string guid)
		{
			if (string.IsNullOrEmpty(guid))
			{
				Debug.LogError($"[SaveableEntity] '{name}' cannot register an empty GUID.", this);

				return false;
			}

			if (s_entitiesByGuid.TryGetValue(guid, out SaveableEntity existing))
			{
				if (existing != null && existing != this)
				{
					Debug.LogError($"[SaveableEntity] Duplicate GUID '{guid}'.\n" +
						$"Existing: '{existing.name}'\n" +
						$"Duplicate: '{name}'",
						this);

					return false;
				}

				s_entitiesByGuid.Remove(guid);
			}

			m_guid = guid;
			s_entitiesByGuid[guid] = this;

			return true;
		}

		private void UnregisterGuid()
		{
			if (string.IsNullOrEmpty(m_guid))
				return;

			if (s_entitiesByGuid.TryGetValue(m_guid,
				out SaveableEntity registeredEntity) &&
				registeredEntity == this)
			{
				s_entitiesByGuid.Remove(m_guid);
			}
		}

		#endregion

		private void Awake()
		{
			m_entity = GetComponent<Entity>();

			if (m_entity != null)
				m_entity.EntityChunkChanged += RegisterToClosestChunk;

			m_collider = GetComponent<Collider>();
			m_rigidbody = GetComponent<Rigidbody>();

			if (m_rigidbody != null)
			{
				m_useGravityByDefault = m_rigidbody.useGravity;
				m_isKinematicByDefault = m_rigidbody.isKinematic;
			}

			DisablePhysicsAndCollision();

			if (IsManuallyAuthored)
			{
				if (string.IsNullOrEmpty(m_guid))
				{
					Debug.LogError($"[SaveableEntity] Authored entity '{name}' " +
						$"has no serialized GUID!!!", this);
				}
				else
					TryRegisterGuid(m_guid);
			}
			else
			{
				m_guid = Guid.NewGuid().ToString();

				TryRegisterGuid(m_guid);
			}

			m_collisionLayerMask = LayerMask.GetMask("Default", "Environment", "Interaction");
		}

		private void Start()
		{
			EnablePhysicsAndCollision();

			Vector2Int chunkXZ = CoordinateUtility.WorldToChunkXZ(transform.position);
			UpdateChunkParent(chunkXZ);
		}

		private void OnEnable()
		{
			Vector2Int chunkXZ = CoordinateUtility.WorldToChunkXZ(transform.position);
			RegisterToClosestChunk(chunkXZ);
		}

		private void OnDestroy()
		{
			if (m_entity != null)
				m_entity.EntityChunkChanged -= RegisterToClosestChunk;

			UnregisterFromCurrentChunk();
			UnregisterGuid();
		}

		#region Chunk Registration

		private void RegisterToClosestChunk(Vector2Int chunkXZ)
		{
			if (m_isRegisteredToChunk)
			{
				if (WorldManager.TryGetActiveChunkData(m_chunkXZ, out TerrainChunk previousChunk))
				{
					previousChunk.UnregisterEntity(gameObject);
				}
			}

			if (!WorldManager.TryGetActiveChunkData(chunkXZ, out TerrainChunk newChunk))
			{
				Debug.LogWarning($"[SaveableEntity] '{name}' attempted to register " +
					$"to unloaded chunk {chunkXZ}. " +
					$"The chunk loader must load the chunk " +
					$"before the entity moves into it.",
					this);

				return;
			}

			m_chunkXZ = chunkXZ;
			m_isRegisteredToChunk = true;

			newChunk.RegisterEntity(gameObject);
			UpdateChunkParent(chunkXZ);
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

		private void UnregisterFromCurrentChunk()
		{
			if (!m_isRegisteredToChunk)
				return;

			if (WorldManager.TryGetActiveChunkData(m_chunkXZ, out TerrainChunk chunk))
				chunk.UnregisterEntity(gameObject);

			m_isRegisteredToChunk = false;
		}

		#endregion

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

			if (string.IsNullOrEmpty(m_guid))
			{
				Debug.LogError($"[SaveableEntity] '{name}' has no GUID and cannot be saved.", this);

				return null;
			}

			Vector3 position = transform.position;
			Quaternion rotation = transform.rotation;

			SerializableEntityData data = new SerializableEntityData
			{
				GUID = m_guid,

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

			if (string.IsNullOrEmpty(data.GUID))
			{
				Debug.LogError($"[SaveableEntity] Attempted to restore '{name}' " +
					$"from save data with no GUID.", this);

				return false;
			}

			// The object may have received a temporary runtime GUID during Instantiate().
			// Replace it with the persistent GUID.
			UnregisterGuid();

			if (!TryRegisterGuid(data.GUID))
			{
				Debug.LogError($"[SaveableEntity] Could not restore '{name}' " +
					$"because GUID '{data.GUID}' already belongs to another entity.", this);

				return false;
			}

			Vector3 position = new Vector3(data.PosX, data.PosY, data.PosZ);

			Quaternion rotation = 
				new Quaternion(data.RotX, data.RotY, data.RotZ, data.RotW).normalized;

			transform.SetPositionAndRotation(position, rotation);
			TransformRestored?.Invoke(position, rotation);

			RegisterToClosestChunk(CoordinateUtility.WorldToChunkXZ(position));

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
	}
}